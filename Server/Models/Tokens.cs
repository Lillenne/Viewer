using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Viewer.Server.Models;

public class Tokens
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
    public required Guid UserId { get; init; }
    public required string RefreshToken { get; init; }
    public DateTime RefreshTokenExpiry { get; init; }
}