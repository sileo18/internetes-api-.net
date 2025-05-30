using Microsoft.AspNetCore.Mvc;
using WordsAPI.DTO_s;
using WordsAPI.Services;
using System.Net.Mime; // Importe para MediaTypeNames

namespace WordsAPI.Controllers
{
    /// <summary>
    /// Controlador para gerenciamento de palavras na API.
    /// </summary>
    [Route("api/word")] // Rota base: /api/word
    [ApiController]
    [Produces(MediaTypeNames.Application.Json)] // Define o tipo de mídia padrão para as respostas
    public class WordController : ControllerBase
    {
        private readonly IWordService _wordService;

        public WordController(IWordService wordService)
        {
            _wordService = wordService;
        }

        /// <summary>
        /// Obtém uma lista paginada de todas as palavras.
        /// </summary>
        /// <param name="pageNumber">O número da página desejada (padrão: 1).</param>
        /// <param name="pageSize">O tamanho da página desejado (padrão: 10, máximo: 100).</param>
        /// <returns>Uma lista paginada de palavras.</returns>
        /// <response code="200">Retorna a lista paginada de palavras.</response>
        /// <response code="500">Se ocorrer um erro interno do servidor.</response>
        [HttpGet]
        [ProducesResponseType(typeof(PaginatedWordsResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<PaginatedWordsResponseDto>> GetAllWords(
            [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 100) pageSize = 100;

            var paginatedResult = await _wordService.GetAllWordsAsync(pageNumber, pageSize);
            return Ok(paginatedResult);
        }

        /// <summary>
        /// Obtém uma palavra específica pelo seu ID.
        /// </summary>
        /// <param name="id">O ID da palavra.</param>
        /// <returns>A palavra encontrada.</returns>
        /// <response code="200">Retorna a palavra encontrada.</response>
        /// <response code="404">Se a palavra com o ID especificado não for encontrada.</response>
        /// <response code="500">Se ocorrer um erro interno do servidor.</response>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(WordResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<WordResponseDto>> GetWordById(long id)
        {
            var word = await _wordService.GetWordByIdAsync(id);
            if (word == null)
            {
                return NotFound(new { message = $"Palavra com ID {id} não encontrada." });
            }
            return Ok(word);
        }

        /// <summary>
        /// Cria uma nova palavra.
        /// </summary>
        /// <remarks>
        /// Exemplo de requisição:
        ///
        ///     POST /api/word/create
        ///     {
        ///        "term": "Exemplo",
        ///        "definition": "Uma palavra utilizada para ilustrar algo.",
        ///        "language": "Português"
        ///     }
        /// </remarks>
        /// <param name="createWordDto">Dados para criar a nova palavra.</param>
        /// <returns>A palavra recém-criada.</returns>
        /// <response code="201">Retorna a palavra recém-criada.</response>
        /// <response code="400">Se os dados da requisição forem inválidos.</response>
        /// <response code="409">Se uma palavra com o mesmo termo já existir.</response>
        /// <response code="500">Se ocorrer um erro interno do servidor.</response>
        [HttpPost("create")]
        [Consumes(MediaTypeNames.Application.Json)] // Define o tipo de mídia que o endpoint consome
        [ProducesResponseType(typeof(WordResponseDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
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
                    return Conflict(new { message = $"O termo '{createWordDto.Term}' já existe." });
                }
                return CreatedAtAction(nameof(GetWordById), new { id = newWord.Id }, newWord);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Ocorreu um erro interno ao criar a palavra." });
            }
        }

        /// <summary>
        /// Busca palavras por um termo específico (case-insensitive).
        /// </summary>
        /// <param name="q">O termo de busca.</param>
        /// <returns>Uma lista de palavras que correspondem ao termo de busca.</returns>
        /// <response code="200">Retorna a lista de palavras encontradas.</response>
        /// <response code="400">Se o parâmetro de busca 'q' for vazio ou nulo.</response>
        /// <response code="500">Se ocorrer um erro interno do servidor.</response>
        [HttpGet("search")]
        [ProducesResponseType(typeof(IEnumerable<WordResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IEnumerable<WordResponseDto>>> SearchWords([FromQuery] string q)
        {
            if (string.IsNullOrWhiteSpace(q))
            {
                return BadRequest(new { message = "O parâmetro de busca 'q' não pode ser vazio." });
            }
            var words = await _wordService.SearchWordsAsync(q);
            return Ok(words);
        }

        // Você pode descomentar e documentar este método quando for implementá-lo.
        /*
        /// <summary>
        /// Marca uma palavra como "adotada" (com um novo significado ou contexto, por exemplo).
        /// </summary>
        /// <remarks>
        /// Exemplo de requisição:
        ///
        ///     PUT /api/word/{id}/adopt
        ///     {
        ///        "adoptedMeaning": "Um novo significado para a palavra.",
        ///        "adoptedByUserId": "usuario123"
        ///     }
        /// </remarks>
        /// <param name="id">O ID da palavra a ser marcada como adotada.</param>
        /// <param name="adoptWordDto">Os dados da adoção da palavra.</param>
        /// <returns>A palavra atualizada após a adoção.</returns>
        /// <response code="200">Retorna a palavra atualizada.</response>
        /// <response code="400">Se os dados da requisição forem inválidos.</response>
        /// <response code="404">Se a palavra com o ID especificado não for encontrada.</response>
        /// <response code="409">Se a palavra já estiver adotada ou houver outro conflito.</response>
        /// <response code="500">Se ocorrer um erro interno do servidor.</response>
        [HttpPut("{id}/adopt")]
        [Consumes(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(WordResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<WordResponseDto>> MarkWordAsAdopted(long id, [FromBody] AdoptWordDto adoptWordDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var updatedWord = await _wordService.MarkWordAsAdoptedAsync(id, adoptWordDto);
                if (updatedWord == null)
                {
                    return NotFound(new { message = $"Palavra com ID {id} não encontrada para adoção." });
                }
                return Ok(updatedWord);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Ocorreu um erro interno ao marcar a palavra como adotada." });
            }
        }
        */
    }
}