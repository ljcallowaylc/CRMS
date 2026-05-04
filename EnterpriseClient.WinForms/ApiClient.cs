using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EnterpriseClient;

public record UserResponse(int Id, string Username, string Role, string FullName, string Email, string Phone, DateTime CreatedAt);
public record CarResponse(int Id, string Make, string Model, int Year, string Category, decimal DailyRate, string LicencePlate, string Colour, string Status);
public record BookingResponse(int Id, int CustomerId, string CustomerName, int CarId, string CarDescription, DateTime PickupDate, DateTime ReturnDate, decimal TotalAmount, string Status, int? ApprovedById, string? ApprovedByName, DateTime CreatedAt);

public class ApiClient
{
    private readonly HttpClient _http;
    private static readonly JsonSerializerOptions _json = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public string? Username { get; private set; }
    public string? Role { get; private set; }

    public ApiClient(string baseUrl)
    {
        _http = new HttpClient { BaseAddress = new Uri(baseUrl) };
    }

    public void SetCredentials(string username, string password)
    {
        var token = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"));
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", token);
        Username = username;
    }

    public void ClearCredentials()
    {
        _http.DefaultRequestHeaders.Authorization = null;
        Username = null; Role = null;
    }

    private static async Task<T?> ParseAsync<T>(HttpResponseMessage res)
    {
        var body = await res.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(body)) return default;
        try { return JsonSerializer.Deserialize<T>(body, _json); } catch { return default; }
    }

    private static async Task<string?> ReadErrorAsync(HttpResponseMessage res)
    {
        var body = await res.Content.ReadAsStringAsync();
        try
        {
            var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("error", out var e)) return e.GetString();
        }
        catch { }
        return !string.IsNullOrEmpty(body) ? body : $"HTTP {(int)res.StatusCode}";
    }

    private StringContent Json(object obj) =>
        new(JsonSerializer.Serialize(obj), Encoding.UTF8, "application/json");

    public async Task<(bool ok, string role, string? error)> LoginAsync(string username, string password)
    {
        SetCredentials(username, password);
        var res = await _http.GetAsync("/cars");
        if (!res.IsSuccessStatusCode)
        {
            ClearCredentials();
            return (false, "", res.StatusCode == System.Net.HttpStatusCode.Unauthorized
                ? "Invalid username or password."
                : $"Login failed ({(int)res.StatusCode}).");
        }
        var adminCheck = await _http.GetAsync("/users");
        if (adminCheck.IsSuccessStatusCode) { Role = "Admin"; return (true, "Admin", null); }
        var staffCheck = await _http.GetAsync("/bookings");
        Role = staffCheck.IsSuccessStatusCode ? "Staff" : "Customer";
        return (true, Role, null);
    }

    public async Task<(bool ok, string? error)> RegisterAsync(string username, string password, string fullName, string email, string phone)
    {
        var res = await _http.PostAsync("/auth/register", Json(new { username, password, fullName, email, phone }));
        if (res.IsSuccessStatusCode) return (true, null);
        if (res.StatusCode == System.Net.HttpStatusCode.Conflict) return (false, "Username is already taken.");
        return (false, await ReadErrorAsync(res));
    }

    public async Task<List<CarResponse>> GetCarsAsync()
    {
        var res = await _http.GetAsync("/cars");
        return res.IsSuccessStatusCode ? await ParseAsync<List<CarResponse>>(res) ?? [] : [];
    }

    public async Task<(bool ok, BookingResponse? booking, string? error)> CreateBookingAsync(int carId, DateTime pickupDate, DateTime returnDate)
    {
        var res = await _http.PostAsync("/bookings", Json(new { carId, pickupDate, returnDate }));
        if (res.IsSuccessStatusCode) return (true, await ParseAsync<BookingResponse>(res), null);
        if (res.StatusCode == System.Net.HttpStatusCode.Conflict)
            return (false, null, "That car is already booked for the selected dates.");
        return (false, null, await ReadErrorAsync(res));
    }

    public async Task<List<BookingResponse>> GetMyBookingsAsync()
    {
        var res = await _http.GetAsync("/bookings/my");
        return res.IsSuccessStatusCode ? await ParseAsync<List<BookingResponse>>(res) ?? [] : [];
    }

    public async Task<(bool ok, string? error)> CancelBookingAsync(int id)
    {
        var res = await _http.DeleteAsync($"/bookings/{id}");
        return res.IsSuccessStatusCode ? (true, null) : (false, await ReadErrorAsync(res));
    }

    public async Task<List<BookingResponse>> GetAllBookingsAsync()
    {
        var res = await _http.GetAsync("/bookings");
        return res.IsSuccessStatusCode ? await ParseAsync<List<BookingResponse>>(res) ?? [] : [];
    }

    public async Task<(bool ok, string? error)> BookingActionAsync(int id, string action)
    {
        var res = await _http.PutAsync($"/bookings/{id}/{action}", Json(new { }));
        return res.IsSuccessStatusCode ? (true, null) : (false, await ReadErrorAsync(res));
    }

    public async Task<List<UserResponse>> GetUsersAsync()
    {
        var res = await _http.GetAsync("/users");
        return res.IsSuccessStatusCode ? await ParseAsync<List<UserResponse>>(res) ?? [] : [];
    }

    public async Task<(bool ok, CarResponse? car, string? error)> CreateCarAsync(string make, string model, int year, string category, decimal dailyRate, string licencePlate, string colour, string status)
    {
        var res = await _http.PostAsync("/cars", Json(new { make, model, year, category, dailyRate, licencePlate, colour, status }));
        if (res.IsSuccessStatusCode) return (true, await ParseAsync<CarResponse>(res), null);
        return (false, null, await ReadErrorAsync(res));
    }

    public async Task<(bool ok, CarResponse? car, string? error)> UpdateCarAsync(int id, string make, string model, int year, string category, decimal dailyRate, string licencePlate, string colour, string status)
    {
        var res = await _http.PutAsync($"/cars/{id}", Json(new { make, model, year, category, dailyRate, licencePlate, colour, status }));
        if (res.IsSuccessStatusCode) return (true, await ParseAsync<CarResponse>(res), null);
        return (false, null, await ReadErrorAsync(res));
    }

    public async Task<(bool ok, string? error)> DeleteCarAsync(int id)
    {
        var res = await _http.DeleteAsync($"/cars/{id}");
        return res.IsSuccessStatusCode ? (true, null) : (false, await ReadErrorAsync(res));
    }
}