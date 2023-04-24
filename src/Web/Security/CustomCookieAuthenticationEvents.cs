using Forum.Data;
using Forum.Entities;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Forum.Security;

public class CustomCookieAuthenticationEvents : CookieAuthenticationEvents
{
    private readonly DataContext _context;

    public CustomCookieAuthenticationEvents(DataContext context)
    {
        _context = context;
    }

    public override async Task ValidatePrincipal(CookieValidatePrincipalContext context)
    {
        var userPrincipal = context.Principal;

        // Look for the LastChanged claim.
        var lastChanged = (from c in userPrincipal.Claims
                           where c.Type == "LastChanged"
                           select c.Value).FirstOrDefault();

        // User Id
        var userId = (from c in userPrincipal.Claims
                           where c.Type == "UserId"
                           select c.Value).FirstOrDefault();

        if (string.IsNullOrEmpty(lastChanged) || !(await ValidateLastChanged(lastChanged, userId)))
        {
            context.RejectPrincipal();

            await context.HttpContext.SignOutAsync("WhirlAuth");
        }
    }

    private async Task<bool> ValidateLastChanged(string lastChanged, string userId)
    {
        long convertedLastChanged = Int64.Parse(lastChanged);
        int convertedUserId = Int32.Parse(userId);

        ApplicationUser user = await _context.ApplicationUsers.FindAsync(convertedUserId);

        return convertedLastChanged >= user.LastChanged;
    }
}