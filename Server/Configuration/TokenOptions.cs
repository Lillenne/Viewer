namespace Viewer.Server.Configuration;

public class TokenOptions
{
    public TimeSpan LifeSpan { get; set; } = TimeSpan.FromDays(7);
}