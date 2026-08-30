using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PETHUB.Data;
using PETHUB.Models;
using PETHUB.Services;
using PETHUB.Hubs;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Add DbContext with SQL Server
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // Require users to confirm their email before logging in
    options.SignIn.RequireConfirmedEmail = true;

    // Uses the custom 10-minute provider for password resets
    options.Tokens.PasswordResetTokenProvider = "PETHubPasswordReset";

    // Require every user to have a unique email address
    options.User.RequireUniqueEmail = true;

    
})
// Configure token provider for password reset
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders()
.AddTokenProvider<PasswordResetTokenProvider<ApplicationUser>>("PETHubPasswordReset")
.AddTokenProvider<AdminInvitationTokenProvider<ApplicationUser>>("PETHubAdminInvitation");


builder.Services.AddScoped<IProfileService, ProfileService>();
builder.Services.AddScoped<AdminIProfileService, AdminProfileService>();

// Register EmailSender here
builder.Services.AddTransient<EmailSender>();

// Register NotificationService here
builder.Services.AddScoped<NotificationService>();

// Register MessagingService here
builder.Services.AddScoped<MessagingService>();

// Register SignalR here
builder.Services.AddSignalR();

var app = builder.Build();

//  Role seeding block
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

    Task.Run(async () =>
    {
        string[] roles = { "Admin", "Member" };

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }
    }).GetAwaiter().GetResult();
}



app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication(); //Added to identify the user and redirect appropriately to the guest page if not authenticated
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

// Seed the admin user
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    await DbInitializer.SeedAdminAsync(services);
}

app.MapHub<ChatHub>("/chatHub");

app.Run();
