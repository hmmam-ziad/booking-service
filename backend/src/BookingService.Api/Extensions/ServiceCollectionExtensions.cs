using BookingService.Application.Interfaces;
using BookingService.Infrastructure.Persistence;
using BookingService.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace BookingService.Api.Extensions
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Registers EF Core DbContext + repository/service implementations.
        /// </summary>
        public static IServiceCollection AddBookingServiceInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddDbContext<BookingDbContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

            services.AddScoped<IBookingRepository, BookingRepository>();
            // I used `BookingService.Application.Services.BookingService` because the project name matches the class name.
            // u can change the class name to `BookingManagmentService` or something else if you want to avoid confusion.
            services.AddScoped<IBookingService, BookingService.Application.Services.BookingService>();
            services.AddScoped<IUnitOfWork, EfUnitOfWork>();

            return services;
        }

        /// <summary>
        /// Registers CORS policy for the Next.js frontend.
        /// </summary>
        public static IServiceCollection AddFrontendCors(this IServiceCollection services)
        {
            services.AddCors(options =>
            {
                options.AddPolicy("AllowFrontend", policy =>
                {
                    policy.WithOrigins("http://localhost:3000", "https://localhost:3000")
                          .AllowAnyHeader()
                          .AllowAnyMethod();
                });
            });

            return services;
        }
    }
}
