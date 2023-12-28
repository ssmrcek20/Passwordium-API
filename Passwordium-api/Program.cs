using Microsoft.EntityFrameworkCore;
using Passwordium_api.Data;

var builder = WebApplication.CreateBuilder(args);

#region Database
builder.Services.AddDbContext<DatabaseContext>(options => 
    options.UseNpgsql(builder.Configuration["DatabaseConnectionString"] 
        ?? throw new InvalidOperationException("Connection string 'DatabaseContext' not found.")
    ));
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
