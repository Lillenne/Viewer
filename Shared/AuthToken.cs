using System.Diagnostics.CodeAnalysis;

namespace Viewer.Shared;

public readonly record struct AuthToken
{
    public required string Token { get; init; }
    public required string? RefreshToken { get; init; }

    public AuthToken() { }

    [SetsRequiredMembers]
    public AuthToken(string token, string? refreshToken)
    {
        Token = token;
        RefreshToken = refreshToken;
    }
}
