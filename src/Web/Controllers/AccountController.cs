using Forum.Data;
using Forum.Entities;
using Forum.Services;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication;

namespace Forum.Controllers;

public class AccountController : Controller
{
    private readonly DataContext _context;
    private readonly IPasswordService _passwordService;

    public AccountController(DataContext context, IPasswordService passwordService)
    {
        _context = context;
        _passwordService = passwordService;
    }

    [HttpGet]
    public IActionResult Register()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Register(string registerUsername, string registerPassword)
    {
        if (await TestIfUserExists(registerUsername)) {
            return new BadRequestResult();
        }

        byte[] passwordHash, passwordSalt;
        _passwordService.CreatePasswordHash(registerPassword, out passwordHash, out passwordSalt);

        DateTime currentTime = DateTime.UtcNow;
        long unixTime = ((DateTimeOffset)currentTime).ToUnixTimeSeconds();

        ApplicationUser user = new ApplicationUser
        {
            Username = registerUsername,
            PasswordHash = passwordHash,
            PasswordSalt = passwordSalt,
            Role = "User",
            CreatedTS = unixTime,
            ShowModControls = false
        };

        await _context.ApplicationUsers.AddAsync(user);
        await _context.SaveChangesAsync();

        // TODO: Redirect to another page which thanks user for registering and tells them to go to login page.
        return RedirectToAction("Index", "Forum");
    }

    [HttpGet]
    public IActionResult Login()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Login(string loginUsername, string loginPassword)
    {
        ApplicationUser user = await VerifyUser(loginUsername, loginPassword);

        if (user == null) return new BadRequestResult();

        var claims = new List<Claim>
        {
            new Claim("UserId", user.Id.ToString()),
            new Claim("Username", user.Username),
            new Claim("ShowModControls", user.ShowModControls.ToString())
        };

        var claimsIdentitiy = new ClaimsIdentity(claims, "WhirlAuth");
        var authProperties = new AuthenticationProperties { };
        var claimsPrincipal = new ClaimsPrincipal(claimsIdentitiy);
        
        await HttpContext.SignInAsync("WhirlAuth", claimsPrincipal, authProperties);

        return RedirectToAction("Index", "Forum");
    }

    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync("WhirlAuth");
        
        return RedirectToAction("Index", "Forum");
    }

    [HttpGet]
    public async Task<bool> TestIfUserExists(string username)
    {
        return await _context.ApplicationUsers.AnyAsync(u => u.Username == username);
    }

    [HttpGet]
    public async Task<ApplicationUser> VerifyUser(string username, string password)
    {
        var user = await _context.ApplicationUsers.SingleOrDefaultAsync(u => u.Username == username && u.Role != "Banned");

        if (user == null) return null;

        if (_passwordService.VerifyPasswordHash(password, user.PasswordHash, user.PasswordSalt))
        {
            return user;
        }
        else
        {
            return null;
        }
    }
}