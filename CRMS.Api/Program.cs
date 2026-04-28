using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using CRMS.Api.Models;
using CRMS.Api.Data;

var builder = WebApplication.CreateBuilder(args);

//
// =====================
// DATABASE
// =====================
//
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddEndpointsApiExplorer();

//
// =====================
// SWAGGER + BASIC AUTH
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
            new string[] {}
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
        await ctx.Response.WriteAsync("Missing auth");
        return;
    }

    var encoded = header["Basic ".Length..];
    var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(encoded));
    var parts = decoded.Split(':');

    if (parts.Length != 2)
    {
        ctx.Response.StatusCode = 401;
        await ctx.Response.WriteAsync("Invalid auth");
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
// AUTH
// =====================
//
app.MapPost("/auth/register", async (AppDbContext db, User user) =>
{
    user.PasswordHash = Convert.ToHexString(
        SHA256.HashData(Encoding.UTF8.GetBytes(user.PasswordHash))
    );

    user.Role = "Customer";
    user.CreatedAt = DateTime.UtcNow;

    db.Users.Add(user);
    await db.SaveChangesAsync();

    return Results.Ok(user);
});

//
// =====================
// USERS (ADMIN)
// =====================
//
app.MapGet("/users", (HttpContext ctx, AppDbContext db) =>
{
    if (!Role(ctx, "Admin")) return Results.Forbid();
    return Results.Ok(db.Users.ToList());
});

//
// =====================
// CARS
// =====================
//
app.MapGet("/cars", (AppDbContext db) =>
{
    return Results.Ok(db.Cars.ToList());
});

app.MapGet("/cars/{id}", (int id, AppDbContext db) =>
{
    var car = db.Cars.Find(id);
    return car == null ? Results.NotFound() : Results.Ok(car);
});

app.MapPost("/cars", async (HttpContext ctx, AppDbContext db, Car car) =>
{
    if (!Role(ctx, "Admin")) return Results.Forbid();

    db.Cars.Add(car);
    await db.SaveChangesAsync();
    return Results.Ok(car);
});

app.MapPut("/cars/{id}", async (HttpContext ctx, int id, AppDbContext db, Car updated) =>
{
    if (!Role(ctx, "Admin")) return Results.Forbid();

    var car = await db.Cars.FindAsync(id);
    if (car == null) return Results.NotFound();

    car.Make = updated.Make;
    car.Model = updated.Model;
    car.Year = updated.Year;
    car.Category = updated.Category;
    car.DailyRate = updated.DailyRate;
    car.LicencePlate = updated.LicencePlate;
    car.Colour = updated.Colour;
    car.Status = updated.Status;

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
// BOOKINGS
// =====================
//
app.MapPost("/bookings", async (HttpContext ctx, AppDbContext db, Booking b) =>
{
    var user = ctx.Items["User"] as User;

    var car = await db.Cars.FindAsync(b.CarId);
    if (car == null) return Results.NotFound();

    bool conflict = db.Bookings.Any(x =>
        x.CarId == b.CarId &&
        (x.Status == "Approved" || x.Status == "Active") &&
        b.PickupDate < x.ReturnDate &&
        b.ReturnDate > x.PickupDate
    );

    if (conflict) return Results.Conflict("Car already booked");

    var days = (b.ReturnDate - b.PickupDate).Days;
    if (days <= 0) return Results.BadRequest("Invalid dates");

    b.CustomerId = user!.Id;
    b.Status = "Pending";
    b.TotalAmount = days * car.DailyRate;
    b.CreatedAt = DateTime.UtcNow;

    db.Bookings.Add(b);
    await db.SaveChangesAsync();

    return Results.Ok(b);
});

app.MapGet("/bookings/my", (HttpContext ctx, AppDbContext db) =>
{
    var user = ctx.Items["User"] as User;

    return Results.Ok(db.Bookings
        .Where(x => x.CustomerId == user!.Id)
        .ToList());
});

app.MapGet("/bookings", (HttpContext ctx, AppDbContext db) =>
{
    if (!Role(ctx, "Staff", "Admin")) return Results.Forbid();

    return Results.Ok(db.Bookings.ToList());
});

//
// APPROVE
//
app.MapPut("/bookings/{id}/approve", async (HttpContext ctx, int id, AppDbContext db) =>
{
    if (!Role(ctx, "Staff", "Admin")) return Results.Forbid();

    var b = await db.Bookings.FindAsync(id);
    if (b == null) return Results.NotFound();

    if (b.Status != "Pending") return Results.BadRequest();

    b.Status = "Approved";
    await db.SaveChangesAsync();

    return Results.Ok(b);
});

//
// REJECT
//
app.MapPut("/bookings/{id}/reject", async (HttpContext ctx, int id, AppDbContext db) =>
{
    if (!Role(ctx, "Staff", "Admin")) return Results.Forbid();

    var b = await db.Bookings.FindAsync(id);
    if (b == null) return Results.NotFound();

    if (b.Status != "Pending") return Results.BadRequest();

    b.Status = "Rejected";
    await db.SaveChangesAsync();

    return Results.Ok(b);
});

//
// COMPLETE
//
app.MapPut("/bookings/{id}/complete", async (HttpContext ctx, int id, AppDbContext db) =>
{
    if (!Role(ctx, "Staff", "Admin")) return Results.Forbid();

    var b = await db.Bookings.FindAsync(id);
    if (b == null) return Results.NotFound();

    if (b.Status != "Active") return Results.BadRequest();

    b.Status = "Completed";
    await db.SaveChangesAsync();

    return Results.Ok(b);
});

//
// CANCEL
//
app.MapDelete("/bookings/{id}", async (HttpContext ctx, int id, AppDbContext db) =>
{
    var user = ctx.Items["User"] as User;

    var b = await db.Bookings.FindAsync(id);
    if (b == null) return Results.NotFound();

    if (b.CustomerId != user!.Id) return Results.Forbid();

    if (b.Status != "Pending") return Results.BadRequest();

    b.Status = "Cancelled";

    await db.SaveChangesAsync();
    return Results.Ok(b);
});

app.Run();