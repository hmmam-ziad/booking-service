using BookingService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace BookingService.Infrastructure.Persistence.Configurations
{
    public class BookingConfiguration : IEntityTypeConfiguration<Booking>
    {
        public void Configure(EntityTypeBuilder<Booking> builder)
        {
            builder.ToTable("Bookings");

            builder.HasKey(b => b.Id);

            builder.Property(b => b.ResourceId)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(b => b.UserId)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(b => b.StartDateTime)
                .IsRequired();

            builder.Property(b => b.EndDateTime)
                .IsRequired();

            builder.Property(b => b.Status)
                .IsRequired()
                .HasConversion<string>()   // enum stored as string - readable in DB, safe if enum order changes
                .HasMaxLength(20);

            builder.Property(b => b.CreatedAt)
                .IsRequired();

            // Critical index: every overlap check filters by ResourceId + Status
            // and range-scans on the date columns. This is the #1 query in the system.
            builder.HasIndex(b => new { b.ResourceId, b.Status, b.StartDateTime, b.EndDateTime })
                .HasDatabaseName("IX_Bookings_ResourceId_Status_DateRange");
        }
    }
}
