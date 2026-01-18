using System.Security.Cryptography;
using OrgManagement.Application.Common.Interfaces;

namespace OrgManagement.Infrastructure.Services;

public class PasswordService : IPasswordService
{
    public string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password, BCrypt.Net.BCrypt.GenerateSalt(12));
    }

    public bool VerifyPassword(string password, string hash)
    {
        return BCrypt.Net.BCrypt.Verify(password, hash);
    }

    public string GenerateRandomPassword(int length = 16)
    {
        const string validChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890!@#$%^&*";
        var randomBytes = new byte[length];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);

        var chars = new char[length];
        for (int i = 0; i < length; i++)
        {
            chars[i] = validChars[randomBytes[i] % validChars.Length];
        }

        // Ensure at least one uppercase, one lowercase, one digit, and one special char
        chars[0] = "ABCDEFGHIJKLMNOPQRSTUVWXYZ"[randomBytes[0] % 26];
        chars[1] = "abcdefghijklmnopqrstuvwxyz"[randomBytes[1] % 26];
        chars[2] = "1234567890"[randomBytes[2] % 10];
        chars[3] = "!@#$%^&*"[randomBytes[3] % 8];

        // Shuffle
        for (int i = chars.Length - 1; i > 0; i--)
        {
            int j = randomBytes[i] % (i + 1);
            (chars[i], chars[j]) = (chars[j], chars[i]);
        }

        return new string(chars);
    }
}
