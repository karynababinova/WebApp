using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using LibraryDomain.Model;
using LibraryInfrastructure.Models;
using LibraryInfrastructure.Models.Auth;
using LibraryInfrastructure.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibraryInfrastructure.Controllers;

public class AccountController : Controller
{
    private readonly DbLibraryContext _context;

    public AccountController(DbLibraryContext context)
    {
        _context = context;
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var normalizedEmail = model.Email.Trim().ToLowerInvariant();
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail);

        if (user == null || !PasswordService.Verify(model.Password, user.PasswordHash))
        {
            ModelState.AddModelError(string.Empty, "Невірний email або пароль.");
            return View(model);
        }

        if (user.IsBlocked)
        {
            ModelState.AddModelError(string.Empty, "Ваш акаунт заблоковано адміністратором.");
            return View(model);
        }

        if (model.Password == user.PasswordHash)
        {
            user.PasswordHash = PasswordService.HashPassword(model.Password);
            await _context.SaveChangesAsync();
        }

        await SignInAsync(user, model.RememberMe);

        if (!string.IsNullOrWhiteSpace(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
        {
            return Redirect(model.ReturnUrl);
        }

        return RedirectToAction("Index", "Fanfics");
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Register()
    {
        return View(new RegisterViewModel());
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var normalizedEmail = model.Email.Trim().ToLowerInvariant();
        var normalizedUsername = model.Username.Trim().ToLowerInvariant();

        var emailExists = await _context.Users.AnyAsync(u => u.Email.ToLower() == normalizedEmail);
        if (emailExists)
        {
            ModelState.AddModelError(nameof(model.Email), "Цей email вже зайнятий.");
            return View(model);
        }

        var usernameExists = await _context.Users.AnyAsync(u => u.Username.ToLower() == normalizedUsername);
        if (usernameExists)
        {
            ModelState.AddModelError(nameof(model.Username), "Це ім'я користувача вже зайняте.");
            return View(model);
        }

        var user = new User
        {
            Username = model.Username.Trim(),
            Email = model.Email.Trim(),
            PasswordHash = PasswordService.HashPassword(model.Password),
            Role = AppRoles.Author,
            IsBlocked = false,
            Bio = "Привіт! Я новий автор.",
            AvatarUrl = null,
            CreatedAt = DateTime.Now
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        await SignInAsync(user, isPersistent: true);
        return RedirectToAction("Index", "Fanfics");
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        var userId = User.GetCurrentUserId();
        if (!userId.HasValue)
        {
            return Challenge();
        }

        var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId.Value);
        if (user == null)
        {
            return NotFound();
        }

        var model = new ProfileViewModel
        {
            Username = user.Username,
            Email = user.Email,
            Bio = user.Bio,
            Role = user.Role,
            RoleDisplayName = AppRoles.ToDisplayName(user.Role),
            MyWorks = await LoadMyWorksAsync(user.Id)
        };

        return View(model);
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(ProfileViewModel model)
    {
        var userId = User.GetCurrentUserId();
        if (!userId.HasValue)
        {
            return Challenge();
        }

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId.Value);
        if (user == null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            model.Role = user.Role;
            model.RoleDisplayName = AppRoles.ToDisplayName(user.Role);
            model.MyWorks = await LoadMyWorksAsync(user.Id);
            return View(model);
        }

        var normalizedEmail = model.Email.Trim().ToLowerInvariant();
        var normalizedUsername = model.Username.Trim().ToLowerInvariant();

        var emailExists = await _context.Users.AnyAsync(u => u.Id != user.Id && u.Email.ToLower() == normalizedEmail);
        if (emailExists)
        {
            ModelState.AddModelError(nameof(model.Email), "Цей email вже зайнятий.");
        }

        var usernameExists = await _context.Users.AnyAsync(u => u.Id != user.Id && u.Username.ToLower() == normalizedUsername);
        if (usernameExists)
        {
            ModelState.AddModelError(nameof(model.Username), "Це ім'я користувача вже зайняте.");
        }

        if (!ModelState.IsValid)
        {
            model.Role = user.Role;
            model.RoleDisplayName = AppRoles.ToDisplayName(user.Role);
            model.MyWorks = await LoadMyWorksAsync(user.Id);
            return View(model);
        }

        user.Username = model.Username.Trim();
        user.Email = model.Email.Trim();
        user.Bio = string.IsNullOrWhiteSpace(model.Bio) ? null : model.Bio.Trim();
        await _context.SaveChangesAsync();

        await SignInAsync(user, isPersistent: true);
        ViewData["Saved"] = true;
        model.Role = user.Role;
        model.RoleDisplayName = AppRoles.ToDisplayName(user.Role);
        model.MyWorks = await LoadMyWorksAsync(user.Id);
        return View(model);
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Bookmarks()
    {
        var userId = User.GetCurrentUserId();
        if (!userId.HasValue)
        {
            return Challenge();
        }

        var bookmarks = await _context.Bookmarks
            .AsNoTracking()
            .Include(b => b.Fanfic)
                .ThenInclude(f => f.ContentRating)
            .Where(b => b.UserId == userId.Value)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();

        return View(bookmarks);
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }

    [Authorize(Roles = AppRoles.Admin)]
    [HttpGet]
    public async Task<IActionResult> Users()
    {
        var users = await _context.Users
            .AsNoTracking()
            .OrderBy(u => u.Username)
            .ToListAsync();

        return View(users);
    }

    [Authorize(Roles = AppRoles.Admin)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleBlock(int id)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
        if (user == null)
        {
            return NotFound();
        }

        var currentUserId = User.GetCurrentUserId();
        if (currentUserId.HasValue && currentUserId.Value == user.Id)
        {
            TempData["UsersError"] = "Адміністратор не може заблокувати сам себе.";
            return RedirectToAction(nameof(Users));
        }

        user.IsBlocked = !user.IsBlocked;
        await _context.SaveChangesAsync();

        TempData["UsersSuccess"] = user.IsBlocked
            ? $"Користувача {user.Username} заблоковано."
            : $"Користувача {user.Username} розблоковано.";

        return RedirectToAction(nameof(Users));
    }

    private async Task SignInAsync(User user, bool isPersistent)
    {
        var role = ClaimsPrincipalExtensions.ResolveRole(user.Role);
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, role)
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties { IsPersistent = isPersistent });
    }

    private async Task<IReadOnlyList<Fanfic>> LoadMyWorksAsync(int userId)
    {
        return await _context.Fanfics
            .AsNoTracking()
            .Include(f => f.ContentRating)
            .Where(f => f.UserId == userId)
            .OrderByDescending(f => f.UpdatedAt)
            .ToListAsync();
    }

}
