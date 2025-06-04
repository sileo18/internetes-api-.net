using Microsoft.Extensions.Options;
using SendGrid; // Para SendGridClient
using SendGrid.Helpers.Mail; // Para EmailAddress e SendGridMessage
using System.Threading.Tasks;
using WordsAPI.Config;
using WordsAPI.DTO_s;

namespace WordsAPI.Services
{
    public class EmailService : IEmailService
    {
        private readonly EmailSettings _emailSettings;
        private readonly ILogger<EmailService> _logger;
        private readonly ISendGridClient _sendGridClient; // Injetar o cliente SendGrid

        // Modifique o construtor para receber ISendGridClient
        public EmailService(IOptions<EmailSettings> emailSettings, ILogger<EmailService> logger, ISendGridClient sendGridClient)
        {
            _emailSettings = emailSettings.Value;
            _logger = logger;
            _sendGridClient = sendGridClient; // Cliente SendGrid injetado
        }

        public async Task SendContactFormEmailAsync(ContactFormDTO contactFormDto)
        {
            try
            {
                var fromEmail = new EmailAddress(_emailSettings.SenderEmail, _emailSettings.SenderName);
                var toEmail = new EmailAddress(_emailSettings.ReceiverEmail); // Email que receberá o contato

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

                var msg = MailHelper.CreateSingleEmail(fromEmail, toEmail, subject, "", body);
                
                // Opcional: Adicionar o email do contato como Reply-To para facilitar a resposta
                msg.ReplyTo = new EmailAddress(contactFormDto.email, contactFormDto.name);

                var response = await _sendGridClient.SendEmailAsync(msg);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Email de contato enviado com sucesso via SendGrid de {ContactEmail} para {ReceiverEmail}. Status: {StatusCode}", contactFormDto.email, _emailSettings.ReceiverEmail, response.StatusCode);
                }
                else
                {
                    var responseBody = await response.Body.ReadAsStringAsync();
                    _logger.LogError("Falha ao enviar email de contato via SendGrid de {ContactEmail} para {ReceiverEmail}. Status: {StatusCode}. Corpo: {ResponseBody}", contactFormDto.email, _emailSettings.ReceiverEmail, response.StatusCode, responseBody);
                    throw new Exception($"Falha ao enviar email via SendGrid. Status: {response.StatusCode}. Detalhes: {responseBody}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao enviar email de contato (SendGrid) de {ContactEmail}", contactFormDto.email);
                throw;
            }
        }
    }
}