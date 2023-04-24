using Forum.Data;
using Forum.Entities;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication;

namespace Forum.Controllers;

[Authorize(Policy = "IsManager")]
public class AdminController : Controller
{
    private readonly DataContext _context;
    private readonly IAuthorizationService _authorizationService;

    public AdminController(DataContext context, IAuthorizationService authorizationService)
    {
        _context = context;
        _authorizationService = authorizationService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        Claim? currentUserIdClaim = User.Claims.Where(c => c.Type == "UserId").FirstOrDefault();

        if (currentUserIdClaim == null)
        {
            return new ForbidResult();
        }

        int currentUserId = Int32.Parse(currentUserIdClaim.Value);

        ApplicationUser currentUser = await _context.ApplicationUsers.Where(u => u.Id == currentUserId && u.Role != "User" && u.Role != "Banned").FirstOrDefaultAsync();

        if (currentUser == null)
        {
            return new ForbidResult();
        }

        return View(currentUser);
    }

    [HttpGet]
    public async Task<IActionResult> UserManagement(int? page)
    {
        List<ApplicationUser> users = new List<ApplicationUser>();

        int pageIndex = page - 1 ?? 0;
        int totalItems = 0;

        Claim? currentUserRoleClaim = User.Claims.Where(c => c.Type == "Role").FirstOrDefault();

        if (currentUserRoleClaim == null)
        {
            return new ForbidResult();
        }

        string currentUserRole = currentUserRoleClaim.Value;

        if (currentUserRole == "Root")
        {
            users = await _context.ApplicationUsers
            .Skip(2 * pageIndex)
            .Take(2)
            .ToListAsync();

            totalItems = await _context.ApplicationUsers.CountAsync();
        }
        else if (currentUserRole == "Admin")
        {
            users = await _context.ApplicationUsers.Where(u => u.Role != "Root" && u.Role != "Admin")
            .Skip(2 * pageIndex)
            .Take(2)
            .ToListAsync();

            totalItems = await _context.ApplicationUsers.Where(u => u.Role != "Root" && u.Role != "Admin").CountAsync();
        }
        else if (currentUserRole == "Moderator")
        {
            users = await _context.ApplicationUsers.Where(u => u.Role != "Root" && u.Role != "Admin" && u.Role != "Moderator")
            .Skip(2 * pageIndex)
            .Take(2)
            .ToListAsync();

            totalItems = await _context.ApplicationUsers.Where(u => u.Role != "Root" && u.Role != "Admin" && u.Role != "Moderator").CountAsync();
        }

        ViewBag.totalItems = totalItems;
        ViewBag.itemsPerPage = 2;
        ViewBag.currentPage = pageIndex + 1;
        ViewBag.totalPages = int.Parse(Math.Ceiling(((decimal)totalItems / 2)).ToString());
        ViewBag.previous = pageIndex;
        ViewBag.next = pageIndex + 2;

        return View(users);
    }

    [HttpGet]
    public async Task<IActionResult> EditUser(int id)
    {
        var authorizationResult = await _authorizationService.AuthorizeAsync(User, id, "IsHigherAuthority");

        if (!authorizationResult.Succeeded)
        {
            return new ForbidResult();
        }

        ApplicationUser userToEdit = await _context.ApplicationUsers.FindAsync(id);

        Claim? currentUserRoleClaim = User.Claims.Where(c => c.Type == "Role").FirstOrDefault();

        if (currentUserRoleClaim == null)
        {
            return new ForbidResult();
        }

        string currentUserRole = currentUserRoleClaim.Value;

        List<string> roles;

        if (currentUserRole == "Root")
        {
            roles = new List<string> { "Root", "Admin", "Moderator", "User", "Banned" };
        }
        else if (currentUserRole == "Admin")
        {
            roles = new List<string> { "Moderator", "User", "Banned" };
        }
        else if (currentUserRole == "Moderator")
        {
            roles = new List<string>();
        }
        else
        {
            return new ForbidResult();
        }

        ViewBag.editRoles = roles;
        ViewBag.categories = await _context.Categories.Where(c => c.IsActive).ToListAsync();
        ViewBag.selectedModeratedCategories = await _context.ModeratorLinks.Where(m => m.ApplicationUserId == userToEdit.Id).ToListAsync();
        ViewBag.selectedBannedCategories = await _context.BannedLinks.Where(m => m.ApplicationUserId == userToEdit.Id).ToListAsync();

        return View(userToEdit);
    }

