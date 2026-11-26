using System.Security.Cryptography;
using System.Text;

namespace Planboard.Api.Auth;

public static class ApiTokenHasher
{
    public static byte[] Hash(string token) => SHA256.HashData(Encoding.UTF8.GetBytes(token));
}
