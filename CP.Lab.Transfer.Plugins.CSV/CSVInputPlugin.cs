using CP.Lab.Transfer.Common;
using log4net;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataAccess;
using System.Runtime.InteropServices;
using CP.Lab.Transfer.Plugins.SQLServer;
using CP.Lab.Transfer.Common.Exceptions;

namespace CP.Lab.Transfer.Plugins.CSV
{
    public class CSVInputPlugin : IInputPlugin
    {
        ILog _log;
        private string _filePath;
        private string _separator;
        private bool _hasHeader;
        static int STATIC_ROW_VALUE;
        static int INCLUDE_FIRST_ROW;
        DataTable _dt;

        public CSVInputPlugin(ILog log)
        {
            _log = log ?? throw new ArgumentNullException(nameof(log));
        }

        public void Init(IDictionary<string, string> parameters)
        {
            _log.Debug("Checking if all required parameters exist");

            if (parameters == null)
                throw new ArgumentNullException(nameof(parameters));
            if (!parameters.ContainsKey("filepath"))
                throw new ArgumentException("Missing 'filepath' input plugin parameter");
            if (!parameters.ContainsKey("separator"))
                throw new ArgumentException("Missing 'separator' input plugin parameter");
            if (!parameters.ContainsKey("hasheader"))
                throw new ArgumentException("Missing 'hasheader' input plugin parameter");

            _log.Debug("Filepath, separator, hasheader parameters exists");
            _log.Info("Init input plguin");
            _log.Debug("Init input plugin started");
            _filePath = parameters["filepath"];
            _separator = parameters["separator"];
            _hasHeader = Convert.ToBoolean(parameters["hasheader"]);

            if (parameters["hasheader"] == "false")
            {
                INCLUDE_FIRST_ROW = 1;
                _log.Debug("Include first row? = " + INCLUDE_FIRST_ROW);
            }
            else
            {
                INCLUDE_FIRST_ROW = 0;
                _log.Debug("Include first row? = " + INCLUDE_FIRST_ROW);
            }
            ReadCSVFile();
            _log.Debug("Plugin parameters: filepath: " + _filePath + " separator: " + _separator + " hasheader?: " + _hasHeader.ToString());
            _log.Info("Init input plugin ended");
        }

        private void ReadCSVFile()
        {
            _log.Debug("Reading and loading CSV file");
            _dt = DataTable.New.ReadLazy(_filePath);
        }

        public bool Read(out IDictionary<string, object> item)
        {
            _log.Debug("Reading item started");
            item = new Dictionary<string, object>();
            
            for (int r = STATIC_ROW_VALUE; r <= _dt.Rows.Count(); r++)
            {
                STATIC_ROW_VALUE++;
                for (int i = 0; i < _dt.ColumnNames.Count(); i++)
                {
                    if (_hasHeader == true)
                    {
                        try
                        {
                            _dt.Rows.ElementAt(r);
                        }
                        catch
                        {
                            return false;
                        }
                        item.Add(_dt.Rows.ElementAt(r).ColumnNames.ElementAt(i), _dt.Rows.ElementAt(r).Values.ElementAt(i));
                    }
                    else
                    {
                        if (INCLUDE_FIRST_ROW == 1)
                        {
                            item.Add(i.ToString(), _dt.ColumnNames.ElementAt(i));
                        }
                        else
                        {
                            if (r - 1 == _dt.Rows.Count())
                                item.Add(i.ToString(), _dt.Rows.ElementAt(r).Values.ElementAt(i)); //never reached?
                            else
                                item.Add(i.ToString(), _dt.Rows.ElementAt(r - 1).Values.ElementAt(i));
                        }
                    }
                }
                INCLUDE_FIRST_ROW = 0;
                _log.Debug("Reading item ended");
                return true;
            }     
            return false;
        }

        public void Init(string parametersInput)
        {
            _log.Debug("Reading input plugin parameters started");

            IDictionary<string, string> parameters = new Dictionary<string, string>();

            List<string> keyValuePairs = parametersInput.Split('|').ToList();
            try
            {
                foreach (var keyValuePair in keyValuePairs)
                {
                    string firstPart = keyValuePair.Split('=')[0].Trim();
                    string secondPart = keyValuePair.Split('=')[1].Trim();
                    parameters.Add(firstPart, secondPart);
                }

                _log.Debug("Reading input plugin parameters ended");
                if (!File.Exists(parameters["filepath"])) { _log.Error("File path is wrong"); throw new FilePathException("File path is wrong"); }
                if (String.IsNullOrEmpty(parameters["hasheader"]) || (!parameters["hasheader"].Equals("false") && !parameters["hasheader"].Equals("true")))
                {
                    _log.Error("Hasheader is not specified or it's value is different than true/false");
                    throw new InputParametersNotFoundException();
                }
                Init(parameters);
            }
            catch(Exception)
            {  
                _log.Error("One or more parameters were not found");
                throw new InputParametersNotFoundException();
            }
        }

        public string GetHelp()
        {
            string helpMessage = "You didn't provide all or invalid required parameters. CSV input plugin needs following parameters in following format:" +
                "--op \"filepath=here_provide_filepath_to_csv_file_from_your_disk|" +
                "separator=here_provide_separator e.g , ;|" +
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
                    
                }
                
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            _log.Debug("Disposing csv input plugin");
            Dispose(true);
            GC.SuppressFinalize(this);
        }


        #endregion
    }
}
