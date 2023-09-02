using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Viewer.Shared.Users;

namespace Viewer.Server.Models;

public class Upload
{
    /// <summary>
    /// The ID for this upload
    /// </summary>
    [Key,DatabaseGenerated(DatabaseGeneratedOption.None)]
    public required Guid UploadId { get; init; }
    
    /// <summary>
    /// The ID of the owner of the image
    /// </summary>
    [ForeignKey(nameof(Owner))] public required Guid OwnerId { get; init; }
    
    /// <summary>
    /// The owner of the image
    /// </summary>
    public virtual User? Owner { get; set; }
    
    /// <summary>
    /// The original upload file name
    /// </summary>
    public required string OriginalFileName { get; set; }
    
    /// <summary>
    /// The relative directory of the upload
    /// </summary>
    public string? DirectoryPrefix { get; set; }
    
    /// <summary>
    /// The default access policy for the upload
    /// </summary>
    public required Visibility Visibility { get; set; }

    public string StoredName() => DirectoryPrefix is null
        ? UploadId.ToString()
        : Path.Combine(DirectoryPrefix, UploadId.ToString());
}