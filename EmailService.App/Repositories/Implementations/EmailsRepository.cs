using System;
using System.Collections.Generic;
using System.Configuration;
using System.Threading.Tasks;
using EmailService.App.Models;
using MySql.Data;
using MySql.Data.MySqlClient;

namespace EmailService.App.Repositories.Implementations
{
    public class EmailsRepository : IEmailsRepository
    {
        /// <inheritdoc />
        public async Task<List<EmailModel>> GetEmailsToSendAsync()
        {
            var emailsToSend = new List<EmailModel>();

            using (var connection = CreateConnection())
            {
                await connection.OpenAsync();

                var sqlQuery = @"SELECT
                                 *
                                 FROM
                                   `testserveremail`.`emailstosend`
                                 WHERE 
                                    sent = 0";
                var command = connection.CreateCommand();
                command.CommandText = sqlQuery;

                var reader = await command.ExecuteReaderAsync();
                while (reader.Read())
                {
                    var emailModel = new EmailModel()
                    {
                        EmailId = reader.GetFieldValue<uint>("emailid"),
                        EmailFrom = reader.GetFieldValue<string>("emailfrom"),
                        EmailPassword = reader.GetFieldValue<string>("emailpassword"),
                        OutgoingServer = reader.GetFieldValue<string>("outgoingserver"),
                        OutgoingPort = reader.GetFieldValue<string>("outgoingport").ParseInt() ?? 0,
                        EmailTo = reader.GetFieldValue<string>("emailto"),
                        EmailCc = reader.GetFieldValue<string>("emailcc"),
                        EmailBcc = reader.GetFieldValue<string>("emailbcc"),
                        EmailSubject = reader.GetFieldValue<string>("emailsubject"),
                        EmailSaluation = reader.GetFieldValue<string>("emailsalutation"),
                        EmailBody = reader.GetFieldValue<string>("emailbody"),
                        Status = (EmailStatus)reader.GetFieldValue<int>("sent"),
                        CreatedDateTime = reader.GetFieldValue<DateTime>("enteredindb"),
                        SentDateTime = reader.GetFieldValue<DateTime?>("actuallysent"),
                        EmailAttachments = reader.GetFieldValue<string>("attachments"),
                        DeleteAfterSend = reader.GetFieldValue<bool>("deleteaftersend")
                    };

                    emailsToSend.Add(emailModel);
                }
            }

            return emailsToSend;
        }

        /// <inheritdoc />
        public async Task UpdateEmailStatusAsync(int emailId, EmailStatus status)
        {
            using (var connection = CreateConnection())
            {
                await connection.OpenAsync();

                string sqlQuery;
                switch (status)
                {
                    case EmailStatus.Sent:
                        sqlQuery = @"UPDATE emailstosend
                                 SET sent = @status,
                                     actuallysent = @sentTime,
                                     emailpassword = ''
                                 WHERE emailid = @emailId";
                        break;
                    default:
                        sqlQuery = @"UPDATE emailstosend
                                 SET sent = @status,
                                     actuallysent = @sentTime
                                 WHERE emailid = @emailId";
                        break;
                }

                var command = connection.CreateCommand();
                command.CommandText = sqlQuery;
                command.Parameters.AddWithValue("@emailId", emailId);
                command.Parameters.AddWithValue("@status", (int)status);
                command.Parameters.AddWithValue("@sentTime", status == EmailStatus.Sent
                                                                ? (object)DateTime.Now
                                                                : null);

                await command.ExecuteReaderAsync();
            }
        }

        #region Private methods

        private MySqlConnection CreateConnection()
        {
            var connectionString = ConfigurationManager.ConnectionStrings["MySqlConnection"].ConnectionString;
            return new MySqlConnection(connectionString);
        }

        #endregion
    }
}