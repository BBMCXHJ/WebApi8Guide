using System.Collections;
using System.Data;
using System.Data.SqlClient;

namespace Web.Data.SQLHelper
{
    //*********************************************************************
    // Microsoft Data Access Application Block for .NET
    // http://msdn.microsoft.com/library/en-us/dnbda/html/daab-rm.asp
    //
    // SQLHelper.cs
    //
    // This file contains the implementations of the SqlHelper and SqlHelperParameterCache
    // classes.
    //
    // For more information see the Data Access Application Block Implementation Overview. 
    // 
    //*********************************************************************
    // Copyright (C) 2000-2001 Microsoft Corporation
    // All rights reserved.
    // THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY
    // OF ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT
    // LIMITED TO THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR
    // FITNESS FOR A PARTICULAR PURPOSE.
    //*********************************************************************

    public sealed class ResultWithOutputParameters<T>
    {
        public ResultWithOutputParameters(T result, IEnumerable<SqlParameter> outputParameters)
        {
            Result = result;
            OutputParameters = outputParameters;
        }

        public T Result { get; private set; }
        public IEnumerable<SqlParameter> OutputParameters { get; private set; }
    }

    //*********************************************************************
    //
    // The SqlHelper class is intended to encapsulate high performance, scalable best practices for 
    // common uses of SqlClient.
    //
    //*********************************************************************

    public sealed class SqlHelper
    {
        //*********************************************************************
        //
        // Since this class provides only static methods, make the default constructor private to prevent 
        // instances from being created with "new SqlHelper()".
        //
        //*********************************************************************

        private SqlHelper() { }

        //*********************************************************************
        //
        // This method is used to attach array of SqlParameters to a SqlCommand.
        // 
        // This method will assign a value of DbNull to any parameter with a direction of
        // InputOutput and a value of null.  
        // 
        // This behavior will prevent default values from being used, but
        // this will be the less common case than an intended pure output parameter (derived as InputOutput)
        // where the user provided no input value.
        // 
        // param name="command" The command to which the parameters will be added
        // param name="commandParameters" an array of SqlParameters tho be added to command
        //
        //*********************************************************************


        private static void AttachParameters(SqlCommand command, SqlParameter[] commandParameters)
        {
            foreach (SqlParameter p in commandParameters)
            {
                //check for derived output value with no value assigned
                if (p.Direction == ParameterDirection.InputOutput && p.Value == null)
                {
                    p.Value = DBNull.Value;
                }

                command.Parameters.Add(p);
            }
        }

        //*********************************************************************
        //
        // This method assigns an array of values to an array of SqlParameters.
        // 
        // param name="commandParameters" array of SqlParameters to be assigned values
        // param name="parameterValues" array of objects holding the values to be assigned
        //
        //*********************************************************************

        private static void AssignParameterValues(SqlParameter[] commandParameters, object[] parameterValues)
        {
            if (commandParameters == null || parameterValues == null)
            {
                //do nothing if we get no data
                return;
            }

            // we must have the same number of values as we pave parameters to put them in
            if (commandParameters.Length != parameterValues.Length)
            {
                throw new ArgumentException("Parameter count does not match Parameter Value count.");
            }

            //iterate through the SqlParameters, assigning the values from the corresponding position in the 
            //value array
            for (int i = 0; i < commandParameters.Length; i++)
            {
                commandParameters[i].Value = parameterValues[i];
            }
        }

        //*** Check validity of dataset
        public static bool IsDatasetValid(DataSet ds)
        {
            return ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0 ? true : false;
        }
        //*************


        //*********************************************************************
        //
        // This method opens (if necessary) and assigns a connection, transaction, command type and parameters 
        // to the provided command.
        // 
        // param name="command" the SqlCommand to be prepared
        // param name="connection" a valid SqlConnection, on which to execute this command
        // param name="transaction" a valid SqlTransaction, or 'null'
        // param name="commandType" the CommandType (stored procedure, text, etc.)
        // param name="commandText" the stored procedure name or T-SQL command
        // param name="commandParameters" an array of SqlParameters to be associated with the command or 'null' if no parameters are required
        //
        //*********************************************************************

        private static void PrepareCommand(SqlCommand command, SqlConnection connection, SqlTransaction transaction, CommandType commandType, string commandText, SqlParameter[] commandParameters)
        {
            //if the provided connection is not open, we will open it
            if (connection.State != ConnectionState.Open)
            {
                connection.Open();
            }

            //associate the connection with the command
            command.Connection = connection;

            //set the command text (stored procedure name or SQL statement)
            command.CommandText = commandText;

            //if we were provided a transaction, assign it.
            if (transaction != null)
            {
                command.Transaction = transaction;
            }

            //set the command type
            command.CommandType = commandType;

            //attach the command parameters if they are provided
            if (commandParameters != null)
            {
                AttachParameters(command, commandParameters);
            }

            return;
        }

