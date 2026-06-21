using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlySplit.Application.Interfaces;
using OnlySplit.Infrastructure.Database;
using OnlySplit.Shared.Responses;

namespace OnlySplit.API.Controllers;

[ApiController]
[Authorize]
[Route("api/users")]
public sealed class UsersController(
    IFileUploadService fileUploadService,
    ICurrentUserService currentUserService,
    OnlySplitDbContext context,
    ILogger<UsersController> logger
) : ControllerBase
{
    [HttpPost("avatar")]
    public async Task<ActionResult<ApiResponse<AvatarUploadResponse>>> UploadAvatar(
        IFormFile? file, CancellationToken cancellationToken)
    {
        // 1. Check if file is null or empty
        if (file is null || file.Length == 0)
        {
            return BadRequest(ApiResponse<AvatarUploadResponse>.Fail("A file is required."));
        }

        var userId = currentUserService.UserId;

        FileUploadResult uploadResult;
        try
        {
            // 2. Call IFileUploadService for avatar upload
            await using var stream = file.OpenReadStream();
            uploadResult = await fileUploadService.UploadAvatarAsync(
                userId,
                stream,
                file.FileName,
                file.ContentType,
                file.Length,
                cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            // Validation failure from FileUploadService → 400
            return BadRequest(ApiResponse<AvatarUploadResponse>.Fail(ex.Message));
        }
        catch (IOException ex)
        {
            // File write failure → 500
            logger.LogError(ex, "Failed to write avatar file for user {UserId}", userId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponse<AvatarUploadResponse>.Fail("An error occurred while storing the file."));
        }

        // 3. Get the current user and check for existing avatar
        var user = await context.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null)
        {
            // Clean up the newly uploaded file since we can't associate it
            await fileUploadService.DeleteFileAsync(uploadResult.PublicUrl, cancellationToken);
            return NotFound(ApiResponse<AvatarUploadResponse>.Fail("User not found."));
        }

        var oldAvatarUrl = user.AvatarUrl;

        // 4. If old avatar exists and starts with /uploads/avatars/, delete it
        if (!string.IsNullOrEmpty(oldAvatarUrl) &&
            oldAvatarUrl.StartsWith("/uploads/avatars/", StringComparison.OrdinalIgnoreCase))
        {
            await fileUploadService.DeleteFileAsync(oldAvatarUrl, cancellationToken);
        }

        // 5. Update user.AvatarUrl with the new public URL
        user.AvatarUrl = uploadResult.PublicUrl;
        user.UpdatedAt = DateTimeOffset.UtcNow;

        try
        {
            // 6. Save to DB
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            // 7. If DB save fails, clean up the new file to prevent orphaned files
            logger.LogError(ex, "Failed to save avatar URL to database for user {UserId}", userId);
            await fileUploadService.DeleteFileAsync(uploadResult.PublicUrl, cancellationToken);
            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponse<AvatarUploadResponse>.Fail("An error occurred while storing the file."));
        }

        // 8. Return 200 with the new avatarUrl
        var response = new AvatarUploadResponse(uploadResult.PublicUrl);
        return Ok(ApiResponse<AvatarUploadResponse>.Ok(response, "Avatar uploaded successfully."));
    }
}

public sealed record AvatarUploadResponse(string AvatarUrl);
