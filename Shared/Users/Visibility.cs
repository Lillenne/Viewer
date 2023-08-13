namespace Viewer.Shared.Users;

public enum Visibility
{
    Hidden = 0, // Visible to only the user
    Private, // Visible to the user and the user's teams
    Public, // Visible to everyone
}