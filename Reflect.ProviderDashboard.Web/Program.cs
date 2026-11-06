using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Data.Sqlite;
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
// Do not let the framework's convenience switch silently trust every proxy.
if (builder.Configuration.GetValue<bool>("ForwardedHeaders_Enabled")
    || string.Equals(Environment.GetEnvironmentVariable("ASPNETCORE_FORWARDEDHEADERS_ENABLED"), "true", StringComparison.OrdinalIgnoreCase)
    || string.Equals(Environment.GetEnvironmentVariable("DOTNET_FORWARDEDHEADERS_ENABLED"), "true", StringComparison.OrdinalIgnoreCase))
    throw new InvalidOperationException("Unrestricted forwarded headers are disabled. Configure Hosting:KnownProxies explicitly.");
var proxyAddresses = builder.Configuration.GetSection("Hosting:KnownProxies").Get<string[]>() ?? [];
var trustedProxies = proxyAddresses.Select(address =>
{
    if (!System.Net.IPAddress.TryParse(address, out var parsed)
        || parsed.Equals(System.Net.IPAddress.Any) || parsed.Equals(System.Net.IPAddress.IPv6Any))
        throw new InvalidOperationException("Hosting:KnownProxies requires explicit proxy IP addresses.");
    return parsed;
}).ToArray();
var httpsAtEdge = builder.Configuration.GetValue<bool>("Hosting:HttpsAtEdge");
if (httpsAtEdge && trustedProxies.Length > 0)
    throw new InvalidOperationException("Choose Hosting:HttpsAtEdge or Hosting:KnownProxies, not both.");
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.ForwardLimit = 1;
    options.KnownProxies.Clear();
    options.KnownIPNetworks.Clear();
    foreach (var address in trustedProxies) options.KnownProxies.Add(address);
});
builder.WebHost.ConfigureKestrel(options => options.AddServerHeader = false);
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, ct) =>
    {
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retry))
            context.HttpContext.Response.Headers.RetryAfter = Math.Ceiling(retry.TotalSeconds).ToString(System.Globalization.CultureInfo.InvariantCulture);
        await context.HttpContext.Response.WriteAsync("Too many sign-in attempts. Please wait a minute and try again.", ct);
    };
    // Chat is per signed-in provider; keeps a future paid model call from being hammered.
    options.AddPolicy("chat", context => RateLimitPartition.GetFixedWindowLimiter(
        context.User.Identity?.Name ?? "anonymous", _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 20, Window = TimeSpan.FromMinutes(1), QueueLimit = 0
        }));
    options.AddPolicy("account", _ => RateLimitPartition.GetFixedWindowLimiter("account-global", _ => new FixedWindowRateLimiterOptions
    {
        PermitLimit = 120, Window = TimeSpan.FromMinutes(1), QueueLimit = 0
    }));
    // Applied to credential POSTs only: browsing and logout never consume this budget.
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        AccountRateLimit.IsCredentialAttempt(context)
            ? RateLimitPartition.GetFixedWindowLimiter(AccountRateLimit.ClientKey(context), _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 30, Window = TimeSpan.FromMinutes(1), QueueLimit = 0
            })
            : RateLimitPartition.GetNoLimiter("not-credentials"));
});
builder.Services.AddHealthChecks().AddCheck<AccountStoreHealthCheck>("account-store", tags: ["ready"]);

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
    options.UseSqlite(new SqliteConnectionStringBuilder { DataSource = accountDatabasePath }.ToString()));

var clinicalProvider = builder.Configuration["Clinical:Provider"] ?? "Sqlite";
if (clinicalProvider is not ("Sqlite" or "Postgres"))
    throw new InvalidOperationException("Clinical:Provider must be Sqlite or Postgres.");
var clinicalSqlitePath = Path.GetFullPath(
    builder.Configuration["Clinical:Path"] ?? Path.Combine("App_Data", "provider-clinical.db"),
    builder.Environment.ContentRootPath);
if (clinicalProvider == "Postgres" && string.IsNullOrWhiteSpace(builder.Configuration.GetConnectionString("Clinical")))
    throw new InvalidOperationException("Clinical:Provider=Postgres requires ConnectionStrings:Clinical supplied through host secrets.");
if (clinicalProvider == "Sqlite")
    Directory.CreateDirectory(Path.GetDirectoryName(clinicalSqlitePath)!);
builder.Services.AddDbContext<ClinicalDbContext>(options =>
{
    if (clinicalProvider == "Postgres")
        options.UseNpgsql(builder.Configuration.GetConnectionString("Clinical"));
    else
        options.UseSqlite(new SqliteConnectionStringBuilder { DataSource = clinicalSqlitePath }.ToString());
});

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
builder.Services.AddScoped<EfPatientRepository>();
builder.Services.AddScoped<IPatientRepository>(sp => sp.GetRequiredService<EfPatientRepository>());
builder.Services.AddScoped<ITherapySessionRepository, EfTherapySessionRepository>();
builder.Services.AddScoped<ICareDeviceRepository, InMemoryCareDeviceRepository>();
builder.Services.AddScoped<Reflect.ProviderDashboard.Web.Services.Chat.IChatResponder, Reflect.ProviderDashboard.Web.Services.Chat.StubChatResponder>();

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
        // Fail closed rather than silently leaving an old password active after rotation.
        if (!await users.CheckPasswordAsync(user, builder.Configuration["Demo:Password"]))
            throw new InvalidOperationException("Configured demo password differs from the existing account. Reset the disposable demo account store before restarting.");
        if (!await users.IsInRoleAsync(user, ProviderAuthorization.ProviderRole))
        {
            var assigned = await users.AddToRoleAsync(user, ProviderAuthorization.ProviderRole);
            if (!assigned.Succeeded) throw new InvalidOperationException("Demo role assignment failed.");
        }
        InMemoryDataStore.AssignPatientsToProvider(user.Id);
        // Disposable synthetic demo: reload the dataset each start so relative dates stay current.
        await ClinicalSeeder.EnsureAsync(scope.ServiceProvider.GetRequiredService<ClinicalDbContext>(), user.Id, reseed: true);
    }
    else
    {
        // Development: synthetic rows exist but are unassigned, so new accounts start with an empty caseload.
        await ClinicalSeeder.EnsureAsync(scope.ServiceProvider.GetRequiredService<ClinicalDbContext>(), null, reseed: false);
    }
    if (clinicalProvider == "Sqlite" && !OperatingSystem.IsWindows())
        File.SetUnixFileMode(clinicalSqlitePath, UnixFileMode.UserRead | UnixFileMode.UserWrite);
}

// Enable only behind a host that enforces HTTPS and blocks direct public access to this port.
if (trustedProxies.Length > 0) app.UseForwardedHeaders();
if (httpsAtEdge)
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

app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false }).AllowAnonymous();
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("ready")
}).AllowAnonymous();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Dashboard}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
