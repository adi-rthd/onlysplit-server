namespace OnlySplit.Infrastructure.Services;

public sealed class FileStorageOptions
{
    public const string SectionName = "FileStorage";

    public string UploadPath { get; set; } = "/app/uploads";
    public long MaxAvatarSizeBytes { get; set; } = 5_242_880;
    public long MaxProofSizeBytes { get; set; } = 10_485_760;
    public string[] AllowedImageExtensions { get; set; } = [".jpg", ".jpeg", ".png", ".webp"];
}
