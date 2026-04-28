namespace CRMS.Api.Models;

public class Car
{
    public int Id { get; set; }
    public string Make { get; set; } = "";
    public string Model { get; set; } = "";
    public int Year { get; set; }
    public string Category { get; set; } = "";
    public decimal DailyRate { get; set; }
    public string LicencePlate { get; set; } = "";
    public string Colour { get; set; } = "";
    public string Status { get; set; } = "Available";
}