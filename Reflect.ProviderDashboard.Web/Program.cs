using Microsoft.AspNetCore.Authorization;
using Reflect.ProviderDashboard.Domain.Interfaces;
using Reflect.ProviderDashboard.Web.Infrastructure;
using Reflect.ProviderDashboard.Web.Services;

InMemoryDataStore.Seed();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews(options =>
{
    options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
});
builder.Services.AddRazorPages();

builder.Services.AddSingleton<ICurrentUserService, FakeCurrentUserService>();
builder.Services.AddSingleton<IPatientRepository, InMemoryPatientRepository>();
builder.Services.AddSingleton<ITherapySessionRepository, InMemoryTherapySessionRepository>();

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(ProviderAuthorization.PolicyName, policy =>
        policy.RequireRole(ProviderAuthorization.ProviderRole));
});

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = "DemoProvider";
    options.DefaultChallengeScheme = "DemoProvider";
}).AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, DemoProviderAuthenticationHandler>(
    "DemoProvider", _ => { });

var app = builder.Build();

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