        //*********************************************************************
        //
        // Execute a SqlCommand (that returns no resultset) against the database specified in the connection string 
        // using the provided parameters.
        //
        // e.g.:  
        //  int result = ExecuteNonQuery(connString, CommandType.StoredProcedure, "PublishOrders", new SqlParameter("@prodid", 24));
        //
        // param name="connectionString" a valid connection string for a SqlConnection
        // param name="commandType" the CommandType (stored procedure, text, etc.)
        // param name="commandText" the stored procedure name or T-SQL command
        // param name="commandParameters" an array of SqlParamters used to execute the command
        // returns an int representing the number of rows affected by the command
        //
        //*********************************************************************

        public static int ExecuteNonQuery(string connectionString, CommandType commandType, string commandText, params SqlParameter[] commandParameters)
        {
            //create & open a SqlConnection, and dispose of it after we are done.
            using (SqlConnection cn = new SqlConnection(connectionString))
            {
                cn.Open();

                //call the overload that takes a connection in place of the connection string
                return ExecuteNonQuery(cn, commandType, commandText, commandParameters);
            }
        }

        //*********************************************************************
        //
        // Execute a stored procedure via a SqlCommand (that returns no resultset) against the database specified in 
        // the connection string using the provided parameter values.  This method will query the database to discover the parameters for the 
        // stored procedure (the first time each stored procedure is called), and assign the values based on parameter order.
        // 
        // This method provides no access to output parameters or the stored procedure's return value parameter.
        // 
        // e.g.:  
        //  int result = ExecuteNonQuery(connString, "PublishOrders", 24, 36);
        //
        // param name="connectionString" a valid connection string for a SqlConnection
        // param name="spName" the name of the stored prcedure
        // param name="parameterValues" an array of objects to be assigned as the input values of the stored procedure
        // returns an int representing the number of rows affected by the command
        //
        //*********************************************************************

        public static int ExecuteNonQuery(string connectionString, string spName, params object[] parameterValues)
        {
            //if we receive parameter values, we need to figure out where they go
            if (parameterValues != null && parameterValues.Length > 0)
            {
                //pull the parameters for this stored procedure from the parameter cache (or discover them & populate the cache)
                SqlParameter[] commandParameters = SqlHelperParameterCache.GetSpParameterSet(connectionString, spName);

                //assign the provided values to these parameters based on parameter order
                AssignParameterValues(commandParameters, parameterValues);

                //call the overload that takes an array of SqlParameters
                return ExecuteNonQuery(connectionString, CommandType.StoredProcedure, spName, commandParameters);
            }
            //otherwise we can just call the SP without params
            else
            {
                return ExecuteNonQuery(connectionString, CommandType.StoredProcedure, spName);
            }
        }

        //*********************************************************************
        //
        // Execute a SqlCommand (that returns no resultset) against the specified SqlConnection 
        // using the provided parameters.
        // 
        // e.g.:  
        //  int result = ExecuteNonQuery(conn, CommandType.StoredProcedure, "PublishOrders", new SqlParameter("@prodid", 24));
        // 
        // param name="connection" a valid SqlConnection 
        // param name="commandType" the CommandType (stored procedure, text, etc.) 
        // param name="commandText" the stored procedure name or T-SQL command 
        // param name="commandParameters" an array of SqlParamters used to execute the command 
        // returns an int representing the number of rows affected by the command
        //
        //*********************************************************************

        public static int ExecuteNonQuery(SqlConnection connection, CommandType commandType, string commandText, params SqlParameter[] commandParameters)
        {
            int retval;

            //create a command and prepare it for execution
            using (SqlCommand cmd = new SqlCommand())
            {
                cmd.CommandTimeout = 0;
                PrepareCommand(cmd, connection, null, commandType, commandText, commandParameters);

                //finally, execute the command.
                retval = cmd.ExecuteNonQuery();
            }

            // detach the SqlParameters from the command object, so they can be used again.
            return retval;
        }

