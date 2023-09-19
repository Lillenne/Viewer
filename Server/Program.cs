using System.IdentityModel.Tokens.Jwt;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Viewer.Server.Services;
using Viewer.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Viewer.Server;
using Viewer.Server.Configuration;
using Viewer.Server.Models;
using Viewer.Server.Services.AuthServices;
using Viewer.Server.Services.Email;
using Viewer.Server.Services.ImageServices;
using Viewer.Server.Services.UserServices;
using MinioImageClient = Viewer.Server.Services.ImageServices.MinioImageClient;

[assembly:InternalsVisibleTo("Viewer.Tests")]

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;

// Add services to the container.

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

// ??
builder.Services.AddEndpointsApiExplorer();

// MassTransit
builder.Services.AddMassTransit(x =>
{
    x.AddConsumers(Assembly.GetEntryAssembly());
    x.AddDelayedMessageScheduler();
    
    x.UsingInMemory((ctx, cfg) =>
    {
        cfg.UseDelayedMessageScheduler();
        cfg.ConfigureEndpoints(ctx);
    });
});

// MinIO
builder.Services.AddOptions<MinioOptions>()
    .Bind(builder.Configuration.GetSection("Minio"))
    .ValidateDataAnnotations();
builder.Services.AddTransient<MinioImageClient>();
builder.Services.AddScoped<IImageService, MinioImageService>();

// Jwt
builder.Services.AddScoped<IClaimsParser,JwtClaimsParser>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.Configure<JwtOptions>(config.GetSection("JwtSettings"));
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
        o.SaveToken = true; // TODO ?
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
            NameClaimType = JwtRegisteredClaimNames.Name,
        };
        o.Events = new JwtBearerEvents()
        {
            OnAuthenticationFailed = ctx =>
            {
                if (ctx.Exception is SecurityTokenExpiredException)
                    ctx.Response.Headers.Add(Constants.TokenExpiredHeader, Constants.TokenExpiredValue);
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization(o =>
{
    o.AddPolicy(Policies.UploadPolicy, policy => policy.RequireAuthenticatedUser().RequireRole(Roles.Upload));
    o.AddPolicy(Policies.DownloadPolicy, policy => policy.RequireAuthenticatedUser().RequireRole(Roles.Download));
    o.AddPolicy(Policies.AuthenticatedPolicy, policy => policy.RequireAuthenticatedUser());
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

// Databases
builder.Services.AddDbContext<DataContext>(
    o => o.UseNpgsql(builder.Configuration.GetConnectionString("viewer_users")));
builder.Services.AddScoped<DataContext>();
builder.Services.AddScoped<IUserRepository>(sp => sp.GetRequiredService<DataContext>());
builder.Services.AddScoped<IUploadRepository>(sp => sp.GetRequiredService<DataContext>());
builder.Services.AddScoped<ITokenRepository>(sp => sp.GetRequiredService<DataContext>());

// Misc DI
builder.Services.AddScoped<Cart>();
builder.Services.AddTransient<IFriendSuggestor, FirstInDbSuggestor>();
builder.Services.AddTransient<ITokenService, JwtTokenService>();

// Email
builder.Services.AddTransient<EmailClient>();
builder.Services.AddOptions<EmailOptions>()
    .Bind(builder.Configuration.GetSection("Email"))
    .ValidateDataAnnotations();


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

// Ensure database creation
using (var scope = app.Services.CreateScope())
{
    using var db = scope.ServiceProvider.GetRequiredService<DataContext>();
    db.Database.EnsureCreated();
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
