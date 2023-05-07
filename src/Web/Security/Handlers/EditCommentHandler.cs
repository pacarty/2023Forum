using Forum.Services;

using Microsoft.AspNetCore.Authorization;

namespace Forum.Security;

public class EditCommentHandler : AuthorizationHandler<EditCommentRequirement, int>
{
    private readonly IAuthService _authService;

    public EditCommentHandler(IAuthService authService)
    {
        _authService = authService;
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, EditCommentRequirement requirement, int commentId)
    {
        if (await _authService.CanEditComment(context.User, commentId))
        {
            context.Succeed(requirement);
        }
    }
}