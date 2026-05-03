namespace CRMS.API.DTOs;

// ─── Auth ──────────────────────────────────────────────────────────────────

public record RegisterRequest(
    string Username,
    string Password,
    string FullName,
    string Email,
    string Phone
);

public record UserResponse(
    int Id,
    string Username,
    string Role,
    string FullName,
    string Email,
    string Phone,
    DateTime CreatedAt
);

// ─── Cars ──────────────────────────────────────────────────────────────────

public record CarResponse(
    int Id,
    string Make,
    string Model,
    int Year,
    string Category,
    decimal DailyRate,
    string LicencePlate,
    string Colour,
    string Status
);

public record CreateCarRequest(
    string Make,
    string Model,
    int Year,
    string Category,
    decimal DailyRate,
    string LicencePlate,
    string Colour,
    string Status
);

public record UpdateCarRequest(
    string Make,
    string Model,
    int Year,
    string Category,
    decimal DailyRate,
    string LicencePlate,
    string Colour,
    string Status
);

// ─── Bookings ──────────────────────────────────────────────────────────────

public record CreateBookingRequest(
    int CarId,
    DateTime PickupDate,
    DateTime ReturnDate
);

public record BookingResponse(
    int Id,
    int CustomerId,
    string CustomerName,
    int CarId,
    string CarDescription,
    DateTime PickupDate,
    DateTime ReturnDate,
    decimal TotalAmount,
    string Status,
    int? ApprovedById,
    string? ApprovedByName,
    DateTime CreatedAt
);