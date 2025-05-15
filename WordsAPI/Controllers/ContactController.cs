using Microsoft.AspNetCore.Mvc;
using WordsAPI.DTO_s;
using WordsAPI.Services;

namespace WordsAPI.Controllers
{
    [Route("api/contact")]
    [ApiController]
    public class ContactController : ControllerBase
    {
        private readonly EmailService _emailService;
        private readonly ILogger<ContactController> _logger;

        public ContactController(EmailService emailService, ILogger<ContactController> logger)
        {
            _emailService = emailService;
            _logger = logger;
        }

        [HttpPost("send")]
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
