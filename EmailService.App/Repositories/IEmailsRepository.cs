using System.Collections.Generic;
using System.Threading.Tasks;
using EmailService.App.Models;

namespace EmailService.App.Repositories
{
    public interface IEmailsRepository
    {
        /// <summary>
        /// Get email which needs to be sent to customers
        /// </summary>
        Task<List<EmailModel>> GetEmailsToSendAsync();

        /// <summary>
        /// Update status of email
        /// Email which were not sent because of error should be updated to status error
        /// </summary>
        /// <param name="emailId">The id of email which needs to be updated</param>
        /// <param name="status">destination status</param>
        Task UpdateEmailStatusAsync(int emailId, EmailStatus status);
    }
}