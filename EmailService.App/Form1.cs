using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using EmailService.App.Models;
using EmailService.App.Repositories;
using EmailService.App.Repositories.Implementations;
using log4net;
using log4net.Config;
using log4net.Repository.Hierarchy;
using log4net.Util;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Newtonsoft.Json;

namespace EmailService.App
{
    public partial class Form1 : Form
    {
        public CancellationTokenSource CancellationToken { get; set; } = new CancellationTokenSource();
        public Task EmailSendingWorker { get; set; }
        public Task AttachmentsCleanupTask { get; set; }

        #region Configuration
        public int EmailSendingInterval => int.Parse(ConfigurationManager.AppSettings["EmailSendingInterval"]);
        public int AttachmentsCleanupInterval => int.Parse(ConfigurationManager.AppSettings["AttachmentsCleanupInterval"]);
        public int MaxEmailsPerUserPerBatch => int.Parse(ConfigurationManager.AppSettings["MaxEmailsPerUserPerBatch"]);
        public int? MaxCcCount => ConfigurationManager.AppSettings.GetAppConfigValue<int?>("MaxCcCount");
        public int? MaxBccCount => ConfigurationManager.AppSettings.GetAppConfigValue<int?>("MaxBccCount");
        #endregion

        private readonly IEmailsRepository _emailsRepository = new EmailsRepository();
        private readonly IAwsFileRepository _awsFileRepository = new AwsFileRepository();
        private readonly ConcurrentQueue<string> _attachmentsToDelete = new ConcurrentQueue<string>();

        /// <summary>
        /// logger is configured to send errors to EMAIL + to database
        /// also all info messages will be logged to database
        /// </summary>
        private readonly ILog _log = LogManager.GetLogger(typeof(Form1));

        public Form1()
        {
            InitializeComponent();
            btnStop.Enabled = false;

            XmlConfigurator.Configure(new FileInfo("logging.config"));

            AttachmentsCleanupTask = new Task(CleanupAttachments);
            AttachmentsCleanupTask.Start();
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            btnStart.Enabled = false;
            btnStop.Enabled = true;

            EmailSendingWorker = new Task(Start);
            EmailSendingWorker.Start();
        }
        private void btnStop_Click(object sender, EventArgs e)
        {
            //Send cancellation token.. so email sending loop will be stopped when it will be a good point to stop
            CancellationToken.Cancel();
        }

        private void Start()
        {
            try
            {
                while (true)
                {
                    var emails = _emailsRepository.GetEmailsToSendAsync().Result;
                    //var emails = GetRandomEmails(50);

                    if (emails.Count > 0)
                    {
                        emails = ApplyMaxEmailsPerUserPerBatch(emails);
                    }

                    _log.Info($"Found {emails.Count} emails to send.");

                    var stopWatch = new Stopwatch();
                    stopWatch.Start();

                    var tasks = emails.Select(e => Task.Factory.StartNew(() => SendEmail(e)))
                                      .ToArray();

                    try
                    {
                        Task.WaitAll(tasks);
                    }
                    catch (AggregateException) { /* Ignore errors in tasks, errors are being logged */}

                    stopWatch.Stop();

                    _log.Info($"{tasks.Count(t => !t.IsFaulted)} emails were sent.");
                    _log.Info($"{tasks.Count(t => t.IsFaulted)} emails were fault due to exception.");
                    _log.Info($"{emails.Count} emails sending took {stopWatch.Elapsed.TotalSeconds} seconds.");

                    var sleepSeconds = EmailSendingInterval - (int)stopWatch.Elapsed.TotalSeconds;
                    sleepSeconds = sleepSeconds < 0
                                            ? 0
                                            : sleepSeconds;

                    _log.Info($"Sleeping {sleepSeconds} seconds.");
                    Thread.Sleep(sleepSeconds * 1000);

                    //Stop worker if cancellation was requested
                    if (CancellationToken.Token.IsCancellationRequested)
                    {
                        btnStart.InvokeEx(b => b.Enabled = true);
                        btnStop.InvokeEx(b => b.Enabled = false);

                        _log.Info($"{nameof(EmailSendingWorker)} was stopped by user.");
                        break;
                    }
                }
            }
            catch (Exception e)
            {
                _log.Error($"{nameof(EmailSendingWorker)} was stopped because of exception.", e);
                btnStart.InvokeEx(b => b.Enabled = true);
                btnStop.InvokeEx(b => b.Enabled = false);
            }
        }

        private void CleanupAttachments()
        {
            while (true)
            {
                if (!_attachmentsToDelete.IsEmpty)
                {
                    string attachment;
                    while (_attachmentsToDelete.TryDequeue(out attachment))
                    {
                        var filesToDelete = new List<string> { attachment };
                        if (attachment.Contains(',')
                            || attachment.Contains(';'))
                        {
                            filesToDelete = attachment.Split(new[] { ",", ";" }, StringSplitOptions.RemoveEmptyEntries)
                                                      .ToList();
                        }

                        //delete files
                        filesToDelete.ForEach(f =>
                        {
                            try
                            {
                                _awsFileRepository.RemoveFile(f);
                                _log.Debug($"[{nameof(AttachmentsCleanupTask)}] Removed '{f}' file.");
                            }
                            catch (Exception e)
                            {
                                _log.Error($"[{nameof(AttachmentsCleanupTask)}] Error during deleting attachment '{attachment}'.", e);
                            }
                        });
                    }
                }

                _log.Debug($"[{nameof(AttachmentsCleanupTask)}] Sleeping {AttachmentsCleanupInterval} seconds.");


                Thread.Sleep(AttachmentsCleanupInterval * 1000);
            }
        }

