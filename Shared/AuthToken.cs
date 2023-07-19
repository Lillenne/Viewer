using System.Diagnostics.CodeAnalysis;

namespace Viewer.Shared;

public readonly record struct AuthToken
{
    public required string Token { get; init; }

    public AuthToken() { }

    [SetsRequiredMembers]
    public AuthToken(string token)
    {
        Token = token;
    }
}
