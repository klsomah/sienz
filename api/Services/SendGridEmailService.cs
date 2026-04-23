using SendGrid;
using SendGrid.Helpers.Mail;
using SienzApi.Models;

namespace SienzApi.Services;

public class SendGridEmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<SendGridEmailService> _logger;

    public SendGridEmailService(IConfiguration config, ILogger<SendGridEmailService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task<bool> SendContactEmailAsync(ContactRequest request)
    {
        // API key is NEVER in appsettings.json — loaded from user-secrets (dev) or env vars (prod)
        var apiKey = _config["SendGrid:ApiKey"]
            ?? throw new InvalidOperationException("SendGrid:ApiKey is not configured.");

        var toEmail = _config["SendGrid:ToEmail"]
            ?? throw new InvalidOperationException("SendGrid:ToEmail is not configured.");

        var client = new SendGridClient(apiKey);

        var from = new EmailAddress(
            _config["SendGrid:FromEmail"] ?? "noreply@siyense.tech",
            _config["SendGrid:FromName"] ?? "Sienz Website"
        );

        var to = new EmailAddress(toEmail, _config["SendGrid:ToName"] ?? "Sienz");

        var subject = $"New Enquiry: {request.Service} — {request.FirstName} {request.LastName}";

        var plainText = $"""
            New contact form submission from the Sienz website.

            Name:    {request.FirstName} {request.LastName}
            Email:   {request.Email}
            Company: {request.Company ?? "Not provided"}
            Service: {request.Service}
            Budget:  {request.Budget ?? "Not specified"}

            Message:
            {request.Message}
            """;

        var htmlContent = $"""
            <div style="font-family:Arial,sans-serif;max-width:600px;margin:0 auto;">
              <div style="background:linear-gradient(135deg,#0f62fe,#8338ec);padding:28px 32px;border-radius:10px 10px 0 0;">
                <h2 style="color:white;margin:0;font-size:1.15rem;font-weight:700;">New Contact Form Submission</h2>
                <p style="color:rgba(255,255,255,0.7);margin:5px 0 0;font-size:.85rem;">Sienz Website</p>
              </div>
              <div style="border:1px solid #e0e0e0;border-top:none;padding:28px 32px;border-radius:0 0 10px 10px;">
                <table style="width:100%;border-collapse:collapse;font-size:.9rem;line-height:1.6;">
                  <tr>
                    <td style="padding:7px 0;color:#6f6f6f;width:100px;vertical-align:top;">Name</td>
                    <td style="padding:7px 0;font-weight:600;">{Encode(request.FirstName)} {Encode(request.LastName)}</td>
                  </tr>
                  <tr>
                    <td style="padding:7px 0;color:#6f6f6f;vertical-align:top;">Email</td>
                    <td style="padding:7px 0;">
                      <a href="mailto:{Encode(request.Email)}" style="color:#0f62fe;">{Encode(request.Email)}</a>
                    </td>
                  </tr>
                  <tr>
                    <td style="padding:7px 0;color:#6f6f6f;vertical-align:top;">Company</td>
                    <td style="padding:7px 0;">{(string.IsNullOrWhiteSpace(request.Company) ? "<em style='color:#aaa;'>Not provided</em>" : Encode(request.Company))}</td>
                  </tr>
                  <tr>
                    <td style="padding:7px 0;color:#6f6f6f;vertical-align:top;">Service</td>
                    <td style="padding:7px 0;">{Encode(request.Service)}</td>
                  </tr>
                  <tr>
                    <td style="padding:7px 0;color:#6f6f6f;vertical-align:top;">Budget</td>
                    <td style="padding:7px 0;">{(string.IsNullOrWhiteSpace(request.Budget) ? "<em style='color:#aaa;'>Not specified</em>" : Encode(request.Budget))}</td>
                  </tr>
                </table>
                <hr style="border:none;border-top:1px solid #e0e0e0;margin:20px 0;"/>
                <p style="color:#6f6f6f;font-size:.75rem;text-transform:uppercase;letter-spacing:.08em;font-weight:700;margin-bottom:10px;">Message</p>
                <p style="color:#161616;line-height:1.75;white-space:pre-wrap;font-size:.9rem;">{Encode(request.Message)}</p>
                <hr style="border:none;border-top:1px solid #e0e0e0;margin:20px 0;"/>
                <p style="color:#aaa;font-size:.78rem;">
                  Reply directly to this email to respond to {Encode(request.FirstName)}.
                </p>
              </div>
            </div>
            """;

        var msg = MailHelper.CreateSingleEmail(from, to, subject, plainText, htmlContent);

        // Reply-to is set to the enquirer so you can reply directly from your inbox
        msg.ReplyTo = new EmailAddress(request.Email, $"{request.FirstName} {request.LastName}");

        var response = await client.SendEmailAsync(msg);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Body.ReadAsStringAsync();
            _logger.LogError("SendGrid returned {StatusCode}: {Body}", (int)response.StatusCode, body);
            return false;
        }

        return true;
    }

    private static string Encode(string? value) =>
        System.Net.WebUtility.HtmlEncode(value ?? string.Empty);
}
