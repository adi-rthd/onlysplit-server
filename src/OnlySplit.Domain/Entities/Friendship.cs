using OnlySplit.Domain.Entities;

namespace OnlySplit.Domain.Entities;

public sealed class Friendship
{
    public Guid Id { get; set; }

    public Guid RequesterId { get; set; }
    public User Requester { get; set; } = null!;

    public Guid AddresseeId { get; set; }
    public User Addressee { get; set; } = null!;

    public string Status { get; set; } = "Pending";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}