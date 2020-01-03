using CP.Lab.Transfer.Common;
using CP.Lab.Transfer.Common.Exceptions;
using CP.Lab.Transfer.Common.Models;
using CP.Lab.Transfer.Plugins.CSV;
using log4net;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace CP.Lab.Transfer.Business
{
    public class DefaultTransferManager : ITransferManager
    {
        ILog _log;
        private bool _stopOnItemError;
        public DefaultTransferManager(ILog log)
        {
            _log = log ?? throw new ArgumentNullException(nameof(log));
        }

        public void Init(IDictionary<string, string> parameters)
        {
            _log.Debug("Checking transfer manager parameter");

            if (parameters == null)
                throw new ArgumentNullException(nameof(parameters));
            if (!parameters.ContainsKey("stopOnItemError"))
                throw new ArgumentException("Missing 'stopOnItemError' transfer manager parameter");

            _log.Debug("Init transfer manager");
            _stopOnItemError = Convert.ToBoolean(parameters["stopOnItemError"]);
            _log.InfoFormat("Transfer manager initialized");
        }

        public void Init(string input)
        {
            _log.Debug("Reading transfer manager parameters");
            IDictionary<string, string> parameters = new Dictionary<string, string>();
            try
            {
                string firstPart = input.Split('=')[0].Trim();
                string secondPart = input.Split('=')[1].Trim();
                parameters.Add(firstPart, secondPart);
                Init(parameters);
            }
            catch(Exception)
            {
                _log.Error("Not all or invalid required parameters were provided");
                throw new InputParametersNotFoundException("Not all or invalid required parameters were provided");
            }
        }

        public TransferResult Transfer(IInputPlugin inputPlugin, IOutputPlugin outputPlugin)
        {
            _log.InfoFormat("Transfer started");
            try
            {
                int counter = 0;
                while (true)
                {
                    try
                    {
                        _log.Info("Reading next item from input plugin");
                        if (!inputPlugin.Read(out IDictionary<string, object> item))
                            break;

                        _log.Info("Writing item to output plugin");
                        outputPlugin.Write(item);
                        counter++;
                    }
                    catch (Exception itemEx) when (!_stopOnItemError)
                    {
                        _log.Warn(itemEx);
                    }
                }
                TransferResult success = new TransferResult { Id = 1, Text = "Success", ItemsTransfered = counter};
                _log.InfoFormat("Items transfered successfully");
                _log.Debug(success.Text + "--> Number of items transfered: " + success.ItemsTransfered);
                return success;
            }
            catch(Exception e)
            {
                TransferResult notCompleted = new TransferResult { Id = 2, Text = "Transfer not completed" + e.Message, ItemsTransfered = 0 };
                _log.InfoFormat("Items transfered not successfully");
                _log.Debug(notCompleted.Text + "--> Number of items transfered: " + notCompleted.ItemsTransfered);
                return notCompleted;
            }
            finally
            {
                _log.InfoFormat("Items transfer ended");
                _log.Debug("Disposing resources");
            }
        }

        public string GetHelp()
        {
            string helpMessage = "You didn't provide all or invalid required parameters. Default transfer manager needs following parameter in following format:" +
                "--tp  \"stopOnItemError=here write true or false based on if you want manager to stop when it encounter an error during item's transfer \"";
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
            _log.Debug("Disposing default transfer manager");
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
