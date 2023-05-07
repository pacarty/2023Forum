using Forum.Services;

using Microsoft.AspNetCore.Authorization;

namespace Forum.Security;

public class EditDeleteCategoryTopicHandler : AuthorizationHandler<EditDeleteCategoryTopicRequirement>
{
    private readonly IAuthService _authService;

    public EditDeleteCategoryTopicHandler(IAuthService authService)
    {
        _authService = authService;
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, EditDeleteCategoryTopicRequirement requirement)
    {
        if (await _authService.CanEditCategoriesAndTopics(context.User))
        {
            context.Succeed(requirement);
        }
    }
}