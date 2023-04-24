using Whirl1.Services;

using Microsoft.AspNetCore.Authorization;

namespace Whirl1.Security;

public class DeleteCommentHandler : AuthorizationHandler<DeleteCommentRequirement, int>
{
    private readonly IAuthService _authService;

    public DeleteCommentHandler(IAuthService authService)
    {
        _authService = authService;
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, DeleteCommentRequirement requirement, int commentId)
    {
        if (await _authService.CanDeleteComment(context.User, commentId) || await _authService.CanModerateComment(context.User, commentId))
        {
            context.Succeed(requirement);
        }
    }
}