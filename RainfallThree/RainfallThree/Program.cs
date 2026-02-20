using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using RainfallThree.Data;
using RainfallThree.Models;
using RainfallThree.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IEmailSender, EmailSender>();

// Add services to the container.
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = false)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();


builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapRazorPages()
   .WithStaticAssets();


// Seed roles and primary user
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

    // 1Seed Roles
    string[] roles = { "PrimaryUser", "NormalUser" };
    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
        }
    }

    // Primary User Info
    string primaryUserEmail = "kiyashcsha@gmail.com";
    string primaryUserPassword = "KickAss8*";

    // Check if user exists
    var user = await userManager.FindByEmailAsync(primaryUserEmail);
    if (user == null)
    {
        // User doesn't exist → create user
        user = new ApplicationUser { UserName = primaryUserEmail, Email = primaryUserEmail };
        var result = await userManager.CreateAsync(user, primaryUserPassword);
        if (!result.Succeeded)
        {
            throw new Exception("Failed to create primary user: " +
                                string.Join(", ", result.Errors.Select(e => e.Description)));
        }
    }

    // Add PrimaryUser role if not already assigned
    if (!await userManager.IsInRoleAsync(user, "PrimaryUser"))
    {
        await userManager.AddToRoleAsync(user, "PrimaryUser");
    }
}

app.Run();