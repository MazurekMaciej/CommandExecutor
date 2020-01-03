using CP.Lab.Transfer.Common;
using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CsvHelper;
using System.IO;
using CP.Lab.Transfer.Common.Exceptions;

namespace CP.Lab.Transfer.Plugins.CSV
{
    public class CSVOutputPlugin : IOutputPlugin
    {
        ILog _log;
        private string _filePath;
        private string _separator;
        private bool _hasHeader;
        IDictionary<string, string> _mapping = new Dictionary<string, string>();
        StreamWriter _streamWriter;
        private static object _locker = new object();

        public CSVOutputPlugin(ILog log)
        {
            _log = log ?? throw new ArgumentNullException(nameof(log));
        }

        public void Init(IDictionary<string, string> parameters)
        {
            _log.Debug("Checking if all required parameters exist");

            if (parameters == null)
                throw new ArgumentNullException(nameof(parameters));
            if (!parameters.ContainsKey("filepath"))
                throw new ArgumentException("Missing 'filepath' output plugin parameter");
            if (!parameters.ContainsKey("separator"))
                throw new ArgumentException("Missing 'separator' output plugin parameter");
            if (!parameters.ContainsKey("hasheader"))
                throw new ArgumentException("Missing 'hasheader' output plugin parameter");


            _log.Info("Init output plguin");
            _log.Debug("Init output plugin started");

            _filePath = parameters["filepath"];
            _separator = parameters["separator"];
            _hasHeader = Convert.ToBoolean(parameters["hasheader"]);

            OpenFileStream();
            if (_hasHeader == true)
            {
                WriteHeader();
            }

            _log.Debug("Plugin parameters: filepath: " + _filePath + " separator: " + _separator + " hasheader?: " + _hasHeader.ToString());
            _log.Info("Init output plugin ended");
        }

        private void OpenFileStream()
        {
            //using (var fileStream = new FileStream(String.Format("", sth), FileMode.OpenOrCreate))
            lock (_locker)
            {
                _log.Debug("Creating stream writer with given filepath");
                _streamWriter = new StreamWriter(_filePath, true);  //fileStream
                _log.Debug("Stream writer with given filepath created");
            }
        }

        private void WriteHeader()
        {
            _log.Info("Writing item's header");
            var records = new List<string>();
            for (int column = 0; column < _mapping.Count(); column++)
            {
                var headerField = string.Format("{0}", _mapping.ElementAt(column).Value);
                records.Add(headerField);
            }
            var csvWriter = new CsvWriter(_streamWriter);
            csvWriter.Configuration.Delimiter = _separator;
            foreach (var value in records)
            {
                csvWriter.WriteField(value);
            }
            csvWriter.NextRecord();
            csvWriter.Flush();
            _log.Info("Item's header written");
        }

        public void Write(IDictionary<string, object> item)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            _log.Debug("Writing item to csv file started");

            var records = new List<string>();
            for (int i = 0; i < _mapping.Count(); i++) 
            {
                var field = item.Where(k => k.Key == _mapping.ElementAt(i).Value).SingleOrDefault();
                var columnField = string.Format("{0}", field.Value);
                records.Add(columnField);
            }

            var csvWriter = new CsvWriter(_streamWriter);
            csvWriter.Configuration.Delimiter = _separator;
            foreach (var value in records)
            {
                csvWriter.WriteField(value);
            }
            csvWriter.NextRecord();
            csvWriter.Flush();

            _log.Debug("Writing item to csv file ended");
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

                if (!Directory.Exists(Path.GetDirectoryName(parameters["filepath"])))
                {
                    _log.Error("Given directory does not exists");
                    throw new FilePathException("Given directory is not valid");
                }
                if (String.IsNullOrEmpty(parameters["hasheader"]) || (!parameters["hasheader"].Equals("false") && !parameters["hasheader"].Equals("true")))
                {
                    _log.Error("Hasheader's not specified or it's value is different than true/false");
                    throw new InputParametersNotFoundException("Not all or invalid required parameters were provided");
                }
                if (String.IsNullOrEmpty(parameters["separator"]))
                {
                    _log.Error("Separator is not specified");
                    throw new InputParametersNotFoundException("Separator is not specified");
                }
                _log.Debug("Reading output plugin parameters ended");
                ReadMappingParameters(parameters["mapping"]);
                Init(parameters);
            }
            catch(Exception)
            {
                //_log.Error("Not all required parameters were provided");
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

        public string GetHelp()
        {
            string helpMessage = "You didn't provide all or invalid required parameters. CSV output plugin needs following parameters in following format:" +
                "--op \"filepath=here_provide_filepath_to_csv_file_from_your_disk|" +
                "separator=here_provide_separator e.g , ; |" +
                "hasheader=here_write true or false based on if csv file has column header|" +
                "mapping=here_provide_mapping i.e. column_name:item_field~column_name2:item_field2\"";
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
                    if (_streamWriter != null)
                        _streamWriter.Dispose();
                }    
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            _log.Debug("Disposing csv output plugin");
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion

    }
}
