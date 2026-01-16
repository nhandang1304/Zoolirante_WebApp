using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Zoolirante_Open_Minded.Models;

namespace Zoolirante_Open_Minded.Models;

public partial class ZooliranteDatabaseContext : DbContext
{
    public ZooliranteDatabaseContext()
    {
    }

    public ZooliranteDatabaseContext(DbContextOptions<ZooliranteDatabaseContext> options)
        : base(options)
    {
    }
    public DbSet<EventAnimal> EventAnimal {  get; set; }
    public DbSet<PendingEmail> PendingEmails { get; set; }
    public virtual DbSet<EntranceTicket> EntranceTicket { get; set; }
    
    public virtual  DbSet<AnimalFavourite> AnimalFavourite { get; set; }

    public virtual DbSet<Animal> Animals { get; set; }

    public virtual DbSet<Event> Events { get; set; }

    public virtual DbSet<Merchandise> Merchandises { get; set; }

    public virtual DbSet<Order> Orders { get; set; }

    public virtual DbSet<OrderItem> OrderItems { get; set; }

    public virtual DbSet<PickupLocation> PickupLocations { get; set; }

    public virtual DbSet<User> Users { get; set; }

	public virtual DbSet<Membership> Memberships { get; set; }

    public virtual DbSet<UserDetail> UserDetails { get; set; }




    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Data Source=LAPTOP-F352A8E7\\SQLEXPRESS;Initial Catalog=ZooliranteDB;Integrated Security=True;Encrypt=False;Trust Server Certificate=True");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {

        modelBuilder.Entity<EventAnimal>()
        .HasKey(ea => ea.Id);

        modelBuilder.Entity<EventAnimal>()
            .HasOne(ea => ea.Event)
            .WithOne(e => e.EventAnimal)
            .HasForeignKey<EventAnimal>(ea => ea.EventId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<UserDetail>(entity =>
        {
            entity.ToTable("UserDetail");
            entity.HasKey(e => e.UserDetailId);
            entity.HasIndex(e => e.UserId).IsUnique();

            entity.Property(e => e.FirstName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.MiddleName).HasMaxLength(100);
            entity.Property(e => e.LastName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Phone).HasMaxLength(30);
            entity.Property(e => e.Street).HasMaxLength(150);

            entity.HasOne(d => d.User)
                  .WithOne(p => p.UserDetail)                 
                  .HasForeignKey<UserDetail>(d => d.UserId)
                  .OnDelete(DeleteBehavior.Cascade)
                  .HasConstraintName("FK_UserDetail_User");
        });

        modelBuilder.Entity<Animal>(entity =>
        {
            entity.HasKey(e => e.AnimalId).HasName("PK__Animals__A21A730703C3EA3B");

            entity.HasIndex(e => e.Name, "IX_Animals_Name");

            entity.HasIndex(e => e.Region, "IX_Animals_Region");

            entity.HasIndex(e => e.Species, "IX_Animals_Species");

            entity.Property(e => e.ConservationStatus).HasMaxLength(20);
            entity.Property(e => e.ExhibitLocation).HasMaxLength(100);
            entity.Property(e => e.Habitat).HasMaxLength(100);
            entity.Property(e => e.ImageUrl).HasMaxLength(255);
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.Region).HasMaxLength(50);
            entity.Property(e => e.Species).HasMaxLength(100);
        });

        modelBuilder.Entity<Event>(entity =>
        {
            entity.HasKey(e => e.EventId).HasName("PK__Events__7944C81096ED2C0C");

            entity.HasIndex(e => e.StartTime, "IX_Events_StartTime");

            entity.Property(e => e.EndTime).HasPrecision(0);
            entity.Property(e => e.Location).HasMaxLength(100);
            entity.Property(e => e.Price).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.StartTime).HasPrecision(0);
            entity.Property(e => e.Title).HasMaxLength(150);

            entity.HasMany(d => d.Animals).WithMany(p => p.Events)
                .UsingEntity<Dictionary<string, object>>(
                    "EventAnimal",
                    r => r.HasOne<Animal>().WithMany()
                        .HasForeignKey("AnimalId")
                        .HasConstraintName("FK_EventAnimals_Animal"),
                    l => l.HasOne<Event>().WithMany()
                        .HasForeignKey("EventId")
                        .HasConstraintName("FK_EventAnimals_Event"),
                    j =>
                    {
                        j.HasKey("EventId", "AnimalId");
                        j.ToTable("EventAnimals");
                    });
        });

        modelBuilder.Entity<Merchandise>(entity =>
        {
            entity.HasKey(e => e.ProductId).HasName("PK__Merchand__B40CC6CDE5E5113D");

            entity.ToTable("Merchandise");

            entity.HasIndex(e => e.Category, "IX_Merch_Category");

            entity.HasIndex(e => e.Name, "IX_Merch_Name");

            entity.Property(e => e.Category).HasMaxLength(50);
            entity.Property(e => e.ImageUrl).HasMaxLength(255);
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.Price).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.SpecialPrice).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.SpecialReason).HasMaxLength(200);
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.OrderId).HasName("PK__Orders__C3905BCFD539484F");

            entity.Property(e => e.OrderDate).HasDefaultValueSql("(sysdatetime())");
            
            entity.Property(e => e.Items)
                    .HasColumnType("varchar(max)")
                    .IsRequired(false);
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasDefaultValue("Pending");
            entity.Property(e => e.TotalAmount).HasColumnType("decimal(10, 2)");

            entity.HasOne(d => d.PickupLocation).WithMany(p => p.Orders)
                .HasForeignKey(d => d.PickupLocationId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Orders_PickupLocation");

            entity.HasOne(d => d.User).WithMany(p => p.Orders)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Orders_User");
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.HasKey(e => e.OrderItemId).HasName("PK__OrderIte__57ED0681C34FB9DC");

            entity.Property(e => e.UnitPrice).HasColumnType("decimal(10, 2)");

            entity.HasOne(d => d.Order).WithMany(p => p.OrderItems)
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OrderItems_Order");

            entity.HasOne(d => d.Product).WithMany(p => p.OrderItems)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OrderItems_Product");
        });

        modelBuilder.Entity<PickupLocation>(entity =>
        {
            entity.HasKey(e => e.PickupLocationId).HasName("PK__PickupLo__F6FC9D68744AAE45");

            entity.ToTable("PickupLocations");

            entity.Property(e => e.Name).HasMaxLength(150);
            entity.Property(e => e.OpenFrom).HasPrecision(0);
            entity.Property(e => e.OpenTo).HasPrecision(0);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__Users__1788CC4C7BEABE7C");

            entity.HasIndex(e => e.Email, "UQ__Users__A9D10534D8A3AF50").IsUnique();

            entity.Property(e => e.Email).HasMaxLength(150);
            entity.Property(e => e.FullName).HasMaxLength(150);
            entity.Property(e => e.PasswordHash).HasMaxLength(255);
            entity.Property(e => e.Role)
                .HasMaxLength(50)
                .HasDefaultValue("Customer");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);

}
