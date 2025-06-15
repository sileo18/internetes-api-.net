using Microsoft.Extensions.Options;
using SendGrid; // Para SendGridClient
using SendGrid.Helpers.Mail; // Para EmailAddress e SendGridMessage
using System.Threading.Tasks;
using WordsAPI.Config;
using WordsAPI.DTO_s;
using Microsoft.Extensions.Logging;
using WordsAPI.Config.WordsAPI.Config; // Para ILogger

namespace WordsAPI.Services
{
    public class EmailService : IEmailService
    {
        private readonly EmailSettings _emailSettings;
        private readonly ILogger<EmailService> _logger;
        private readonly ISendGridClient _sendGridClient;
        public EmailService(IOptions<EmailSettings> emailSettings, ILogger<EmailService> logger, ISendGridClient sendGridClient)
        {
            _emailSettings = emailSettings.Value;
            _logger = logger;
            _sendGridClient = sendGridClient;
        }

        public async Task SendContactFormEmailAsync(ContactFormDTO contactFormDto)
        {
            try
            {
                var fromEmail = new EmailAddress(_emailSettings.SenderEmail, _emailSettings.SenderName);
                
                var toEmail = new EmailAddress(_emailSettings.ReceiverEmail);

                string subject = $"Novo Contato do Site Internetes: {contactFormDto.Subject}"; 
                string body = $@"
                    <p>Você recebeu uma nova mensagem de contato do site Internetes:</p>
                    <p><strong>Nome:</strong> {contactFormDto.Name}</p>
                    <p><strong>Email para Resposta:</strong> {contactFormDto.Email}</p>
                    <p><strong>Assunto:</strong> {contactFormDto.Subject}</p>
                    <p><strong>Mensagem:</strong></p>
                    <p style=""white-space: pre-wrap;"">{contactFormDto.Message}</p>
                    <hr>
                    <p><em>Este email foi enviado através do formulário de contato do site Internetes.</em></p>";
                
                var msg = MailHelper.CreateSingleEmail(fromEmail, toEmail, subject, null, body);
                
                msg.ReplyTo = new EmailAddress(contactFormDto.Email, contactFormDto.Name);
                
                var response = await _sendGridClient.SendEmailAsync(msg);

                // Verifica a resposta do SendGrid
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Email de contato enviado com sucesso via SendGrid de {ContactEmail} para {ReceiverEmail}. Status: {StatusCode}", contactFormDto.Email, _emailSettings.ReceiverEmail, response.StatusCode);
                }
                else
                {
                    var responseBody = await response.Body.ReadAsStringAsync(); 
                    _logger.LogError("Falha ao enviar email de contato via SendGrid de {ContactEmail} para {ReceiverEmail}. Status: {StatusCode}. Corpo: {ResponseBody}", contactFormDto.Email, _emailSettings.ReceiverEmail, response.StatusCode, responseBody);
                    throw new Exception($"Falha ao enviar email via SendGrid. Status: {response.StatusCode}. Detalhes: {responseBody}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao enviar email de contato (SendGrid) de {ContactEmail}", contactFormDto.Email);
                throw; 
            }
        }
    }
}