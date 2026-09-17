using System.Security.Cryptography;
using Identity.Application.Common.Interfaces;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;

namespace Identity.Infrastructure.Services;

public class PasswordHasher : IPasswordHasher
{
    public string HashPassword(string password)
    {
        byte[] salt = RandomNumberGenerator.GetBytes(128 / 8);

        byte[] subkey = KeyDerivation.Pbkdf2(
            password: password,
            salt: salt,
            prf: KeyDerivationPrf.HMACSHA256,
            iterationCount: 100000,
            numBytesRequested: 256 / 8);

        var outputBytes = new byte[13 + salt.Length + subkey.Length];
        outputBytes[0] = 0x01; // Format marker
        Buffer.BlockCopy(salt, 0, outputBytes, 1, salt.Length);
        Buffer.BlockCopy(subkey, 0, outputBytes, 1 + salt.Length, subkey.Length);

        return Convert.ToBase64String(outputBytes);
    }

    public bool VerifyPassword(string password, string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(passwordHash))
            return false;

        try
        {
            byte[] decoded = Convert.FromBase64String(passwordHash);
            if (decoded.Length < 1 + 16 + 32 || decoded[0] != 0x01)
                return false;

            byte[] salt = new byte[16];
            Buffer.BlockCopy(decoded, 1, salt, 0, 16);

            byte[] expectedSubkey = new byte[32];
            Buffer.BlockCopy(decoded, 17, expectedSubkey, 0, 32);

            byte[] actualSubkey = KeyDerivation.Pbkdf2(
                password: password,
                salt: salt,
                prf: KeyDerivationPrf.HMACSHA256,
                iterationCount: 100000,
                numBytesRequested: 32);

            return CryptographicOperations.FixedTimeEquals(actualSubkey, expectedSubkey);
        }
        catch
        {
            return false;
        }
    }
}
