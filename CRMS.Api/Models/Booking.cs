namespace CRMS.Api.Models;

public class Booking
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public int CarId { get; set; }

    public DateTime PickupDate { get; set; }
    public DateTime ReturnDate { get; set; }

    public decimal TotalAmount { get; set; }

    public string Status { get; set; } = "Pending";

    public int? ApprovedById { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}