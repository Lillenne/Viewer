using System.Text;

namespace Viewer.Server.Controllers;

public class JwtOptions
{
    public const string JwtSettings = "JwtSettings";
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public int ExpiryTimeMinutes { get; set; }
    public string HashAlgorithm { get; set; } = "HmacSha256Signature";
    public byte[] KeyBytesUtf8 => _keyBytesUtf8 ??= Encoding.UTF8.GetBytes(Key);
    private byte[]? _keyBytesUtf8;
}