        //*********************************************************************
        //
        // Execute a SqlCommand (that returns a resultset) against the database specified in the connection string 
        // using the provided parameters.
        // 
        // e.g.:  
        //  DataSet ds = ExecuteDataset(connString, CommandType.StoredProcedure, "GetOrders", new SqlParameter("@prodid", 24));
        // 
        // param name="connectionString" a valid connection string for a SqlConnection 
        // param name="commandType" the CommandType (stored procedure, text, etc.) 
        // param name="commandText" the stored procedure name or T-SQL command 
        // param name="commandParameters" an array of SqlParamters used to execute the command 
        // returns a dataset containing the resultset generated by the command
        //
        //*********************************************************************

        public static DataSet ExecuteDataset(string connectionString, CommandType commandType, string commandText, params SqlParameter[] commandParameters)
        {
            //create & open a SqlConnection, and dispose of it after we are done.
            using (SqlConnection cn = new SqlConnection(connectionString))
            {
                cn.Open();
                SqlCommand command = cn.CreateCommand();
                command.CommandTimeout = 0;

                //call the overload that takes a connection in place of the connection string
                return ExecuteDataset(cn, commandType, commandText, commandParameters);
            }
        }

        public static T ExecuteScalar<T>(string connectionString, string spName, params object[] parameterValues) where T : IConvertible
        {
            //if we receive parameter values, we need to figure out where they go
            if (parameterValues == null || parameterValues.Length <= 0) return ExecuteScalar<T>(connectionString, CommandType.StoredProcedure, spName);
            //pull the parameters for this stored procedure from the parameter cache (or discover them & populate the cache)
            var commandParameters = SqlHelperParameterCache.GetSpParameterSet(connectionString, spName);

            //assign the provided values to these parameters based on parameter order
            AssignParameterValues(commandParameters, parameterValues);

            //call the overload that takes an array of SqlParameters
            return ExecuteScalar<T>(connectionString, CommandType.StoredProcedure, spName, commandParameters);
        }

        public static T ExecuteScalar<T>(string connectionString, CommandType commandType, string commandText, params SqlParameter[] commandParameters) where T : IConvertible
        {
            //create & open a SqlConnection, and dispose of it after we are done.
            using (var cn = new SqlConnection(connectionString))
            {
                cn.Open();

                //call the overload that takes a connection in place of the connection string
                return ExecuteScalar<T>(cn, commandType, commandText, commandParameters);
            }
        }

        public static T ExecuteScalar<T>(SqlConnection connection, CommandType commandType, string commandText, params SqlParameter[] commandParameters) where T : IConvertible
        {
            //create a command and prepare it for execution
            T retval = default;
            using (var cmd = new SqlCommand { CommandTimeout = 0 })
            {
                PrepareCommand(cmd, connection, null, commandType, commandText, commandParameters);

                //execute the command & return the results
                var obj = cmd.ExecuteScalar();
                if (obj != DBNull.Value)
                    retval = (T)obj;

                // detach the SqlParameters from the command object, so they can be used again.
                cmd.Parameters.Clear();
            }
            return retval;
        }

