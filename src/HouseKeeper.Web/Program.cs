using HouseKeeper.Web;
using HouseKeeper.Web.Services;

using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.Components.Authorization;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

string apiBaseUrl = builder.Configuration["ApiBaseUrl"]
    ?? throw new InvalidOperationException("ApiBaseUrl configuration is required.");

builder.Services.AddScoped(_ => new HttpClient
{
    BaseAddress = new Uri(apiBaseUrl, UriKind.Absolute)
});
string authenticationMode = builder.Configuration["Authentication:Mode"] ?? "Development";
builder.Services.AddAuthorizationCore();

if (string.Equals(authenticationMode, "Development", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddScoped<DevelopmentSession>();
    builder.Services.AddScoped<AuthenticationStateProvider, DevelopmentAuthenticationStateProvider>();
    builder.Services.AddScoped<IApiAuthentication, DevelopmentApiAuthentication>();
}
else
{
    builder.Services.AddOidcAuthentication(options =>
    {
        builder.Configuration.Bind("Authentication:Cognito", options.ProviderOptions);
        options.ProviderOptions.ResponseType = "code";
        options.ProviderOptions.DefaultScopes.Add("openid");
        options.ProviderOptions.DefaultScopes.Add("email");
        options.ProviderOptions.DefaultScopes.Add("profile");
        options.ProviderOptions.DefaultScopes.Add("housekeeper-api/read");
        options.ProviderOptions.DefaultScopes.Add("housekeeper-api/write");
    });
    builder.Services.AddScoped<IApiAuthentication, CognitoApiAuthentication>();
}

builder.Services.AddScoped<HouseKeeperApiClient>();

await builder.Build().RunAsync();
