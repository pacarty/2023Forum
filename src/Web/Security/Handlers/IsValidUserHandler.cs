using Whirl1.Services;

using Microsoft.AspNetCore.Authorization;

namespace Whirl1.Security;

public class IsValidUserHandler : AuthorizationHandler<IsValidUserRequirement, int>
{
    private readonly IAuthService _authService;

    public IsValidUserHandler(IAuthService authService)
    {
        _authService = authService;
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, IsValidUserRequirement requirement, int categoryId)
    {
        if (await _authService.IsValidUser(context.User, categoryId))
        {
            context.Succeed(requirement);
        }
    }
}