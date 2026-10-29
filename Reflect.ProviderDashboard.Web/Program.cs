using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Reflect.ProviderDashboard.Domain.Interfaces;
using Reflect.ProviderDashboard.Web.Infrastructure;
using Reflect.ProviderDashboard.Web.Services;
using Reflect.ProviderDashboard.Web.Data;
using Reflect.ProviderDashboard.Web.Models.Identity;

InMemoryDataStore.Seed();

var builder = WebApplication.CreateBuilder(args);
var demoEnabled = builder.Configuration.GetValue<bool>("Demo:Enabled");
if (demoEnabled && (string.IsNullOrWhiteSpace(builder.Configuration["Demo:Email"])
    || string.IsNullOrWhiteSpace(builder.Configuration["Demo:Password"])))
    throw new InvalidOperationException("Demo mode requires Demo:Email and Demo:Password supplied through host secrets.");
if (!builder.Environment.IsDevelopment())
{
    var hosts = (builder.Configuration["AllowedHosts"] ?? "").Split(';', StringSplitOptions.TrimEntries);
    if (hosts.Any(host => string.IsNullOrWhiteSpace(host) || host.Contains('*')
        || Uri.CheckHostName(host) == UriHostNameType.Unknown))
        throw new InvalidOperationException("AllowedHosts must contain explicit hostnames or IP addresses separated by semicolons, without wildcards, schemes, paths, or ports.");
}
builder.WebHost.ConfigureKestrel(options => options.AddServerHeader = false);
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    // A bounded global budget is intentional for this single-instance competition demo.
    options.AddFixedWindowLimiter("account", limiter =>
    {
        limiter.PermitLimit = 30;
        limiter.Window = TimeSpan.FromMinutes(1);
        limiter.QueueLimit = 0;
    });
});
builder.Services.AddHealthChecks();

var configuredAccountDatabasePath = builder.Configuration["AccountStore:Path"];
var accountDatabasePath = Path.GetFullPath(
    configuredAccountDatabasePath ?? Path.Combine("App_Data", "provider-accounts.db"),
    builder.Environment.ContentRootPath);
var accountDatabaseDirectory = Path.GetDirectoryName(accountDatabasePath)!;
if (OperatingSystem.IsWindows())
    Directory.CreateDirectory(accountDatabaseDirectory);
else
    Directory.CreateDirectory(accountDatabaseDirectory,
        UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);

builder.Services.AddControllersWithViews(options =>
{
    options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
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
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
        ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.SlidingExpiration = false;
    options.ExpireTimeSpan = TimeSpan.FromHours(1);
});

builder.Services.AddMemoryCache();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<ITicketStore, InMemoryTicketStore>();
builder.Services.AddOptions<CookieAuthenticationOptions>(IdentityConstants.ApplicationScheme)
    .Configure<ITicketStore>((options, store) => options.SessionStore = store);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<IPatientRepository, InMemoryPatientRepository>();
builder.Services.AddScoped<ITherapySessionRepository, InMemoryTherapySessionRepository>();

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(ProviderAuthorization.PolicyName, policy =>
        policy.RequireRole(ProviderAuthorization.ProviderRole));
});

builder.Services.AddAntiforgery(options =>
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
        ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always);

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
    if (demoEnabled)
    {
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ProviderUser>>();
        var email = builder.Configuration["Demo:Email"].Trim();
        var user = await users.FindByEmailAsync(email);
        if (user is null)
        {
            user = new ProviderUser { UserName = email, Email = email, DisplayName = "Competition reviewer (synthetic demo)" };
            var created = await users.CreateAsync(user, builder.Configuration["Demo:Password"]);
            if (!created.Succeeded) throw new InvalidOperationException("Demo account creation failed. Check email and password policy.");
        }
        if (!await users.IsInRoleAsync(user, ProviderAuthorization.ProviderRole))
        {
            var assigned = await users.AddToRoleAsync(user, ProviderAuthorization.ProviderRole);
            if (!assigned.Succeeded) throw new InvalidOperationException("Demo role assignment failed.");
        }
        InMemoryDataStore.AssignPatientsToProvider(user.Id);
    }
}

// Enable only behind a host that enforces HTTPS and blocks direct public access to this port.
if (builder.Configuration.GetValue<bool>("Hosting:HttpsAtEdge"))
    app.Use((context, next) => { context.Request.Scheme = "https"; return next(context); });

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseMiddleware<CspMiddleware>();
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health/live").AllowAnonymous();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Dashboard}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
