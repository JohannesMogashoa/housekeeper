using HouseKeeper.Web;
using HouseKeeper.Web.Services;

using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

string apiBaseUrl = builder.Configuration["ApiBaseUrl"]
    ?? throw new InvalidOperationException("ApiBaseUrl configuration is required.");

builder.Services.AddScoped(_ => new HttpClient
{
    BaseAddress = new Uri(apiBaseUrl, UriKind.Absolute)
});
builder.Services.AddScoped<DevelopmentSession>();
builder.Services.AddScoped<HouseKeeperApiClient>();

await builder.Build().RunAsync();
