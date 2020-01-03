using CP.Lab.Transfer.Common;
using CP.Lab.Transfer.Common.Exceptions;
using log4net;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using MailKit.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace CP.Lab.Transfer.Plugins.Mail
{
    //reads only gmail mails, with given format e.g.
    //Column name: column value
    //Column name2: column value2
    public class MailInputPlugin : IInputPlugin
    {
        ILog _log;
        IDictionary<string, string> _mapping = new Dictionary<string, string>();
        private string _sourceEmail;
        private string _login;
        private string _password;
        private string _emailSubject;
        public MailInputPlugin(ILog log)
        {
            _log = log ?? throw new ArgumentNullException(nameof(log));
        }
        public void Init(IDictionary<string, string> parameters)
        {
            _log.Debug("Checking if all required output plugin parameters exist");

            if (parameters == null)
                throw new ArgumentNullException(nameof(parameters));
            if (!parameters.ContainsKey("email"))
                throw new ArgumentException("Missing 'email' output plugin parameter");
            if (!parameters.ContainsKey("login"))
                throw new ArgumentException("Missing 'login' output plugin parameter");
            if (!parameters.ContainsKey("password"))
                throw new ArgumentException("Missing 'password' output plugin parameter");
            if (!parameters.ContainsKey("emailSubject"))
                throw new ArgumentException("Missing 'emailSubject' output plugin parameter");

            _log.Debug("Initializing output plugin started");
            _log.InfoFormat("Init output plugin started");
            try
            {
                _sourceEmail = parameters["email"];
                _login = parameters["login"];
                _password = parameters["password"];
                _emailSubject = parameters["emailSubject"];
            }
            catch (Exception e)
            {
                throw new InputParametersNotFoundException("Not all required input parameters were provided");
            }

            _log.Debug("Init output plugin ended");
            _log.InfoFormat("Output plugin initialized");
        }

        public void Init(string inputParameters)
        {
            _log.Debug("Reading output plugin parameters started");

            IDictionary<string, string> parameters = new Dictionary<string, string>();
            try
            {
                List<string> keyValuePairs = inputParameters.Split('|').ToList();
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
            catch (Exception)
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

        public bool Read(out IDictionary<string, object> item)
        {
            //  throw new NotImplementedException();
            item = new Dictionary<string, object>();

            using (var client = new ImapClient())
            {
                /*   var query = SearchQuery.DeliveredAfter(DateTime.Parse("2019-12-12"))
                                           .And SearchQuery.SubjectContains(_emailSubject).And(SearchQuery.Seen);*/
                // For demo-purposes, accept all SSL certificates
                client.ServerCertificateValidationCallback = (s, c, h, e) => true;

                client.Connect("imap.gmail.com", 993, SecureSocketOptions.SslOnConnect);
                //client.AuthenticationMechanisms.Remove("XOAUTH2");

                client.Authenticate(_login, _password);

                // The Inbox folder is always available on all IMAP servers...
                var inbox = client.Inbox;
                inbox.Open(FolderAccess.ReadWrite);
                

                foreach (var summary in inbox.Fetch(0, -1, MessageSummaryItems.Full | MessageSummaryItems.UniqueId | MessageSummaryItems.Flags))
                {
                    if (summary.Envelope.Subject.Contains("Item transfer") && summary.Flags.Value != MessageFlags.Seen)
                    {
                        Console.WriteLine("[summary] {0:D2}: {1} {2}", summary.Index, summary.Envelope.Subject, summary.Flags);
                        if (summary.TextBody != null)
                        {
                            // this will download *just* the text/plain part
                            var text = inbox.GetBodyPart(summary.UniqueId, summary.TextBody);
                            // Console.WriteLine(text);
                            string[] lines = text.ToString().Split(
                                new[] { "\r\n", "\r", "\n" },
                                StringSplitOptions.None);

                            for (int i = 0; i < _mapping.Count(); i++)
                            {
                                var line = lines.Where(z => z.Contains(_mapping.Keys.ElementAt(i))).FirstOrDefault();
                                try
                                {
                                    var key = line.Split(':')[0];
                                    var value = line.Split(':')[1].Trim(' ');
                                    item.Add(key, value);
                                    Console.WriteLine("Key: " + key + " Value: " + value);
                                }
                                catch (IndexOutOfRangeException e)
                                {
                                    continue;
                                }
                            }
                            inbox.AddFlags(summary.UniqueId, MessageFlags.Seen, true);
                            return true;
                            //  ParseHtmlTable(text.ToString());
                            //XElement table = XElement.Parse(text.ToString());
                        }
                    }
                }
                client.Disconnect(true);
                return false;
            }
        }

        public string GetHelp()
        {
            string helpMessage = "You didn't provide all or invalid required parameters. Mail input plugin needs following parameters in following format:" +
                "--op \"email=here provide destination email address e.g. email_address@gmail.com|" +
                "login=here write login to your email|" +
                "passoword=here write password to your email|" +
                "emailSubject=here write subject of email you want to search|" +
                "mapping=here provide mapping i.e. column_name:item_field~column_name2:item_field2\"";
            return helpMessage;
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

        private void ParseHtmlTable(string html)
        {
            var columns = new List<List<string>>();

            XElement table = XElement.Parse(html);
            XElement headings = table.Elements("tr").First();

            foreach (XElement th in headings.Elements("th"))
            {
                string heading = th.Value;
                var column = new List<string> { heading };
                columns.Add(column);
            }

            foreach (XElement tr in table.Elements("tr").Skip(1))
            {
                int i = 0;

                foreach (XElement td in tr.Elements("td"))
                {
                    string value = td.Value;
                    columns[i].Add(value);
                    i++;
                }
            }

            int rows = columns[0].Count;
            int cols = columns.Count;

            for (int i = 1; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    Console.WriteLine("{0}/{1}", columns[j][0], columns[j][i]);
                }

                Console.WriteLine();
            }
        }

    }
}
