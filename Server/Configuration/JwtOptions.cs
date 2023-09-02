using System.ComponentModel.DataAnnotations;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Viewer.Server.Configuration;

public class JwtOptions
{
    public const string JwtSettings = "JwtSettings";
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    [Range(1,int.MaxValue)]
    public int ExpiryTimeSeconds { get; set; }
    [Range(1,int.MaxValue)]
    public int RefreshExpiryTimeMinutes { get; set; }
    public string HashAlgorithm { get; set; } = SecurityAlgorithms.HmacSha256Signature;
    public byte[] KeyBytes => _keyBytes ??= Encoding.UTF8.GetBytes(Key);
    private byte[]? _keyBytes;
}