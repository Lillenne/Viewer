using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Viewer.Shared.Users;

namespace Viewer.Server.Models;

public class Upload
{
    [Key] public required Guid UploadId { get; init; }
    [ForeignKey(nameof(Owner))] public required Guid OwnerId { get; init; }
    public User? Owner { get; set; }
    public required string Name { get; set; }
    public required string? Prefix { get; set; }
    public required Visibility Visibility { get; set; }
}