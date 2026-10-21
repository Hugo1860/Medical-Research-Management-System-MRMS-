using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace ScientificResearch.Infrastucture.Storage
{
    public class MinioStorageProvider : IStorageProvider
    {
        private readonly MinioConfig _config;
        private readonly HttpClient _httpClient;

        public string ProviderName => "Minio";
        public string DefaultBucket => _config.BucketName;

        public MinioStorageProvider(MinioConfig config, HttpClient? httpClient = null)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _httpClient = httpClient ?? new HttpClient();
        }

        private string GetInternalEndpointBase()
        {
            var scheme = _config.UseSSL ? "https" : "http";
            var endpoint = _config.Endpoint.Trim();
            if (!endpoint.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                !endpoint.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                endpoint = $"{scheme}://{endpoint}";
            }
            return endpoint.TrimEnd('/');
        }

        private string GetExternalEndpointBase()
        {
            var endpoint = string.IsNullOrWhiteSpace(_config.ExternalEndpoint)
                ? GetInternalEndpointBase()
                : _config.ExternalEndpoint.Trim();
            if (!endpoint.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                !endpoint.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                var scheme = _config.UseSSL ? "https" : "http";
                endpoint = $"{scheme}://{endpoint}";
            }
            return endpoint.TrimEnd('/');
        }

        public async Task EnsureBucketExistsAsync(string? bucket = null)
        {
            var targetBucket = bucket ?? _config.BucketName;
            var uri = new Uri($"{GetInternalEndpointBase()}/{targetBucket}");
            var auth = S3Signer.GenerateAuthHeaders("HEAD", uri, _config.AccessKey, _config.SecretKey, _config.Region, Array.Empty<byte>());

            using var headReq = new HttpRequestMessage(HttpMethod.Head, uri);
            foreach (var h in auth) headReq.Headers.TryAddWithoutValidation(h.Key, h.Value);

            var resp = await _httpClient.SendAsync(headReq);
            if (resp.StatusCode == HttpStatusCode.NotFound)
            {
                // Create bucket
                var putAuth = S3Signer.GenerateAuthHeaders("PUT", uri, _config.AccessKey, _config.SecretKey, _config.Region, Array.Empty<byte>());
                using var putReq = new HttpRequestMessage(HttpMethod.Put, uri);
                foreach (var h in putAuth) putReq.Headers.TryAddWithoutValidation(h.Key, h.Value);
                var putResp = await _httpClient.SendAsync(putReq);
                if (!putResp.IsSuccessStatusCode && putResp.StatusCode != HttpStatusCode.Conflict)
                {
                    var msg = await putResp.Content.ReadAsStringAsync();
                    throw new InvalidOperationException($"创建 MinIO 存储桶 {targetBucket} 失败: {putResp.StatusCode} {msg}");
                }
            }
        }

        public async Task<PresignedUploadResult> GeneratePresignedUploadUrlAsync(
            string objectKey, string contentType, TimeSpan expiry, string? bucket = null)
        {
            var targetBucket = bucket ?? _config.BucketName;
            await EnsureBucketExistsAsync(targetBucket);
            var externalBase = GetExternalEndpointBase();
            var url = S3Signer.GeneratePresignedUrl(
                "PUT",
                externalBase,
                targetBucket,
                objectKey,
                _config.AccessKey,
                _config.SecretKey,
                _config.Region,
                expiry);

            var result = new PresignedUploadResult
            {
                UploadUrl = url,
                Method = "PUT",
                Expiration = DateTime.UtcNow.Add(expiry)
            };
            if (!string.IsNullOrWhiteSpace(contentType))
            {
                result.Headers["Content-Type"] = contentType;
            }
            return result;
        }

        public Task<string> GeneratePresignedDownloadUrlAsync(
            string objectKey, string downloadFileName, TimeSpan expiry, string? bucket = null)
        {
            var targetBucket = bucket ?? _config.BucketName;
            var externalBase = GetExternalEndpointBase();

            var queryParams = new Dictionary<string, string>();
            if (!string.IsNullOrWhiteSpace(downloadFileName))
            {
                var encodedFilename = Uri.EscapeDataString(downloadFileName);
                queryParams["response-content-disposition"] = $"attachment; filename=\"{encodedFilename}\"; filename*=UTF-8''{encodedFilename}";
            }

            var url = S3Signer.GeneratePresignedUrl(
                "GET",
                externalBase,
                targetBucket,
                objectKey,
                _config.AccessKey,
                _config.SecretKey,
                _config.Region,
                expiry,
                queryParams);

            return Task.FromResult(url);
        }

        public async Task<string> PutObjectAsync(
            string objectKey, Stream contentStream, string contentType, string? bucket = null)
        {
            var targetBucket = bucket ?? _config.BucketName;
            await EnsureBucketExistsAsync(targetBucket);

            byte[] buffer;
            if (contentStream is MemoryStream ms)
            {
                buffer = ms.ToArray();
            }
            else
            {
                using var temp = new MemoryStream();
                await contentStream.CopyToAsync(temp);
                buffer = temp.ToArray();
            }

            var uri = new Uri($"{GetInternalEndpointBase()}/{targetBucket}/{objectKey.TrimStart('/')}");
            var auth = S3Signer.GenerateAuthHeaders("PUT", uri, _config.AccessKey, _config.SecretKey, _config.Region, buffer);

            using var req = new HttpRequestMessage(HttpMethod.Put, uri)
            {
                Content = new ByteArrayContent(buffer)
            };
            req.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(
                string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType);

            foreach (var h in auth) req.Headers.TryAddWithoutValidation(h.Key, h.Value);

            var resp = await _httpClient.SendAsync(req);
            if (!resp.IsSuccessStatusCode)
            {
                var err = await resp.Content.ReadAsStringAsync();
                throw new InvalidOperationException($"上传对象到 MinIO 失败: {resp.StatusCode} {err}");
            }

            return objectKey;
        }

        public async Task<Stream> GetObjectAsync(string objectKey, string? bucket = null)
        {
            var targetBucket = bucket ?? _config.BucketName;
            var uri = new Uri($"{GetInternalEndpointBase()}/{targetBucket}/{objectKey.TrimStart('/')}");
            var auth = S3Signer.GenerateAuthHeaders("GET", uri, _config.AccessKey, _config.SecretKey, _config.Region, Array.Empty<byte>());

            using var req = new HttpRequestMessage(HttpMethod.Get, uri);
            foreach (var h in auth) req.Headers.TryAddWithoutValidation(h.Key, h.Value);

            var resp = await _httpClient.SendAsync(req, HttpCompletionOption.ResponseHeadersRead);
            if (!resp.IsSuccessStatusCode)
            {
                throw new FileNotFoundException($"MinIO 对象未找到: {targetBucket}/{objectKey}");
            }

            return await resp.Content.ReadAsStreamAsync();
        }

        public async Task DeleteObjectAsync(string objectKey, string? bucket = null)
        {
            var targetBucket = bucket ?? _config.BucketName;
            var uri = new Uri($"{GetInternalEndpointBase()}/{targetBucket}/{objectKey.TrimStart('/')}");
            var auth = S3Signer.GenerateAuthHeaders("DELETE", uri, _config.AccessKey, _config.SecretKey, _config.Region, Array.Empty<byte>());

            using var req = new HttpRequestMessage(HttpMethod.Delete, uri);
            foreach (var h in auth) req.Headers.TryAddWithoutValidation(h.Key, h.Value);

            var resp = await _httpClient.SendAsync(req);
            if (!resp.IsSuccessStatusCode && resp.StatusCode != HttpStatusCode.NotFound)
            {
                var err = await resp.Content.ReadAsStringAsync();
                throw new InvalidOperationException($"从 MinIO 删除对象失败: {resp.StatusCode} {err}");
            }
        }

        public async Task<ObjectMetadata> GetObjectMetadataAsync(string objectKey, string? bucket = null)
        {
            var targetBucket = bucket ?? _config.BucketName;
            var uri = new Uri($"{GetInternalEndpointBase()}/{targetBucket}/{objectKey.TrimStart('/')}");
            var auth = S3Signer.GenerateAuthHeaders("HEAD", uri, _config.AccessKey, _config.SecretKey, _config.Region, Array.Empty<byte>());

            using var req = new HttpRequestMessage(HttpMethod.Head, uri);
            foreach (var h in auth) req.Headers.TryAddWithoutValidation(h.Key, h.Value);

            var resp = await _httpClient.SendAsync(req);
            if (!resp.IsSuccessStatusCode)
            {
                return new ObjectMetadata { Exists = false };
            }

            return new ObjectMetadata
            {
                Exists = true,
                ContentLength = resp.Content.Headers.ContentLength ?? 0,
                ContentType = resp.Content.Headers.ContentType?.MediaType,
                ETag = resp.Headers.ETag?.Tag,
                LastModified = resp.Content.Headers.LastModified?.UtcDateTime
            };
        }
    }
}
