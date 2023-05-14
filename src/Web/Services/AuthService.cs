using Forum.Data;
using Forum.Entities;

using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Forum.Services;

public interface IAuthService
{
    Task<bool> CanEditComment(ClaimsPrincipal user, int commentId);
    Task<bool> CanDeleteComment(ClaimsPrincipal user, int commentId);
    Task<bool> CanDeletePost(ClaimsPrincipal user, int postId);
    Task<bool> CanEditCategoriesAndTopics(ClaimsPrincipal user);
    Task<bool> CanEditUserRole(ClaimsPrincipal user, int userId);
    Task<bool> IsHigherAuthority(ClaimsPrincipal user, int userId);
    Task<bool> IsValidUser(ClaimsPrincipal user, int categoryId);
    Task<bool> IsManager(ClaimsPrincipal user);
}

// This class is used by the auth handlers, as there is a lot of repeat code across all the handlers.
// The functions return a bool value to let the handler know if auth is successful or not
public class AuthService : IAuthService
{
    private readonly DataContext _context;

    public AuthService(DataContext context)
    {
        _context = context;
    }

    private async Task<bool> IsUsersComment(ClaimsPrincipal user, Comment comment)
    {
        if (comment == null) return false;
        if (!comment.IsActive) return false;

        ApplicationUser currentUser = await GetApplicationUserFromPrincipal(user);

        if (currentUser == null) return false;
        
        return comment.ApplicationUserId == currentUser.Id;
    }

    // Only the user of the comment can edit it
    public async Task<bool> CanEditComment(ClaimsPrincipal user, int commentId)
    {
        return await IsUsersComment(user, await _context.Comments.FindAsync(commentId));
    }

    // The user of the comment and anyone with a higher authority can delete a comment
    public async Task<bool> CanDeleteComment(ClaimsPrincipal user, int commentId)
    {
        Comment comment = await _context.Comments.FindAsync(commentId);
        if (comment == null) return false;

        Post post = await _context.Posts.FindAsync(comment.PostId);
        if (post == null) return false;
        // Cannot delete the first comment in a post. This is to avoid empty posts as well as the idea of the first comment in a post being like an extended title of sorts
        if (post.FirstCommentId == commentId) return false;

        // If it is the users comment, no need to do any further checks
        if (await IsUsersComment(user, comment)) return true;

        ApplicationUser currentUser = await GetApplicationUserFromPrincipal(user);
        if (currentUser == null) return false;

        ApplicationUser commentUser = await _context.ApplicationUsers.FindAsync(comment.ApplicationUserId);
        if (commentUser == null) return false;

        // Check if higher authority as well as mod controls are turned on
        return await CanModerate(currentUser, commentUser) && currentUser.ShowModControls;
    }

    public async Task<bool> CanDeletePost(ClaimsPrincipal user, int postId)
    {
        ApplicationUser currentUser = await GetApplicationUserFromPrincipal(user);

        if (currentUser == null) return false;

        Post editPost = await _context.Posts.FindAsync(postId);

        if (editPost == null) return false;

        ApplicationUser editPostUser = await _context.ApplicationUsers.FindAsync(editPost.ApplicationUserId);

        if (editPostUser == null) return false;

        return await CanModerate(currentUser, editPostUser);
    }

    // Only Root/Admin users with mod controls on can add categories or topics

    public async Task<bool> CanEditCategoriesAndTopics(ClaimsPrincipal user)
    {
        ApplicationUser currentUser = await GetApplicationUserFromPrincipal(user);

        if (currentUser == null)
        {
            return false;
        }
        else
        {
            return (currentUser.Role == "Root" || currentUser.Role == "Admin") && currentUser.ShowModControls;
        }
    }

    // To edit a user role, the user must be root or admin and have a higher authority than the user they are editing
    public async Task<bool> CanEditUserRole(ClaimsPrincipal user, int userId)
    {
        ApplicationUser currentUser = await GetApplicationUserFromPrincipal(user);

        if (currentUser == null) return false;
        if (currentUser.Role != "Root" && currentUser.Role != "Admin") return false;

        ApplicationUser editUser = await _context.ApplicationUsers.FindAsync(userId);

        if (editUser == null) return false;

        return await CanModerate(currentUser, editUser);
    }

    // Higher authority means you are at least one tier higher than the user you are editing. Unless you are a root user in which case you can edit everything.
    // Here, we are simply transforming the claims princips and userId into ApplicationUser objects so we can perform the CanModerate check with the correct parameters.
    public async Task<bool> IsHigherAuthority(ClaimsPrincipal user, int userId)
    {
        ApplicationUser currentUser = await GetApplicationUserFromPrincipal(user);

        if (currentUser == null) return false;

        ApplicationUser editUser = await _context.ApplicationUsers.FindAsync(userId);

        if (editUser == null) return false;

        return await CanModerate(currentUser, editUser);
    }

    public async Task<bool> IsValidUser(ClaimsPrincipal user, int categoryId)
    {
        ApplicationUser currentUser = await GetApplicationUserFromPrincipal(user);

        if (currentUser == null) return false;
        if (currentUser.Role == "Banned") return false;

        BannedLink bannedLink = await _context.BannedLinks.Where(b => b.ApplicationUserId == currentUser.Id && b.CategoryId == categoryId).FirstOrDefaultAsync();

        return bannedLink == null;
    }

    public async Task<bool> IsManager(ClaimsPrincipal user)
    {
        ApplicationUser currentUser = await GetApplicationUserFromPrincipal(user);

        if (currentUser == null)
        {
            return false;
        }
        else
        {
            return currentUser.Role == "Root" || currentUser.Role == "Admin" || currentUser.Role == "Moderator";
        }
    }

    // This simply takes the claims principal and turns it into an ApplicationUser object
    private async Task<ApplicationUser> GetApplicationUserFromPrincipal(ClaimsPrincipal user)
    {
        Claim currentUserIdClaim = user.Claims.Where(c => c.Type == "UserId").FirstOrDefault();

        if (currentUserIdClaim == null) return null;

        int currentUserId = Int32.Parse(currentUserIdClaim.Value);
        return await _context.ApplicationUsers.Where(u => u.Id == currentUserId).FirstOrDefaultAsync();
    }

    // Users can only moderate if they are least one tier higher than the user they are editing. Unless they are a root user in which case they can edit everything
    private async Task<bool> CanModerate(ApplicationUser currentUser, ApplicationUser editUser)
    {
        if (currentUser.Role == "Root")
        {
            return true;
        }
        else if (currentUser.Role == "Admin")
        {
            return editUser.Role != "Root" && editUser.Role != "Admin";
        } else if (currentUser.Role == "Moderator")
        {
            return editUser.Role != "Root" && editUser.Role != "Admin" && editUser.Role != "Moderator";
        }
        else
        {
            return false;
        }
    }
}