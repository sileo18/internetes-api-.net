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
            // Usando ILike para busca case-insensitive.
            // Para PostgreSQL, você pode habilitar a extensão pg_trgm e usar funções de similaridade
            // como SIMILARITY(w.Term, termQuery) > 0.2 OR w.Term ILIKE '%' || termQuery || '%'
            // Isso exigiria uma raw query ou uma função mapeada.
            var query = termQuery.ToLowerInvariant();
            return await _context.Words
                                 .Include(w => w.ExamplesNavigation)
                                 .Include(w => w.SynonymsNavigation)
                                 .Where(w => EF.Functions.ILike(w.Term, $"%{query}%") ||
                                             EF.Functions.ILike(w.Definition, $"%{query}%"))
                                 .OrderBy(w => w.Term)
                                 .ToListAsync();
        }
    }
}
