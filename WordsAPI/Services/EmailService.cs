using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;
using WordsAPI.Config;
using WordsAPI.DTO_s;

namespace WordsAPI.Services
{
    public class EmailService : IEmailService
    {
        private readonly EmailSettings _emailSettings;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IOptions<EmailSettings> emailSettings, ILogger<EmailService> logger)
        {
            _emailSettings = emailSettings.Value; // Acessa o objeto EmailSettings configurado
            _logger = logger;
        }

        public async Task SendContactFormEmailAsync(ContactFormDTO contactFormDto)
        {
            try
            {
                var fromAddress = new MailAddress(_emailSettings.SenderEmail, _emailSettings.SenderName);
                var toAddress = new MailAddress(_emailSettings.ReceiverEmail); // Email que recebe o contato

                string subject = $"Novo Contato do Site Internetes: {contactFormDto.subject}";
                string body = $@"
                    <p>Você recebeu uma nova mensagem de contato do site Internetes:</p>
                    <p><strong>Nome:</strong> {contactFormDto.name}</p>
                    <p><strong>Email para Resposta:</strong> {contactFormDto.email}</p>
                    <p><strong>Assunto:</strong> {contactFormDto.subject}</p>
                    <p><strong>Mensagem:</strong></p>
                    <p style=""white-space: pre-wrap;"">{contactFormDto.message}</p>
                    <hr>
                    <p><em>Este email foi enviado através do formulário de contato do site Internetes.</em></p>";

                using (var smtpClient = new SmtpClient
                {
                    Host = _emailSettings.SmtpHost,
                    Port = _emailSettings.SmtpPort,
                    EnableSsl = _emailSettings.EnableSsl, // Gmail requer SSL/TLS
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(fromAddress.Address, _emailSettings.Password)
                })
                {
                    using (var mailMessage = new MailMessage(fromAddress, toAddress)
                    {
                        Subject = subject,
                        Body = body,
                        IsBodyHtml = true, // Nosso corpo é HTML
                        ReplyToList = { new MailAddress(contactFormDto.email, contactFormDto.name) } // Define o Reply-To
                    })
                    {
                        await smtpClient.SendMailAsync(mailMessage);
                        _logger.LogInformation("Email de contato enviado com sucesso de {ContactEmail} para {ReceiverEmail}", contactFormDto.Email, _emailSettings.ReceiverEmail);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao enviar email de contato de {ContactEmail}", contactFormDto.email);
                throw; // Relança a exceção para ser tratada pelo controller ou middleware
            }
        }
    }
}
