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

    public async Task<bool> CanEditComment(ClaimsPrincipal user, int commentId)
    {
        return await IsUsersComment(user, await _context.Comments.FindAsync(commentId));
    }

    public async Task<bool> CanDeleteComment(ClaimsPrincipal user, int commentId)
    {
        Comment comment = await _context.Comments.FindAsync(commentId);
        if (comment == null) return false;

        Post post = await _context.Posts.FindAsync(comment.PostId);
        if (post == null) return false;
        if (post.FirstCommentId == commentId) return false;

        if (await IsUsersComment(user, comment)) return true;

        ApplicationUser currentUser = await GetApplicationUserFromPrincipal(user);
        if (currentUser == null) return false;

        ApplicationUser commentUser = await _context.ApplicationUsers.FindAsync(comment.ApplicationUserId);
        if (commentUser == null) return false;

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

    public async Task<bool> CanEditUserRole(ClaimsPrincipal user, int userId)
    {
        ApplicationUser currentUser = await GetApplicationUserFromPrincipal(user);

        if (currentUser == null) return false;
        if (currentUser.Role != "Root" && currentUser.Role != "Admin") return false;

        ApplicationUser editUser = await _context.ApplicationUsers.FindAsync(userId);

        if (editUser == null) return false;

        return await CanModerate(currentUser, editUser);
    }

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

    private async Task<ApplicationUser> GetApplicationUserFromPrincipal(ClaimsPrincipal user)
    {
        Claim currentUserIdClaim = user.Claims.Where(c => c.Type == "UserId").FirstOrDefault();

        if (currentUserIdClaim == null) return null;

        int currentUserId = Int32.Parse(currentUserIdClaim.Value);
        return await _context.ApplicationUsers.Where(u => u.Id == currentUserId).FirstOrDefaultAsync();
    }

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