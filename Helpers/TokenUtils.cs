using System.Security.Cryptography;
using System.Text;
namespace E_Commerce.Helpers
{
public static class TokenUtils
{
    public static string GenerateSecureToken(int bytes = 64)
    {
        var data = RandomNumberGenerator.GetBytes(bytes);
        return Convert.ToBase64String(data);
    }

    public static string Sha256(string input)
    {
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
        return Convert.ToBase64String(bytes);
    }
}
}
