using Microsoft.EntityFrameworkCore;
using WordsAPI.Domain;

// Certifique-se de que o namespace do seu DbContext está correto.
// Se ele estiver em uma pasta diferente, ajuste o namespace.
namespace WordsAPI.Domain 
{
    public class ApplicationDbContext : DbContext
    {
        // Construtor padrão que o EF Core usará para criar uma instância
        // durante as migrações e em tempo de execução.
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // DbSets representam as tabelas no seu banco de dados.
        public DbSet<Word> Words { get; set; }
        public DbSet<Synonym> Synonyms { get; set; }
        public DbSet<Example> Examples { get; set; }

        // Método stub para mapear a função `similarity` do Postgres.
        // Ele nunca será executado em C#, apenas traduzido para SQL.
        public static double WordSimilarity(string? text1, string? text2)
                => throw new NotSupportedException("This method is for use with Entity Framework Core only.");

        // OnModelCreating é onde configuramos o modelo de dados, relacionamentos e índices.
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // --- Configurações Gerais do Banco de Dados ---

            // 1. Habilita o uso da extensão pg_trgm para busca por similaridade.
            //    Lembre-se de rodar 'CREATE EXTENSION pg_trgm;' no seu banco da Fly.io.
            modelBuilder.HasPostgresExtension("pg_trgm");

            // 2. Mapeia nosso método C# `WordSimilarity` para a função SQL nativa `similarity`.
            modelBuilder.HasDbFunction(typeof(ApplicationDbContext)
                    .GetMethod(nameof(WordSimilarity), new[] { typeof(string), typeof(string) })!)
                    .HasName("similarity") 
                    .IsBuiltIn();

            // --- Configuração da Entidade 'Word' ---
            modelBuilder.Entity<Word>(entity =>
            {
                // Define o nome da tabela (embora já definido por [Table]).
                entity.ToTable("word");

                // Define a chave primária.
                entity.HasKey(e => e.Id);

                // Define que a propriedade Term é única no banco.
                entity.HasIndex(e => e.Term).IsUnique();

                // Define o índice GIN para busca rápida com trigramas na coluna 'term'.
                // Essencial para a performance da função `similarity`.
                entity.HasIndex(e => e.Term, "idx_word_term_trgm")
                    .HasMethod("gin")
                    .HasOperators("gin_trgm_ops");

                // Configura o valor padrão para a data de criação no banco.
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            });

            // --- Configuração da Entidade 'Example' ---
            modelBuilder.Entity<Example>(entity =>
            {
                entity.ToTable("example");
                entity.HasKey(e => e.Id);

                // Configura o relacionamento "um-para-muitos": Uma Palavra (Word) tem muitos Exemplos (Example).
                entity.HasOne(example => example.Word)            // Um exemplo tem uma palavra...
                      .WithMany(word => word.ExamplesNavigation)   // ...e uma palavra tem muitos exemplos.
                      .HasForeignKey(example => example.WordId)    // A chave estrangeira está em Example.WordId.
                      .OnDelete(DeleteBehavior.Cascade);           // Se uma palavra for deletada, seus exemplos também serão.
            });

            // --- Configuração da Entidade 'Synonym' ---
            modelBuilder.Entity<Synonym>(entity =>
            {
                entity.ToTable("synonym");
                entity.HasKey(e => e.Id);
                
                // Configura o relacionamento "um-para-muitos": Uma Palavra (Word) tem muitos Sinônimos (Synonym).
                entity.HasOne(synonym => synonym.Word)          // Um sinônimo tem uma palavra...
                      .WithMany(word => word.SynonymsNavigation) // ...e uma palavra tem muitos sinônimos.
                      .HasForeignKey(synonym => synonym.WordId)  // A chave estrangeira está em Synonym.WordId.
                      .OnDelete(DeleteBehavior.Cascade);         // Se uma palavra for deletada, seus sinônimos também serão.
            });
        }
    }
}