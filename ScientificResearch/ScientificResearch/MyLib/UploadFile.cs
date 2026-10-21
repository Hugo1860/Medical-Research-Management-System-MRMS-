using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MyLib
{
    /// <summary>
    /// Validates and stores uploaded research attachments.
    /// </summary>
    public static class UploadFile
    {
        private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpeg", ".jpg", ".gif", ".png", ".csv", ".txt", ".xls", ".xlsx", ".doc", ".docx", ".pdf"
        };

        public static async Task<List<string>> Upload(
            IFormFileCollection files,
            string absolutePath,
            string relativePath,
            int uploadFileMaxSize)
        {
            if (files == null || files.Count == 0)
            {
                throw new ArgumentException("没有要上传的文件", nameof(files));
            }

            if (uploadFileMaxSize <= 0)
            {
                throw new InvalidOperationException("上传文件大小限制未配置");
            }

            var normalizedRelativePath = relativePath.Replace('\\', '/').Trim('/');
            if (string.IsNullOrWhiteSpace(normalizedRelativePath) ||
                normalizedRelativePath.Split('/').Any(segment => segment is ".." or "."))
            {
                throw new ArgumentException("上传目录无效", nameof(relativePath));
            }

            var root = Path.GetFullPath(absolutePath);
            var path = Path.GetFullPath(Path.Combine(root, normalizedRelativePath.Replace('/', Path.DirectorySeparatorChar)));
            if (!path.StartsWith(root.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("上传目录超出允许范围", nameof(relativePath));
            }

            Directory.CreateDirectory(path);
            var filesNameList = new List<string>(files.Count);
            foreach (var file in files)
            {
                if (file == null || file.Length == 0)
                {
                    throw new ArgumentException("不能上传空文件");
                }

                if (file.Length > uploadFileMaxSize)
                {
                    var maxMegabytes = Math.Round(uploadFileMaxSize / 1024d / 1024d, 2);
                    throw new ArgumentException($"文件大小不能超出{maxMegabytes}MB");
                }

                var extension = Path.GetExtension(file.FileName);
                if (!AllowedExtensions.Contains(extension))
                {
                    throw new ArgumentException("文件格式不支持");
                }

                var generatedName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
                var absoluteFilename = Path.Combine(path, generatedName);
                await using (var stream = new FileStream(
                    absoluteFilename,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None,
                    bufferSize: 64 * 1024,
                    useAsync: true))
                {
                    await file.CopyToAsync(stream);
                }

                filesNameList.Add("/" + normalizedRelativePath + "/" + generatedName);
            }

            return filesNameList;
        }
    }
}
