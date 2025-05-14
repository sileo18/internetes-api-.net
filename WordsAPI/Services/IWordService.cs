using WordsAPI.DTO_s;

namespace WordsAPI.Services
{
    public interface IWordService
    {
        Task<WordResponseDto?> GetWordByIdAsync(long id);
        Task<PaginatedWordsResponseDto> GetAllWordsAsync(int pageNumber, int pageSize);
        Task<WordResponseDto?> CreateWordAsync(CreateWordDto createWordDto);
        Task<IEnumerable<WordResponseDto>> SearchWordsAsync(string termQuery);
        //Task<WordResponseDto?> MarkWordAsAdoptedAsync(long id, AdoptWordDto adoptWordDto);
    }
}
