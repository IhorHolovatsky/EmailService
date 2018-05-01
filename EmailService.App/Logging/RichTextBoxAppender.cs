using System;
using System.Drawing;
using System.Windows.Forms;
using log4net.Appender;
using log4net.Core;

namespace EmailService.App.Logging
{
    public class RichTextBoxAppender : AppenderSkeleton
    {
        private RichTextBox _textBox;
        public string FormName { get; set; }
        public string ControlName { get; set; }

        protected override void Append(LoggingEvent loggingEvent)
        {
            if (_textBox == null)
            {
                if (string.IsNullOrEmpty(FormName) ||
                    string.IsNullOrEmpty(ControlName))
                    return;

                var form = Application.OpenForms[FormName];
                if (form == null)
                    return;

                _textBox = form.Controls[ControlName] as RichTextBox;
                if (_textBox == null)
                    return;
            }

            _textBox.InvokeEx(tb =>
            {
                if (loggingEvent.Level == Level.Error) tb.SelectionColor = Color.Red;
                else if (loggingEvent.Level == Level.Debug) tb.SelectionColor = Color.Gray;
                else tb.SelectionColor = Color.Black;

                tb.AppendText(RenderLoggingEvent(loggingEvent));
            });

            _textBox.InvokeEx(tb =>
            {
                if (tb.Lines.Length > 1000)
                    tb.Text = string.Empty;
            });
        }
    }
}