using BlazorKeycloack;
using BlazorKeycloack.Components;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using MudBlazor.Services;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddMudServices();
builder.Services.AddServerUI(builder.Configuration);

//  AuthN + AuthZ
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
})
.AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
{
    options.LoginPath = "/login";
    options.LogoutPath = "/logout";

    options.ExpireTimeSpan = TimeSpan.FromMinutes(1); // duración
    options.SlidingExpiration = true;                  //renueva si hay actividad
})
.AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, options =>
{
    var kc = builder.Configuration.GetSection("Keycloack");
    options.Authority = kc["Authority"];                 // realm
    options.ClientId = kc["ClientId"];
    options.ClientSecret = kc["ClientSecret"];           // confidential client
    options.ResponseType = OpenIdConnectResponseType.Code; // Authorization Code Flow
    options.SaveTokens = true;


    // scopes típicos
    options.Scope.Clear();
    options.Scope.Add("openid");
    options.Scope.Add("profile");
    options.Scope.Add("email");

    // Para que roles/claims puedan mapearse mejor (depende de cómo emitas tokens en Keycloak)
    options.GetClaimsFromUserInfoEndpoint = true;
    options.TokenValidationParameters.NameClaimType = "preferred_username";
    options.TokenValidationParameters.RoleClaimType = ClaimTypes.Role;


    options.CallbackPath = "/signin-oidc"; // default, pero explícito


    options.Events = new OpenIdConnectEvents
    {
        OnTokenValidated = context =>
        {
            var expiresAt = context.SecurityToken.ValidTo;

            context.Properties.ExpiresUtc = expiresAt;
            context.Properties.IsPersistent = true;

            context.Properties.RedirectUri = "/page1";
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = options.DefaultPolicy; // todo requiere auth por defecto
});
var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseAntiforgery();

app.MapStaticAssets();

//  MUY IMPORTANTE: en este orden
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/login", async (HttpContext http) =>
{
    var returnUrl =
        http.Request.Query["returnUrl"].ToString();

    if (string.IsNullOrWhiteSpace(returnUrl))
        returnUrl = http.Request.Query["ReturnUrl"].ToString();

    if (string.IsNullOrWhiteSpace(returnUrl))
        returnUrl = "/page2";

    await http.ChallengeAsync(
        OpenIdConnectDefaults.AuthenticationScheme,
        new AuthenticationProperties { RedirectUri = returnUrl });
});

// Endpoint de logout
app.MapGet("/logout", async (HttpContext http) =>
{
    await http.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

    await http.SignOutAsync(
        OpenIdConnectDefaults.AuthenticationScheme,
        new AuthenticationProperties
        {
            RedirectUri = "/"
        });
});

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