    [HttpPost]
    public async Task<IActionResult> EditUserRole(int userId, string role, string[] moderation_categories)
    {
        // TODO: RootOrAdmin takes into account ShowModControls in policy in program.cs
        // Maybe change IsHigherAuthority so that it must be only root or admin. Then remove IsRootOrAdmin result.
        // Also, it doesn't look too good having more than one authorizationResult.
        var authorizationResult = await _authorizationService.AuthorizeAsync(User, userId, "IsRootOrAdmin");
        var authorizationResult2 = await _authorizationService.AuthorizeAsync(User, userId, "IsHigherAuthority");

        if (!authorizationResult.Succeeded || !authorizationResult2.Succeeded)
        {
            return new ForbidResult();
        }

        ApplicationUser editUser = await _context.ApplicationUsers.FindAsync(userId);

        DateTime currentTime = DateTime.UtcNow;
        long unixTime = ((DateTimeOffset)currentTime).ToUnixTimeSeconds();

        foreach (Category category in await _context.Categories.Where(c => c.IsActive).ToListAsync())
        {
            ModeratorLink moderatorLink = await _context.ModeratorLinks.Where(m => m.ApplicationUserId == userId && m.CategoryId == category.Id).FirstOrDefaultAsync();

            if (moderatorLink == null)
            {
                bool isCategoryChecked = false;

                foreach (string cat in moderation_categories)
                {
                    if (Int32.Parse(cat.Substring(20)) == category.Id)
                    {
                        isCategoryChecked = true;
                        break;
                    }
                }

                if (isCategoryChecked)
                {
                    ModeratorLink newModeratorLink = new ModeratorLink
                    {
                        ApplicationUserId = userId,
                        CategoryId = category.Id
                    };

                    await _context.ModeratorLinks.AddAsync(newModeratorLink);
                    await _context.SaveChangesAsync();
                }  
            }
            else
            {
                bool isCategoryChecked = false;

                foreach (string cat in moderation_categories)
                {
                    if (Int32.Parse(cat.Substring(20)) == category.Id)
                    {
                        isCategoryChecked = true;
                        break;
                    }
                }

                if (!isCategoryChecked)
                {
                    _context.Remove(moderatorLink);
                    await _context.SaveChangesAsync();
                }
            }
        }

        editUser.Role = role;
        editUser.LastChanged = unixTime;

        await _context.SaveChangesAsync();

        return RedirectToAction("Index", "Forum");
    }

    [HttpPost]
    public async Task<IActionResult> EditUserBanned(int userId, string[] banned_categories)
    {
        var authorizationResult = await _authorizationService.AuthorizeAsync(User, userId, "IsHigherAuthority");

        if (!authorizationResult.Succeeded)
        {
            return new ForbidResult();
        }

        ApplicationUser editUser = await _context.ApplicationUsers.FindAsync(userId);

        DateTime currentTime = DateTime.UtcNow;
        long unixTime = ((DateTimeOffset)currentTime).ToUnixTimeSeconds();

        foreach (Category category in await _context.Categories.Where(c => c.IsActive).ToListAsync())
        {
            BannedLink bannedLink = await _context.BannedLinks.Where(m => m.ApplicationUserId == userId && m.CategoryId == category.Id).FirstOrDefaultAsync();

            if (bannedLink == null)
            {
                bool isCategoryChecked = false;

                foreach (string cat in banned_categories)
                {
                    if (Int32.Parse(cat.Substring(16)) == category.Id)
                    {
                        isCategoryChecked = true;
                        break;
                    }
                }

                if (isCategoryChecked)
                {
                    BannedLink newBannedLink = new BannedLink
                    {
                        ApplicationUserId = userId,
                        CategoryId = category.Id
                    };

                    await _context.BannedLinks.AddAsync(newBannedLink);
                    await _context.SaveChangesAsync();
                }  
            }
            else
            {
                bool isCategoryChecked = false;

                foreach (string cat in banned_categories)
                {
                    if (Int32.Parse(cat.Substring(16)) == category.Id)
                    {
                        isCategoryChecked = true;
                        break;
                    }
                }

                if (!isCategoryChecked)
                {
                    _context.Remove(bannedLink);
                    await _context.SaveChangesAsync();
                }
            }
        }

        editUser.LastChanged = unixTime;
        await _context.SaveChangesAsync();

        return RedirectToAction("Index", "Forum");
    }

    [HttpPost]
    public async Task<IActionResult> EditShowModControls(string modControls)
    {
        Claim? currentUserIdClaim = User.Claims.Where(c => c.Type == "UserId").FirstOrDefault();

        if (currentUserIdClaim == null)
        {
            return new ForbidResult();
        }

        int currentUserId = Int32.Parse(currentUserIdClaim.Value);

        ApplicationUser currentUser = await _context.ApplicationUsers.Where(u => u.Id == currentUserId && u.Role != "User" && u.Role != "Banned").FirstOrDefaultAsync();

        if (currentUser == null)
        {
            return new ForbidResult();
        }

        bool updatedModControls = false;

        if (modControls == "yes")
        {
            updatedModControls = true;
        }

        currentUser.ShowModControls = updatedModControls;
        await _context.SaveChangesAsync();

        // Refreshing User so they don't have to log out and log back in to use mod controls

        DateTime currentTime = DateTime.UtcNow;
        long unixTime = ((DateTimeOffset)currentTime).ToUnixTimeSeconds();

        var claims = new List<Claim>
        {
            new Claim("UserId", currentUser.Id.ToString()),
            new Claim("Username", currentUser.Username),
            new Claim("Role", currentUser.Role),
            new Claim("ShowModControls", currentUser.ShowModControls.ToString()),
            new Claim("LastChanged", unixTime.ToString())
        };

        var claimsIdentitiy = new ClaimsIdentity(claims, "WhirlAuth");
        var authProperties = new AuthenticationProperties { };
        var claimsPrincipal = new ClaimsPrincipal(claimsIdentitiy);
        
        await HttpContext.SignInAsync("WhirlAuth", claimsPrincipal, authProperties);

        return RedirectToAction("Index", "Admin");
    }

}