using CP.Lab.Transfer.Common;
using CP.Lab.Transfer.Common.Exceptions;
using log4net;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CP.Lab.Transfer.Plugins.SQLServer
{
    public class SQLServerOutputPlugin : IOutputPlugin
    {
        ILog _log;
        private string _connectionString;
        private string _tableName;
        IDictionary<string, string> _mapping = new Dictionary<string, string>();
        SqlConnection _connection;
        StringBuilder _query;

        public SQLServerOutputPlugin(ILog log)
        {
            _log = log ?? throw new ArgumentNullException(nameof(log));
        }

        public void Init(IDictionary<string, string> parameters)
        {
            _log.Debug("Checking if all required output plugin parameters exist");

            if (parameters == null)
                throw new ArgumentNullException(nameof(parameters));
            if (!parameters.ContainsKey("connectionstring"))
                throw new ArgumentException("Missing 'connectionstring' output plugin parameter");
            if (!parameters.ContainsKey("table"))
                throw new ArgumentException("Missing 'table' output plugin parameter");
            if (!parameters.ContainsKey("mapping"))
                throw new ArgumentException("Missing 'mapping' output plugin parameter");

            _log.Debug("Initializing output plugin started");
            _log.InfoFormat("Init output plugin started");
            try
            {
                _connectionString = parameters["connectionstring"];
                _tableName = parameters["table"];
                ReadMappingParameters(parameters["mapping"]);
            }
            catch(Exception e)
            {
                throw new InputParametersNotFoundException("Not all required input parameters were provided");        
            }
            ConnectAndMakeQuery();

            _log.Debug("Init output plugin ended");
            _log.InfoFormat("Output plugin initialized");
        }

        private void ConnectAndMakeQuery()
        {
            _log.Debug("Connecting to database and creating query");
            _connection = new SqlConnection(_connectionString);
            _connection.Open();
            _query = new StringBuilder();
            _query.Append("INSERT INTO ");
            _query.Append("["+_tableName+"]");

            _query.Append("(");
            foreach (var col_name in _mapping)
            {
                _query.Append("[" + col_name.Key + "]");
                _query.Append(",");
            }

            if (_mapping.Count() >= 1) { _query.Length -= 1; }
            _query.Append(")");
            _query.Append(" VALUES(");

            for (int j = 0; j < _mapping.Count(); j++)
            {
                _query.Append("@" + _mapping.ElementAt(j).Value.Replace(" ", string.Empty));
                _query.Append(",");
            }

            if (_mapping.Count() >= 1) { _query.Length -= 1; } 
            _query.Append(")");

            _log.Debug("Database connected and query created");

        }

        public void Write(IDictionary<string, object> item)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            _log.Debug("Wrtiting item started");
            _log.InfoFormat("Wrtiting item started");

           /* bool ifTableExists = CheckIfTableExists(_connectionString, _tableName);
            if (ifTableExists == false)
            {
                CreateTableWithGivenColumns(item);
            }*/

            _log.InfoFormat("Adding item to database: ");

            using (SqlCommand _command = new SqlCommand(_query.ToString(), _connection))
            {
                try
                {
                    for (int j = 0; j < _mapping.Count(); j++)
                    {
                        var record = item.Where(k => k.Key == _mapping.ElementAt(j).Value).SingleOrDefault();
                        _command.Parameters.Add(new SqlParameter() { ParameterName = "@" + record.Key.Replace(" ", string.Empty), Value = record.Value });
                        _log.InfoFormat("Value: " + record.Value);
                    }
                    _command.ExecuteNonQuery();
                    _log.InfoFormat("Item added to database");
                }
                catch (SqlException)
                {
                  //  _log.Error(ex.Message);
                    throw; //throw ex;
                }
            }

            _log.Debug("Writing item ended");
            _log.InfoFormat("Writing item ended");
        }

        //created table has only varchar(50) columns
        private void CreateTableWithGivenColumns(IDictionary<string, object> item)
        {
            _log.Debug("Creating table in database...");

            StringBuilder query = new StringBuilder();
            query.Append("CREATE TABLE ");
            query.Append(_tableName);
            query.Append(" ( ");

            foreach(var row in item)
            {
                query.Append("[" + row.Key +"]");
                query.Append(" ");
                query.Append("VARCHAR(50)");
                query.Append(", ");
            }
            if (item.Count() > 1) { query.Length -= 2; }  //Remove last ", "
            query.Append(")");
        
            SqlConnection conn = new SqlConnection(_connectionString);
            try
            {
                conn.Open();
                SqlCommand sqlQuery = new SqlCommand(query.ToString(), conn);
                SqlDataReader reader = sqlQuery.ExecuteReader();
                _log.Debug("Table in database created");
            }
            catch(SqlException e)
            {
                _log.Error("Error. Table not created");
                throw new Exception();
            }
            finally
            {
                conn?.Close();
                conn?.Dispose();
            }
        }

        private bool CheckIfTableExists(string conString, string tableName)
        {
            _log.Debug("Checking if table ->" + tableName + " exists in database...");
            SqlConnection conn = new SqlConnection(conString);
            try
            {
                // Works in PostgreSQL, MSSQL, MySQL.  
                conn.Open();
                var cmd = new SqlCommand(
                  "select case when exists((select * from information_schema.tables where table_name = '" + tableName + "')) then 1 else 0 end", conn);
                _log.Debug("Given table in database exists");
                return (int)cmd.ExecuteScalar() == 1;
            }
            catch(Exception e)
            {
                _log.Debug("Given table in database does not exist");
                return false; 
            }
            finally
            {
                conn?.Close();
                conn?.Dispose();
                _log.Debug("Checking if table exists - ended");
            }
        }

        public void Init(string parametersInput)
        {
            _log.Debug("Reading output plugin parameters started");
            try
            {
                IDictionary<string, string> parameters = new Dictionary<string, string>(); ;

                List<string> keyValuePairs = parametersInput.Split('|').ToList();
                char[] charsToTrim = { ' ' };
                foreach (var keyValuePair in keyValuePairs)
                {
                    string firstPart = keyValuePair.Split('=')[0].Trim();
                    string secondPart = keyValuePair.Split(new char[] { '=' }, 2)[1].Trim(charsToTrim);
                    parameters.Add(firstPart, secondPart);
                }
                CheckConnectionString(parameters["connectionstring"]);
                _log.Debug("Reading output plugin parameters ended");
                Init(parameters);
            }
            catch(Exception)
            {
                throw new InputParametersNotFoundException();
            }
        }

        private void CheckConnectionString(string conString)
        {
            _log.InfoFormat("Checking if connection string is valid...");
            try
            {
                using (SqlConnection conn = new SqlConnection(conString))
                {
                    conn.Open(); // throws if invalid connection
                }
            }
            catch(Exception e)
            {
                throw new InvalidConnectionStringException("Connection string is invalid");
            }
            _log.InfoFormat("Connection string is valid");
            _log.Debug("Connection string is valid");
        }
        private void ReadMappingParameters(string mappingParameters)
        {
            _log.Debug("Splitting sql mapping parameters started");

            List<string> keyValuePairs = mappingParameters.Split('~').ToList();
            char[] charsToTrim = {' '}; //columns names and item fields without spaces, beginning/end, not in the middle
            
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
            string helpMessage = "You didn't provide all or invalid required parameters. SQLServer output plugin needs following parameters in following format:" +
                "--op \"connectionstring=here_provide_connection_string|" +
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
                    if (_connection != null)
                        _connection.Dispose();       
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            _log.Debug("Disposing sql server output plugin");
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
