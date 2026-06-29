namespace BEScanCV.Infrastructure.Options;

public sealed class CvStorageOptions
{
    public const string SectionName = "CvStorage";

    public string LocalPdfFolder { get; set; } = @"D:\PDFLocal";
}
