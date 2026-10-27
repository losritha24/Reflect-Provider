using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Reflect.ProviderDashboard.Domain.Interfaces;
using Reflect.ProviderDashboard.Web.Infrastructure;
using Reflect.ProviderDashboard.Web.Services;
using Reflect.ProviderDashboard.Web.Data;
using Reflect.ProviderDashboard.Web.Models.Identity;

InMemoryDataStore.Seed();

var builder = WebApplication.CreateBuilder(args);

var configuredAccountDatabasePath = builder.Configuration["AccountStore:Path"];
var accountDatabasePath = Path.GetFullPath(
    configuredAccountDatabasePath ?? Path.Combine("App_Data", "provider-accounts.db"),
    builder.Environment.ContentRootPath);
var accountDatabaseDirectory = Path.GetDirectoryName(accountDatabasePath)!;
Directory.CreateDirectory(accountDatabaseDirectory);
if (!OperatingSystem.IsWindows())
{
    File.SetUnixFileMode(
        accountDatabaseDirectory,
        UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
}

builder.Services.AddControllersWithViews(options =>
{
    options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
});
builder.Services.AddRazorPages();

builder.Services.AddDbContext<ProviderDbContext>(options =>
    options.UseSqlite($"Data Source={accountDatabasePath}"));

builder.Services.AddIdentity<ProviderUser, IdentityRole>(options =>
    {
        options.User.RequireUniqueEmail = true;
        options.Password.RequiredLength = 12;
        options.Password.RequiredUniqueChars = 4;
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = true;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.AllowedForNewUsers = true;
        options.SignIn.RequireConfirmedEmail = false;
    })
    .AddEntityFrameworkStores<ProviderDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/Login";
    options.Cookie.Name = "ReflectProvider.Auth";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.SlidingExpiration = true;
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddSingleton<IPatientRepository, InMemoryPatientRepository>();
builder.Services.AddSingleton<ITherapySessionRepository, InMemoryTherapySessionRepository>();

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(ProviderAuthorization.PolicyName, policy =>
        policy.RequireRole(ProviderAuthorization.ProviderRole));
});

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var database = scope.ServiceProvider.GetRequiredService<ProviderDbContext>();
    await database.Database.EnsureCreatedAsync();

    if (!OperatingSystem.IsWindows())
    {
        File.SetUnixFileMode(
            accountDatabasePath,
            UnixFileMode.UserRead | UnixFileMode.UserWrite);
    }

    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    if (!await roleManager.RoleExistsAsync(ProviderAuthorization.ProviderRole))
    {
        var roleResult = await roleManager.CreateAsync(new IdentityRole(ProviderAuthorization.ProviderRole));
        if (!roleResult.Succeeded)
        {
            throw new InvalidOperationException(
                $"Could not initialize the provider role: {string.Join(", ", roleResult.Errors.Select(error => error.Description))}");
        }
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseMiddleware<CspMiddleware>();
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Dashboard}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
