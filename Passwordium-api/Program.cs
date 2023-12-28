using Microsoft.EntityFrameworkCore;
using Passwordium_api.Data;
using Passwordium_api.Services;

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
builder.Services.AddSwaggerGen();

var app = builder.Build();

#region Development toggles for app
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
#endregion

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
