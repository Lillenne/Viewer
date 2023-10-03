using Viewer.Shared.Users;

namespace Viewer.Server.Models;

public record UserInfo
{
    public required Guid Id { get; set; }
    public required string UserName { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? PhoneNumber { get; set; }
    public required string Email { get; set; }
    public ICollection<String> Roles { get; set; } = new List<String>();
    
    public static implicit operator UserDto(UserInfo u)
    {
        return new UserDto
        {
            Id = u.Id,
            UserName = u.UserName,
            FirstName = u.FirstName,
            Roles = u.Roles.ToList()
        };
    }
    
    public static implicit operator UserInfo(User user)
    {
        return new UserInfo
        {
            Id = user.Id,
            UserName = user.UserName,
            FirstName = user.FirstName,
            LastName = user.LastName,
            PhoneNumber = user.PhoneNumber,
            Email = user.Email,
            Roles = user.Roles.Select(r => r.RoleName).ToList()
        };
    }

    /*
    public static implicit operator User(UserInfo user)
    {
        return new User
        {
            Id = user.Id,
            UserName = user.UserName,
            FirstName = user.FirstName,
            LastName = user.LastName,
            PhoneNumber = user.PhoneNumber,
            Email = user.Email,
            Roles = user.Roles.Select(r => new Role(){ RoleName = r}).ToList()
        };
    }
*/
}