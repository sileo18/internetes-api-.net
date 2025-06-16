using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WordsAPI.CacheService;
using WordsAPI.Services; // Presumindo que ICacheService está aqui
using WordsAPI.Domain;
using WordsAPI.DTO_s;
using WordsAPI.Repositories;

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
            var cacheKey = $"word_id:{id}"; // Boa prática: usar ":" como separador de namespace

            // CORREÇÃO: Chamando o novo método do ICacheService
            var cacheResult = await _cacheService.GetCacheData<WordResponseDto>(cacheKey);

            if (cacheResult != null)
            {
                return cacheResult;
            }
            
            var word = await _wordRepository.GetByIdAsync(id);

            if (word == null)
            {
                return null;
            }

            var response = MapToResponseDto(word);
            
            // CORREÇÃO: Chamando o novo método do ICacheService
            await _cacheService.SetCacheData(cacheKey, response, TimeSpan.FromMinutes(5));

            return response;
        }

        public async Task<PaginatedWordsResponseDto> GetAllWordsAsync(int pageNumber, int pageSize)
        {
            var cacheKey = $"words:all:{pageNumber}:{pageSize}";

            // CORREÇÃO: Chamando o novo método do ICacheService
            var cachedResult = await _cacheService.GetCacheData<PaginatedWordsResponseDto>(cacheKey);

            if (cachedResult != null)
            {
                Console.WriteLine(">>>>> REDIS! (Paginated)");
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

            // CORREÇÃO: Chamando o novo método do ICacheService
            await _cacheService.SetCacheData(cacheKey, response, TimeSpan.FromDays(7));

            return response;
        }

        public async Task<WordResponseDto?> CreateWordAsync(CreateWordDto createWordDto)
        {
            // O ideal é invalidar o cache aqui, não criar.
            // Por exemplo, invalidar o cache da lista de palavras.
            // await _cacheService.RemoveCacheData("words:all:*"); // Implementação de remoção por padrão seria necessária

            if (await _wordRepository.ExistsByTermAsync(createWordDto.Term))
            {
                return null; 
            }
            
            var word = new Word
            {
                Term = createWordDto.Term,
                Definition = createWordDto.Definition,
                PartOfSpeech = createWordDto.PartOfSpeech,
                CreatedAt = DateTime.UtcNow
            };
            
            if (createWordDto.Examples != null && createWordDto.Examples.Any())
            {
                foreach (var exampleContent in createWordDto.Examples)
                {
                    var example = new Example(exampleContent); 
                    example.Word = word; 
                    word.ExamplesNavigation.Add(example); 
                }
            }
            
            if (createWordDto.Synonyms != null && createWordDto.Synonyms.Any())
            {
                foreach (var synonymContent in createWordDto.Synonyms)
                {
                    var synonym = new Synonym(synonymContent); 
                    synonym.Word = word; 
                    word.SynonymsNavigation.Add(synonym); 
                }
            }
            var savedWord = await _wordRepository.AddAsync(word);

            // Após criar uma nova palavra, o cache da lista paginada fica desatualizado.
            // A melhor estratégia é invalidá-lo.
            // Ex: await _cacheService.RemoveCacheData("words:all*");

            return MapToResponseDto(savedWord); 
        }

        public async Task<IEnumerable<WordResponseDto>> SearchWordsAsync(string termQuery)
        {
            var cacheKey = $"word:search:{termQuery.ToLowerInvariant()}";

            // CORREÇÃO: Chamando o novo método do ICacheService
            var cachedResult = await _cacheService.GetCacheData<List<WordResponseDto>>(cacheKey);

            if (cachedResult != null)
            {
                Console.WriteLine(">>>>> REDIS! (Search)");
                return cachedResult;
            }
            
            var words = await _wordRepository.SearchByTermAsync(termQuery);
            var response = words.Select(MapToResponseDto).ToList(); // .ToList() para materializar a lista antes de salvar em cache

            // CORREÇÃO: Chamando o novo método do ICacheService
            await _cacheService.SetCacheData(cacheKey, response, TimeSpan.FromSeconds(60));
            
            return response;
        }

        private WordResponseDto MapToResponseDto(Word word)
        {
            return new WordResponseDto
            {
                Id = word.Id,
                Term = word.Term,
                Definition = word.Definition,
                PartOfSpeech = word.PartOfSpeech,
                CreatedAt = (DateTime)word.CreatedAt,
                Examples = word.ExamplesNavigation?.Select(e => new ExampleDto { Id = e.Id, Content = e.Content }).ToList() ?? new List<ExampleDto>(),
                Synonyms = word.SynonymsNavigation?.Select(s => new SynonymDto { Id = s.Id, Content = s.Content }).ToList() ?? new List<SynonymDto>(),
            };
        }
    }
}