using System;

namespace EmailService.App.Models
{
    public class EmailModel
    {
        public uint EmailId { get; set; }
        public string EmailFrom { get; set; }
        public string EmailPassword { get; set; }
        public string OutgoingServer { get; set; }
        public int OutgoingPort { get; set; }
        public string EmailTo { get; set; }
        public string EmailCc { get; set; }
        public string EmailBcc { get; set; }
        public string EmailSubject { get; set; }
        public string EmailSaluation { get; set; }
        public string EmailBody { get; set; }
        public string EmailAttachments { get; set; }
        public bool DeleteAfterSend { get; set; }

        public EmailStatus Status { get; set; }
        public DateTime CreatedDateTime { get; set; }
        public DateTime? SentDateTime { get; set; }
    }
}