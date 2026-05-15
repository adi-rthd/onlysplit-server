namespace OnlySplit.Infrastructure.Authentication;

public static class PasswordHasher
{
    public static string Hash(string password) => BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);

    public static bool Verify(string password, string passwordHash) => BCrypt.Net.BCrypt.Verify(password, passwordHash);
}
