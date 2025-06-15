using System.ComponentModel.DataAnnotations;

namespace WordsAPI.DTO_s
{
    public class CreateWordDto
    {
        [Required(ErrorMessage = "O termo é obrigatório.")]
        [StringLength(255, MinimumLength = 2, ErrorMessage = "O termo deve ter entre 2 e 255 caracteres.")]
        public string Term { get; set; } = string.Empty;

        [Required(ErrorMessage = "A definição é obrigatória.")]
        public string Definition { get; set; } = string.Empty;

        [StringLength(50, ErrorMessage = "A classe gramatical deve ter no máximo 50 caracteres.")]
        public string? PartOfSpeech { get; set; }

        public List<string>? Examples { get; set; } = new List<string>();
        public List<string>? Synonyms { get; set; } = new List<string>();
    }

    public class WordResponseDto
    {
        public long Id { get; set; }
        public string Term { get; set; } = string.Empty;
        public string Definition { get; set; } = string.Empty;
        public string? PartOfSpeech { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<ExampleDto> Examples { get; set; } = new List<ExampleDto>();
        public List<SynonymDto> Synonyms { get; set; } = new List<SynonymDto>();
       
    }

    public class AdoptWordDto 
    {
        [Required(ErrorMessage = "O nome do adotante é obrigatório ou indique anônimo.")]
        public string AdopterName { get; set; } = string.Empty;

        public string? PlatformTransactionId { get; set; } 
        public string? Message { get; set; } 
    }

    public class PaginatedWordsResponseDto
    {
        public List<WordResponseDto> Words { get; set; } = new List<WordResponseDto>();
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public bool HasPrevious => CurrentPage > 1;
        public bool HasNext => CurrentPage < TotalPages;
    }

    
}
