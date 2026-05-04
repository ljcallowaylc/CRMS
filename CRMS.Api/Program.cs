using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using CRMS.API.Auth;
using CRMS.API.Data;
using CRMS.API.DTOs;
using CRMS.API.Models;

var builder = WebApplication.CreateBuilder(args);

// ── Database ────────────────────────────────────────────────────────────────
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ── Auth ────────────────────────────────────────────────────────────────────
builder.Services.AddAuthentication("BasicAuth")
    .AddScheme<AuthenticationSchemeOptions, BasicAuthHandler>("BasicAuth", null);

builder.Services.AddAuthorization();

// ── Swagger ─────────────────────────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Car Rental Management System API",
        Version = "v1",
        Description = "CRMS REST API — TECH 4263 Semester Project"
    });

    c.AddSecurityDefinition("basicAuth", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "basic",
        Description = "Enter username and password"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "basicAuth" }
            },
            Array.Empty<string>()
        }
    });
});

// ── CORS (allow GUI clients) ─────────────────────────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

var app = builder.Build();

// ── Middleware ───────────────────────────────────────────────────────────────
app.UseCors();
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "CRMS API v1");
    c.RoutePrefix = "swagger";
});

app.UseAuthentication();
app.UseAuthorization();

// ── Auto-migrate on startup ──────────────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

// ════════════════════════════════════════════════════════════════════════════
//  HELPERS
// ════════════════════════════════════════════════════════════════════════════

static int GetUserId(ClaimsPrincipal user) =>
    int.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);

static string GetUserRole(ClaimsPrincipal user) =>
    user.FindFirstValue(ClaimTypes.Role)!;

static BookingResponse ToBookingResponse(Booking b) => new(
    b.Id,
    b.CustomerId,
    b.Customer?.FullName ?? "",
    b.CarId,
    b.Car != null ? $"{b.Car.Make} {b.Car.Model} ({b.Car.Year}) - {b.Car.LicencePlate}" : "",
    b.PickupDate,
    b.ReturnDate,
    b.TotalAmount,
    b.Status,
    b.ApprovedById,
    b.ApprovedBy?.FullName,
    b.CreatedAt
);

static CarResponse ToCarResponse(Car c) => new(
    c.Id, c.Make, c.Model, c.Year, c.Category, c.DailyRate, c.LicencePlate, c.Colour, c.Status);

static UserResponse ToUserResponse(User u) => new(
    u.Id, u.Username, u.Role, u.FullName, u.Email, u.Phone, u.CreatedAt);

// ════════════════════════════════════════════════════════════════════════════
//  AUTH ENDPOINTS
// ════════════════════════════════════════════════════════════════════════════

// POST /auth/register — public
app.MapPost("/auth/register", async (RegisterRequest req, AppDbContext db) =>
{
    if (string.IsNullOrWhiteSpace(req.Username) || string.IsNullOrWhiteSpace(req.Password)
        || string.IsNullOrWhiteSpace(req.FullName) || string.IsNullOrWhiteSpace(req.Email))
        return Results.BadRequest(new { error = "Username, Password, FullName, and Email are required." });

    if (await db.Users.AnyAsync(u => u.Username == req.Username))
        return Results.Conflict(new { error = "Username already taken." });

    var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(req.Password))).ToLower();

    var user = new User
    {
        Username = req.Username,
        PasswordHash = hash,
        Role = "Customer",
        FullName = req.FullName,
        Email = req.Email,
        Phone = req.Phone ?? ""
    };

    db.Users.Add(user);
    await db.SaveChangesAsync();

    return Results.Created($"/users/{user.Id}", ToUserResponse(user));
})
.WithName("Register")
.WithOpenApi();

// GET /users — Admin only
app.MapGet("/users", async (AppDbContext db) =>
{
    var users = await db.Users.OrderBy(u => u.Id).ToListAsync();
    return Results.Ok(users.Select(ToUserResponse));
})
.RequireAuthorization()
.WithName("GetAllUsers")
.WithOpenApi()
.WithMetadata(new Microsoft.AspNetCore.Authorization.AuthorizeAttribute { Roles = "Admin" });

