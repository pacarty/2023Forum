using Forum.Services;

using Microsoft.AspNetCore.Authorization;

namespace Forum.Security;

public class DeletePostHandler : AuthorizationHandler<DeletePostRequirement, int>
{
    private readonly IAuthService _authService;

    public DeletePostHandler(IAuthService authService)
    {
        _authService = authService;
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, DeletePostRequirement requirement, int postId)
    {
        if (await _authService.CanDeletePost(context.User, postId))
        {
            context.Succeed(requirement);
        }
    }
}