using System;
using System.Collections.Generic;

namespace ScientificResearch.Infrastucture.Storage
{
    public class StorageConfig
    {
        public string Provider { get; set; } = "Minio"; // Minio, Local, S3
        public MinioConfig Minio { get; set; } = new();
        public LocalStorageConfig Local { get; set; } = new();
    }

    public class MinioConfig
    {
        public string Endpoint { get; set; } = "minio:9000";
        public string ExternalEndpoint { get; set; } = "http://127.0.0.1:19000";
        public string AccessKey { get; set; } = "admin";
        public string SecretKey { get; set; } = "ResearchAdmin2026!";
        public string BucketName { get; set; } = "scientificresearch-private";
        public string Region { get; set; } = "us-east-1";
        public bool UseSSL { get; set; } = false;
    }

    public class LocalStorageConfig
    {
        public string BasePath { get; set; } = "wwwroot/upload";
        public string BaseUrlPrefix { get; set; } = "/upload";
    }

    public class PresignedUploadResult
    {
        public string UploadUrl { get; set; } = string.Empty;
        public string Method { get; set; } = "PUT";
        public Dictionary<string, string> Headers { get; set; } = new();
        public DateTime Expiration { get; set; }
    }

    public class ObjectMetadata
    {
        public bool Exists { get; set; }
        public long ContentLength { get; set; }
        public string? ContentType { get; set; }
        public string? ETag { get; set; }
        public DateTime? LastModified { get; set; }
    }
}
