namespace WordsAPI.Config
{
    // Config/EmailSettings.cs
    namespace WordsAPI.Config
    {
        public class EmailSettings
        {
            // Propriedades SMTP (podem ser úteis para fallback, mas não para o cliente SendGrid API)
            public string SmtpHost { get; set; } = string.Empty;
            public int SmtpPort { get; set; }
            public bool EnableSsl { get; set; }

            public string SenderName { get; set; } = string.Empty;
            public string SenderEmail { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty; // Não mais usada pelo cliente SendGrid API
            public string ReceiverEmail { get; set; } = string.Empty;

            public string SendGridApiKey { get; set; } = string.Empty; // <<<< ESTA PROPRIEDADE
        }
    }
}
