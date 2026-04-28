using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using CRMS.Api.Models;
using CRMS.Api.Data;
using CRMS.Api.DTOs;

var builder = WebApplication.CreateBuilder(args);

//
// =====================
// DATABASE
// =====================
//
var connString = builder.Configuration["ConnectionStrings:DefaultConnection"];

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connString));

builder.Services.AddEndpointsApiExplorer();

//
// =====================
// SWAGGER
// =====================
//
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "CRMS API",
        Version = "v1"
    });

    c.AddSecurityDefinition("basic", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "basic",
        In = ParameterLocation.Header
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "basic"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

//
// =====================
// BASIC AUTH MIDDLEWARE
// =====================
//
app.Use(async (ctx, next) =>
{
    var path = ctx.Request.Path.Value!.ToLower();

    if (path.Contains("/auth/register") || path.Contains("/swagger"))
    {
        await next();
        return;
    }

    var header = ctx.Request.Headers["Authorization"].ToString();

    if (string.IsNullOrWhiteSpace(header) || !header.StartsWith("Basic "))
    {
        ctx.Response.StatusCode = 401;
        return;
    }

    var encoded = header["Basic ".Length..];
    var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(encoded));
    var parts = decoded.Split(':');

    if (parts.Length != 2)
    {
        ctx.Response.StatusCode = 401;
        return;
    }

    var db = ctx.RequestServices.GetRequiredService<AppDbContext>();

    var user = db.Users.FirstOrDefault(u => u.Username == parts[0]);
    if (user == null)
    {
        ctx.Response.StatusCode = 401;
        return;
    }

    var hash = Convert.ToHexString(
        SHA256.HashData(Encoding.UTF8.GetBytes(parts[1]))
    );

    if (user.PasswordHash != hash)
    {
        ctx.Response.StatusCode = 401;
        return;
    }

    ctx.Items["User"] = user;
    await next();
});

//
// =====================
// ROLE CHECK
// =====================
//
bool Role(HttpContext ctx, params string[] roles)
{
    var u = ctx.Items["User"] as User;
    return u != null && roles.Contains(u.Role);
}

//
// =====================
// REGISTER
// =====================
//
app.MapPost("/auth/register", async (AppDbContext db, RegisterUserDto dto) =>
{
    var user = new User
    {
        Username = dto.Username,
        FullName = dto.FullName,
        Email = dto.Email,
        Phone = dto.Phone,
        Role = "Customer",
        CreatedAt = DateTime.UtcNow,
        PasswordHash = Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(dto.Password))
        )
    };

    db.Users.Add(user);
    await db.SaveChangesAsync();

    return Results.Ok(new UserDto
    {
        Id = user.Id,
        Username = user.Username,
        Role = user.Role,
        FullName = user.FullName,
        Email = user.Email,
        Phone = user.Phone,
        CreatedAt = user.CreatedAt
    });
});

//
// =====================
// USERS (ADMIN)
// =====================
//
app.MapGet("/users", (HttpContext ctx, AppDbContext db) =>
{
    if (!Role(ctx, "Admin")) return Results.Forbid();

    return Results.Ok(db.Users.Select(u => new UserDto
    {
        Id = u.Id,
        Username = u.Username,
        Role = u.Role,
        FullName = u.FullName,
        Email = u.Email,
        Phone = u.Phone,
        CreatedAt = u.CreatedAt
    }).ToList());
});

//
// =====================
// CARS
// =====================
//
app.MapGet("/cars", (AppDbContext db) =>
{
    return Results.Ok(db.Cars.Select(c => new CarDto
    {
        Id = c.Id,
        Make = c.Make,
        Model = c.Model,
        Year = c.Year,
        Category = c.Category,
        DailyRate = c.DailyRate,
        LicencePlate = c.LicencePlate,
        Colour = c.Colour,
        Status = c.Status
    }).ToList());
});

app.MapGet("/cars/{id}", (int id, AppDbContext db) =>
{
    var car = db.Cars.Find(id);
    if (car == null) return Results.NotFound();

    return Results.Ok(new CarDto
    {
        Id = car.Id,
        Make = car.Make,
        Model = car.Model,
        Year = car.Year,
        Category = car.Category,
        DailyRate = car.DailyRate,
        LicencePlate = car.LicencePlate,
        Colour = car.Colour,
        Status = car.Status
    });
});

app.MapPost("/cars", async (HttpContext ctx, AppDbContext db, Car car) =>
{
    if (!Role(ctx, "Admin")) return Results.Forbid();

    car.Status = "Available";
    db.Cars.Add(car);
    await db.SaveChangesAsync();

    return Results.Ok(car);
});

app.MapPut("/cars/{id}", async (HttpContext ctx, int id, AppDbContext db, Car dto) =>
{
    if (!Role(ctx, "Admin")) return Results.Forbid();

    var car = await db.Cars.FindAsync(id);
    if (car == null) return Results.NotFound();

    car.Make = dto.Make;
    car.Model = dto.Model;
    car.Year = dto.Year;
    car.Category = dto.Category;
    car.DailyRate = dto.DailyRate;
    car.LicencePlate = dto.LicencePlate;
    car.Colour = dto.Colour;
    car.Status = dto.Status;

    await db.SaveChangesAsync();
    return Results.Ok(car);
});

app.MapDelete("/cars/{id}", async (HttpContext ctx, int id, AppDbContext db) =>
{
    if (!Role(ctx, "Admin")) return Results.Forbid();

    var car = await db.Cars.FindAsync(id);
    if (car == null) return Results.NotFound();

    db.Cars.Remove(car);
    await db.SaveChangesAsync();

    return Results.Ok();
});

