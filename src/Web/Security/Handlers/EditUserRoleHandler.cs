using Forum.Services;

using Microsoft.AspNetCore.Authorization;

namespace Forum.Security;

public class EditUserRoleHandler : AuthorizationHandler<EditUserRoleRequirement, int>
{
    private readonly IAuthService _authService;

    public EditUserRoleHandler(IAuthService authService)
    {
        _authService = authService;
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, EditUserRoleRequirement requirement, int userToEditId)
    {
        if (await _authService.CanEditUserRole(context.User, userToEditId))
        {
            context.Succeed(requirement);
        }
    }
}