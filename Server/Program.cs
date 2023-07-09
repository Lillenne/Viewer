using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Viewer.Server.Controllers;
using Viewer.Server.Services;
using Viewer.Server.Models;
using Viewer.Shared;
using Microsoft.AspNetCore.Authorization;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;

// Add services to the container.

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
builder.Services.Configure<JwtOptions>(config.GetSection("JwtSettings"));
builder.Services.Configure<MinioOptions>(config.GetSection("Minio"));
builder.Services
    .AddAuthentication(o =>
    {
        o.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        o.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        o.DefaultForbidScheme = JwtBearerDefaults.AuthenticationScheme;
        o.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(o =>
    {
        o.TokenValidationParameters = new TokenValidationParameters()
        {
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(config["JwtSettings:Key"]!)
            ),
            ValidIssuer = config["JwtSettings:Issuer"],
            ValidAudience = config["JwtSettings:Audience"],
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = false, // TODO
            ValidateIssuerSigningKey = true,
        };
    });

builder.Services.AddAuthorization(o =>
{
    o.DefaultPolicy = new AuthorizationPolicyBuilder(JwtBearerDefaults.AuthenticationScheme).RequireAuthenticatedUser().Build();
});

builder.Services.AddCors(
    o =>
        o.AddPolicy(
            "local",
            //policy => policy.WithOrigins("http://localhost*").AllowAnyMethod().AllowAnyHeader()
            policy => policy.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin()
        )
);

builder.Services.AddScoped<Cart>();
builder.Services.AddSingleton<IImageService, AppFileImageService>();
//builder.Services.AddSingleton<IImageService, MinioImageService>();
//builder.Services.AddSingleton<IImageService, ImageServiceStub>();
builder.Services.AddSingleton<IAuthService, JwtAuthService>();
builder.Services.AddSingleton<IUserRepository, UserContext>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<IUserRepository>() as UserContext;
    db?.Database.EnsureCreated();
}

app.UseHttpsRedirection();

app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();
app.MapControllers();
app.MapFallbackToFile("index.html");

app.Run();
