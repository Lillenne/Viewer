using System.ComponentModel.DataAnnotations;

namespace Viewer.Server.Models;

public record Album
{
    /// <summary>
    /// The album's unique identifier
    /// </summary>
    [Key] public required Guid Id { get; init; }
    
    public ICollection<Upload> Uploads { get; init; } = new List<Upload>();
}