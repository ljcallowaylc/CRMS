using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using CRMS.API.Data;
using Microsoft.EntityFrameworkCore;

namespace CRMS.API.Auth;

public class BasicAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly AppDbContext _db;

    public BasicAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        AppDbContext db)
        : base(options, logger, encoder)
    {
        _db = db;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("Authorization", out var authHeader))
            return AuthenticateResult.NoResult();

        var headerVal = authHeader.ToString();
        if (!headerVal.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
            return AuthenticateResult.NoResult();

        string credentials;
        try
        {
            var base64 = headerVal["Basic ".Length..].Trim();
            credentials = Encoding.UTF8.GetString(Convert.FromBase64String(base64));
        }
        catch
        {
            return AuthenticateResult.Fail("Invalid Base64 in Authorization header");
        }

        var colonIndex = credentials.IndexOf(':');
        if (colonIndex < 0)
            return AuthenticateResult.Fail("Invalid Basic Auth format");

        var username = credentials[..colonIndex];
        var password = credentials[(colonIndex + 1)..];

        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(password))).ToLower();

        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Username == username && u.PasswordHash == hash);

        if (user is null)
            return AuthenticateResult.Fail("Invalid username or password");

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim("FullName", user.FullName)
        };

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return AuthenticateResult.Success(ticket);
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.Headers["WWW-Authenticate"] = "Basic realm=\"CRMS\"";
        Response.StatusCode = 401;
        return Task.CompletedTask;
    }
}