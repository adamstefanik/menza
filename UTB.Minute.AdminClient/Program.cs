using UTB.Minute.AdminClient.Services;
using UTB.Minute.AdminClient.Components;

var builder = WebApplication.CreateBuilder(args);

// ⬇️ TOTO PRIDAJ - Aspire service defaults (obsahuje service discovery)
builder.AddServiceDefaults();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// ⬇️ TOTO PRIDAJ - explicitne service discovery
builder.Services.AddServiceDiscovery();

builder.Services.AddHttpClient<IMealClient, MealClient>(client =>
{
    client.BaseAddress = new Uri("https://webapi");
})
.AddServiceDiscovery()
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    // ⬇️ TOTO - len pre development!
    ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
});
builder.Services.AddHttpClient<IOrderClient, OrderClient>(client =>
{
    client.BaseAddress = new Uri("https://webapi");
})
.AddServiceDiscovery()
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
});
builder.Services.AddHttpClient<IMenuClient, MenuClient>(client =>
{
    client.BaseAddress = new Uri("https://webapi");
})
.AddServiceDiscovery()
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
});
var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<UTB.Minute.AdminClient.Components.App>()
    .AddInteractiveServerRenderMode();

app.Run();