using UTB.Minute.AdminClient.Services;
using UTB.Minute.AdminClient.Components;
using Microsoft.AspNetCore.Authentication;
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
    options.ClientId = "admin-client";
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

    options.GetClaimsFromUserInfoEndpoint = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        NameClaimType = "preferred_username",
        RoleClaimType = "roles" 
    };
    
    // Explicitly map the roles claim from the JSON token
    options.ClaimActions.MapJsonKey("roles", "roles");
    
    // Intercept events for OIDC
    options.Events = new OpenIdConnectEvents
    {
        OnTokenValidated = context =>
        {
            var identity = context.Principal?.Identity as System.Security.Claims.ClaimsIdentity;
            if (identity != null)
            {
                var targetRoleType = "roles";
                var username = identity.FindFirst("preferred_username")?.Value;

                // LOCAL DEV FALLBACK: If username is admin, give admin role
                if (username == "admin" && !identity.HasClaim(targetRoleType, "admin"))
                {
                    identity.AddClaim(new System.Security.Claims.Claim(targetRoleType, "admin"));
                }
                // LOCAL DEV FALLBACK: If username is cook, give cook role
                if (username == "cook" && !identity.HasClaim(targetRoleType, "cook"))
                {
                    identity.AddClaim(new System.Security.Claims.Claim(targetRoleType, "cook"));
                }

                // Function to add a unique claim
                void AddUniqueClaim(string value)
                {
                    if (!string.IsNullOrEmpty(value) && !identity.HasClaim(targetRoleType, value))
                        identity.AddClaim(new System.Security.Claims.Claim(targetRoleType, value));
                }

                // Try to parse roles from any "roles" claim Keycloak might have sent
                foreach (var claim in identity.FindAll("roles").ToList())
                {
                    if (claim.Value.Trim().StartsWith("["))
                    {
                        try {
                            using var doc = System.Text.Json.JsonDocument.Parse(claim.Value);
                            foreach (var r in doc.RootElement.EnumerateArray()) AddUniqueClaim(r.GetString()!);
                        } catch { }
                    }
                }
            }
            return Task.CompletedTask;
        },
        OnRedirectToIdentityProvider = context =>
        {
            if (context.Properties.Items.ContainsKey("prompt"))
            {
                context.ProtocolMessage.Prompt = context.Properties.Items["prompt"];
            }
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddHttpContextAccessor();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddServiceDiscovery();

builder.Services.AddScoped<TokenProvider>();
builder.Services.AddScoped<UTB.Minute.AdminClient.Services.TokenHandler>();
builder.Services.AddScoped<SseClientService>();

builder.Services.AddHttpClient("SseClient", client => 
{
    client.Timeout = Timeout.InfiniteTimeSpan;
})
    .RemoveAllLoggers() 
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
    {
        ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
    })
    .AddStandardResilienceHandler(options => 
    {
        options.AttemptTimeout.Timeout = TimeSpan.FromHours(5);
        options.TotalRequestTimeout.Timeout = TimeSpan.FromHours(5);
        options.CircuitBreaker.SamplingDuration = TimeSpan.FromHours(12); 
    });

builder.Services.AddHttpClient("SseClient")
    .AddServiceDiscovery();

builder.Services.AddHttpClient<IMealClient, MealClient>(client =>
{
    client.BaseAddress = new Uri("http://webapi");
    client.DefaultRequestVersion = System.Net.HttpVersion.Version11;
    client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionExact;
})
.AddHttpMessageHandler<UTB.Minute.AdminClient.Services.TokenHandler>()
.AddServiceDiscovery();

builder.Services.AddHttpClient<IOrderClient, OrderClient>(client =>
{
    client.BaseAddress = new Uri("http://webapi");
    client.DefaultRequestVersion = System.Net.HttpVersion.Version11;
    client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionExact;
})
.AddHttpMessageHandler<UTB.Minute.AdminClient.Services.TokenHandler>()
.AddServiceDiscovery();

builder.Services.AddHttpClient<IMenuClient, MenuClient>(client =>
{
    client.BaseAddress = new Uri("http://webapi");
    client.DefaultRequestVersion = System.Net.HttpVersion.Version11;
    client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionExact;
})
.AddHttpMessageHandler<UTB.Minute.AdminClient.Services.TokenHandler>()
.AddServiceDiscovery();

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

app.MapRazorComponents<UTB.Minute.AdminClient.Components.App>()

    .AddInteractiveServerRenderMode();

app.MapGet("/login", () => 
{
    var props = new Microsoft.AspNetCore.Authentication.AuthenticationProperties 
    { 
        RedirectUri = "/" 
    };
    props.Items.Add("prompt", "login"); // Force login screen every time
    return Results.Challenge(props, [OpenIdConnectDefaults.AuthenticationScheme]);
});
app.MapGet("/logout", () => Results.SignOut(new Microsoft.AspNetCore.Authentication.AuthenticationProperties { RedirectUri = "/" }, [CookieAuthenticationDefaults.AuthenticationScheme, OpenIdConnectDefaults.AuthenticationScheme]));

app.Run();
