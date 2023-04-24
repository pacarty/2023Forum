using Forum.Data;
using Forum.Entities;

using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Forum.Services;

public interface IAuthService
{
    int DoThing();
    Task<bool> CanEditComment(ClaimsPrincipal user, int commentId);
    Task<bool> CanDeleteComment(ClaimsPrincipal user, int commentId);
    Task<bool> CanModerateComment(ClaimsPrincipal user, int commentId);
    Task<bool> CanModeratePost(ClaimsPrincipal user, int postId);
    Task<bool> CanModerate(ApplicationUser currentUser, ApplicationUser editUser);
    Task<bool> IsHigherAuthority(ClaimsPrincipal user, int userId);
    Task<bool> IsValidUser(ClaimsPrincipal user, int categoryId);
}

public class AuthService : IAuthService
{
    private readonly DataContext _context;

    public AuthService(DataContext context)
    {
        _context = context;
    }

    public int DoThing()
    {
        return 4;
    }

    public async Task<bool> CanEditComment(ClaimsPrincipal user, int commentId)
    {
        Claim? currentUserIdClaim = user.Claims.Where(c => c.Type == "UserId").FirstOrDefault();

        if (currentUserIdClaim == null)
        {
            return false;
        }

        int currentUserId = Int32.Parse(currentUserIdClaim.Value);

        ApplicationUser currentUser = await _context.ApplicationUsers.Where(u => u.Id == currentUserId && u.Role != "Banned").FirstOrDefaultAsync();

        if (currentUser == null)
        {
            return false;
        }

        Comment editComment = await _context.Comments.FindAsync(commentId);

        if (editComment == null)
        {
            return false;
        }

        if (!editComment.IsActive)
        {
            return false;
        }

        ApplicationUser editCommentUser = await _context.ApplicationUsers.Where(u => u.Id == editComment.ApplicationUserId && u.Role != "Banned").FirstOrDefaultAsync();

        if (editCommentUser == null)
        {
            return false;
        }

        return editComment.ApplicationUserId == currentUserId;
    }

    public async Task<bool> CanDeleteComment(ClaimsPrincipal user, int commentId)
    {
        if (!await CanEditComment(user, commentId))
        {
            return false;
        }

        Comment editComment = await _context.Comments.FindAsync(commentId);
        Post editPost = await _context.Posts.FindAsync(editComment.PostId);

        return editPost.FirstCommentId != commentId;
    }

    public async Task<bool> CanModerateComment(ClaimsPrincipal user, int commentId)
    {
        Claim? currentUserIdClaim = user.Claims.Where(c => c.Type == "UserId").FirstOrDefault();

        if (currentUserIdClaim == null)
        {
            return false;
        }

        int currentUserId = Int32.Parse(currentUserIdClaim.Value);

        ApplicationUser currentUser = await _context.ApplicationUsers.Where(u => u.Id == currentUserId && u.Role != "Banned").FirstOrDefaultAsync();

        if (currentUser == null)
        {
            return false;
        }

        Comment editComment = await _context.Comments.FindAsync(commentId);

        if (editComment == null)
        {
            return false;
        }

        ApplicationUser editCommentUser = await _context.ApplicationUsers.FindAsync(editComment.ApplicationUserId);

        if (editCommentUser == null)
        {
            return false;
        }

        return await CanModerate(currentUser, editCommentUser);
    }

    public async Task<bool> CanModeratePost(ClaimsPrincipal user, int postId)
    {
        Claim? currentUserIdClaim = user.Claims.Where(c => c.Type == "UserId").FirstOrDefault();

        if (currentUserIdClaim == null)
        {
            return false;
        }

        int currentUserId = Int32.Parse(currentUserIdClaim.Value);

        ApplicationUser currentUser = await _context.ApplicationUsers.Where(u => u.Id == currentUserId && u.Role != "Banned").FirstOrDefaultAsync();

        if (currentUser == null)
        {
            return false;
        }

        Post editPost = await _context.Posts.FindAsync(postId);

        if (editPost == null)
        {
            return false;
        }

        ApplicationUser editPostUser = await _context.ApplicationUsers.FindAsync(editPost.ApplicationUserId);

        if (editPostUser == null)
        {
            return false;
        }

        return await CanModerate(currentUser, editPostUser);
    }

    public async Task<bool> CanModerate(ApplicationUser currentUser, ApplicationUser editUser)
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

    public async Task<bool> IsHigherAuthority(ClaimsPrincipal user, int userId)
    {
        Claim? currentUserIdClaim = user.Claims.Where(c => c.Type == "UserId").FirstOrDefault();

        if (currentUserIdClaim == null)
        {
            return false;
        }

        int currentUserId = Int32.Parse(currentUserIdClaim.Value);

        ApplicationUser currentUser = await _context.ApplicationUsers.Where(u => u.Id == currentUserId && u.Role != "Banned").FirstOrDefaultAsync();

        if (currentUser == null)
        {
            return false;
        }

        ApplicationUser editUser = await _context.ApplicationUsers.FindAsync(userId);

        if (editUser == null)
        {
            return false;
        }

        return await CanModerate(currentUser, editUser);
    }

    public async Task<bool> IsValidUser(ClaimsPrincipal user, int categoryId)
    {
        Claim? currentUserIdClaim = user.Claims.Where(c => c.Type == "UserId").FirstOrDefault();

        if (currentUserIdClaim == null)
        {
            return false;
        }

        int currentUserId = Int32.Parse(currentUserIdClaim.Value);

        ApplicationUser currentUser = await _context.ApplicationUsers.Where(u => u.Id == currentUserId && u.Role != "Banned").FirstOrDefaultAsync();

        if (currentUser == null)
        {
            return false;
        }

        BannedLink bannedLink = await _context.BannedLinks.Where(b => b.ApplicationUserId == currentUserId && b.CategoryId == categoryId).FirstOrDefaultAsync();

        return bannedLink == null;
    }
}