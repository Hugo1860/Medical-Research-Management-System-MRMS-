using System;
using System.IO;
using System.Threading.Tasks;

namespace ScientificResearch.Infrastucture.Storage
{
    public interface IStorageProvider
    {
        string ProviderName { get; }
        string DefaultBucket { get; }

        Task EnsureBucketExistsAsync(string? bucket = null);

        Task<PresignedUploadResult> GeneratePresignedUploadUrlAsync(
            string objectKey, string contentType, TimeSpan expiry, string? bucket = null);

        Task<string> GeneratePresignedDownloadUrlAsync(
            string objectKey, string downloadFileName, TimeSpan expiry, string? bucket = null);

        Task<string> PutObjectAsync(
            string objectKey, Stream contentStream, string contentType, string? bucket = null);

        Task<Stream> GetObjectAsync(string objectKey, string? bucket = null);

        Task DeleteObjectAsync(string objectKey, string? bucket = null);

        Task<ObjectMetadata> GetObjectMetadataAsync(string objectKey, string? bucket = null);
    }
}
