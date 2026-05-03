using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
 
namespace CRMS.API.Models;
 
public class User
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
 
    [Required]
    [MaxLength(100)]
    public string Username { get; set; } = string.Empty;
 
    [Required]
    [MaxLength(256)]
    public string PasswordHash { get; set; } = string.Empty;
 
    [Required]
    [MaxLength(50)]
    public string Role { get; set; } = "Customer"; // Customer | Staff | Admin
 
    [Required]
    [MaxLength(150)]
    public string FullName { get; set; } = string.Empty;
 
    [Required]
    [MaxLength(150)]
    public string Email { get; set; } = string.Empty;
 
    [MaxLength(20)]
    public string Phone { get; set; } = string.Empty;
 
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
 
    // Navigation
    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    public ICollection<Booking> ApprovedBookings { get; set; } = new List<Booking>();
}