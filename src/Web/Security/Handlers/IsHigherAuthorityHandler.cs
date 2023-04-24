using Forum.Services;

using Microsoft.AspNetCore.Authorization;

namespace Forum.Security;

public class IsHigherAuthorityHandler : AuthorizationHandler<IsHigherAuthorityRequirement, int>
{
    private readonly IAuthService _authService;

    public IsHigherAuthorityHandler(IAuthService authService)
    {
        _authService = authService;
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, IsHigherAuthorityRequirement requirement, int userToEditId)
    {
        if (await _authService.IsHigherAuthority(context.User, userToEditId))
        {
            context.Succeed(requirement);
        }
    }
}