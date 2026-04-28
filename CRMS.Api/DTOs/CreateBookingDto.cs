using System;
namespace CRMS.Api.DTOs;

public class CreateBookingDto
{
    public int CarId { get; set; }
    public DateTime PickupDate { get; set; }
    public DateTime ReturnDate { get; set; }
}