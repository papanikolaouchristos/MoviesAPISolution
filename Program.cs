using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using MoviesAPI.Data;
using MoviesAPI.DTOs;

var builder = WebApplication.CreateBuilder(args);



builder.Services.AddControllers();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.Configure<EmailSettings>(
    builder.Configuration.GetSection("EmailSettings"));


builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular",
        policy =>
        {
            policy
                .WithOrigins( "http://localhost:4200")
                .AllowAnyHeader()
                .AllowAnyMethod();
        });
});

var app = builder.Build();

var uploadsPath = Path.Combine(AppContext.BaseDirectory, "wwwroot", "uploads");

if (!Directory.Exists(uploadsPath))
{
    Directory.CreateDirectory(uploadsPath);
}

var projectUploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(projectUploadsPath),
    RequestPath = "/uploads"
});



    app.UseSwagger();
    app.UseSwaggerUI();


app.UseCors("AllowAngular");
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAuthorization();


app.MapControllers();

app.Run();
