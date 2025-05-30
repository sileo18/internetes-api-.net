using System.ComponentModel.DataAnnotations;

namespace WordsAPI.DTO_s
{
    public class ContactFormDTO
    {
        [Required] public string name { get; set; } = null!;
        
        [Required]
        public string email { get; set; } = null!;
        
        public string subject {get; set; }
        public string message {get; set; }

        public string ToString()
        {
            return $"Name: {name}, Email: {email}, Subject: {subject}, Message: {message}";
        }
    }
}
