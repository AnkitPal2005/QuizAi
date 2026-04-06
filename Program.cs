using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using AIQuizPlatform.Data;
using AIQuizPlatform.Models;
using AIQuizPlatform.Services;

var builder = WebApplication.CreateBuilder(args);
var port = builder.Configuration["PORT"];

if (!string.IsNullOrWhiteSpace(port))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.SignIn.RequireConfirmedEmail = false;
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.AccessDeniedPath = "/Home/AccessDenied";
});

builder.Services.AddControllersWithViews();
builder.Services.AddHttpClient("Groq");
builder.Services.AddScoped<GroqService>();
builder.Services.AddScoped<QuizService>();
builder.Services.AddTransient<Microsoft.AspNetCore.Identity.UI.Services.IEmailSender, AIQuizPlatform.Services.EmailService>();

var app = builder.Build();

// Seed roles and admin user
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    await db.Database.MigrateAsync();

    foreach (var role in new[] { "Admin", "Student", "Mentor" })
    {
        if (!await roleManager.RoleExistsAsync(role))
            await roleManager.CreateAsync(new IdentityRole(role));
    }

    // Create default admin
    const string adminEmail = "Ankit@admin.com";
    if (await userManager.FindByEmailAsync(adminEmail) == null)
    {
        var admin = new ApplicationUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            FullName = "Ankit Admin",
            EmailConfirmed = true
        };
        var result = await userManager.CreateAsync(admin, "Ankit@123");
        if (result.Succeeded)
            await userManager.AddToRoleAsync(admin, "Admin");
    }
}

if (app.Environment.IsDevelopment())
    app.UseMigrationsEndPoint();
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

app.Run();
