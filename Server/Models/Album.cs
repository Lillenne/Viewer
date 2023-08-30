using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Viewer.Server.Models;

public record Album
{
    /// <summary>
    /// The album's unique identifier
    /// </summary>
    [Key, DatabaseGenerated(DatabaseGeneratedOption.None)] 
    public required Guid Id { get; init; }
    
    public required string Name { get; init; }
    
    public virtual ICollection<Upload> Uploads { get; init; } = new List<Upload>();
}