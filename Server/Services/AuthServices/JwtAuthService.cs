using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Viewer.Server.Configuration;
using Viewer.Shared;
using Viewer.Shared.Users;
using User = Viewer.Server.Models.User;

namespace Viewer.Server.Services.AuthServices;

public class JwtAuthService : IAuthService
{
    private readonly IUserRepository _repo;
    private readonly IClaimsParser _parser;
    private readonly JwtOptions _jwt;

    public JwtAuthService(IOptions<JwtOptions> options, IUserRepository repo)
    {
        _repo = repo;
        _parser = new JwtClaimsParser();
        _jwt = options.Value;
    }

    public async Task<AuthToken> GetToken(Guid userId)
    {
        var user = await GetUserInformation(userId).ConfigureAwait(false);
        return new(CreateToken(user));
    }

    public async Task<AuthToken> Login(UserLogin userLogin)
    {
        if (string.IsNullOrEmpty(userLogin.Email))
            throw new ArgumentException("Username required");
        if (string.IsNullOrEmpty(userLogin.Password))
            throw new ArgumentException("Password required");
        var user = await GetUserInformation(userLogin.Email);
        if (!VerifyPassword(userLogin.Password, user.PasswordHash, user.PasswordSalt))
        {
            throw new ArgumentException("Invalid password");
        }
        var token = CreateToken(user);
        return new AuthToken(token);
    }

    public async Task ChangePassword(ChangePasswordRequest request)
    {
        var user = await GetUserInformation(Guid.Parse(request.UserId)).ConfigureAwait(false);
        if (!VerifyPassword(request.OldPassword, user.PasswordHash, user.PasswordSalt))
            throw new ArgumentException("Invalid password");
        var (hash, salt) = CreatePasswordHash(request.NewPassword);
        user.PasswordHash = hash;
        user.PasswordSalt = salt;
        await _repo.UpdateUser(user);
    }

    public async Task Register(UserRegistration info)
    {
        var (hash, salt) = CreatePasswordHash(info.Password);
        var user = new User
        {
            UserName = info.Username,
            FirstName = info.FirstName,
            LastName = info.LastName,
            Email = info.Email,
            PasswordHash = hash,
            PasswordSalt = salt,
            Id = Guid.NewGuid(),
        };
        await _repo.AddUser(user);
    }

    public Task<UserDto?> WhoAmI(ClaimsPrincipal principal)
    {
        return Task.FromResult<UserDto?>(_parser.ParseClaims(principal));
    }

    private static (byte[] hash, byte[] salt) CreatePasswordHash(string pwd)
    {
        using var hmac = new HMACSHA256();
        return (hmac.ComputeHash(Encoding.UTF8.GetBytes(pwd)), hmac.Key);
    }

    private static bool VerifyPassword(string pwd, byte[] hash, byte[] salt)
    {
        using var hmac = new HMACSHA256(salt);
        var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(pwd));
        return computedHash.SequenceEqual(hash);
    }

    private string CreateToken(User user)
    {
        var handler = new JwtSecurityTokenHandler();

        var time = DateTime.UtcNow;
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Name, user.UserName),
            new(JwtRegisteredClaimNames.NameId, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.AuthTime, time.ToShortTimeString()),
        };
        if (user.Roles is not null)
        {
            foreach (var role in user.Roles.Select(r => r.Role?.RoleName))
            {
                if (role is not null)
                    claims.Add(new Claim(ClaimTypes.Role, role));
            }
        }
        if (user.FirstName is not null)
            claims.Add(new(JwtRegisteredClaimNames.GivenName, user.FirstName));
        if (user.LastName is not null)
            claims.Add(new(JwtRegisteredClaimNames.FamilyName, user.LastName));

        var secKey = new SymmetricSecurityKey(_jwt.KeyBytes);
        var creds = new SigningCredentials(secKey, _jwt.HashAlgorithm);
        var descriptor = new SecurityTokenDescriptor()
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(_jwt.ExpiryTimeMinutes),
            Issuer = _jwt.Issuer,
            Audience = _jwt.Audience,
            SigningCredentials = creds
        };

        var token = handler.CreateToken(descriptor);
        var jwt = handler.WriteToken(token);
        return jwt;
    }

    private Task<User> GetUserInformation(Guid userId)
    {
        return _repo.GetUser(userId);
    }
    private Task<User> GetUserInformation(string email)
    {
        return _repo.GetUser(email);
    }
}
