using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace WordsAPI.Domain;

[Table("example")]
[Index("WordId", Name = "idx_example_word_id")]
public partial class Example
{
    public Example() { }

    public Example(string content)
    {
        this.Content = content;
    }

    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("content")]
    public string Content { get; set; } = null!;

    [Column("word_id")]
    public int WordId { get; set; }

    [ForeignKey("WordId")]
    [InverseProperty("ExamplesNavigation")]
    public virtual Word Word { get; set; } = null!;
}
