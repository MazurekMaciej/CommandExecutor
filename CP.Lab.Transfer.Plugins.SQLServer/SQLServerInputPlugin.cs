using CP.Lab.Transfer.Common;
using CP.Lab.Transfer.Common.Exceptions;
using log4net;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CP.Lab.Transfer.Plugins.SQLServer
{
    public class SQLServerInputPlugin : IInputPlugin
    {
        private ILog _log;
        private string _connectionString;
        private string _tableName;
        private IDictionary<string, string> _mapping = new Dictionary<string, string>();
        SqlConnection _connection;
        SqlCommand _command;
        SqlDataReader _reader;

        public SQLServerInputPlugin(ILog log)
        {
            _log = log ?? throw new ArgumentNullException(nameof(log));
        }
        public void Init(IDictionary<string, string> parameters)
        {
            _log.Debug("Checking if all required input plugin parameters exist");

            if (parameters == null)
                throw new ArgumentNullException(nameof(parameters));
            if (!parameters.ContainsKey("connectionstring"))
                throw new ArgumentException("Missing 'connectionstring' input plugin parameter");
            if (!parameters.ContainsKey("table"))
                throw new ArgumentException("Missing 'table' input plugin parameter");
            if (!parameters.ContainsKey("mapping"))
                throw new ArgumentException("Missing 'mapping' input plugin parameter");

            _log.Debug("Init input plugin started");
            _log.InfoFormat("Init input plugin started");
            try
            {
                _connectionString = parameters["connectionstring"];
                _tableName = parameters["table"];
                SplitMappingParameters(parameters["mapping"]);
            }
            catch
            {
               // _log.Error("Not all required input parameters were provided");
                throw new InputParametersNotFoundException("Not all or invalid required input parameters were provided");
            }
            ConnectAndExecuteQuery();

            _log.Debug("Init input plugin ended");
            _log.InfoFormat("Input plugin initialized");
        }

        private void ConnectAndExecuteQuery()
        {
            _log.Debug("Connecting to database and executing given query");
            try
            {
                _connection = new SqlConnection(_connectionString);
                _connection.Open();
                _command = _connection.CreateCommand();
                _command.CommandType = CommandType.Text;
                _command.CommandText = String.Format("SELECT * FROM [{0}]", _tableName);
                _reader = _command.ExecuteReader();
            }
            catch(Exception e)
            {
                throw;
            }
            _log.Debug("Database connected and query executed");
        }

        public bool Read(out IDictionary<string, object> item)
        {
            _log.Debug("Reading item from database");
            item = new Dictionary<string, object>();

            var result = _reader.Read();

            if (result)
            {
                foreach (var columnName in _mapping.Keys)
                {
                    int columnIndex = -1;
                    try
                    {
                        columnIndex = _reader.GetOrdinal(columnName);
                    }
                    catch (IndexOutOfRangeException ex)
                    {
                        throw new ArgumentOutOfRangeException(String.Format("Wrong mapping: column name {0} does not exists", columnName), ex);
                    }
                    item.Add(_mapping[columnName], _reader.IsDBNull(columnIndex) ? null : _reader.GetValue(columnIndex)?.ToString());
                }
            }
            _log.Debug("Item read from database");
            return result;
        }

        private void CheckConnectionString(string conString)
        {
            _log.Debug("Checking if connection string is valid");
            _log.InfoFormat("Checking if connection string is valid");
            try
            {
                using (SqlConnection conn = new SqlConnection(conString))
                {
                    conn.Open(); // throws if invalid connection
                }
            }
            catch
            {
              //  _log.Error("Connection string is invalid");
                throw new InvalidConnectionStringException("Connection string is invalid");
            }
            _log.Debug("Connection string is valid");
            _log.InfoFormat("Connection string is valid");
        }

        public void Init(string inputParameters)
        {
            try
            {
                _log.Debug("Reading input plugin parameters started");
                _log.InfoFormat("Reading input plugin parameters started");

                IDictionary<string, string> parameters = new Dictionary<string, string>(); ;

                List<string> keyValuePairs = inputParameters.Split('|').ToList();
                char[] charsToTrim = { ' ' };

                foreach (var keyValuePair in keyValuePairs)
                {
                    string firstPart = keyValuePair.Split('=')[0].Trim();
                    string secondPart = keyValuePair.Split(new char[] { '=' }, 2)[1].Trim(charsToTrim);
                    parameters.Add(firstPart, secondPart);
                }
                _log.Debug("Reading input plugin parameters ended");
                _log.InfoFormat("Reading input plugin parameters ended");

                CheckConnectionString(parameters["connectionstring"]);
                Init(parameters);
            }
            catch(Exception)
            {
                throw new InputParametersNotFoundException();
            }

        }

        private void SplitMappingParameters(string mappingParameters)
        {
            _log.Debug("Splitting sql mapping parameters started");

            List<string> keyValuePairs = mappingParameters.Split('~').ToList();
            char[] charsToTrim = { ' ' }; 

            foreach (var keyValuePair in keyValuePairs)
            {
                string firstPart = keyValuePair.Split(':')[0].Trim(charsToTrim);
                string secondPart = keyValuePair.Split(':')[1].Trim(charsToTrim);
                _mapping.Add(firstPart, secondPart);
            }
            _log.Debug("Splitting sql mapping parameters ended");
        }

        public string GetHelp()
        {
            string helpMessage = "You didn't provide all or invalid required parameters. SQLServer input plugin needs following parameters in following format:" +
                "--ip \"connectionstring=here_provide_connection_string|" +
                "table=here_provide_table_name_from_database|" +
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
                    if(_reader!=null)
                        _reader.Close();
                    if (_command != null)
                        _command.Dispose();
                    if (_connection != null)
                        _connection.Dispose();
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            _log.Debug("Disposing sql server input plugin");
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
