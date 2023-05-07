using Forum.Services;

using Microsoft.AspNetCore.Authorization;

namespace Forum.Security;

public class IsManagerHandler : AuthorizationHandler<IsManagerRequirement>
{
    private readonly IAuthService _authService;

    public IsManagerHandler(IAuthService authService)
    {
        _authService = authService;
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, IsManagerRequirement requirement)
    {
        if (await _authService.IsManager(context.User))
        {
            context.Succeed(requirement);
        }
    }
}