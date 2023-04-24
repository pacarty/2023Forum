using Whirl1.Services;

using Microsoft.AspNetCore.Authorization;

namespace Whirl1.Security;

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