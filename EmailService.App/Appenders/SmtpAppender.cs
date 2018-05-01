using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using log4net.Core;
using log4net.Layout;

namespace EmailService.App.Appenders
{
    public class SmtpAppender : log4net.Appender.SmtpAppender
    {
        public PatternLayout SubjectLayout { get; set; }

        protected override void SendBuffer(LoggingEvent[] events)
        {
            PrepareSubject(events);
            base.SendBuffer(events);
        }

        protected virtual void PrepareSubject(IEnumerable<LoggingEvent> events)
        {
            Subject = Subject.Replace("$machineName", Environment.MachineName)
                             .Replace("$evnironement", ConfigurationManager.AppSettings["EnvironmentName"]);
        }
    }
}