using Whirl1.Services;

using Microsoft.AspNetCore.Authorization;

namespace Whirl1.Security;

public class DeletePostHandler : AuthorizationHandler<DeletePostRequirement, int>
{
    private readonly IAuthService _authService;

    public DeletePostHandler(IAuthService authService)
    {
        _authService = authService;
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, DeletePostRequirement requirement, int postId)
    {
        if (await _authService.CanModeratePost(context.User, postId))
        {
            context.Succeed(requirement);
        }
    }
}