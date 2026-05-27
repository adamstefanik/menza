using UTB.Minute.CanteenClient.Components;
using UTB.Minute.CanteenClient.Services;
using UTB.Minute.Contracts;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddServiceDiscovery();

// IMealClient a MealClient sú v AdminClient - to je zlé, mal by byť v CanteenClient
// Alebo ak ich používaš, nechaj tak. Ak nie, odstráň tieto riadky:

// builder.Services.AddHttpClient<IMealClient, MealClient>(client =>
// {
//     client.BaseAddress = new Uri("http://webapi:5132");
// }).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
// {
//     ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
// });

builder.Services.AddHttpClient<IOrderClient, OrderClient>(client =>
{
    client.BaseAddress = new Uri("https://webapi");
})
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

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();