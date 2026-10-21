using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace ScientificResearch.Infrastucture.Storage
{
    /// <summary>
    /// AWS Signature Version 4 signer for S3/MinIO compatible object storage.
    /// Provides dependency-free SigV4 query-string presigning and authorization headers.
    /// </summary>
    public static class S3Signer
    {
        public static string GeneratePresignedUrl(
            string httpMethod,
            string endpointUrl,
            string bucket,
            string objectKey,
            string accessKey,
            string secretKey,
            string region,
            TimeSpan expiry,
            Dictionary<string, string>? additionalQueryParams = null)
        {
            var uri = new Uri(endpointUrl.TrimEnd('/') + "/" + bucket + "/" + objectKey.TrimStart('/'));
            var now = DateTime.UtcNow;
            var dateStamp = now.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
            var amzDate = now.ToString("yyyyMMddTHHmmssZ", CultureInfo.InvariantCulture);
            var credentialScope = $"{dateStamp}/{region}/s3/aws4_request";
            var credential = $"{accessKey}/{credentialScope}";
            var expires = ((long)expiry.TotalSeconds).ToString();

            var queryParams = new SortedDictionary<string, string>(StringComparer.Ordinal)
            {
                { "X-Amz-Algorithm", "AWS4-HMAC-SHA256" },
                { "X-Amz-Credential", credential },
                { "X-Amz-Date", amzDate },
                { "X-Amz-Expires", expires },
                { "X-Amz-SignedHeaders", "host" }
            };

            if (additionalQueryParams != null)
            {
                foreach (var kvp in additionalQueryParams)
                {
                    queryParams[kvp.Key] = kvp.Value;
                }
            }

            var canonicalQueryString = BuildCanonicalQueryString(queryParams);
            var hostHeader = uri.IsDefaultPort ? uri.Host : $"{uri.Host}:{uri.Port}";
            var canonicalHeaders = $"host:{hostHeader}\n";
            var signedHeaders = "host";
            var canonicalUri = GetCanonicalUri(uri.AbsolutePath);

            var canonicalRequest = string.Join("\n",
                httpMethod.ToUpperInvariant(),
                canonicalUri,
                canonicalQueryString,
                canonicalHeaders,
                signedHeaders,
                "UNSIGNED-PAYLOAD"
            );

            var stringToSign = string.Join("\n",
                "AWS4-HMAC-SHA256",
                amzDate,
                credentialScope,
                ToHex(Sha256(Encoding.UTF8.GetBytes(canonicalRequest)))
            );

            var signingKey = GetSignatureKey(secretKey, dateStamp, region, "s3");
            var signature = ToHex(HmacSha256(signingKey, stringToSign));

            return $"{uri.GetLeftPart(UriPartial.Path)}?{canonicalQueryString}&X-Amz-Signature={signature}";
        }

        public static Dictionary<string, string> GenerateAuthHeaders(
            string httpMethod,
            Uri requestUri,
            string accessKey,
            string secretKey,
            string region,
            byte[] payloadBytes)
        {
            var now = DateTime.UtcNow;
            var dateStamp = now.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
            var amzDate = now.ToString("yyyyMMddTHHmmssZ", CultureInfo.InvariantCulture);
            var credentialScope = $"{dateStamp}/{region}/s3/aws4_request";
            var payloadHash = ToHex(Sha256(payloadBytes));

            var hostHeader = requestUri.IsDefaultPort ? requestUri.Host : $"{requestUri.Host}:{requestUri.Port}";
            var headersToSign = new SortedDictionary<string, string>(StringComparer.Ordinal)
            {
                { "host", hostHeader },
                { "x-amz-content-sha256", payloadHash },
                { "x-amz-date", amzDate }
            };

            var canonicalHeaders = string.Join("", headersToSign.Select(kv => $"{kv.Key}:{kv.Value.Trim()}\n"));
            var signedHeaders = string.Join(";", headersToSign.Keys);
            var canonicalUri = GetCanonicalUri(requestUri.AbsolutePath);

            var canonicalQuery = string.Empty;
            if (!string.IsNullOrEmpty(requestUri.Query))
            {
                var parsedQuery = System.Web.HttpUtility.ParseQueryString(requestUri.Query);
                var sorted = new SortedDictionary<string, string>(StringComparer.Ordinal);
                foreach (string key in parsedQuery.AllKeys)
                {
                    if (key != null) sorted[key] = parsedQuery[key] ?? string.Empty;
                }
                canonicalQuery = BuildCanonicalQueryString(sorted);
            }

            var canonicalRequest = string.Join("\n",
                httpMethod.ToUpperInvariant(),
                canonicalUri,
                canonicalQuery,
                canonicalHeaders,
                signedHeaders,
                payloadHash
            );

            var stringToSign = string.Join("\n",
                "AWS4-HMAC-SHA256",
                amzDate,
                credentialScope,
                ToHex(Sha256(Encoding.UTF8.GetBytes(canonicalRequest)))
            );

            var signingKey = GetSignatureKey(secretKey, dateStamp, region, "s3");
            var signature = ToHex(HmacSha256(signingKey, stringToSign));
            var authHeader = $"AWS4-HMAC-SHA256 Credential={accessKey}/{credentialScope}, SignedHeaders={signedHeaders}, Signature={signature}";

            return new Dictionary<string, string>
            {
                { "x-amz-date", amzDate },
                { "x-amz-content-sha256", payloadHash },
                { "Authorization", authHeader }
            };
        }

        private static string GetCanonicalUri(string path)
        {
            if (string.IsNullOrEmpty(path)) return "/";
            var segments = path.Split('/');
            var encodedSegments = segments.Select(s => Uri.EscapeDataString(s));
            return string.Join("/", encodedSegments);
        }

        private static string BuildCanonicalQueryString(SortedDictionary<string, string> queryParams)
        {
            var sb = new StringBuilder();
            foreach (var kvp in queryParams)
            {
                if (sb.Length > 0) sb.Append('&');
                sb.Append(Uri.EscapeDataString(kvp.Key));
                sb.Append('=');
                sb.Append(Uri.EscapeDataString(kvp.Value));
            }
            return sb.ToString();
        }

        private static byte[] GetSignatureKey(string key, string dateStamp, string regionName, string serviceName)
        {
            var kSecret = Encoding.UTF8.GetBytes("AWS4" + key);
            var kDate = HmacSha256(kSecret, dateStamp);
            var kRegion = HmacSha256(kDate, regionName);
            var kService = HmacSha256(kRegion, serviceName);
            var kSigning = HmacSha256(kService, "aws4_request");
            return kSigning;
        }

        private static byte[] HmacSha256(byte[] key, string data)
        {
            using var hmac = new HMACSHA256(key);
            return hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        }

        private static byte[] Sha256(byte[] data)
        {
            using var sha = SHA256.Create();
            return sha.ComputeHash(data);
        }

        private static string ToHex(byte[] bytes)
        {
            var sb = new StringBuilder(bytes.Length * 2);
            foreach (var b in bytes) sb.Append(b.ToString("x2"));
            return sb.ToString();
        }
    }
}
