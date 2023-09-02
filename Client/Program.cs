using System.Text.Json;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.FeatureManagement;
using Viewer.Client;
using MudBlazor.Services;
using Viewer.Client.ServiceClients;
using Viewer.Shared;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services
    .AddHttpClient("api", c => c.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress))
    .AddHttpMessageHandler(sp => sp.GetRequiredService<AuthHandler>());
builder.Services
    .AddHttpClient("direct", c => c.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress));
builder.Services.AddFeatureManagement(builder.Configuration.GetSection("FeatureManagement"));
builder.Services.AddMudServices();
builder.Services.AddTransient<TokenHandler>();
builder.Services.AddScoped<Cart>();
builder.Services.AddScoped<ImagePopUpProvider>();
builder.Services.AddScoped<IAuthClient, AuthClient>();
builder.Services.AddScoped<AuthHandler>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp => (AuthClient)sp.GetRequiredService<IAuthClient>());
builder.Services.AddScoped<IImageClient, ImageClient>();
builder.Services.AddScoped<UserClient>();
builder.Services.AddBlazoredLocalStorageAsSingleton(o => o.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase);
builder.Services.AddAuthorizationCore();

await builder.Build().RunAsync();