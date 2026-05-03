using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRMS.API.Models;

public class Booking
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int CustomerId { get; set; }

    [Required]
    public int CarId { get; set; }

    public DateTime PickupDate { get; set; }

    public DateTime ReturnDate { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal TotalAmount { get; set; }

    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "Pending"; // Pending | Approved | Active | Completed | Cancelled | Rejected

    public int? ApprovedById { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    [ForeignKey("CustomerId")]
    public User Customer { get; set; } = null!;

    [ForeignKey("CarId")]
    public Car Car { get; set; } = null!;

    [ForeignKey("ApprovedById")]
    public User? ApprovedBy { get; set; }
}