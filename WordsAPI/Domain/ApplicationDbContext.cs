using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace WordsAPI.Domain;

public partial class ApplicationDbContext : DbContext
{
    public ApplicationDbContext()
    {
    }

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Example> Examples { get; set; }

    public virtual DbSet<FlywaySchemaHistory> FlywaySchemaHistories { get; set; }

    public virtual DbSet<Synonym> Synonyms { get; set; }

    public virtual DbSet<Word> Words { get; set; }

    public static double WordSimilarity(string? text1, string? text2)
            => throw new NotSupportedException("This method is for use with Entity Framework Core only.");

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=internetes;Username=postgres;Password=password;SSL Mode=Prefer;Trust Server Certificate=true;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("pg_trgm");

        modelBuilder.HasDbFunction(typeof(ApplicationDbContext)
                .GetMethod(nameof(WordSimilarity), new[] { typeof(string), typeof(string) })!)
                .HasName("similarity") // Nome da função no PostgreSQL
                .IsBuiltIn();

        modelBuilder.Entity<Example>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("example_pkey");

            entity.HasOne(d => d.Word).WithMany(p => p.ExamplesNavigation).HasConstraintName("fk_example_word");
        });

        modelBuilder.Entity<FlywaySchemaHistory>(entity =>
        {
            entity.HasKey(e => e.InstalledRank).HasName("flyway_schema_history_pk");

            entity.Property(e => e.InstalledRank).ValueGeneratedNever();
            entity.Property(e => e.InstalledOn).HasDefaultValueSql("now()");
        });

        modelBuilder.Entity<Synonym>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("synonym_pkey");

            entity.HasOne(d => d.Word).WithMany(p => p.SynonymsNavigation).HasConstraintName("fk_synonym_word");
        });

        modelBuilder.Entity<Word>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("word_pkey");

            entity.HasIndex(e => e.Term, "idx_word_term_trgm")
                .HasMethod("gin")
                .HasOperators(new[] { "gin_trgm_ops" });

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
