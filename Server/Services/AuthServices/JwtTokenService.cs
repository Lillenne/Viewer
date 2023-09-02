using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Viewer.Server.Configuration;

namespace Viewer.Server.Services.AuthServices;

public class JwtTokenService : ITokenService
{
    private readonly JwtOptions _jwt;

    public JwtTokenService(IOptions<JwtOptions> options)
    {
        _jwt = options.Value;
    }

    public ClaimsPrincipal GetClaims(string token)
    {
        var val = new TokenValidationParameters
        {
            ValidateAudience = true,
            ValidAudience = _jwt.Audience,
            ValidateIssuer = true,
            ValidIssuer = _jwt.Issuer,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(_jwt.KeyBytes),
            ValidateLifetime = false,
        };
        var principal = new JwtSecurityTokenHandler().ValidateToken(token, val, out var secToken);
        if (secToken is not JwtSecurityToken)
            throw new SecurityTokenException();
        return principal;
    }

    public string CreateToken(ClaimsIdentity identity)
    {
        var handler = new JwtSecurityTokenHandler();
        var secKey = new SymmetricSecurityKey(_jwt.KeyBytes);
        var creds = new SigningCredentials(secKey, _jwt.HashAlgorithm);
        var descriptor = new SecurityTokenDescriptor()
        {
            Subject = identity,
            Expires = DateTime.UtcNow.AddSeconds(_jwt.ExpiryTimeSeconds),
            Issuer = _jwt.Issuer,
            Audience = _jwt.Audience,
            SigningCredentials = creds
        };

        var token = handler.CreateToken(descriptor);
        var jwt = handler.WriteToken(token);
        return jwt;
    }

    public string CreateRefreshToken()
    {
        var b = new byte[32];
        Guid.NewGuid().TryWriteBytes(b.AsSpan(0, 16));
        Guid.NewGuid().TryWriteBytes(b.AsSpan(16, 16));
        return Convert.ToBase64String(b);
    }
}