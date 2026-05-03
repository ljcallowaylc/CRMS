using Microsoft.EntityFrameworkCore;
using CRMS.API.Models;
using System.Security.Cryptography;
using System.Text;

namespace CRMS.API.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Car> Cars => Set<Car>();
    public DbSet<Booking> Bookings => Set<Booking>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Unique constraints
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Username)
            .IsUnique();

        modelBuilder.Entity<Car>()
            .HasIndex(c => c.LicencePlate)
            .IsUnique();

        // Booking → Customer (restrict delete so we don't lose booking history)
        modelBuilder.Entity<Booking>()
            .HasOne(b => b.Customer)
            .WithMany(u => u.Bookings)
            .HasForeignKey(b => b.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        // Booking → ApprovedBy (no cascade)
        modelBuilder.Entity<Booking>()
            .HasOne(b => b.ApprovedBy)
            .WithMany(u => u.ApprovedBookings)
            .HasForeignKey(b => b.ApprovedById)
            .OnDelete(DeleteBehavior.Restrict);

        // Seed users: one of each role
        modelBuilder.Entity<User>().HasData(
            new User
            {
                Id = 1,
                Username = "customer1",
                PasswordHash = Hash("password123"),
                Role = "Customer",
                FullName = "Alice Johnson",
                Email = "alice@example.com",
                Phone = "555-0101",
                CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new User
            {
                Id = 2,
                Username = "staff1",
                PasswordHash = Hash("password123"),
                Role = "Staff",
                FullName = "Bob Smith",
                Email = "bob@example.com",
                Phone = "555-0102",
                CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new User
            {
                Id = 3,
                Username = "admin1",
                PasswordHash = Hash("password123"),
                Role = "Admin",
                FullName = "Carol White",
                Email = "carol@example.com",
                Phone = "555-0103",
                CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            }
        );

        // Seed some cars
        modelBuilder.Entity<Car>().HasData(
            new Car { Id = 1, Make = "Toyota", Model = "Camry", Year = 2022, Category = "Sedan", DailyRate = 55.00m, LicencePlate = "ABC-1001", Colour = "Silver", Status = "Available" },
            new Car { Id = 2, Make = "Ford", Model = "Explorer", Year = 2023, Category = "SUV", DailyRate = 85.00m, LicencePlate = "ABC-1002", Colour = "Black", Status = "Available" },
            new Car { Id = 3, Make = "Honda", Model = "Civic", Year = 2021, Category = "Sedan", DailyRate = 45.00m, LicencePlate = "ABC-1003", Colour = "Blue", Status = "Available" },
            new Car { Id = 4, Make = "Chrysler", Model = "Pacifica", Year = 2022, Category = "Van", DailyRate = 95.00m, LicencePlate = "ABC-1004", Colour = "White", Status = "Available" },
            new Car { Id = 5, Make = "Tesla", Model = "Model 3", Year = 2023, Category = "Sedan", DailyRate = 110.00m, LicencePlate = "ABC-1005", Colour = "Red", Status = "Available" }
        );
    }

    private static string Hash(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).ToLower();
    }
}