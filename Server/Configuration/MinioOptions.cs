#nullable disable
namespace Viewer.Server.Configuration;

public class MinioOptions
{
    public string AccessKey { get; set; }
    public string SecretKey { get; set; }
    public string Endpoint { get; set; }
    public int Port { get; set; }
    public string Bucket { get; set; }
    public int[] ThumbnailWidths { get; set; }
    public bool UseHttps { get; set;}
    public int DefaultLinkExpiryTimeSeconds { get; set; } = 60 * 60 * 24;
}
