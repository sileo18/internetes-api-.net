using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WordsAPI.CacheService;
using WordsAPI.Domain;
using WordsAPI.DTO_s;
using WordsAPI.Repositories;
using WordsAPI.Services;

namespace WordsAPI.Services
{
    public class WordService : IWordService
    {
        private readonly IWordRepository _wordRepository;

        private readonly ICacheService _cacheService;

        public WordService(IWordRepository wordRepository, ICacheService cacheService)
        {
            _wordRepository = wordRepository;
            _cacheService = cacheService;
        }

        public async Task<WordResponseDto?> GetWordByIdAsync(long id)
        {
            var cacheKey = $"word_id{id}";

            var cacheResult = await _cacheService.GetAsync<WordResponseDto>(cacheKey);

            if (cacheResult != null)
            {
                return cacheResult;
            }
            
            var word = await _wordRepository.GetByIdAsync(id);

            await _cacheService.SetAsync(cacheKey, word, TimeSpan.FromMinutes(5));
            
            return word != null ? MapToResponseDto(word) : null;
        }

        public async Task<PaginatedWordsResponseDto> GetAllWordsAsync(int pageNumber, int pageSize)
        {
            var cacheKey = $"words_all_{pageNumber}_{pageSize}";

            var cachedResult = await _cacheService.GetAsync<PaginatedWordsResponseDto>(cacheKey);

            if (cachedResult != null)
            {
                Console.WriteLine((">>>>>>REDIS!"));
                return cachedResult;
            }
            
            var words = await _wordRepository.GetAllAsync(pageNumber, pageSize);
            
            var totalCount = await _wordRepository.GetTotalCountAsync();

            PaginatedWordsResponseDto response = new PaginatedWordsResponseDto
            {
                Words = words.Select(MapToResponseDto).ToList(),
                CurrentPage = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            };

            await _cacheService.SetAsync(cacheKey, response, TimeSpan.FromDays(7));

            return response;
        }

        public async Task<WordResponseDto?> CreateWordAsync(CreateWordDto createWordDto)
        {
            if (await _wordRepository.ExistsByTermAsync(createWordDto.Term))
            {
                // Considerar lançar uma exceção específica que o controller possa tratar
                // para retornar um status HTTP 409 (Conflict)
                // throw new TermAlreadyExistsException($"O termo '{createWordDto.Term}' já existe.");
                return null; // Indicando falha por termo duplicado
            }

            // 1. Crie a instância principal de Word
            var word = new Word
            {
                Term = createWordDto.Term,
                Definition = createWordDto.Definition,
                PartOfSpeech = createWordDto.PartOfSpeech,
                CreatedAt = DateTime.UtcNow
                // As coleções Examples e Synonyms serão inicializadas como vazias pelo construtor de Word
                // Se não, inicialize-as aqui:
                // ExamplesNavigation = new List<Example>(),
                // SynonymsNavigation = new List<Synonym>()
            };

            // 2. Crie e associe Examples, se houver
            if (createWordDto.Examples != null && createWordDto.Examples.Any())
            {
                foreach (var exampleContent in createWordDto.Examples)
                {
                    var example = new Example(exampleContent); // Usando o construtor que exige 'content'
                    // example.Content = exampleContent; // Se usar construtor vazio e setar a propriedade
                    example.Word = word; // Define a referência de volta para a Word (importante!)
                                         // Se WordId for exposto em Example e você não quiser definir a navegação completa:
                                         // example.WordId = word.Id; // Isso só funcionaria se word já tivesse um Id (após salvar word primeiro)
                                         // É melhor definir a propriedade de navegação 'Word'
                    word.ExamplesNavigation.Add(example); // Adiciona à coleção da Word
                }
            }

            // 3. Crie e associe Synonyms, se houver
            if (createWordDto.Synonyms != null && createWordDto.Synonyms.Any())
            {
                foreach (var synonymContent in createWordDto.Synonyms)
                {
                    var synonym = new Synonym(synonymContent); // Usando o construtor
                    // synonym.Content = synonymContent;
                    synonym.Word = word; // Define a referência de volta para a Word
                    word.SynonymsNavigation.Add(synonym); // Adiciona à coleção da Word
                }
            }

            // 4. Salve a Word (e o EF Core, devido ao Cascade e às associações, salvará Examples e Synonyms)
            var savedWord = await _wordRepository.AddAsync(word);
            return MapToResponseDto(savedWord); // Mapeie a entidade salva (com IDs gerados) para o DTO
        }

        public async Task<IEnumerable<WordResponseDto>> SearchWordsAsync(string termQuery)
        {
            var cacheKey = $"word_{termQuery.ToLowerInvariant()}";

            var cachedResult = await _cacheService.GetAsync<List<WordResponseDto>>(cacheKey);

            if (cachedResult != null)
            {
                Console.WriteLine((">>>>REDIS!!"));
                return cachedResult;
            }
            
            var words = await _wordRepository.SearchByTermAsync(termQuery);
            
            var response = words.Select(MapToResponseDto);

            await _cacheService.SetAsync(cacheKey, response, TimeSpan.FromSeconds(60));
            
            return response;


        }

        //public async Task<WordResponseDto?> MarkWordAsAdoptedAsync(long id, AdoptWordDto adoptWordDto)
        //{
        //    var word = await _wordRepository.GetByIdAsync(id);
        //    if (word == null)
        //    {
        //        return null; // Palavra não encontrada
        //    }

        //    if (word.Adopted)
        //    {
        //        // Lógica para lidar com palavra já adotada (ex: lançar exceção)
        //        // throw new InvalidOperationException("Esta palavra já foi adotada.");
        //        return MapToResponseDto(word); // Ou retornar como está
        //    }

        //    word.Adopted = true;
        //    word.AdoptedByName = adoptWordDto.AdopterName;
        //    word.AdoptionDate = DateTime.UtcNow.Date; // Apenas data
        //    word.AdoptionPlatformId = adoptWordDto.PlatformTransactionId;
        //    word.AdoptionMessage = adoptWordDto.Message;

        //    var updatedWord = await _wordRepository.UpdateAsync(word);
        //    return updatedWord != null ? MapToResponseDto(updatedWord) : null;
        //}

        // Método helper para mapear Entidade para DTO
        private WordResponseDto MapToResponseDto(Word word)
        {
            return new WordResponseDto
            {
                Id = word.Id,
                Term = word.Term,
                Definition = word.Definition,
                PartOfSpeech = word.PartOfSpeech,
                CreatedAt = (DateTime)word.CreatedAt,
                // Correção aqui: Selecionar a propriedade 'Content' de cada Example/Synonym
                Examples = word.ExamplesNavigation?.Select(e => e.Content).ToList() ?? new List<string>(),
                Synonyms = word.SynonymsNavigation?.Select(s => s.Content).ToList() ?? new List<string>(),
               // Adopted = word.Adopted,
               // AdoptedByName = word.AdoptedByName,
               // AdoptionDate = word.AdoptionDate
            };
        }
    }
}