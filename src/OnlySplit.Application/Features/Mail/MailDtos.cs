namespace OnlySplit.Application.Features.Mail;
public sealed class EmailSettings
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 25;
    public string FromEmail { get; set; } = "noreply@onlylabs.in";
    public string FromName { get; set; } = "OnlySplit";
}