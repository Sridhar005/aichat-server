using AIChatApp.Data;
using AIChatApp.Services;
using DotNetEnv;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

Env.Load(".env");
var builder = WebApplication.CreateBuilder(args);

//builder.Services.AddCors(options =>
//{
//    options.AddPolicy("React", policy =>
//    {
//        policy.WithOrigins(
//            "https://aichat-client-woad.vercel.app",
//            "http://localhost:5173",
//            "http://localhost:5174"
//            )
//              .AllowAnyHeader()
//              .AllowAnyMethod()
//              .AllowCredentials();
//    });
//});

builder.Services.AddCors(options =>
{
    options.AddPolicy("React", policy =>
    {
        policy
            // ✅ Allow ALL Vercel deployments for your frontend
            .SetIsOriginAllowed(origin =>
                origin.StartsWith("https://aichat-client") ||
                origin.StartsWith("http://localhost"))
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

builder.Services.AddControllers(options =>
{
    var policy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();

    options.Filters.Add(
        new Microsoft.AspNetCore.Mvc.Authorization.AuthorizeFilter(policy)
    );
});
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")
    )
);
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<ChatService>();
builder.Services.AddScoped<CacheService>();

builder.Services.AddHttpClient<GeminiClient>();
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = "localhost:6379";
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)
            ),

            ValidateIssuer = true,
            ValidateAudience = true,

            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],

            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var token = context.Request.Cookies["AuthToken"];

                if (!string.IsNullOrEmpty(token))
                {
                    context.Token = token;
                }

                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();
var app = builder.Build();


app.UseMiddleware<ExceptionMiddleware>();

app.UseCors("React");
app.UseAuthentication();
app.UseAuthorization();
app.MapGet("/health", () => Results.Ok("Healthy"));
app.MapControllers();

app.Run();
