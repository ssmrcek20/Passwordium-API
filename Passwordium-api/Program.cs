using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Passwordium_api;
using Passwordium_api.Data;
using Passwordium_api.Services;
using Passwordium_api.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

#region Database
string databaseKey = Environment.GetEnvironmentVariable("DatabaseConnectionString");

if(databaseKey == null)
{
    builder.Services.AddDbContext<DatabaseContext>(options => 
        options.UseNpgsql(builder.Configuration["DatabaseConnectionString"] 
            ?? throw new InvalidOperationException("Connection string 'DatabaseContext' not found.")
        ));
}
else
{
    builder.Services.AddDbContext<DatabaseContext>(options => 
           options.UseNpgsql(databaseKey));
}
#endregion

#region JWT Key
builder.Services.AddSingleton<AppConfiguration>(provider =>
{
    var jwtKey = Environment.GetEnvironmentVariable("JWT-Key") ?? builder.Configuration["JWT:Key"];
    if (jwtKey == null)
    {
        throw new InvalidOperationException("JWT key not found.");
    }

    return new AppConfiguration { JwtKey = jwtKey };
});
#endregion

#region Services
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<HashService>();
builder.Services.AddScoped<TokenService>();
#endregion

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

#region Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var appConfiguration = builder.Services.BuildServiceProvider().GetService<AppConfiguration>();
        if (appConfiguration == null || string.IsNullOrEmpty(appConfiguration.JwtKey))
        {
            throw new InvalidOperationException("JWT key not configured.");
        }

        options.TokenValidationParameters = new TokenValidationParameters
        {
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(appConfiguration.JwtKey)),
            ValidateIssuerSigningKey = true,
            ValidateIssuer = false,
            ValidateAudience = false
        };
    })
    .AddJwtBearer("NoExpiryCheck", options =>
    {
        var appConfiguration = builder.Services.BuildServiceProvider().GetService<AppConfiguration>();
        if (appConfiguration == null || string.IsNullOrEmpty(appConfiguration.JwtKey))
        {
            throw new InvalidOperationException("JWT key not configured.");
        }

        options.TokenValidationParameters = new TokenValidationParameters
        {
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(appConfiguration.JwtKey)),
            ValidateIssuerSigningKey = true,
            ValidateIssuer = false,
            ValidateAudience = false,

            ValidateLifetime = false
        };
    });
#endregion

#region Swagger
builder.Services.AddSwaggerGen();
builder.Services.AddTransient<IConfigureOptions<SwaggerGenOptions>, ConfigureSwaggerOptions>();
#endregion

var app = builder.Build();

#region Development toggles for app
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
#endregion

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
