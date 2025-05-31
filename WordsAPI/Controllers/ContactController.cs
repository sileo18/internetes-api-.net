using Microsoft.AspNetCore.Mvc;
using WordsAPI.DTO_s;
using WordsAPI.Services;
using Microsoft.Extensions.Logging; // Importe para ILogger
using System.Net.Mime; // Importe para MediaTypeNames

namespace WordsAPI.Controllers
{
    /// <summary>
    /// Controlador para gerenciamento de formulários de contato e envio de emails.
    /// </summary>
    [Route("api/contact")]
    [ApiController]
    [Produces(MediaTypeNames.Application.Json)] // Define o tipo de mídia padrão para as respostas
    public class ContactController : ControllerBase
    {
        private readonly IEmailService _emailService;
        private readonly ILogger<ContactController> _logger;

        public ContactController(IEmailService emailService, ILogger<ContactController> logger)
        {
            _emailService = emailService;
            _logger = logger;
        }

        /// <summary>
        /// Envia uma mensagem de formulário de contato.
        /// </summary>
        /// <remarks>
        /// Este endpoint permite que usuários enviem uma mensagem de contato, que será enviada por e-mail.
        ///
        /// Exemplo de requisição:
        ///
        ///     POST /api/contact/send
        ///     {
        ///        "name": "João da Silva",
        ///        "email": "joao.silva@example.com",
        ///        "subject": "Dúvida sobre a API",
        ///        "message": "Gostaria de saber mais sobre a funcionalidade de busca de palavras."
        ///     }
        /// </remarks>
        /// <param name="contactFormDto">Os dados do formulário de contato.</param>
        /// <returns>Uma mensagem de sucesso ou erro.</returns>
        /// <response code="200">A mensagem foi enviada com sucesso.</response>
        /// <response code="400">Os dados do formulário são inválidos.</response>
        /// <response code="500">Ocorreu um erro interno ao tentar enviar a mensagem.</response>
        [HttpPost("send")]
        [Consumes(MediaTypeNames.Application.Json)] // Define o tipo de mídia que o endpoint consome
        [ProducesResponseType(StatusCodes.Status200OK)] // Retorna OK com um objeto de mensagem
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> SendContactForm([FromBody] ContactFormDTO contactFormDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            try
            {
                await _emailService.SendContactFormEmailAsync(contactFormDto);
                return Ok(new { message = "Mensagem enviada com sucesso!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao enviar email de contato.");
                return StatusCode(500, new { message = "Erro ao enviar mensagem. Tente novamente mais tarde." });
            }
        }
    }
}