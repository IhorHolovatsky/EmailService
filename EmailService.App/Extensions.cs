using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Configuration;
using System.Data.Common;
using System.Linq;
using log4net;
using MimeKit;

namespace EmailService.App
{
    public static class Extensions
    {
        public static void InvokeEx<T>(this T @this, Action<T> action) where T : ISynchronizeInvoke
        {
            if (@this.InvokeRequired)
            {
                @this.Invoke(action, new object[] { @this });
            }
            else
            {
                action(@this);
            }
        }

        public static T GetFieldValue<T>(this DbDataReader reader, string columnName)
        {
            if (reader[columnName] is DBNull)
            {
                return default(T);
            }

            return (T)reader[columnName];
        }

        public static int? ParseInt(this string value)
        {
            int intVal;

            if (int.TryParse(value, out intVal))
                return intVal;

            return null;
        }

        public static List<MailboxAddress> GetEmailAddresses(this string value, int? maxCount = null)
        {
            if (value.Contains(",")
                || value.Contains(";"))
            {
                var emailAddresses = value.Split(new[] { ";", "," }, StringSplitOptions.RemoveEmptyEntries)
                                          .Select(e => new MailboxAddress(e))
                                          .ToList();

                return maxCount.HasValue
                       && emailAddresses.Count > maxCount  
                            ? emailAddresses.Take(maxCount.Value).ToList() 
                            : emailAddresses;
            }

            return new List<MailboxAddress> { new MailboxAddress(value)};
        }

        public static T GetAppConfigValue<T>(this NameValueCollection colleciton, 
            string appSettingName,
            T defaultValue = default(T))
        {
            if (!colleciton.AllKeys.Contains(appSettingName))
                return defaultValue;

            return (T) Convert.ChangeType(colleciton[appSettingName], typeof(T));
        } 
    }
}