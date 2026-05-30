using UTB.Minute.CanteenClient.Components;
using UTB.Minute.CanteenClient.Services;
using UTB.Minute.Contracts;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

Microsoft.IdentityModel.Logging.IdentityModelEventSource.ShowPII = true;

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
})
.AddCookie()
.AddOpenIdConnect(options =>
{
    var keycloakUrl = builder.Configuration.GetConnectionString("keycloak");
    
    if (string.IsNullOrEmpty(keycloakUrl))
    {
        keycloakUrl = "https://127.0.0.1:8080"; 
    }
    else if (!keycloakUrl.Contains("://"))
    {
        keycloakUrl = "https://" + keycloakUrl; 
    }

    options.Authority = $"{keycloakUrl}/realms/menza";
    options.ClientId = "canteen-client";
    options.ResponseType = "code";
    options.SaveTokens = true;
    options.RequireHttpsMetadata = false;

    // Use a custom handler to bypass SSL and force HTTP/1.1
    options.BackchannelHttpHandler = new HttpClientHandler
    {
        ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
    };
    
    options.Backchannel = new HttpClient(options.BackchannelHttpHandler)
    {
        DefaultRequestVersion = System.Net.HttpVersion.Version11,
        DefaultVersionPolicy = HttpVersionPolicy.RequestVersionExact,
        Timeout = TimeSpan.FromSeconds(30)
    };

    options.TokenValidationParameters = new TokenValidationParameters
    {
        NameClaimType = "preferred_username",
        RoleClaimType = "roles"
    };
});

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddHttpContextAccessor();

builder.Services.AddServiceDiscovery();

builder.Services.AddScoped<TokenProvider>();
builder.Services.AddScoped<UTB.Minute.CanteenClient.Services.TokenHandler>();
builder.Services.AddScoped<SseClientService>();

builder.Services.AddHttpClient("SseClient", client => 
{
    client.Timeout = Timeout.InfiniteTimeSpan;
})
    .RemoveAllLoggers() // Odstránime predvolené logovanie, ktoré robí problémy so streamom
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
    {
        ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
    })
    .AddStandardResilienceHandler(options => 
    {
        options.AttemptTimeout.Timeout = TimeSpan.FromHours(5);
        options.TotalRequestTimeout.Timeout = TimeSpan.FromHours(5);
        options.CircuitBreaker.SamplingDuration = TimeSpan.FromHours(12); // Safe range (0.5s - 24h) AND >= 2 * AttemptTimeout
    });

builder.Services.AddHttpClient("SseClient")
    .AddServiceDiscovery();

builder.Services.AddHttpClient<IOrderClient, OrderClient>(client =>
{
    client.BaseAddress = new Uri("http://webapi");
})
.AddHttpMessageHandler<UTB.Minute.CanteenClient.Services.TokenHandler>()
.AddServiceDiscovery()
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
});

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();
app.UseAuthentication();
app.UseAuthorization();

app.MapRazorComponents<App>()

    .AddInteractiveServerRenderMode();

app.MapGet("/login", () => Results.Challenge(new Microsoft.AspNetCore.Authentication.AuthenticationProperties { RedirectUri = "/" }, [OpenIdConnectDefaults.AuthenticationScheme]));
app.MapGet("/logout", () => Results.SignOut(new Microsoft.AspNetCore.Authentication.AuthenticationProperties { RedirectUri = "/" }, [CookieAuthenticationDefaults.AuthenticationScheme, OpenIdConnectDefaults.AuthenticationScheme]));

app.Run();