        //*********************************************************************
        //
        // Execute a stored procedure via a SqlCommand (that returns a resultset) against the database specified in 
        // the connection string using the provided parameter values.  This method will query the database to discover the parameters for the 
        // stored procedure (the first time each stored procedure is called), and assign the values based on parameter order.
        // 
        // This method provides no access to output parameters or the stored procedure's return value parameter.
        // 
        // e.g.:  
        //  DataSet ds = ExecuteDataset(connString, "GetOrders", 24, 36);
        // 
        // param name="connectionString" a valid connection string for a SqlConnection
        // param name="spName" the name of the stored procedure
        // param name="parameterValues" an array of objects to be assigned as the input values of the stored procedure
        // returns a dataset containing the resultset generated by the command
        //
        //*********************************************************************
        public static DataSet ExecuteDataset(string connectionString, string spName, params object[] parameterValues)
        {
            //if we receive parameter values, we need to figure out where they go
            if (parameterValues != null && parameterValues.Length > 0)
            {
                //pull the parameters for this stored procedure from the parameter cache (or discover them & populate the cache)
                SqlParameter[] commandParameters = SqlHelperParameterCache.GetSpParameterSet(connectionString, spName);

                //assign the provided values to these parameters based on parameter order
                try
                {
                    AssignParameterValues(commandParameters, parameterValues);
                }
                catch
                {
                    AssignParameterValues2(commandParameters, parameterValues);
                }
                //call the overload that takes an array of SqlParameters
                return ExecuteDataset(connectionString, CommandType.StoredProcedure, spName, commandParameters);
            }
            //otherwise we can just call the SP without params
            else
            {
                return ExecuteDataset(connectionString, CommandType.StoredProcedure, spName);
            }
        }
        public static DataSet ExecuteDatasetWithTableType(string connectionString, string spName, params object[] parameterValues)
        {
            //if we receive parameter values, we need to figure out where they go
            if (parameterValues != null && parameterValues.Length > 0)
            {
                //pull the parameters for this stored procedure from the parameter cache (or discover them & populate the cache)
                SqlParameter[] commandParameters = SqlHelperParameterCache.GetSpParameterSet(connectionString, spName);

                //assign the provided values to these parameters based on parameter order
                try
                {
                    FixTableValueTypeNames(commandParameters, connectionString);
                    AssignParameterValues(commandParameters, parameterValues);
                }
                catch
                {
                    AssignParameterValues2(commandParameters, parameterValues);
                }
                //call the overload that takes an array of SqlParameters
                return ExecuteDataset(connectionString, CommandType.StoredProcedure, spName, commandParameters);
            }
            //otherwise we can just call the SP without params
            else
            {
                return ExecuteDataset(connectionString, CommandType.StoredProcedure, spName);
            }
        }
        private static void FixTableValueTypeNames(SqlParameter[] commandParameters, string connectionString)
        {
            SqlConnection connection = null;

            if (connectionString == null || connectionString.Length == 0)
            {
                throw new ArgumentNullException("connectionString");
            }

            try
            {
                if (commandParameters != null)
                {
                    // This is the added code in charge to remove the database name from any table-value parameter
                    connection = new SqlConnection(connectionString);

                    string DBNamePart = connection.Database + ".";

                    foreach (SqlParameter s in commandParameters)
                    {
                        if (s.SqlDbType == SqlDbType.Structured)
                        {
                            if (s.TypeName.Contains(DBNamePart))
                            {
                                s.TypeName = s.TypeName.Replace(DBNamePart, "");
                            }
                        }
                    }

                    connection.Dispose();
                }
            }
            catch (Exception ex)
            {

                if (connection != null)
                {
                    connection.Dispose();
                }

                throw ex;
            }
        }
        public static int ExecuteSingleDataset(string connectionString, string spName, long id)
        {
            int totalcount = 0;
            DataSet ds = ExecuteDataset(connectionString, spName, id);

            if (ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                totalcount = Convert.ToInt32(ds.Tables[0].Rows[0].ItemArray[0]);

            return totalcount;
        }

        public static int ExecuteSingleTable(string connectionString, string spName, params SqlParameter[] parameterValues)
        {

            int returnvalue = 0;

            DataSet ds = ExecuteDataset(connectionString, CommandType.StoredProcedure, spName, parameterValues);
            if (ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                returnvalue = Convert.ToInt32(ds.Tables[0].Rows[0].ItemArray[0]);

            return returnvalue;
        }

        ///*  Adding this fix. Testing. July-2-2015  ---------------------------------------------------------- */
        private static void AssignParameterValues2(SqlParameter[] commandParameters, object[] parameterValues) //, bool allowLessParams) 
        {
            try
            {
                if (parameterValues.Length < commandParameters.Length)
                {
                    for (int i = 0; i < parameterValues.Length; i++)
                    {
                        commandParameters[i].Value = parameterValues[i];
                    }
                }
            }
            catch
            {
                throw new ArgumentException("Parameter count mismatch with SP.");
            }
        }


        //*********************************************************************
        //
        // Execute a SqlCommand (that returns a resultset) against the specified SqlConnection 
        // using the provided parameters.
        // 
        // e.g.:  
        //  DataSet ds = ExecuteDataset(conn, CommandType.StoredProcedure, "GetOrders", new SqlParameter("@prodid", 24));
        //
        // param name="connection" a valid SqlConnection
        // param name="commandType" the CommandType (stored procedure, text, etc.)
        // param name="commandText" the stored procedure name or T-SQL command
        // param name="commandParameters" an array of SqlParamters used to execute the command
        // returns a dataset containing the resultset generated by the command
        //
        //*********************************************************************
        public static DataSet ExecuteDataset(SqlConnection connection, CommandType commandType, string commandText, params SqlParameter[] commandParameters)
        {
            DataSet ds = new DataSet();

            using (connection)
            {
                //create a command and prepare it for execution
                using (SqlCommand cmd = new SqlCommand())
                {
                    cmd.CommandTimeout = 0;
                    PrepareCommand(cmd, connection, null, commandType, commandText, commandParameters);

                    //create the DataAdapter & DataSet
                    using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                    {
                        //fill the DataSet using default values for DataTable names, etc.
                        da.Fill(ds);
                    }
                }
            }

            return ds;
        }

        //*********************************************************************
        //
        // Execute a SqlCommand (that returns a 1x1 resultset) against the database specified in the connection string 
        // using the provided parameters.
        // 
        // e.g.:  
        //  int orderCount = (int)ExecuteScalar(connString, CommandType.StoredProcedure, "GetOrderCount", new SqlParameter("@prodid", 24));
        // 
        // param name="connectionString" a valid connection string for a SqlConnection 
        // param name="commandType" the CommandType (stored procedure, text, etc.) 
        // param name="commandText" the stored procedure name or T-SQL command 
        // param name="commandParameters" an array of SqlParamters used to execute the command 
        // returns an object containing the value in the 1x1 resultset generated by the command
        //
        //*********************************************************************

        public static object ExecuteScalar(string connectionString, CommandType commandType, string commandText, params SqlParameter[] commandParameters)
        {
            //create & open a SqlConnection, and dispose of it after we are done.
            using (SqlConnection cn = new SqlConnection(connectionString))
            {
                cn.Open();

                //call the overload that takes a connection in place of the connection string
                return ExecuteScalar(cn, commandType, commandText, commandParameters);
            }
        }

        //*********************************************************************
        //
        // Execute a stored procedure via a SqlCommand (that returns a 1x1 resultset) against the database specified in 
        // the connection string using the provided parameter values.  This method will query the database to discover the parameters for the 
        // stored procedure (the first time each stored procedure is called), and assign the values based on parameter order.
        // 
        // This method provides no access to output parameters or the stored procedure's return value parameter.
        // 
        // e.g.:  
        //  int orderCount = (int)ExecuteScalar(connString, "GetOrderCount", 24, 36);
        // 
        // param name="connectionString" a valid connection string for a SqlConnection 
        // param name="spName" the name of the stored procedure 
        // param name="parameterValues" an array of objects to be assigned as the input values of the stored procedure 
        // returns an object containing the value in the 1x1 resultset generated by the command
        //
        //*********************************************************************

        public static object ExecuteScalar(string connectionString, string spName, params object[] parameterValues)
        {
            //if we receive parameter values, we need to figure out where they go
            if (parameterValues != null && parameterValues.Length > 0)
            {
                //pull the parameters for this stored procedure from the parameter cache (or discover them & populate the cache)
                SqlParameter[] commandParameters = SqlHelperParameterCache.GetSpParameterSet(connectionString, spName);

                //assign the provided values to these parameters based on parameter order
                // AssignParameterValues(commandParameters,parameterValues);

                //assign the provided values to these parameters based on parameter order
                try
                {
                    AssignParameterValues(commandParameters, parameterValues);
                }
                catch
                {
                    AssignParameterValues2(commandParameters, parameterValues);
                }

                //call the overload that takes an array of SqlParameters
                return ExecuteScalar(connectionString, CommandType.StoredProcedure, spName, commandParameters);
            }
            //otherwise we can just call the SP without params
            else
            {
                return ExecuteScalar(connectionString, CommandType.StoredProcedure, spName);
            }
        }

        //*********************************************************************
        //
        // Execute a SqlCommand (that returns a 1x1 resultset) against the specified SqlConnection 
        // using the provided parameters.
        // 
        // e.g.:  
        //  int orderCount = (int)ExecuteScalar(conn, CommandType.StoredProcedure, "GetOrderCount", new SqlParameter("@prodid", 24));
        // 
        // param name="connection" a valid SqlConnection 
        // param name="commandType" the CommandType (stored procedure, text, etc.) 
        // param name="commandText" the stored procedure name or T-SQL command 
        // param name="commandParameters" an array of SqlParamters used to execute the command 
        // returns an object containing the value in the 1x1 resultset generated by the command
        //
        //*********************************************************************

        public static object ExecuteScalar(SqlConnection connection, CommandType commandType, string commandText, params SqlParameter[] commandParameters)
        {
            object retval = null;

            using (connection)
            {
                //create a command and prepare it for execution
                using (SqlCommand cmd = new SqlCommand())
                {
                    cmd.CommandTimeout = 0;
                    PrepareCommand(cmd, connection, null, commandType, commandText, commandParameters);

                    //execute the command & return the results
                    retval = cmd.ExecuteScalar();
                }
            }

            return retval;
        }

        /// <summary>
        /// Execute a SqlCommand (that returns a resultset) against the database specified in the connection string using the provided parameters. 
        /// </summary>
        /// <param name="connectionString">A valid connection string for a SqlConnection </param>
        /// <param name="commandType">The CommandType (stored procedure, text, etc.)</param>
        /// <param name="commandText">The stored procedure name or T-SQL command</param>
        /// <param name="commandParameters">An array of SqlParamters used to execute the command, including output parameter(s)</param>
        /// <returns>Returns a dataset containing the resultset generated by the command and the output parameter(s) value(s)</returns>
        public static ResultWithOutputParameters<DataSet> ExecuteDatasetWithOutputParameters(string connectionString, CommandType commandType, string commandText, params SqlParameter[] commandParameters)
        {
            //create & open a SqlConnection, and dispose of it after we are done.
            using (SqlConnection cn = new SqlConnection(connectionString))
            {
                cn.Open();
                SqlCommand command = cn.CreateCommand();
                command.CommandTimeout = 0;

                //call the overload that takes a connection in place of the connection string
                return ExecuteDatasetWithOutputParameters(cn, commandType, commandText, commandParameters);
            }
        }

        /// <summary>
        /// Execute a stored procedure via a SqlCommand (that returns a resultset) against the database specified in 
        /// the connection string using the provided parameter values.  This method will query the database to discover the parameters for the 
        /// stored procedure (the first time each stored procedure is called), and assign the values based on parameter order.
        /// </summary>
        /// <param name="connectionString">A valid connection string for a SqlConnection</param>
        /// <param name="spName">The name of the stored procedure</param>
        /// <param name="parameterValues">An array of objects to be assigned as the input values of the stored procedure</param>
        /// <returns>returns a dataset containing the resultset generated by the command and output parameters values</returns>
        public static ResultWithOutputParameters<DataSet> ExecuteDatasetWithOutputParameters(string connectionString, string spName, params object[] parameterValues)
        {
            //pull the parameters for this stored procedure from the parameter cache (or discover them & populate the cache)
            SqlParameter[] commandParameters = SqlHelperParameterCache.GetSpParameterSet(connectionString, spName);

            //if we receive parameter values, we need to figure out where they go
            if (parameterValues != null && parameterValues.Length > 0)
            {
                //assign the provided values to these parameters based on parameter order
                AssignParameterValues(commandParameters, parameterValues);
            }
            return ExecuteDatasetWithOutputParameters(connectionString, CommandType.StoredProcedure, spName, commandParameters);
        }

        /// <summary>
        /// Execute a SqlCommand (that returns a resultset) against the specified SqlConnection using the provided parameters.
        /// </summary>
        /// <param name="connection">A valid SqlConnection</param>
        /// <param name="commandType">The CommandType (stored procedure, text, etc.)</param>
        /// <param name="commandText">The stored procedure name or T-SQL command</param>
        /// <param name="commandParameters">An array of SqlParamters used to execute the command, including output parameter(s)</param>
        /// <returns>returns a dataset containing the resultset generated by the command and output parameters values</returns>
        public static ResultWithOutputParameters<DataSet> ExecuteDatasetWithOutputParameters(SqlConnection connection, CommandType commandType, string commandText, params SqlParameter[] commandParameters)
        {
            DataSet ds = new DataSet();
            List<SqlParameter> outputParameters = new List<SqlParameter>();

            //create a command and prepare it for execution
            using (SqlCommand cmd = new SqlCommand())
            {
                cmd.CommandTimeout = 0;
                PrepareCommand(cmd, connection, null, commandType, commandText, commandParameters);

                //create the DataAdapter & DataSet
                using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                {
                    //fill the DataSet using default values for DataTable names, etc.
                    da.Fill(ds);

                    if (commandParameters.Any(com => com.Direction == ParameterDirection.InputOutput || com.Direction == ParameterDirection.Output))
                        outputParameters.AddRange(commandParameters.Where(com => com.Direction == ParameterDirection.InputOutput || com.Direction == ParameterDirection.Output));
                }
            }

            //return the dataset
            return new ResultWithOutputParameters<DataSet>(ds, outputParameters);
        }

        /// <summary>
        /// <para>Execute a stored procedure via a SqlCommand (that returns no resultset) against the database specified in the connection</para> 
        /// <para>string using the provided parameter values.  This method will query the database to discover the parameters for the</para> 
        /// <para>stored procedure (the first time each stored procedure is called), and assign the values based on parameter order.</para>
        /// </summary>
        /// <param name="result">Contains the output parameter value</param>
        /// <param name="connectionString">a valid connection string for a SqlConnection</param>
        /// <param name="spName">the name of the stored prcedure</param>
        /// <param name="parameterValues">an array of objects to be assigned as the input values of the stored procedure</param>
        /// <typeparam name="T">The output parameter value type</typeparam>
        public static void ExecuteNonQueryWithOutputParameter<T>(out T result, string connectionString, string spName, params object[] parameterValues)
        {
            result = default;
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                using (var cmd = new SqlCommand())
                {
                    var commandParameters = SqlHelperParameterCache.GetSpParameterSet(connectionString, spName);
                    AssignParameterValues(commandParameters, parameterValues);
                    PrepareCommand(cmd, con, null, CommandType.StoredProcedure, spName, commandParameters);
                    // finally, execute the command.
                    cmd.ExecuteNonQuery();
                    var param = commandParameters.First(p => p.Direction == ParameterDirection.Output || p.Direction == ParameterDirection.InputOutput);
                    result = (T)param.Value;
                }
            }
        }
        /// <summary>
        /// <para>Execute a stored procedure via a SqlCommand (that returns no resultset) against the database specified in the connection</para> 
        /// <para>string using the provided parameter values.  This method will query the database to discover the parameters for the</para> 
        /// <para>stored procedure (the first time each stored procedure is called), and assign the values based on parameter order.</para>
        /// </summary>
        /// <param name="result">Contains the output parameter value</param>
        /// <param name="connectionString">a valid connection string for a SqlConnection</param>
        /// <param name="spName">the name of the stored prcedure</param>
        /// <param name="parameterValues">an array of objects to be assigned as the input values of the stored procedure</param>
        /// <typeparam name="T1">The first output parameter value type</typeparam>
        /// <typeparam name="T2">The second output parameter value type</typeparam>
        public static void ExecuteNonQueryWithOutputParameter<T1, T2>(out T1 param1, out T2 param2, string connectionString, string spName, params object[] parameterValues)
        {
            param1 = default;
            param2 = default;
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                using (var cmd = new SqlCommand())
                {
                    var commandParameters = SqlHelperParameterCache.GetSpParameterSet(connectionString, spName);
                    AssignParameterValues(commandParameters, parameterValues);
                    PrepareCommand(cmd, con, null, CommandType.StoredProcedure, spName, commandParameters);
                    // finally, execute the command.
                    cmd.ExecuteNonQuery();
                    var paramList = commandParameters.ToList();
                    var parameters = commandParameters.Where(p => p.Direction == ParameterDirection.Output || p.Direction == ParameterDirection.InputOutput).OrderBy(p => paramList.IndexOf(p));
                    param1 = (T1)parameters.ElementAt(0).Value;
                    param2 = (T2)parameters.ElementAt(1).Value;
                }
            }
        }
        public static int ExecuteNonQueryWithFormatName(string connectionString, CommandType commandType, string commandText, params SqlParameter[] commandParameters)
        {
            //create & open a SqlConnection, and dispose of it after we are done.
            using (SqlConnection cn = new SqlConnection(connectionString))
            {
                cn.Open();

                //Formate TypeName
                foreach (SqlParameter parameter in commandParameters)
                {
                    if (parameter.SqlDbType != SqlDbType.Structured)
                    {
                        continue;
                    }
                    string name = parameter.TypeName;
                    int index = name.IndexOf(".");
                    if (index == -1)
                    {
                        continue;
                    }
                    name = name.Substring(index + 1);
                    if (name.Contains("."))
                    {
                        parameter.TypeName = name;
                    }
                }

                //call the overload that takes a connection in place of the connection string
                return ExecuteNonQuery(cn, commandType, commandText, commandParameters);
            }
        }

