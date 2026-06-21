namespace OnlySplit.Application.Interfaces;

public interface IFileUploadService
{
    Task<FileUploadResult> UploadAvatarAsync(
        Guid userId, Stream fileStream, string fileName, string contentType,
        long fileSize, CancellationToken cancellationToken = default);

    Task<FileUploadResult> UploadProofAsync(
        Guid paymentId, Stream fileStream, string fileName, string contentType,
        long fileSize, CancellationToken cancellationToken = default);

    Task DeleteFileAsync(string relativePath, CancellationToken cancellationToken = default);
}

public sealed record FileUploadResult(string PublicUrl, string FileName, long FileSize);
