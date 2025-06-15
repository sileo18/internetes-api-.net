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

            if (word == null)
            {
                return null;
            }

            var response = MapToResponseDto(word);
            
            await _cacheService.SetAsync(cacheKey, response, TimeSpan.FromMinutes(5));

            return response;
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
            return MapToResponseDto(savedWord); 
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
                Examples = word.ExamplesNavigation?.Select(e => new ExampleDto { Id = e.Id, Content = e.Content }).ToList() ?? new List<ExampleDto>(),
                Synonyms = word.SynonymsNavigation?.Select(s => new SynonymDto { Id = s.Id, Content = s.Content }).ToList() ?? new List<SynonymDto>(),
            };
        }
    }
}