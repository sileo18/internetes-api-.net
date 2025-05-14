using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace WordsAPI.Domain;

[Table("synonym")]
[Index("WordId", Name = "idx_synonym_word_id")]
public partial class Synonym
{
    public Synonym() { }

    public Synonym(string content)
    {
        this.Content = content;
    }


    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("content")]
    [StringLength(255)]
    public string Content { get; set; } = null!;

    [Column("word_id")]
    public int WordId { get; set; }

    [ForeignKey("WordId")]
    [InverseProperty("SynonymsNavigation")]
    public virtual Word Word { get; set; } = null!;
}