// ════════════════════════════════════════════════════════════════════════════
//  CAR ENDPOINTS
// ════════════════════════════════════════════════════════════════════════════

// GET /cars — any authenticated user
app.MapGet("/cars", async (AppDbContext db) =>
{
    var cars = await db.Cars.OrderBy(c => c.Id).ToListAsync();
    return Results.Ok(cars.Select(ToCarResponse));
})
.RequireAuthorization()
.WithName("GetAllCars")
.WithOpenApi();

// GET /cars/{id} — any authenticated user
app.MapGet("/cars/{id:int}", async (int id, AppDbContext db) =>
{
    var car = await db.Cars.FindAsync(id);
    return car is null ? Results.NotFound(new { error = "Car not found." }) : Results.Ok(ToCarResponse(car));
})
.RequireAuthorization()
.WithName("GetCarById")
.WithOpenApi();

// POST /cars — Admin only
app.MapPost("/cars", async (CreateCarRequest req, AppDbContext db) =>
{
    if (string.IsNullOrWhiteSpace(req.Make) || string.IsNullOrWhiteSpace(req.Model)
        || string.IsNullOrWhiteSpace(req.LicencePlate))
        return Results.BadRequest(new { error = "Make, Model, and LicencePlate are required." });

    if (await db.Cars.AnyAsync(c => c.LicencePlate == req.LicencePlate))
        return Results.Conflict(new { error = "Licence plate already exists." });

    var car = new Car
    {
        Make = req.Make, Model = req.Model, Year = req.Year,
        Category = req.Category, DailyRate = req.DailyRate,
        LicencePlate = req.LicencePlate, Colour = req.Colour,
        Status = req.Status ?? "Available"
    };

    db.Cars.Add(car);
    await db.SaveChangesAsync();
    return Results.Created($"/cars/{car.Id}", ToCarResponse(car));
})
.RequireAuthorization()
.WithName("CreateCar")
.WithOpenApi()
.WithMetadata(new Microsoft.AspNetCore.Authorization.AuthorizeAttribute { Roles = "Admin" });

// PUT /cars/{id} — Admin only
app.MapPut("/cars/{id:int}", async (int id, UpdateCarRequest req, AppDbContext db) =>
{
    var car = await db.Cars.FindAsync(id);
    if (car is null) return Results.NotFound(new { error = "Car not found." });

    // Check licence plate uniqueness (allow same car to keep its plate)
    if (await db.Cars.AnyAsync(c => c.LicencePlate == req.LicencePlate && c.Id != id))
        return Results.Conflict(new { error = "Licence plate already in use by another car." });

    car.Make = req.Make; car.Model = req.Model; car.Year = req.Year;
    car.Category = req.Category; car.DailyRate = req.DailyRate;
    car.LicencePlate = req.LicencePlate; car.Colour = req.Colour;
    car.Status = req.Status;

    await db.SaveChangesAsync();
    return Results.Ok(ToCarResponse(car));
})
.RequireAuthorization()
.WithName("UpdateCar")
.WithOpenApi()
.WithMetadata(new Microsoft.AspNetCore.Authorization.AuthorizeAttribute { Roles = "Admin" });

// DELETE /cars/{id} — Admin only
app.MapDelete("/cars/{id:int}", async (int id, AppDbContext db) =>
{
    var car = await db.Cars.FindAsync(id);
    if (car is null) return Results.NotFound(new { error = "Car not found." });

    db.Cars.Remove(car);
    await db.SaveChangesAsync();
    return Results.NoContent();
})
.RequireAuthorization()
.WithName("DeleteCar")
.WithOpenApi()
.WithMetadata(new Microsoft.AspNetCore.Authorization.AuthorizeAttribute { Roles = "Admin" });

// ════════════════════════════════════════════════════════════════════════════
//  BOOKING ENDPOINTS
// ════════════════════════════════════════════════════════════════════════════

