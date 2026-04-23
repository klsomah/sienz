using System.Threading.RateLimiting;
using SienzApi.Models;
using SienzApi.Services;

var builder = WebApplication.CreateBuilder(args);

// ── Services ──────────────────────────────────────────────
builder.Services.AddScoped<IEmailService, SendGridEmailService>();

// ── CORS ──────────────────────────────────────────────────
// In production, replace with your actual domain(s)
var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? ["http://localhost:5500", "http://127.0.0.1:5500"];

builder.Services.AddCors(options =>
{
    options.AddPolicy("ContactFormPolicy", policy =>
        policy.WithOrigins(allowedOrigins)
              .WithMethods("POST")
              .WithHeaders("Content-Type"));
});

// ── Rate Limiting ─────────────────────────────────────────
// Max 5 submissions per IP address per hour
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("ContactFormLimiter", limiterOptions =>
    {
        limiterOptions.Window            = TimeSpan.FromHours(1);
        limiterOptions.PermitLimit       = 5;
        limiterOptions.QueueLimit        = 0;
        limiterOptions.AutoReplenishment = true;
    });
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

var app = builder.Build();

app.UseCors("ContactFormPolicy");
app.UseRateLimiter();

// ── Endpoint ──────────────────────────────────────────────
app.MapPost("/api/contact", async (ContactRequest request, IEmailService emailService) =>
{
    if (!request.IsValid(out var errors))
        return Results.BadRequest(new { errors });

    var success = await emailService.SendContactEmailAsync(request);

    return success
        ? Results.Ok(new { message = "Your message has been sent." })
        : Results.Problem(
            detail: "Failed to send email. Please try again later.",
            statusCode: StatusCodes.Status502BadGateway);
})
.RequireRateLimiting("ContactFormLimiter");

app.Run();