        public static int ExecuteNonQueryWithTableType(string connectionString, string spName, params object[] parameterValues)
        {
            //if we receive parameter values, we need to figure out where they go
            if (parameterValues != null && parameterValues.Length > 0)
            {
                //pull the parameters for this stored procedure from the parameter cache (or discover them & populate the cache)
                SqlParameter[] commandParameters = SqlHelperParameterCache.GetSpParameterSet(connectionString, spName);

                //assign the provided values to these parameters based on parameter order
                AssignParameterValues(commandParameters, parameterValues);

                //call the overload that takes an array of SqlParameters
                return ExecuteNonQueryWithFormatName(connectionString, CommandType.StoredProcedure, spName, commandParameters);
            }
            //otherwise we can just call the SP without params
            else
            {
                return ExecuteNonQuery(connectionString, CommandType.StoredProcedure, spName);
            }
        }
    }

    //*********************************************************************
    //
    // SqlHelperParameterCache provides functions to leverage a static cache of procedure parameters, and the
    // ability to discover parameters for stored procedures at run-time.
    //
    //*********************************************************************

    public sealed class SqlHelperParameterCache
    {
        //*********************************************************************
        //
        // Since this class provides only static methods, make the default constructor private to prevent 
        // instances from being created with "new SqlHelperParameterCache()".
        //
        //*********************************************************************

