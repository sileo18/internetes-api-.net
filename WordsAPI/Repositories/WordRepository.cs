using Microsoft.EntityFrameworkCore;
using WordsAPI.Domain;

namespace WordsAPI.Repositories
{
    public class WordRepository : IWordRepository
    {
        private readonly ApplicationDbContext _context;

        public WordRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Word> AddAsync(Word word)
        {
            _context.Words.Add(word);
            await _context.SaveChangesAsync();
            return word;
        }

        public async Task<bool> ExistsByTermAsync(string term)
        {
            return await _context.Words.AnyAsync(w => w.Term.ToLower() == term.ToLower());
        }

        public async Task<IEnumerable<Word>> GetAllAsync(int pageNumber, int pageSize)
        {
            return await _context.Words
                                 .Include(w => w.ExamplesNavigation)
                                 .Include(w => w.SynonymsNavigation)
                                 .OrderBy(w => w.CreatedAt) 
                                 .Skip((pageNumber - 1) * pageSize)
                                 .Take(pageSize)
                                 .ToListAsync();
        }

        public async Task<Word?> GetByIdAsync(long id)
        {
            return await _context.Words
                                 .Include(w => w.ExamplesNavigation) 
                                 .Include(w => w.SynonymsNavigation) 
                                 .FirstOrDefaultAsync(w => w.Id == id);
        }

        public async Task<int> GetTotalCountAsync()
        {
            return await _context.Words.CountAsync();
        }

        public async Task<IEnumerable<Word>> SearchByTermAsync(string termQuery)
        {
            const double similarityThreshold = 0.3;
            var queryLower = termQuery.ToLowerInvariant(); // Para ILIKE

            
            var query = _context.Words
                .Include(w => w.ExamplesNavigation) 
                .Include(w => w.SynonymsNavigation) 
                .Select(w => new 
                {
                    Word = w,
                    SimilarityScore = ApplicationDbContext.WordSimilarity(w.Term, termQuery)
                })
                .Where(x => x.SimilarityScore > similarityThreshold ||
                             EF.Functions.ILike(x.Word.Term, $"%{queryLower}%")
                             ) 
                .OrderByDescending(x => x.SimilarityScore) 
                .ThenBy(x => x.Word.Term) 
                .Select(x => x.Word); 

            return await query.ToListAsync();
        }
    }
}
