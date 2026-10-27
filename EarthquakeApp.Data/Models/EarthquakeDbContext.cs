using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace EarthquakeApp.Data.Models;

public partial class EarthquakeDbContext : DbContext
{
    public EarthquakeDbContext(DbContextOptions<EarthquakeDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Earthquake> Earthquakes { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Earthquake>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Earthqua__3214EC07F1B7010A");

            entity.Property(e => e.Id)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Place).HasMaxLength(255);
            entity.Property(e => e.Url).HasMaxLength(500);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
