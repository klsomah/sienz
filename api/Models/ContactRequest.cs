namespace SienzApi.Models;

public record ContactRequest(
    string FirstName,
    string LastName,
    string Email,
    string? Company,
    string Service,
    string? Budget,
    string Message,
    bool Consent
)
{
    public bool IsValid(out List<string> errors)
    {
        errors = [];

        if (string.IsNullOrWhiteSpace(FirstName) || FirstName.Length > 100)
            errors.Add("First name is required and must be under 100 characters.");

        if (string.IsNullOrWhiteSpace(LastName) || LastName.Length > 100)
            errors.Add("Last name is required and must be under 100 characters.");

        if (string.IsNullOrWhiteSpace(Email) || !IsValidEmail(Email))
            errors.Add("A valid email address is required.");

        if (string.IsNullOrWhiteSpace(Service))
            errors.Add("Service selection is required.");

        if (string.IsNullOrWhiteSpace(Message) || Message.Length < 10)
            errors.Add("Message must be at least 10 characters.");

        if (Message?.Length > 5000)
            errors.Add("Message must be under 5000 characters.");

        if (!Consent)
            errors.Add("Consent is required to submit the form.");

        return errors.Count == 0;
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email && email.Length <= 320;
        }
        catch { return false; }
    }
}
