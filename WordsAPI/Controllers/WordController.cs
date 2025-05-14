using Microsoft.AspNetCore.Mvc;
using WordsAPI.DTO_s;
using WordsAPI.Services;

namespace WordsAPI.Controllers
{
    [Route("api/word")] // Rota base: /api/word
    [ApiController]
    public class WordController : ControllerBase
    {
        private readonly IWordService _wordService;

        public WordController(IWordService wordService)
        {
            _wordService = wordService;
        }

        // GET /api/word?pageNumber=1&pageSize=10
        [HttpGet]
        public async Task<ActionResult<PaginatedWordsResponseDto>> GetAllWords(
            [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 100) pageSize = 100; // Limite o tamanho da página

            var paginatedResult = await _wordService.GetAllWordsAsync(pageNumber, pageSize);
            return Ok(paginatedResult);
        }

        // GET /api/word/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<WordResponseDto>> GetWordById(long id)
        {
            var word = await _wordService.GetWordByIdAsync(id);
            if (word == null)
            {
                return NotFound(new { message = $"Palavra com ID {id} não encontrada." });
            }
            return Ok(word);
        }

        // POST /api/word
        [HttpPost("create")] // Alterei a rota para /api/word/create para ser idêntico ao seu Spring
        public async Task<ActionResult<WordResponseDto>> CreateWord([FromBody] CreateWordDto createWordDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var newWord = await _wordService.CreateWordAsync(createWordDto);
                if (newWord == null)
                {
                    // Isso aconteceria se o serviço retornasse null (ex: termo já existe)
                    return Conflict(new { message = $"O termo '{createWordDto.Term}' já existe." });
                }
                // Retorna 201 Created com o local do novo recurso e o próprio recurso
                return CreatedAtAction(nameof(GetWordById), new { id = newWord.Id }, newWord);
            }
            catch (InvalidOperationException ex) // Captura exceções específicas do serviço
            {
                return Conflict(new { message = ex.Message });
            }
            catch (Exception ex) // Captura genérica para outros erros inesperados
            {
                // Logar o erro ex.ToString() em um sistema de logging real
                return StatusCode(500, new { message = "Ocorreu um erro interno ao criar a palavra." });
            }
        }

        // GET /api/word/search?q=meuTermo
        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<WordResponseDto>>> SearchWords([FromQuery] string q)
        {
            if (string.IsNullOrWhiteSpace(q))
            {
                return BadRequest(new { message = "O parâmetro de busca 'q' não pode ser vazio." });
            }
            var words = await _wordService.SearchWordsAsync(q);
            return Ok(words);
        }

        // PUT /api/word/{id}/adopt
        //[HttpPut("{id}/adopt")]
        //public async Task<ActionResult<WordResponseDto>> MarkWordAsAdopted(long id, [FromBody] AdoptWordDto adoptWordDto)
        //{
        //    if (!ModelState.IsValid)
        //    {
        //        return BadRequest(ModelState);
        //    }

        //    try
        //    {
        //        var updatedWord = await _wordService.MarkWordAsAdoptedAsync(id, adoptWordDto);
        //        if (updatedWord == null)
        //        {
        //            return NotFound(new { message = $"Palavra com ID {id} não encontrada para adoção." });
        //        }
        //        return Ok(updatedWord);
        //    }
        //    catch (InvalidOperationException ex) // Ex: Palavra já adotada
        //    {
        //        return Conflict(new { message = ex.Message });
        //    }
        //    catch (Exception ex)
        //    {
        //        // Logar erro
        //        return StatusCode(500, new { message = "Ocorreu um erro interno ao marcar a palavra como adotada." });
        //    }
        //}
    }
}
