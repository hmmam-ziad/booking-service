using Microsoft.EntityFrameworkCore;

namespace BookingService.Api.Extensions
{
    public static class WebApplicationExtensions
    {
        /// <summary>
        /// Applies any pending EF Core migrations at startup.
        /// Convenient for local/dev; in production this is usually
        /// done via a separate deployment step instead.
        /// </summary>
        public static async Task ApplyMigrationsAsync(this WebApplication app)
        {
            using var scope = app.Services.CreateScope();
            var db = scope.ServiceProvider
                .GetRequiredService<BookingService.Infrastructure.Persistence.BookingDbContext>();

            await db.Database.MigrateAsync();
        }
    }
}
