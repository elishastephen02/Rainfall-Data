using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using RainfallThree.Models;

[Authorize(Roles = "PrimaryUser")]
public class AdminController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;

    public AdminController(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
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
            await _userManager.UpdateAsync(user);
        }

        return RedirectToAction(nameof(PendingUsers));
    }
}
