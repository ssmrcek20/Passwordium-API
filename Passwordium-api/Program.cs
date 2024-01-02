using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Passwordium_api.Data;
using Passwordium_api.Services;
using Passwordium_api.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

#region Database
builder.Services.AddDbContext<DatabaseContext>(options => 
    options.UseNpgsql(builder.Configuration["DatabaseConnectionString"] 
        ?? throw new InvalidOperationException("Connection string 'DatabaseContext' not found.")
    ));
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
        options.TokenValidationParameters = new TokenValidationParameters
        {
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JWT:Key"])),
            ValidateIssuerSigningKey = true,
            ValidateIssuer = false,
            ValidateAudience = false
        };
    })
    .AddJwtBearer("NoExpiryCheck", options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JWT:Key"])),
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
