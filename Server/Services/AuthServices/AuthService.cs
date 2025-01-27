using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Viewer.Server.Configuration;
using Viewer.Server.Models;
using Viewer.Shared;
using Viewer.Shared.Users;

namespace Viewer.Server.Services.AuthServices;

public class AuthService : IAuthService
{
    private readonly ITokenService _tokenService;
    private readonly IUserRepository _userRepo;
    private readonly ITokenRepository _tokenRepository;
    private readonly IClaimsParser _parser;
    private readonly TokenOptions _tokenOptions;

    public AuthService(ITokenService tokenService, IClaimsParser parser, IUserRepository userRepo, ITokenRepository tokenRepository, IOptions<TokenOptions> options)
    {
        _tokenService = tokenService;
        _parser = parser;
        _userRepo = userRepo;
        _tokenRepository = tokenRepository;
        _tokenOptions = options.Value;
    }

    public async Task<string> Refresh(string oldToken, string refreshToken) // TODO people to update table
    {
        var principal = _tokenService.GetClaims(oldToken);
        var usr = _parser.ParseClaims(principal);
        var info = await _tokenRepository.GetTokenInfoAsync(usr.Id).ConfigureAwait(false);
        if (info is null)
            throw new InvalidOperationException("Refresh token unavailable");
        if (info.RefreshTokenExpiry < DateTime.Now)
            throw new InvalidOperationException("Refresh token expired");
        if (info.RefreshToken != refreshToken)
            throw new ArgumentException("Refresh token mismatch");
        var updated = await _userRepo.GetUser(usr.Id).ConfigureAwait(false);
        var id = new ClaimsIdentity(_parser.ToClaims(updated));
        return _tokenService.CreateToken(id);
    }

    public async Task<AuthToken> Login(UserLogin userLogin)
    {
        if (string.IsNullOrEmpty(userLogin.Email))
            throw new ArgumentException("Username required");
        if (string.IsNullOrEmpty(userLogin.Password))
            throw new ArgumentException("Password required");
        var user = await _userRepo.GetUser(userLogin.Email).ConfigureAwait(false);
        if (!VerifyPassword(userLogin.Password, user.Password.Hash, user.Password.Salt))
            throw new ArgumentException("Invalid password");
        var claims = _parser.ToClaims(user);
        var token = _tokenService.CreateToken(new ClaimsIdentity(claims));
        var refresh = await CreateRefreshToken(user.Id).ConfigureAwait(false);
        return new AuthToken(token, refresh);
    }

    public async Task ChangePassword(ChangePasswordRequest request)
    {
        var user = await _userRepo.GetPassword(request.UserId).ConfigureAwait(false);
        if (!VerifyPassword(request.OldPassword, user.Hash, user.Salt))
            throw new ArgumentException("Invalid password");
        var (hash, salt) = CreatePasswordHash(request.NewPassword);
        await _userRepo.SetPassword(request.UserId, new UserPassword
        {
            Hash = hash,
            Salt = salt
        }).ConfigureAwait(false);
    }

    public async Task<AuthToken> Register(UserRegistration info)
    {
        var (hash, salt) = CreatePasswordHash(info.Password);
        var user = new User
        {
            UserName = info.Username,
            FirstName = info.FirstName,
            LastName = info.LastName,
            Email = info.Email,
            Password = new UserPassword(hash, salt),
            Id = Guid.NewGuid(),
        };
        await _userRepo.AddUser(user).ConfigureAwait(false);
        var claims = _parser.ToClaims(user);
        var token = _tokenService.CreateToken(new ClaimsIdentity(claims));
        var refresh = await CreateRefreshToken(user.Id).ConfigureAwait(false);
        return new AuthToken(token, refresh);
    }

    public Task<UserDto?> WhoAmI(ClaimsPrincipal principal) => Task.FromResult<UserDto?>(_parser.ParseClaims(principal));

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
    
    private async Task<string> CreateRefreshToken(Guid userId)
    {
        var refresh = _tokenService.CreateRefreshToken();
        await _tokenRepository.UpdateTokenInfoAsync(new Tokens
        {
            UserId = userId,
            RefreshToken = refresh,
            RefreshTokenExpiry = DateTime.UtcNow + _tokenOptions.LifeSpan
        });
        return refresh;
    }

}
