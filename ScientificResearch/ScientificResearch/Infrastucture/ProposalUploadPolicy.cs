using Microsoft.AspNetCore.Http;

namespace ScientificResearch.Infrastucture;

public static class ProposalUploadPolicy
{
    public const int MaxFiles = 10;
    public const long MaxTotalBytes = 100L * 1024 * 1024;
    // Reserve space for multipart boundaries, headers and form metadata.
    public const long MaxRequestBytes = MaxTotalBytes + 2L * 1024 * 1024;
    public const string LimitsMessage = "每次请选择 1–10 份 PDF，每份不能为空且不能超过 20 MB，文件总计不能超过 100 MB";

    public static void Validate(IReadOnlyCollection<IFormFile> files)
    {
        if (files == null || files.Count is < 1 or > MaxFiles ||
            files.Any(f => f == null || f.Length is <= 0 or > ProposalPdfParser.MaxFileBytes ||
                !Path.GetExtension(f.FileName).Equals(".pdf", StringComparison.OrdinalIgnoreCase)))
            throw new ArgumentException(LimitsMessage);
        if (files.Sum(f => f.Length) > MaxTotalBytes) throw new ArgumentException(LimitsMessage);
        if (files.Any(f => Path.GetFileName(f.FileName.Replace('\\', '/')).Length > 300))
            throw new ArgumentException("文件名不能超过 300 字");
    }
}