        private SqlHelperParameterCache() { }

        private static Hashtable paramCache = Hashtable.Synchronized(new Hashtable());

        //*********************************************************************
        //
        // resolve at run time the appropriate set of SqlParameters for a stored procedure
        // 
        // param name="connectionString" a valid connection string for a SqlConnection 
        // param name="spName" the name of the stored procedure 
        // param name="includeReturnValueParameter" whether or not to include their return value parameter 
        //
        //*********************************************************************

        private static SqlParameter[] DiscoverSpParameterSet(string connectionString, string spName, bool includeReturnValueParameter)
        {
            using (SqlConnection cn = new SqlConnection(connectionString))
            using (SqlCommand cmd = new SqlCommand(spName, cn))
            {
                cn.Open();
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandTimeout = 0;

                SqlCommandBuilder.DeriveParameters(cmd);

                if (!includeReturnValueParameter)
                {
                    cmd.Parameters.RemoveAt(0);
                }

                SqlParameter[] discoveredParameters = new SqlParameter[cmd.Parameters.Count]; ;

                cmd.Parameters.CopyTo(discoveredParameters, 0);

                return discoveredParameters;
            }
        }

        private static SqlParameter[] CloneParameters(SqlParameter[] originalParameters)
        {
            //deep copy of cached SqlParameter array
            SqlParameter[] clonedParameters = new SqlParameter[originalParameters.Length];

            for (int i = 0, j = originalParameters.Length; i < j; i++)
            {
                clonedParameters[i] = (SqlParameter)((ICloneable)originalParameters[i]).Clone();
            }

            return clonedParameters;
        }

