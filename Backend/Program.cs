using Backend.Data;
using Backend.Models;
using Backend.Seed;
using Microsoft.AspNetCore.Identity; 
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    //Setari de verificare a parolei pentru utilizatorii creati
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireUppercase = true;
    options.Password.RequiredLength = 6;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();


//Legatura dintre .net si react pe porturi diferite
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReact",
        policy => policy.WithOrigins("http://localhost:5173")
                          .AllowAnyMethod()
                          .AllowAnyHeader());
});

builder.Services.AddControllers();

//Tool pentru a testa api-urile in backend
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
// ==========================================

var app = builder.Build();

//Activeaza tool-ul de testare a api-urilor doar in development
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        // Call the static seed method
        await SeedData.SeedUsersAsync(services);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}


app.UseCors("AllowReact");

app.UseAuthentication();
app.UseAuthorization();


app.MapControllers();

app.Run();