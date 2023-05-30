using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Viewer.Client;
using MudBlazor.Services;
using Viewer.Client.ServiceClients;
using Viewer.Shared;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddMudServices();
builder.Services.AddScoped<Cart>();
builder.Services.AddScoped<IAuthClient, AuthClient>();
builder.Services.AddScoped<IImageClient, ImageClient>();
builder.Services.AddScoped<AuthenticationStateProvider, AuthProvider>();
builder.Services.AddAuthorizationCore();

await builder.Build().RunAsync();
