namespace WordsAPI.DTO_s
{
    public class ContactFormDTO
    {
        public string name {get; set;} 
        public string email {get; set;}  
        public string subject {get; set; }
        public string message {get; set; }

        public string ToString()
        {
            return $"Name: {name}, Email: {email}, Subject: {subject}, Message: {message}";
        }
    }
}
