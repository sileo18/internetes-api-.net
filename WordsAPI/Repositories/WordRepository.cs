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
            // Limite de similaridade
            const double similarityThreshold = 0.3;
            var queryLower = termQuery.ToLowerInvariant(); // Para ILIKE

            // Construir a query
            var query = _context.Words
                .Include(w => w.ExamplesNavigation) // Ajuste para ExamplesNavigation se for o nome
                .Include(w => w.SynonymsNavigation) // Ajuste para SynonymsNavigation se for o nome
                .Select(w => new // Projetar para um objeto anônimo para incluir a similaridade
                {
                    Word = w,
                    SimilarityScore = ApplicationDbContext.WordSimilarity(w.Term, termQuery)
                })
                .Where(x => x.SimilarityScore > similarityThreshold ||
                             EF.Functions.ILike(x.Word.Term, $"%{queryLower}%")// Fallback para ILIKE no termo
                             ) // Opcional: ILIKE na definição também
                .OrderByDescending(x => x.SimilarityScore) // Ordenar pela similaridade
                .ThenBy(x => x.Word.Term) // Ordenação secundária (opcional)
                .Select(x => x.Word); // Selecionar apenas a entidade Word no final

            return await query.ToListAsync();
        }
    }
}
