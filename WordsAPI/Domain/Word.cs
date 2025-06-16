using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore; // <-- ADICIONE ESTA LINHA

namespace WordsAPI.Domain;

[Table("word")]
[Index(nameof(Term), IsUnique = true)] // Corrigido para ser mais seguro
public partial class Word
{
    public Word()
    {
    }
    
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("term")]
    [StringLength(255)]
    public string Term { get; set; } = null!;

    [Column("definition")]
    public string Definition { get; set; } = null!;

    [Column("part_of_speech")]
    [StringLength(50)]
    public string? PartOfSpeech { get; set; }

    [Column("created_at", TypeName = "timestamp with time zone")]
    public DateTime? CreatedAt { get; set; }
    
    [InverseProperty("Word")]
    public virtual ICollection<Example> ExamplesNavigation { get; set; } = new List<Example>();

    [InverseProperty("Word")]
    public virtual ICollection<Synonym> SynonymsNavigation { get; set; } = new List<Synonym>();
}