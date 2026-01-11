using BackendV2.Api.Dto.Auth;
using BackendV2.Api.Model.Auth;

namespace BackendV2.Api.Mapping.Auth;

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
