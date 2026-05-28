using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using RainfallThree.Models;

[Authorize(Roles = "PrimaryUser")]
public class AdminController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IEmailSender _emailSender;

    public AdminController(UserManager<ApplicationUser> userManager, IEmailSender emailSender)
    {
        _userManager = userManager;
        _emailSender = emailSender;
    }

    [HttpGet]
    public IActionResult PendingUsers()
    {
        var pendingUsers = _userManager.Users
            .Where(u => !u.IsApproved)
            .ToList();

        var allUsers = _userManager.Users.ToList();

        var vm = new UserViewModel
        {
            PendingUsers = pendingUsers,
            AllUsers = allUsers
        };

        return View(vm); 
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(string id)
    {
        var user = await _userManager.FindByIdAsync(id);

        if (user != null)
        {
            user.IsApproved = true;
            user.Status = "Approved";
            await _userManager.UpdateAsync(user);

            await _emailSender.SendEmailAsync(
                user.Email,
                "Account Approved",
                $"Hello {user.UserName}, your EDRE account has been approved. You can now log in.");
        }

        return RedirectToAction(nameof(PendingUsers));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deny(string id)
    {
        var user = await _userManager.FindByIdAsync(id);

        if (user != null)
        {
            user.IsApproved = false;
            user.Status = "Pending";
            await _userManager.UpdateAsync(user);

            await _emailSender.SendEmailAsync(
                user.Email,
                "Account Disabled",
                $"Hello {user.UserName}, your account access has been disabled by an administrator. Contact admin@edre.ethekwinifews.durban for assisstance."
            );
        }

        return RedirectToAction(nameof(PendingUsers));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(string id)
    {
        var user = await _userManager.FindByIdAsync(id);

        if (user != null)
        {
            user.Status = "Rejected";
            await _userManager.UpdateAsync(user);

            await _emailSender.SendEmailAsync(
                user.Email,
                "Account Rejected",
                $"Hello {user.UserName}, your EDRE account request has been rejected by an administrator. Contact admin@edre.ethekwinifews.durban for assisstance."
            );

            //await _userManager.DeleteAsync(user);
        }

        return RedirectToAction(nameof(PendingUsers));
    }
}