// POST /bookings — Customer only
app.MapPost("/bookings", async (CreateBookingRequest req, ClaimsPrincipal userClaim, AppDbContext db) =>
{
    if (GetUserRole(userClaim) != "Customer")
        return Results.Forbid();

    if (req.ReturnDate <= req.PickupDate)
        return Results.BadRequest(new { error = "ReturnDate must be after PickupDate." });

    var car = await db.Cars.FindAsync(req.CarId);
    if (car is null) return Results.NotFound(new { error = "Car not found." });

    // Availability check — reject if any Active or Approved booking overlaps
    var conflict = await db.Bookings.AnyAsync(b =>
        b.CarId == req.CarId &&
        (b.Status == "Approved" || b.Status == "Active") &&
        b.PickupDate < req.ReturnDate &&
        b.ReturnDate > req.PickupDate);

    if (conflict)
        return Results.Conflict(new { error = "Car is not available for the selected dates." });

    var days = (int)Math.Ceiling((req.ReturnDate - req.PickupDate).TotalDays);
    var total = car.DailyRate * days;

    var booking = new Booking
    {
        CustomerId = GetUserId(userClaim),
        CarId = req.CarId,
        PickupDate = req.PickupDate,
        ReturnDate = req.ReturnDate,
        TotalAmount = total,
        Status = "Pending"
    };

    db.Bookings.Add(booking);
    await db.SaveChangesAsync();

    // Re-load with navigation properties for response
    await db.Entry(booking).Reference(b => b.Customer).LoadAsync();
    await db.Entry(booking).Reference(b => b.Car).LoadAsync();

    return Results.Created($"/bookings/{booking.Id}", ToBookingResponse(booking));
})
.RequireAuthorization()
.WithName("CreateBooking")
.WithOpenApi()
.WithMetadata(new Microsoft.AspNetCore.Authorization.AuthorizeAttribute { Roles = "Customer" });

// GET /bookings/my — Customer only (must be defined BEFORE /bookings/{id} to avoid routing conflict)
app.MapGet("/bookings/my", async (ClaimsPrincipal userClaim, AppDbContext db) =>
{
    var customerId = GetUserId(userClaim);
    var bookings = await db.Bookings
        .Include(b => b.Customer)
        .Include(b => b.Car)
        .Include(b => b.ApprovedBy)
        .Where(b => b.CustomerId == customerId)
        .OrderByDescending(b => b.CreatedAt)
        .ToListAsync();

    return Results.Ok(bookings.Select(ToBookingResponse));
})
.RequireAuthorization()
.WithName("GetMyBookings")
.WithOpenApi()
.WithMetadata(new Microsoft.AspNetCore.Authorization.AuthorizeAttribute { Roles = "Customer" });

// DELETE /bookings/{id} — Customer only, cancel Pending booking
app.MapDelete("/bookings/{id:int}", async (int id, ClaimsPrincipal userClaim, AppDbContext db) =>
{
    var customerId = GetUserId(userClaim);
    var booking = await db.Bookings.FindAsync(id);

    if (booking is null) return Results.NotFound(new { error = "Booking not found." });
    if (booking.CustomerId != customerId) return Results.Forbid(); // can't cancel another customer's booking
    if (booking.Status != "Pending") return Results.BadRequest(new { error = "Only Pending bookings can be cancelled." });

    booking.Status = "Cancelled";
    await db.SaveChangesAsync();
    return Results.NoContent();
})
.RequireAuthorization()
.WithName("CancelBooking")
.WithOpenApi()
.WithMetadata(new Microsoft.AspNetCore.Authorization.AuthorizeAttribute { Roles = "Customer" });

// GET /bookings — Staff or Admin
app.MapGet("/bookings", async (AppDbContext db) =>
{
    var bookings = await db.Bookings
        .Include(b => b.Customer)
        .Include(b => b.Car)
        .Include(b => b.ApprovedBy)
        .OrderByDescending(b => b.CreatedAt)
        .ToListAsync();

    return Results.Ok(bookings.Select(ToBookingResponse));
})
.RequireAuthorization()
.WithName("GetAllBookings")
.WithOpenApi()
.WithMetadata(new Microsoft.AspNetCore.Authorization.AuthorizeAttribute { Roles = "Staff,Admin" });

