using SienzApi.Models;

namespace SienzApi.Services;

public interface IEmailService
{
    Task<bool> SendContactEmailAsync(ContactRequest request);
}
