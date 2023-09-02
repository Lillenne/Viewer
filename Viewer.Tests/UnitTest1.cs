using Viewer.Server.Services.ImageServices;

namespace Viewer.Tests;

public class UnitTest1
{
    [Fact]
    public void ThumbnailParser_ThumbnailName_ParsesSize()
    {
        var randNum = (int)(Random.Shared.NextDouble() * int.MaxValue);
        var id = MinioImageClient.AppendThumbnailTag(Guid.NewGuid().ToString(), randNum);
        var parsed = MinioImageClient.ParseWidthFromThumbnailName(id);
        Assert.Equal(randNum, parsed);
    }
}