        #region For testing

        private List<EmailModel> GetRandomEmails(int emailCount)
        {
            return Enumerable.Range(0, emailCount)
                .Select(i => new EmailModel()
                {
                    EmailId = (uint)i,
                    EmailFrom = "jeff.smith.test1@gmail.com",
                    EmailPassword = "w4DkUp8Ftt",
                    EmailTo = "Ihor.Golovatskiy@outlook.com",
                    EmailBody = "IhorTest",
                    EmailSubject = $"Hi Jeff {i}"
                })
                .ToList();
        }

        #endregion


        private SmtpClient CreateSmtpClient()
        {
            var client = new SmtpClient
            {
                // For demo-purposes, accept all SSL certificates (in case the server supports STARTTLS)
                ServerCertificateValidationCallback = (s, c, h, e) => true
            };

            return client;
        }

        private void SendEmail(EmailModel e)
        {
            try
            {
                var mailClient = CreateSmtpClient();

                //TODO: login to pass needed smtp paramters
                mailClient.Connect(e.OutgoingServer, e.OutgoingPort);

                // Note: since we don't have an OAuth2 token, disable
                // the XOAUTH2 authentication mechanism.
                mailClient.AuthenticationMechanisms.Remove("XOAUTH2");
                mailClient.Authenticate(e.EmailFrom, e.EmailPassword);

                var message = CreateMailMessage(e);

                mailClient.Send(message);
                mailClient.Disconnect(true);

               Task.WaitAll(_emailsRepository.UpdateEmailStatusAsync((int)e.EmailId, EmailStatus.Sent));

                //Add to queue
                if (e.DeleteAfterSend
                    && !string.IsNullOrEmpty(e.EmailAttachments))
                {
                    _attachmentsToDelete.Enqueue(e.EmailAttachments);
                }

                _log.Debug($"EmailId - '{e.EmailId} was sent.");
            }
            catch (Exception ex)
            {
                _log.Error($@"Error during sending email to {e.EmailTo}.
                              Model: {JsonConvert.SerializeObject(e)}", ex);
                Task.WaitAll(_emailsRepository.UpdateEmailStatusAsync((int)e.EmailId, EmailStatus.Error));

                throw;
            }
        }

        private MimeMessage CreateMailMessage(EmailModel model)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(model.EmailFrom));

            message.To.AddRange(model.EmailTo.GetEmailAddresses());

            if (!string.IsNullOrEmpty(model.EmailCc))
                message.Cc.AddRange(model.EmailTo.GetEmailAddresses(MaxCcCount));

            if (!string.IsNullOrEmpty(model.EmailBcc))
                message.Bcc.AddRange(model.EmailTo.GetEmailAddresses(MaxBccCount));

            message.Subject = model.EmailSubject;

            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = !string.IsNullOrEmpty(model.EmailSaluation)
                                    ? $"{model.EmailSaluation} <br/> {model.EmailBody}"
                                    : model.EmailBody
            };

            //Login for attachments
            if (!string.IsNullOrEmpty(model.EmailAttachments))
            {
                var attachmentPaths = model.EmailAttachments.Contains(";")
                                      || model.EmailAttachments.Contains(",")
                                        ? model.EmailAttachments.Split(new[] { ",", ";" }, StringSplitOptions.RemoveEmptyEntries)
                                                                .ToList()
                                        : new List<string> { model.EmailAttachments };

                attachmentPaths.ForEach(a =>
                {
                    var file = _awsFileRepository.DownloadFile(a);
                    bodyBuilder.Attachments.Add(Path.GetFileName(a), file);
                });
            }

            message.Body = bodyBuilder.ToMessageBody();
            return message;
        }

        private List<EmailModel> ApplyMaxEmailsPerUserPerBatch(List<EmailModel> emails)
        {
            _log.Debug(string.Join("\n", emails.GroupBy(e => e.EmailFrom)
                                                       .Select(e => $"{e.Key} - {e.Count()} pending emails")));

            _log.Debug($"Applying {nameof(MaxEmailsPerUserPerBatch)} - {MaxEmailsPerUserPerBatch}");
            emails = emails.GroupBy(e => e.EmailFrom)
                           .Select(e => e.Take(MaxEmailsPerUserPerBatch))
                           .SelectMany(e => e)
                           .ToList();

            _log.Debug(string.Join("\n", emails.GroupBy(e => e.EmailFrom)
                                               .Select(e => $"{e.Key} - {e.Count()} pending emails")));

            return emails;
        }
    }
}
