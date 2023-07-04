using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using LanguageExt.Common;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Viewer.Server.Controllers;
using Viewer.Server.Models;
using Viewer.Shared;
using Viewer.Shared.Dtos;
using Viewer.Shared.Requests;
using User = Viewer.Server.Models.User;

namespace Viewer.Server.Services;

public class JwtAuthService : IAuthService
{
    private readonly IUserRepository _repo;
    private readonly JwtOptions _jwt;

    public JwtAuthService(IOptions<JwtOptions> options, IUserRepository repo) // TODO hook up DB
    {
        _repo = repo;
        _jwt = options.Value;
    }

    public async Task<Result<AuthToken>> Login(UserLogin userLogin)
    {
        if (string.IsNullOrEmpty(userLogin.Username))
            return new Result<AuthToken>(new ArgumentException("Username required"));
        var user = await GetUserInformation(userLogin.Username);
        if (!VerifyPassword(userLogin.Password, user.PasswordHash, user.PasswordSalt))
        {
            return new Result<AuthToken>(new ArgumentException("Incorrect password"));
        }
        var token = CreateToken(user);
        return new Result<AuthToken>(new AuthToken(token));
    }
    
    public async Task<Result<bool>> ChangePassword(ChangePasswordRequest request)
    {
        var user = await GetUserInformation(request.UserId);
        if (!VerifyPassword(request.OldPassword, user.PasswordHash, user.PasswordSalt))
            return new Result<bool>(new ArgumentException("Invalid password"));
        var (hash, salt) = CreatePasswordHash(request.NewPassword);
        user = user with { PasswordHash = hash, PasswordSalt = salt };
        return await SaveUser(user);
    }

    // Todo change sig
    private async Task<Result<bool>> SaveUser(User user)
    {
        // TODO 
        await _repo.AddUser(user);
        return true;
    }

    public async Task<Result<bool>> Register(UserRegistration info)
    {
        var (hash, salt) = CreatePasswordHash(info.Password);
        var user = new User
        {
            Id = Guid.NewGuid(),
            UserName = info.Username,
            FirstName = info.FirstName,
            LastName = info.LastName,
            Email = info.Email,
            PasswordHash = hash,
            PasswordSalt = salt,
            Groups = Array.Empty<UserGroup>()
        };
        return await SaveUser(user);
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
            user.UserGroupClaim()
        };

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

    private Task<User> GetUserInformation(string username) => _repo.GetUserByUsername(username);

    // TODO
    private Task<User> GetUserInformation(Guid userId)
    {
        return _repo.GetUserById(userId);
        /*
        // TODO
        return new User
        {
            Id = userId,
            UserName = "Test",
            Email = "a@gmail.com",
            PasswordHash = "Test"u8.ToArray(),
            PasswordSalt = "Salt"u8.ToArray(),
            Groups = new List<UserGroup>()
            {
                new UserGroup
                {
                    Name = "TestUg",
                    Owners = new List<Shared.User>()
                    {
                        new Shared.User()
                        {
                            Id = Guid.NewGuid(),
                            Name = "UgUser"
                        }
                    },
                    Policy = AuthorizationMode.Public
                }
            }
        };
    */
    }
}