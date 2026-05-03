using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRMS.API.Models;

public class Car
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Make { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Model { get; set; } = string.Empty;

    public int Year { get; set; }

    [Required]
    [MaxLength(50)]
    public string Category { get; set; } = string.Empty; // Sedan, SUV, Van, etc.

    [Column(TypeName = "decimal(10,2)")]
    public decimal DailyRate { get; set; }

    [Required]
    [MaxLength(20)]
    public string LicencePlate { get; set; } = string.Empty;

    [MaxLength(50)]
    public string Colour { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "Available"; // Available | Rented | Maintenance

    // Navigation
    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}