        //*********************************************************************
        //
        // add parameter array to the cache
        //
        // param name="connectionString" a valid connection string for a SqlConnection 
        // param name="commandText" the stored procedure name or T-SQL command 
        // param name="commandParameters" an array of SqlParamters to be cached 
        //
        //*********************************************************************

        public static void CacheParameterSet(string connectionString, string commandText, params SqlParameter[] commandParameters)
        {
            string hashKey = connectionString + ":" + commandText;

            paramCache[hashKey] = commandParameters;
        }

        //*********************************************************************
        //
        // Retrieve a parameter array from the cache
        // 
        // param name="connectionString" a valid connection string for a SqlConnection 
        // param name="commandText" the stored procedure name or T-SQL command 
        // returns an array of SqlParamters
        //
        //*********************************************************************

        public static SqlParameter[] GetCachedParameterSet(string connectionString, string commandText)
        {
            string hashKey = connectionString + ":" + commandText;

            SqlParameter[] cachedParameters = (SqlParameter[])paramCache[hashKey];

            if (cachedParameters == null)
            {
                return null;
            }
            else
            {
                return CloneParameters(cachedParameters);
            }
        }

        //*********************************************************************
        //
        // Retrieves the set of SqlParameters appropriate for the stored procedure
        // 
        // This method will query the database for this information, and then store it in a cache for future requests.
        // 
        // param name="connectionString" a valid connection string for a SqlConnection 
        // param name="spName" the name of the stored procedure 
        // returns an array of SqlParameters
        //
        //*********************************************************************

        public static SqlParameter[] GetSpParameterSet(string connectionString, string spName)
        {
            return GetSpParameterSet(connectionString, spName, false);
        }

        //*********************************************************************
        //
        // Retrieves the set of SqlParameters appropriate for the stored procedure
        // 
        // This method will query the database for this information, and then store it in a cache for future requests.
        // 
        // param name="connectionString" a valid connection string for a SqlConnection 
        // param name="spName" the name of the stored procedure 
        // param name="includeReturnValueParameter" a bool value indicating whether the return value parameter should be included in the results 
        // returns an array of SqlParameters
        //
        //*********************************************************************

        public static SqlParameter[] GetSpParameterSet(string connectionString, string spName, bool includeReturnValueParameter)
        {
            string hashKey = connectionString + ":" + spName + (includeReturnValueParameter ? ":include ReturnValue Parameter" : "");

            SqlParameter[] cachedParameters;

            cachedParameters = (SqlParameter[])paramCache[hashKey];

            if (cachedParameters == null)
            {
                cachedParameters = (SqlParameter[])(paramCache[hashKey] = DiscoverSpParameterSet(connectionString, spName, includeReturnValueParameter));
            }

            return CloneParameters(cachedParameters);
        }
    }
}
