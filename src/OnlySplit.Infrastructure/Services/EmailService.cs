using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using OnlySplit.Application.Features.Mail;
using OnlySplit.Application.Interfaces;
public sealed class EmailService : IEmailService
{
    private readonly EmailSettings _settings;

    public EmailService(IOptions<EmailSettings> settings)
    {
        _settings = settings.Value;
    }

    public async Task SendAsync(
        string to,
        string subject,
        string htmlBody,
        CancellationToken cancellationToken = default)
    {
        var message = new MimeMessage();

        message.From.Add(
            new MailboxAddress(
                _settings.FromName,
                _settings.FromEmail));

        message.To.Add(MailboxAddress.Parse(to));

        message.Subject = subject;

        message.Body = new BodyBuilder
        {
            HtmlBody = htmlBody
        }.ToMessageBody();

        using var client = new SmtpClient();
        Console.WriteLine($"SMTP Host = {_settings.Host}");
        Console.WriteLine($"SMTP Port = {_settings.Port}");
        await client.ConnectAsync(
            _settings.Host,
            _settings.Port,
            SecureSocketOptions.None);

        // NO AUTHENTICATION REQUIRED

        await client.SendAsync(message, cancellationToken);

        await client.DisconnectAsync(
            true,
            cancellationToken);
    }
}