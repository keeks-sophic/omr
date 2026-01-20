using BackendV3.Modules.Auth.Dto;
using BackendV3.Modules.Auth.Model;

namespace BackendV3.Modules.Auth.Mapping;

public static class UserMapper
{
    public static UserDto ToDto(User user, string[] roles)
    {
        return new UserDto
        {
            UserId = user.UserId,
            Username = user.Username,
            DisplayName = user.DisplayName,
            Roles = roles,
            IsDisabled = user.IsDisabled
        };
    }
}
