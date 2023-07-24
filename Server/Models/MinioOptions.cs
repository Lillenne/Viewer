#nullable disable
namespace Viewer.Server.Models;

public class MinioOptions
{
    public string AccessKey { get; set; }
    public string SecretKey { get; set; }
    public string Endpoint { get; set; }
    public int Port { get; set; }
    public string ImageBucket { get; set; }
    public string ThumbnailBucket { get; set; }
    public int[] ThumbnailWidths { get; set; }
    public bool UseHttps { get; set;}
}
