// ActivityResponse.cs

namespace OnlySplit.Application.Activities.DTOs;

public class ActivityResponse
{
    public Guid Id { get; set; }

    public string Type { get; set; } = "expense";

    public string Title { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public string Currency { get; set; } = "USD";

    public Guid GroupId { get; set; }

    public string GroupName { get; set; } = string.Empty;

    public Guid UserId { get; set; } 
    public Guid UserName { get; set; } 
    public DateTimeOffset CreatedAt { get; set; }
}