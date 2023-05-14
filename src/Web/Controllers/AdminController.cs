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

        // If no page is passed in, it is page 0
        int pageIndex = page - 1 ?? 0;
        int totalItems = 0;
        int itemsPerPage = 2;

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

        // The users that can be viewed depend on the current users role
        if (currentUser.Role == "Root")
        {
            users = await _context.ApplicationUsers
            .Skip(itemsPerPage * pageIndex)
            .Take(itemsPerPage)
            .ToListAsync();

            totalItems = await _context.ApplicationUsers.CountAsync();
        }
        else if (currentUser.Role == "Admin")
        {
            users = await _context.ApplicationUsers.Where(u => u.Role != "Root" && u.Role != "Admin")
            .Skip(itemsPerPage * pageIndex)
            .Take(itemsPerPage)
            .ToListAsync();

            totalItems = await _context.ApplicationUsers.Where(u => u.Role != "Root" && u.Role != "Admin").CountAsync();
        }
        else if (currentUser.Role == "Moderator")
        {
            users = await _context.ApplicationUsers.Where(u => u.Role != "Root" && u.Role != "Admin" && u.Role != "Moderator")
            .Skip(itemsPerPage * pageIndex)
            .Take(itemsPerPage)
            .ToListAsync();

            totalItems = await _context.ApplicationUsers.Where(u => u.Role != "Root" && u.Role != "Admin" && u.Role != "Moderator").CountAsync();
        }

        // Pagination variables, stored in ViewBag
        ViewBag.totalItems = totalItems;
        ViewBag.itemsPerPage = itemsPerPage;
        ViewBag.currentPage = pageIndex + 1;
        ViewBag.totalPages = int.Parse(Math.Ceiling(((decimal)totalItems / itemsPerPage)).ToString());
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

        Claim? currentUserIdClaim = User.Claims.Where(c => c.Type == "UserId").FirstOrDefault();

        if (currentUserIdClaim == null)
        {
            return new ForbidResult();
        }

        int currentUserId = Int32.Parse(currentUserIdClaim.Value);
        // TODO: can change this (and others) to FindAsync, because we know the user has the correct role from IsHigherAuthority authorization check earlier
        ApplicationUser currentUser = await _context.ApplicationUsers.Where(u => u.Id == currentUserId && u.Role != "User" && u.Role != "Banned").FirstOrDefaultAsync();

        if (currentUser == null)
        {
            return new ForbidResult();
        }

        List<string> roles;
        List<Category> categories;

        // The roles able to be edited depend on the current users role. If moderator, they cannot edit roles.
        // Also for moderators, only categories that they are moderators of can be edited.
        if (currentUser.Role == "Root")
        {
            roles = new List<string> { "Root", "Admin", "Moderator", "User", "Banned" };
            categories = await _context.Categories.Where(c => c.IsActive).ToListAsync();
        }
        else if (currentUser.Role == "Admin")
        {
            roles = new List<string> { "Moderator", "User", "Banned" };
            categories = await _context.Categories.Where(c => c.IsActive).ToListAsync();
        }
        else if (currentUser.Role == "Moderator")
        {
            roles = new List<string>();

            // Moderators can only ban from categories they moderate in.
            var query = from c in _context.Categories
                        join m in _context.ModeratorLinks
                        on c.Id equals m.CategoryId
                        where m.ApplicationUserId == currentUser.Id && c.IsActive
                        select c;

            categories = await query.ToListAsync();
        }
        else
        {
            return new ForbidResult();
        }

        // The lists for roles and categories are stored in ViewBag and passed to the view.
        // We also check which categories the user we are editing are moderators of or banned in. This is so we can check the box of that category in the view.
        ViewBag.editRoles = roles;
        ViewBag.categories = categories;
        ViewBag.selectedModeratedCategories = await _context.ModeratorLinks.Where(m => m.ApplicationUserId == userToEdit.Id).ToListAsync();
        ViewBag.selectedBannedCategories = await _context.BannedLinks.Where(m => m.ApplicationUserId == userToEdit.Id).ToListAsync();

        return View(userToEdit);
    }

    [HttpPost]
    public async Task<IActionResult> EditUserRole(int userId, string role, string[] moderation_categories)
    {
        var authorizationResult = await _authorizationService.AuthorizeAsync(User, userId, "EditUserRole");

        if (!authorizationResult.Succeeded)
        {
            return new ForbidResult();
        }

        ApplicationUser editUser = await _context.ApplicationUsers.FindAsync(userId);

        foreach (Category category in await _context.Categories.Where(c => c.IsActive).ToListAsync())
        {
            // For each category, we check if there exists a record in ModeratorLink where the categoryId equals userId
            ModeratorLink moderatorLink = await _context.ModeratorLinks.Where(m => m.ApplicationUserId == userId && m.CategoryId == category.Id).FirstOrDefaultAsync();

            // Note: We need the below if statement because we can't remove a record that doesn't exist
            // If no link then we see if we need to add category to table
            if (moderatorLink == null)
            {
                bool isCategoryChecked = false;

                foreach (string cat in moderation_categories)
                {
                    // In EditUser.cshtml, the checkbox id is moderation_category_@categories[i].Id.
                    // By starting at index 20, we remove moderation_category_ and just take the category Id
                    if (Int32.Parse(cat.Substring(20)) == category.Id)
                    {
                        isCategoryChecked = true;
                        break;
                    }
                }

                // If the category is checked, we create a new ModeratorLink record matching the user to the category
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
            // If there is a link, we see if we need to remove record from table
            else
            {
                bool isCategoryChecked = false;

                foreach (string cat in moderation_categories)
                {
                    // In EditUser.cshtml, the checkbox id is moderation_category_@categories[i].Id.
                    // By starting at index 20, we remove moderation_category_ and just take the category Id
                    if (Int32.Parse(cat.Substring(20)) == category.Id)
                    {
                        isCategoryChecked = true;
                        break;
                    }
                }

                // If the category is not checked, we remove the existing ModeratorLink record with that user id and category id
                if (!isCategoryChecked)
                {
                    _context.Remove(moderatorLink);
                    await _context.SaveChangesAsync();
                }
            }
        }

        editUser.Role = role;
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

        Claim? currentUserIdClaim = User.Claims.Where(c => c.Type == "UserId").FirstOrDefault();
        ApplicationUser currentUser = await _context.ApplicationUsers.FindAsync(Int32.Parse(currentUserIdClaim.Value));

        List<Category> categories;
        // Moderators can only ban from categories they moderate in.
        if (currentUser.Role == "Moderator")
        {
            var query = from c in _context.Categories
                        join m in _context.ModeratorLinks
                        on c.Id equals m.CategoryId
                        where m.ApplicationUserId == currentUser.Id && c.IsActive
                        select c;

            categories = await query.ToListAsync();
        }
        else
        {   // the only other users that would reach this point (because of IsHigherAuthority check) are Root/Admin users. They can edit all categories.
            categories = await _context.Categories.Where(c => c.IsActive).ToListAsync();
        }

        foreach (Category category in categories)
        {
            // For each category, we check if there exists a record in BannedLink where the categoryId equals userId
            BannedLink bannedLink = await _context.BannedLinks.Where(m => m.ApplicationUserId == userId && m.CategoryId == category.Id).FirstOrDefaultAsync();

            // Note: We need the below if statement because we can't remove a record that doesn't exist
            // If no link then we see if we need to add category to table
            if (bannedLink == null)
            {
                bool isCategoryChecked = false;

                foreach (string cat in banned_categories)
                {
                    // In EditUser.cshtml, the checkbox id is banned_category_@categories[i].Id.
                    // By starting at index 16, we remove banned_category_ and just take the category Id
                    if (Int32.Parse(cat.Substring(16)) == category.Id)
                    {
                        isCategoryChecked = true;
                        break;
                    }
                }
                
                // If the category is checked, we create a new BannedLink record matching the user to the category
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
            // If there is a link, we see if we need to remove record from table
            else
            {
                bool isCategoryChecked = false;

                foreach (string cat in banned_categories)
                {
                    // In EditUser.cshtml, the checkbox id is banned_category_@categories[i].Id.
                    // By starting at index 16, we remove banned_category_ and just take the category Id
                    if (Int32.Parse(cat.Substring(16)) == category.Id)
                    {
                        isCategoryChecked = true;
                        break;
                    }
                }

                // If the category is not checked, we remove the existing BannedLink record with that user id and category id
                if (!isCategoryChecked)
                {
                    _context.Remove(bannedLink);
                    await _context.SaveChangesAsync();
                }
            }
        }

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
        // Currently we are not utilising the ShowModControls claim (everywhere in the system that we need ShowModControls we get it from the database). However, it could potentially be used in the future and it is harmless because it is a claim that only the user can change themselves (unlike Role, which can turn into a problem when a higher user changes the role of a lower user and the changes aren't reflected in that users current claim).

        var claims = new List<Claim>
        {
            new Claim("UserId", currentUser.Id.ToString()),
            new Claim("Username", currentUser.Username),
            new Claim("ShowModControls", currentUser.ShowModControls.ToString())
        };

        var claimsIdentitiy = new ClaimsIdentity(claims, "WhirlAuth");
        var authProperties = new AuthenticationProperties { };
        var claimsPrincipal = new ClaimsPrincipal(claimsIdentitiy);
        
        await HttpContext.SignInAsync("WhirlAuth", claimsPrincipal, authProperties);

        return RedirectToAction("Index", "Admin");
    }

}