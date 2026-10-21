using System;
using System.IO;
using System.Threading.Tasks;

namespace ScientificResearch.Infrastucture.Storage
{
    public class LocalStorageProvider : IStorageProvider
    {
        private readonly LocalStorageConfig _config;
        private readonly string _rootPath;

        public string ProviderName => "Local";
        public string DefaultBucket => "default";

        public LocalStorageProvider(LocalStorageConfig config, string? webRootPath = null)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _rootPath = string.IsNullOrWhiteSpace(webRootPath)
                ? Path.GetFullPath(_config.BasePath)
                : Path.GetFullPath(Path.Combine(webRootPath, "upload"));
            Directory.CreateDirectory(_rootPath);
        }

        public Task EnsureBucketExistsAsync(string? bucket = null)
        {
            var path = GetBucketDirectory(bucket);
            Directory.CreateDirectory(path);
            return Task.CompletedTask;
        }

        private string GetBucketDirectory(string? bucket)
        {
            var b = string.IsNullOrWhiteSpace(bucket) ? "default" : bucket.Trim();
            return Path.Combine(_rootPath, b);
        }

        private string GetPhysicalPath(string objectKey, string? bucket)
        {
            var bucketDir = GetBucketDirectory(bucket);
            var safeKey = objectKey.Replace('\\', '/').TrimStart('/');
            return Path.Combine(bucketDir, safeKey);
        }

        public Task<PresignedUploadResult> GeneratePresignedUploadUrlAsync(
            string objectKey, string contentType, TimeSpan expiry, string? bucket = null)
        {
            // Local fallback uses the API's stream endpoint
            var url = $"/api/Attachment/UploadStream?objectKey={Uri.EscapeDataString(objectKey)}&bucket={Uri.EscapeDataString(bucket ?? DefaultBucket)}";
            return Task.FromResult(new PresignedUploadResult
            {
                UploadUrl = url,
                Method = "POST",
                Expiration = DateTime.UtcNow.Add(expiry)
            });
        }

        public Task<string> GeneratePresignedDownloadUrlAsync(
            string objectKey, string downloadFileName, TimeSpan expiry, string? bucket = null)
        {
            // Local fallback uses the API's download stream endpoint
            var url = $"/api/Attachment/DownloadStreamByKey?objectKey={Uri.EscapeDataString(objectKey)}&bucket={Uri.EscapeDataString(bucket ?? DefaultBucket)}&fileName={Uri.EscapeDataString(downloadFileName)}";
            return Task.FromResult(url);
        }

        public async Task<string> PutObjectAsync(
            string objectKey, Stream contentStream, string contentType, string? bucket = null)
        {
            var filePath = GetPhysicalPath(objectKey, bucket);
            var dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

            await using var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 64 * 1024, true);
            await contentStream.CopyToAsync(fs);
            return objectKey;
        }

        public Task<Stream> GetObjectAsync(string objectKey, string? bucket = null)
        {
            var filePath = GetPhysicalPath(objectKey, bucket);
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"本地文件不存在: {filePath}");
            }
            Stream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 64 * 1024, true);
            return Task.FromResult(fs);
        }

        public Task DeleteObjectAsync(string objectKey, string? bucket = null)
        {
            var filePath = GetPhysicalPath(objectKey, bucket);
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
            return Task.CompletedTask;
        }

        public Task<ObjectMetadata> GetObjectMetadataAsync(string objectKey, string? bucket = null)
        {
            var filePath = GetPhysicalPath(objectKey, bucket);
            if (!File.Exists(filePath))
            {
                return Task.FromResult(new ObjectMetadata { Exists = false });
            }
            var fi = new FileInfo(filePath);
            return Task.FromResult(new ObjectMetadata
            {
                Exists = true,
                ContentLength = fi.Length,
                LastModified = fi.LastWriteTimeUtc
            });
        }
    }
}
