using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OnlySplit.Application.Interfaces;

namespace OnlySplit.Infrastructure.Services;

public sealed class FileUploadService(
    IOptions<FileStorageOptions> options,
    ILogger<FileUploadService> logger
) : IFileUploadService
{
    private readonly FileStorageOptions _options = options.Value;

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp"
    };

    private static readonly HashSet<string> BlacklistedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".exe", ".bat", ".cmd", ".sh", ".ps1", ".dll", ".com", ".msi"
    };

    private static readonly Dictionary<string, string> ExtensionToMimeType = new(StringComparer.OrdinalIgnoreCase)
    {
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".png"] = "image/png",
        [".webp"] = "image/webp"
    };

    // Magic byte signatures
    private static readonly byte[] JpegMagicBytes = [0xFF, 0xD8, 0xFF];
    private static readonly byte[] PngMagicBytes = [0x89, 0x50, 0x4E, 0x47];
    private static readonly byte[] RiffMagicBytes = [(byte)'R', (byte)'I', (byte)'F', (byte)'F'];
    private static readonly byte[] WebpSignature = [(byte)'W', (byte)'E', (byte)'B', (byte)'P'];

    public async Task<FileUploadResult> UploadAvatarAsync(
        Guid userId, Stream fileStream, string fileName, string contentType,
        long fileSize, CancellationToken cancellationToken = default)
    {
        ValidateFile(fileName, contentType, fileSize, _options.MaxAvatarSizeBytes);
        await ValidateMagicBytesAsync(fileStream, fileName, cancellationToken);

        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        var generatedFileName = $"{userId}_{Guid.NewGuid()}{extension}";
        var subdirectory = "avatars";
        var relativePath = Path.Combine(subdirectory, generatedFileName);
        var fullPath = Path.Combine(_options.UploadPath, relativePath);

        await WriteFileAsync(fullPath, fileStream, cancellationToken);

        var publicUrl = $"/uploads/{subdirectory}/{generatedFileName}";
        return new FileUploadResult(publicUrl, generatedFileName, fileSize);
    }

    public async Task<FileUploadResult> UploadProofAsync(
        Guid paymentId, Stream fileStream, string fileName, string contentType,
        long fileSize, CancellationToken cancellationToken = default)
    {
        ValidateFile(fileName, contentType, fileSize, _options.MaxProofSizeBytes);
        await ValidateMagicBytesAsync(fileStream, fileName, cancellationToken);

        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        var generatedFileName = $"{paymentId}_{Guid.NewGuid()}{extension}";
        var subdirectory = "proofs";
        var relativePath = Path.Combine(subdirectory, generatedFileName);
        var fullPath = Path.Combine(_options.UploadPath, relativePath);

        await WriteFileAsync(fullPath, fileStream, cancellationToken);

        var publicUrl = $"/uploads/{subdirectory}/{generatedFileName}";
        return new FileUploadResult(publicUrl, generatedFileName, fileSize);
    }

    public Task DeleteFileAsync(string relativePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
            return Task.CompletedTask;

        try
        {
            // Convert public URL path to filesystem path
            // relativePath expected format: /uploads/avatars/filename or /uploads/proofs/filename
            var normalizedPath = relativePath.TrimStart('/');
            if (normalizedPath.StartsWith("uploads/", StringComparison.OrdinalIgnoreCase))
            {
                normalizedPath = normalizedPath["uploads/".Length..];
            }

            var fullPath = Path.Combine(_options.UploadPath, normalizedPath);

            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
                logger.LogInformation("Deleted file: {FilePath}", fullPath);
            }
        }
        catch (Exception ex)
        {
            // Log warning but don't throw — old file deletion failure is non-critical
            logger.LogWarning(ex, "Failed to delete file at path: {RelativePath}", relativePath);
        }

        return Task.CompletedTask;
    }

    private void ValidateFile(string fileName, string contentType, long fileSize, long maxSizeBytes)
    {
        // Check empty file
        if (fileSize <= 0)
        {
            throw new InvalidOperationException("File must not be empty.");
        }

        var extension = Path.GetExtension(fileName)?.ToLowerInvariant();

        // Check executable blacklist
        if (!string.IsNullOrEmpty(extension) && BlacklistedExtensions.Contains(extension))
        {
            throw new InvalidOperationException("File extension is not allowed.");
        }

        // Check extension whitelist
        if (string.IsNullOrEmpty(extension) || !AllowedExtensions.Contains(extension))
        {
            throw new InvalidOperationException("File type not allowed. Accepted types: .jpg, .jpeg, .png, .webp");
        }

        // Check content-type matches extension
        if (!ExtensionToMimeType.TryGetValue(extension, out var expectedMimeType) ||
            !string.Equals(contentType, expectedMimeType, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("File content-type does not match the file extension.");
        }

        // Check file size
        if (fileSize > maxSizeBytes)
        {
            var maxSizeMb = maxSizeBytes / (1024 * 1024);
            throw new InvalidOperationException($"File size exceeds the maximum allowed size of {maxSizeMb} MB.");
        }
    }

    private static async Task ValidateMagicBytesAsync(Stream fileStream, string fileName, CancellationToken cancellationToken)
    {
        var extension = Path.GetExtension(fileName)?.ToLowerInvariant();

        // Read enough bytes for validation (12 bytes covers all signatures including WEBP)
        var buffer = new byte[12];
        var originalPosition = fileStream.Position;

        if (fileStream.CanSeek)
        {
            fileStream.Position = 0;
        }

        var bytesRead = await fileStream.ReadAsync(buffer.AsMemory(0, 12), cancellationToken);

        // Reset stream position for subsequent file writing
        if (fileStream.CanSeek)
        {
            fileStream.Position = originalPosition;
        }

        var isValid = extension switch
        {
            ".jpg" or ".jpeg" => bytesRead >= 3 && buffer[0] == JpegMagicBytes[0] &&
                                  buffer[1] == JpegMagicBytes[1] && buffer[2] == JpegMagicBytes[2],
            ".png" => bytesRead >= 4 && buffer[0] == PngMagicBytes[0] && buffer[1] == PngMagicBytes[1] &&
                      buffer[2] == PngMagicBytes[2] && buffer[3] == PngMagicBytes[3],
            ".webp" => bytesRead >= 12 &&
                       buffer[0] == RiffMagicBytes[0] && buffer[1] == RiffMagicBytes[1] &&
                       buffer[2] == RiffMagicBytes[2] && buffer[3] == RiffMagicBytes[3] &&
                       buffer[8] == WebpSignature[0] && buffer[9] == WebpSignature[1] &&
                       buffer[10] == WebpSignature[2] && buffer[11] == WebpSignature[3],
            _ => false
        };

        if (!isValid)
        {
            throw new InvalidOperationException("File content does not match the declared file type.");
        }
    }

    private async Task WriteFileAsync(string fullPath, Stream fileStream, CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        if (fileStream.CanSeek)
        {
            fileStream.Position = 0;
        }

        await using var fileStreamOut = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None);
        await fileStream.CopyToAsync(fileStreamOut, cancellationToken);
    }
}
