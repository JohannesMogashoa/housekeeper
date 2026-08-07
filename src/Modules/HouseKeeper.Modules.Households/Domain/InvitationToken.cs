using System.Security.Cryptography;
using System.Text;

namespace HouseKeeper.Modules.Households.Domain;

internal static class InvitationToken
{
    public static string Create() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

    public static string Digest(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    public static bool MatchesDigest(string value, string expectedDigest) =>
        CryptographicOperations.FixedTimeEquals(
            Convert.FromHexString(expectedDigest),
            Convert.FromHexString(Digest(value)));
}
