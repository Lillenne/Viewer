using System.Collections.Immutable;
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
    private JwtOptions Jwt;

    public JwtAuthService(IOptions<JwtOptions> options) // TODO hook up DB
    {
        Jwt = options.Value;
    }

    private string CreateToken(User user)
    {
        var handler = new JwtSecurityTokenHandler();
        
        var time = DateTime.UtcNow;
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Name, user.Name),
            new(JwtRegisteredClaimNames.NameId, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.AuthTime, time.ToShortTimeString()),
            new(Claims.UserGroupClaimName, Claims.ToUserGroupClaim(user.Groups)),
        };

        var descriptor = new SecurityTokenDescriptor()
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(Jwt.ExpiryTimeMinutes), // TODO
            Issuer = Jwt.Issuer,
            Audience = Jwt.Audience,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(Jwt.KeyBytesUtf8), Jwt.HashAlgorithm) //TODO
        };

        var token = handler.CreateToken(descriptor);
        return handler.WriteToken(token);
    }

    public async Task<Result<AuthToken>> Login(UserLogin userLogin)
    {
        if (string.IsNullOrEmpty(userLogin.Username))
            return new Result<AuthToken>(new ArgumentException("Invalid username"));
        if (string.IsNullOrEmpty(userLogin.Password))
            return new Result<AuthToken>(new ArgumentException("Invalid password"));
        
        var user = await GetUserInformation();
        if (!VerifyPassword(userLogin.Password, user.PasswordHash, user.PasswordSalt))
        {
            return new Result<AuthToken>(new ArgumentException("Incorrect password"));
        }
        var token = CreateToken(user);
        return new Result<AuthToken>(new AuthToken(token));
    }

    private void CreatePasswordHash(string pwd, out byte[] hash, out byte[] salt)
    {
        using (var hmac = new HMACSHA256())
        {
            salt = hmac.Key;
            hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(pwd));
        }
    }

    private bool VerifyPassword(string pwd, byte[] hash, byte[] salt)
    {
        using (var hmac = new HMACSHA256(salt))
        {
            var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(pwd));
            return computedHash.SequenceEqual(hash);
        }
    }

    private async Task<User> GetUserInformation()
    {
        // TODO
        return new User
        {
            Id = Guid.NewGuid(),
            Name = "Test",
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
    }

    public Task<Result<bool>> ChangePassword(ChangePasswordRequest request)
    {
        throw new NotImplementedException();
    }

    public Task<Result<bool>> Register(UserRegistration info)
    {
        throw new NotImplementedException();
    }
}