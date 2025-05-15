using WordsAPI.DTO_s;

namespace WordsAPI.Services
{
    public interface IEmailService
    {
        Task SendContactFormEmailAsync(ContactFormDTO contactFormDto);
    }
}
