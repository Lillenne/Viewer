namespace Viewer.Shared.Users;

public enum Visibility
{
    /// <summary>
    /// Visible to only the user
    /// </summary>
    Hidden = 0, 
    /// <summary>
    /// Visible to the user and the user's teams
    /// </summary>
    Private,
    /// <summary>
    /// Visible to everyone
    /// </summary>
    Public,
}