//
// =====================
// CREATE BOOKING
// =====================
//
app.MapPost("/bookings", async (HttpContext ctx, AppDbContext db, CreateBookingDto dto) =>
{
    var user = ctx.Items["User"] as User;
    if (user == null) return Results.Unauthorized();

    var car = await db.Cars.FindAsync(dto.CarId);
    if (car == null) return Results.NotFound();

    var conflict = await db.Bookings.AnyAsync(x =>
        x.CarId == dto.CarId &&
        (x.Status == "Approved" || x.Status == "Active") &&
        dto.PickupDate < x.ReturnDate &&
        dto.ReturnDate > x.PickupDate
    );

    if (conflict) return Results.Conflict("Car already booked");

    var days = (decimal)Math.Ceiling(
        (dto.ReturnDate.Date - dto.PickupDate.Date).TotalDays
    );

    if (days <= 0) return Results.BadRequest("Invalid dates");

    var booking = new Booking
    {
        CarId = dto.CarId,
        CustomerId = user.Id,
        PickupDate = dto.PickupDate,
        ReturnDate = dto.ReturnDate,
        Status = "Pending",
        TotalAmount = days * car.DailyRate,
        CreatedAt = DateTime.UtcNow
    };

    db.Bookings.Add(booking);
    await db.SaveChangesAsync();

    return Results.Ok(new BookingDto
    {
        Id = booking.Id,
        CarId = booking.CarId,
        CustomerId = booking.CustomerId,
        PickupDate = booking.PickupDate,
        ReturnDate = booking.ReturnDate,
        TotalAmount = booking.TotalAmount,
        Status = booking.Status
    });
});

//
// =====================
// MY BOOKINGS
// =====================
//
app.MapGet("/bookings/my", (HttpContext ctx, AppDbContext db) =>
{
    var user = ctx.Items["User"] as User;
    if (user == null) return Results.Unauthorized();

    return Results.Ok(db.Bookings
        .Where(b => b.CustomerId == user.Id)
        .Select(b => new BookingDto
        {
            Id = b.Id,
            CarId = b.CarId,
            CustomerId = b.CustomerId,
            PickupDate = b.PickupDate,
            ReturnDate = b.ReturnDate,
            TotalAmount = b.TotalAmount,
            Status = b.Status
        }).ToList());
});

//
// =====================
// ALL BOOKINGS
// =====================
//
app.MapGet("/bookings", (HttpContext ctx, AppDbContext db) =>
{
    if (!Role(ctx, "Staff", "Admin")) return Results.Forbid();

    return Results.Ok(db.Bookings.Select(b => new BookingDto
    {
        Id = b.Id,
        CarId = b.CarId,
        CustomerId = b.CustomerId,
        PickupDate = b.PickupDate,
        ReturnDate = b.ReturnDate,
        TotalAmount = b.TotalAmount,
        Status = b.Status
    }).ToList());
});

//
// =====================
// CANCEL BOOKING
// =====================
//
app.MapDelete("/bookings/{id}", async (HttpContext ctx, int id, AppDbContext db) =>
{
    var user = ctx.Items["User"] as User;
    if (user == null) return Results.Unauthorized();

    var booking = await db.Bookings.FindAsync(id);
    if (booking == null) return Results.NotFound();

    if (booking.CustomerId != user.Id) return Results.Forbid();

    if (booking.Status != "Pending")
        return Results.BadRequest("Only pending bookings can be cancelled");

    booking.Status = "Cancelled";
    await db.SaveChangesAsync();

    return Results.Ok();
});

//
// =====================
// APPROVE
// =====================
//
app.MapPut("/bookings/{id}/approve", async (HttpContext ctx, int id, AppDbContext db) =>
{
    if (!Role(ctx, "Staff", "Admin")) return Results.Forbid();

    var booking = await db.Bookings.FindAsync(id);
    if (booking == null) return Results.NotFound();

    if (booking.Status != "Pending") return Results.BadRequest();

    var car = await db.Cars.FindAsync(booking.CarId);

    booking.Status = "Approved";
    booking.ApprovedById = (ctx.Items["User"] as User)!.Id;
    if (car != null) car.Status = "Rented";

    await db.SaveChangesAsync();
    return Results.Ok();
});

//
// =====================
// REJECT
// =====================
//
app.MapPut("/bookings/{id}/reject", async (HttpContext ctx, int id, AppDbContext db) =>
{
    if (!Role(ctx, "Staff", "Admin")) return Results.Forbid();

    var booking = await db.Bookings.FindAsync(id);
    if (booking == null) return Results.NotFound();

    if (booking.Status != "Pending") return Results.BadRequest();

    booking.Status = "Rejected";
    booking.ApprovedById = (ctx.Items["User"] as User)!.Id;

    await db.SaveChangesAsync();
    return Results.Ok();
});

//
// =====================
// COMPLETE
// =====================
//
app.MapPut("/bookings/{id}/complete", async (HttpContext ctx, int id, AppDbContext db) =>
{
    if (!Role(ctx, "Staff", "Admin")) return Results.Forbid();

    var booking = await db.Bookings.FindAsync(id);
    if (booking == null) return Results.NotFound();

    if (booking.Status != "Active") return Results.BadRequest();

    var car = await db.Cars.FindAsync(booking.CarId);

    booking.Status = "Completed";
    if (car != null) car.Status = "Available";

    await db.SaveChangesAsync();
    return Results.Ok();
});

app.Run();