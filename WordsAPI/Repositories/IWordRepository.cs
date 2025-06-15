using System.Collections;
using WordsAPI.Domain;

namespace WordsAPI.Repositories
{
    public interface IWordRepository
    {
        Task<Word?> GetByIdAsync(long id);
        Task<IEnumerable<Word>> GetAllAsync(int pageNumber, int pageSize);
        Task<int> GetTotalCountAsync();
        Task<Word> AddAsync(Word word);
        //Task<Word?> UpdateAsync(Word word); 
        Task<IEnumerable<Word>> SearchByTermAsync(string termQuery);
        Task<bool> ExistsByTermAsync(string term);
    }
}
