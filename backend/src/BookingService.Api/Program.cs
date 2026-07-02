using BookingService.Application.Interfaces;
using BookingService.Infrastructure.Persistence;
using BookingService.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddDbContext<BookingDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Application & Infrastructure DI
builder.Services.AddScoped<IBookingRepository, BookingRepository>();
// I used `BookingService.Application.Services.BookingService` because the project name matches the class name.
// u can change the class name to `BookingManagmentService` or something else if you want to avoid confusion.
builder.Services.AddScoped<IBookingService, BookingService.Application.Services.BookingService>();

// CORS for the Next.js frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseCors("AllowFrontend");

app.MapControllers();

app.Run();