// PUT /bookings/{id}/approve — Staff or Admin
app.MapPut("/bookings/{id:int}/approve", async (int id, ClaimsPrincipal userClaim, AppDbContext db) =>
{
    var booking = await db.Bookings
        .Include(b => b.Customer).Include(b => b.Car).Include(b => b.ApprovedBy)
        .FirstOrDefaultAsync(b => b.Id == id);

    if (booking is null) return Results.NotFound(new { error = "Booking not found." });
    if (booking.Status != "Pending") return Results.BadRequest(new { error = "Only Pending bookings can be approved." });

    booking.Status = "Approved";
    booking.ApprovedById = GetUserId(userClaim);
    await db.SaveChangesAsync();

    await db.Entry(booking).Reference(b => b.ApprovedBy).LoadAsync();
    return Results.Ok(ToBookingResponse(booking));
})
.RequireAuthorization()
.WithName("ApproveBooking")
.WithOpenApi()
.WithMetadata(new Microsoft.AspNetCore.Authorization.AuthorizeAttribute { Roles = "Staff,Admin" });

// PUT /bookings/{id}/reject — Staff or Admin
app.MapPut("/bookings/{id:int}/reject", async (int id, ClaimsPrincipal userClaim, AppDbContext db) =>
{
    var booking = await db.Bookings
        .Include(b => b.Customer).Include(b => b.Car).Include(b => b.ApprovedBy)
        .FirstOrDefaultAsync(b => b.Id == id);

    if (booking is null) return Results.NotFound(new { error = "Booking not found." });
    if (booking.Status != "Pending") return Results.BadRequest(new { error = "Only Pending bookings can be rejected." });

    booking.Status = "Rejected";
    booking.ApprovedById = GetUserId(userClaim);
    await db.SaveChangesAsync();

    await db.Entry(booking).Reference(b => b.ApprovedBy).LoadAsync();
    return Results.Ok(ToBookingResponse(booking));
})
.RequireAuthorization()
.WithName("RejectBooking")
.WithOpenApi()
.WithMetadata(new Microsoft.AspNetCore.Authorization.AuthorizeAttribute { Roles = "Staff,Admin" });

// PUT /bookings/{id}/complete — Staff or Admin
app.MapPut("/bookings/{id:int}/complete", async (int id, ClaimsPrincipal userClaim, AppDbContext db) =>
{
    var booking = await db.Bookings
        .Include(b => b.Customer).Include(b => b.Car).Include(b => b.ApprovedBy)
        .FirstOrDefaultAsync(b => b.Id == id);

    if (booking is null) return Results.NotFound(new { error = "Booking not found." });
    if (booking.Status != "Active") return Results.BadRequest(new { error = "Only Active bookings can be marked as completed." });

    booking.Status = "Completed";
    if (booking.Car != null) booking.Car.Status = "Available";
    await db.SaveChangesAsync();

    return Results.Ok(ToBookingResponse(booking));
})
.RequireAuthorization()
.WithName("CompleteBooking")
.WithOpenApi()
.WithMetadata(new Microsoft.AspNetCore.Authorization.AuthorizeAttribute { Roles = "Staff,Admin" });

// ── Also handle Approved → Active transition ─────────────────────────────────
// Staff can manually mark an Approved booking as Active (pickup date reached)
app.MapPut("/bookings/{id:int}/activate", async (int id, ClaimsPrincipal userClaim, AppDbContext db) =>
{
    var booking = await db.Bookings
        .Include(b => b.Customer).Include(b => b.Car).Include(b => b.ApprovedBy)
        .FirstOrDefaultAsync(b => b.Id == id);

    if (booking is null) return Results.NotFound(new { error = "Booking not found." });
    if (booking.Status != "Approved") return Results.BadRequest(new { error = "Only Approved bookings can be activated." });

    booking.Status = "Active";
    if (booking.Car != null) booking.Car.Status = "Rented";
    await db.SaveChangesAsync();

    return Results.Ok(ToBookingResponse(booking));
})
.RequireAuthorization()
.WithName("ActivateBooking")
.WithOpenApi()
.WithMetadata(new Microsoft.AspNetCore.Authorization.AuthorizeAttribute { Roles = "Staff,Admin" });

app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();

app.Run();