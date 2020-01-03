using CP.Lab.Transfer.Common;
using CP.Lab.Transfer.Common.Exceptions;
using log4net;
using MimeKit;
using MailKit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MailKit.Net.Smtp;
using MailKit.Security;
using System.IO;

namespace CP.Lab.Transfer.Plugins.Mail
{
    public class MailOutputPlugin : IOutputPlugin
    {
        ILog _log;
        IDictionary<string, string> _mapping = new Dictionary<string, string>();
        private string _smtpServer;
        private string _port;
        private string _sourceEmail;
        private string _login;
        private string _password;
        private string _emailSubject;
        private string _bodyFilepath;

        public MailOutputPlugin(ILog log)
        {
            _log = log ?? throw new ArgumentNullException(nameof(log));
        }

        public void Init(IDictionary<string, string> parameters)
        {
            _log.Debug("Checking if all required output plugin parameters exist");

            if (parameters == null)
                throw new ArgumentNullException(nameof(parameters));
            if (!parameters.ContainsKey("smtpServer"))
                throw new ArgumentException("Missing 'smtpServer' output plugin parameter");
            if (!parameters.ContainsKey("port"))
                throw new ArgumentException("Missing 'port' output plugin parameter");
            if (!parameters.ContainsKey("email"))
                throw new ArgumentException("Missing 'email' output plugin parameter");
            if (!parameters.ContainsKey("login"))
                throw new ArgumentException("Missing 'login' output plugin parameter");
            if (!parameters.ContainsKey("password"))
                throw new ArgumentException("Missing 'password' output plugin parameter");
            if (!parameters.ContainsKey("subject"))
                throw new ArgumentException("Missing 'subject' output plugin parameter");
            if (!parameters.ContainsKey("bodyFilepath"))
                throw new ArgumentException("Missing 'bodyFilepath' output plugin parameter");
            if (!parameters.ContainsKey("mapping"))
                throw new ArgumentException("Missing 'mapping' output plugin parameter");

            _log.Debug("Initializing output plugin started");
            _log.InfoFormat("Init output plugin started");
            try
            {
                _smtpServer = parameters["smtpServer"];
                _port = parameters["port"];
                _sourceEmail = parameters["email"];
                _login = parameters["login"];
                _password = parameters["password"];
                _emailSubject = parameters["subject"];
                _bodyFilepath = parameters["bodyFilepath"];
            }
            catch (Exception e)
            {
                _log.Error(e.Message);
                throw new InputParametersNotFoundException("Not all required input parameters were provided");
            }

            _log.Debug("Init output plugin ended");
            _log.InfoFormat("Output plugin initialized");
        }

        public void Init(string outputParameters)
        {
            _log.Debug("Reading output plugin parameters started");

            IDictionary<string, string> parameters = new Dictionary<string, string>();
            try
            {
                List<string> keyValuePairs = outputParameters.Split('|').ToList();
                parameters = new Dictionary<string, string>();
                foreach (var keyValuePair in keyValuePairs)
                {
                    string firstPart = keyValuePair.Split('=')[0].Trim();
                    string secondPart = keyValuePair.Split('=')[1].Trim();
                    parameters.Add(firstPart, secondPart);
                }
                _log.Debug("Reading output plugin parameters ended");
                ReadMappingParameters(parameters["mapping"]);
                Init(parameters);
            }
            catch(Exception)
            {
                _log.Error("Not all required parameters were provided");
                throw new InputParametersNotFoundException("Not all required or invalid parameters were provided");
            }
        }
        private void ReadMappingParameters(string mapParameters)
        {
            _log.Debug("Reading mapping parameters started");
            List<string> pairs = mapParameters.Split('~').ToList();

            foreach (var pair in pairs)
            {
                string firstPart = pair.Split(':')[0];
                string secondPart = pair.Split(':')[1];
                _mapping.Add(firstPart, secondPart);
            }
            _log.Debug("Mapping parameters read");
        }

        public void Write(IDictionary<string, object> item)
        {
            _log.Debug("Writing mail item started");

            var message = new MimeMessage();
            var destinationEmail = item.Where(k => k.Key.ToLower().Contains("email")).SingleOrDefault();
            message.From.Add(new MailboxAddress("Command Executor User", _sourceEmail));
            message.To.Add(new MailboxAddress("Mail plugin client", destinationEmail.Value.ToString()));
            message.Subject = _emailSubject;

            var bodyMessage = GetTextFromFile(_bodyFilepath);
            message.Body = new TextPart("plain")
            {
                Text = bodyMessage
            };

            using (var client = new SmtpClient())
            {
                client.ServerCertificateValidationCallback = (s, c, h, e) => true;
                client.Connect(_smtpServer, int.Parse(_port), SecureSocketOptions.StartTls);
                client.Authenticate(_login, _password);       
                
                client.Send(message);
                client.Disconnect(true);
            }
            _log.Debug("Writing mail item ended");
        }

        public string GetHelp()
        {
            _log.Debug("returning help message about plugin");
            string helpMessage = "You didn't provide all or invalid required parameters. Mail output plugin needs following parameters in following format:" +
                "--op \"smtpServer=here provide smtp server of your email|" +
                "port=here provide port of smtp server|" +
                "email=here provide destination email address e.g. email_address@gmail.com|" +
                "login=login of your email|" +
                "password=password of your email|" +
                "subject=here provide subject of email|bodyFilePath=here provide filepath to txt file you want to have as a body|\"";
            return helpMessage;
        }

        public string GetTextFromFile(string filePath)
        {
            _log.Debug("reading text from txt file");
            var readResults = new StringBuilder();
            _log.Debug("creating StreamReader");
            using (var reader = new StreamReader(@filePath))
            {
                _log.Debug("loop while (!reader.EndOfStream)");
                while (!reader.EndOfStream)
                {
                    _log.Debug("reading line from reader");
                    var line = reader.ReadLine();
                    readResults.AppendLine(line);
                    _log.Debug("readResult=" + readResults);
                }
            }
            _log.Debug("returning results");
            return readResults.ToString();
        }

        #region IDisposable Support
        private bool disposedValue = false; 

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    
                }
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion

    }
}
