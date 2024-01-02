using VRM_PCS_SERVICE.Module;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Data.SqlTypes;

namespace VRM_PCS_SERVICE.Services
{
    public class DBHelper : IDBHelper
    {

        private readonly IOptions<Configurations> _configuration;
        private readonly ILogger<DBHelper> _logger;

        public DBHelper(IOptions<Configurations> configuration, ILogger<DBHelper> logger)
        {
            _logger = logger;
            _configuration = configuration;
        }

        public bool DeleteCDRData()
        {


            try
            {
                using (SqlConnection sqlConnection = new SqlConnection(_configuration.Value.connectionString.DbConnectionString))
                {
                    sqlConnection.Open();
                    SqlCommand sqlCommd = new SqlCommand("SP_DELETE_CDRDATA", sqlConnection);
                    sqlCommd.CommandType = System.Data.CommandType.StoredProcedure;
                    sqlCommd.ExecuteNonQuery();
                    sqlCommd.Dispose();
                    sqlConnection.Close();
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return true;
        }

        public DataTable GetLCMDataFromDB()
        {
            DataTable dtResult = new DataTable();

            try
            {
                using (SqlConnection sqlConnection = new SqlConnection(_configuration.Value.connectionString.DBConnectionString))
                {
                    sqlConnection.Open();
                    SqlCommand sqlCommd = new SqlCommand("SP_GET_DATA", sqlConnection);
                    sqlCommd.CommandType = System.Data.CommandType.StoredProcedure;
                    SqlDataReader reader = sqlCommd.ExecuteReader();
                    dtResult.Load(reader);
                    sqlCommd.Dispose();
                    sqlConnection.Close();
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return dtResult;
        }

        public bool InsertTxtDataToCUSTOMDatabase(DataTable dataTable)
        {
            bool rest = false;
            try
            {
                _logger.LogInformation("Inside InsertTxtDataToDatabase method");
                FileProcess _fileprocess = _configuration.Value.Appsettings.FileProcess[0];

                var configFilecolumn = _fileprocess.FileColumnNames.ToUpper().Split(",");

                var configDBcolumn = _fileprocess.DBColumnNames.Split(",");
                using (SqlConnection sqlConnection = new SqlConnection(_configuration.Value.connectionString.DbConnectionString))
                {
                    sqlConnection.Open();
                    SqlBulkCopy objbulk = new SqlBulkCopy(sqlConnection);
                    objbulk.BulkCopyTimeout = 0;
                    objbulk.DestinationTableName = _configuration.Value.Appsettings.CDRTableName.ToString();
                    for (int i = 0; i < configFilecolumn.Length; i++)
                    {
                        objbulk.ColumnMappings.Add(configFilecolumn[i], configDBcolumn[i]);
                    }
                    objbulk.WriteToServer(dataTable);
                    sqlConnection.Close();
                }
                rest = true;
            }
            catch (Exception ex)
            {
                rest = false;
                _logger.LogError($"DBError : Failed Insert data to database : {ex}");
            }
            _logger.LogInformation("Outside InsertTxtDataToDatabase method");
            return rest;
        }


        public bool InsertTxtDataToLCMDatabase(DataTable dataTable)
        {
            bool rest = false;
            try
            {
                _logger.LogInformation("Inside InsertTxtDataToLCMDatabase method");
                FileProcess _fileprocess = _configuration.Value.Appsettings.FileProcess[0];

                // var configFilecolumn = _fileprocess.FileColumnNames.ToUpper().Split(",");

                var configDBcolumn = _fileprocess.LCMColumnNames.Split(",");
                using (SqlConnection sqlConnection = new SqlConnection(_configuration.Value.connectionString.DBConnectionString))
                {
                    sqlConnection.Open();
                    SqlBulkCopy objbulk = new SqlBulkCopy(sqlConnection);
                    objbulk.BulkCopyTimeout = 0;
                    objbulk.DestinationTableName = _configuration.Value.Appsettings.LCMTableName.ToString();
                    for (int i = 0; i < configDBcolumn.Length; i++)
                    {
                        _logger.LogInformation("CDR Column: {0},LCM Column: {1} ", dataTable.Columns[i].ColumnName, configDBcolumn[i]);
                        objbulk.ColumnMappings.Add(dataTable.Columns[i].ColumnName, configDBcolumn[i]);
                    }
                    //objbulk.WriteToServer(dataTable);
                    objbulk.WriteToServerAsync(dataTable);
                    sqlConnection.Close();
                }
                _logger.LogInformation("CDR data inserted into the LCM table");

                rest = true;
            }
            catch (Exception ex)
            {
                rest = false;
                _logger.LogError($"DBErroR Failed Insert data to database : {ex}");
            }
            _logger.LogInformation("Outside InsertTxtDataToDatabase method");
            return rest;
        }


        public bool InsertTxtDataToTable(InputReq inputreq)
        {
            bool result = false;
            try
            {
                _logger.LogInformation("Inside InsertTxtDataToDatabase_SP method count : {0}", inputreq.Agentid);


                using (SqlConnection sqlConnection = new SqlConnection(_configuration.Value.connectionString.DBConnectionString))
                {
                    sqlConnection.Open();
                    SqlCommand sqlCmd = new SqlCommand("CD_SP_INSERTDATAT", sqlConnection);
                    sqlCmd.CommandType = System.Data.CommandType.StoredProcedure;
                    sqlCmd.Parameters.AddWithValue("@AgentId", SqlDbType.VarChar).Value = inputreq.Agentid;
                    sqlCmd.Parameters.AddWithValue("@AgentName", SqlDbType.VarChar).Value = inputreq.AgentName;
                    sqlCmd.Parameters.AddWithValue("@Campaignid", SqlDbType.VarChar).Value = inputreq.Campaignid;


                    sqlCmd.ExecuteNonQuery();
                    sqlCmd.Dispose();
                    sqlConnection.Close();
                }

                _logger.LogInformation("All data inserted into the table");


                result = true;
            }
            catch (Exception ex)
            {
                result = false;
                _logger.LogError($"Failed to insert data into the database: {ex}");
            }
            _logger.LogInformation("Outside InsertTxtDataToDatabase method");
            return result;
        }



        public bool InsertIvrDetailsToDb(IvrRequest ivrRequest)
        {
            bool result = false;
            try
            {
                _logger.LogInformation("Inside InsertIvrCallDetailsToDatabase_SP method count : {0}", ivrRequest.conversationID);

                using (SqlConnection sqlConnection = new SqlConnection(_configuration.Value.connectionString.DBConnectionString))
                {
                    sqlConnection.Open();
                    SqlCommand sqlCmd = new SqlCommand("CD_SP_INSERTIVRDATA", sqlConnection);
                    sqlCmd.CommandType = System.Data.CommandType.StoredProcedure;


                    sqlCmd.Parameters.AddWithValue("@conversationID", SqlDbType.Int).Value = ivrRequest.conversationID;
                    sqlCmd.Parameters.AddWithValue("@time", SqlDbType.DateTime).Value = ivrRequest.time;
                    sqlCmd.Parameters.AddWithValue("@callId", SqlDbType.VarChar).Value = ivrRequest.callId;
                    sqlCmd.Parameters.AddWithValue("@ani", SqlDbType.VarChar).Value = ivrRequest.ani;
                    sqlCmd.Parameters.AddWithValue("@dnis", SqlDbType.VarChar).Value = ivrRequest.dnis;
                    sqlCmd.Parameters.AddWithValue("@callType", SqlDbType.VarChar).Value = ivrRequest.callType;
                    sqlCmd.Parameters.AddWithValue("@routerCallKey", SqlDbType.VarChar).Value = ivrRequest.routerCallKey;
                    sqlCmd.Parameters.AddWithValue("@routerCallKeyDay", SqlDbType.VarChar).Value = ivrRequest.routerCallKeyDay;
                    sqlCmd.Parameters.AddWithValue("@consent", SqlDbType.VarChar).Value = ivrRequest.consent;
                    sqlCmd.Parameters.AddWithValue("@rating", SqlDbType.Int).Value = ivrRequest.rating;
                    


                    sqlCmd.ExecuteNonQuery();
                    sqlCmd.Dispose();
                    sqlConnection.Close();
                }

                _logger.LogInformation("All data inserted into the table");

                result = true;
            }
            catch (Exception ex)
            {
                result = false;
                _logger.LogError($"Failed to insert data into the database: {ex}");

            }
            _logger.LogInformation("Outside InsertIvrCallDataToDatabase method");

            return result;
        }


        //------------------------------------------

        public bool InsertEmployeeDetailsToDb(EmployeeReq employeeReq)
        {
            bool result = false;
                
            try
            {
                _logger.LogInformation("Inside InsertEmoloyeeDetailsToDB_SP method count : {0}", employeeReq.userId);

                using (SqlConnection sqlConnection = new SqlConnection(_configuration.Value.connectionString.DBConnectionString))
                {
                    sqlConnection.Open();
                    SqlCommand sqlcmd = new SqlCommand("SP_INSERTEMPDATA", sqlConnection);
                    sqlcmd.CommandType = System.Data.CommandType.StoredProcedure;

                    sqlcmd.Parameters.AddWithValue("@userId", SqlDbType.VarChar).Value = employeeReq.userId;
                    sqlcmd.Parameters.AddWithValue("@jobTitleName", SqlDbType.VarChar).Value = employeeReq.jobTitleName;
                    sqlcmd.Parameters.AddWithValue("@firstName", SqlDbType.VarChar).Value = employeeReq.firstName;
                    sqlcmd.Parameters.AddWithValue("@lastName", SqlDbType.VarChar).Value = employeeReq.jobTitleName;
                    sqlcmd.Parameters.AddWithValue("@employeeCode", SqlDbType.VarChar).Value = employeeReq.employeeCode;
                    sqlcmd.Parameters.AddWithValue("@region", SqlDbType.VarChar).Value = employeeReq.region;
                    sqlcmd.Parameters.AddWithValue("@phoneNumber", SqlDbType.VarChar).Value = employeeReq.phoneNumber;
                    sqlcmd.Parameters.AddWithValue("@emailAddress", SqlDbType.VarChar).Value = employeeReq.emailAddress;
                    sqlcmd.Parameters.AddWithValue("@InActive", SqlDbType.VarChar).Value = employeeReq.InActive;

                    sqlcmd.ExecuteNonQuery();
                    sqlcmd.Dispose();
                    sqlConnection.Close();

                }

                _logger.LogInformation("All data inserted into the table");

                result = true;
            }
            catch(Exception ex)
            {
                result = false;
                _logger.LogInformation($"Failed to inserted into the database: {ex}");
            }
            _logger.LogInformation("outside Insert employeeReqTO db");

            return result;
        }
       

        //-----------------------------------------

        public bool InsertTxtDataToLCMDatabase_SP(DataTable dataTable)
        {
            bool rest = false;
            try
            {
                _logger.LogInformation("Inside InsertTxtDataToLCMDatabase_SP method count : {0}", dataTable.Rows.Count);
                foreach (DataRow row in dataTable.Rows)
                {
                    using (SqlConnection sqlConnection = new SqlConnection(_configuration.Value.connectionString.DbConnectionString))
                    {
                        sqlConnection.Open();
                        SqlCommand sqlCommd = new SqlCommand("CM_SP_INSERTCDRDATA", sqlConnection);
                        sqlCommd.CommandType = System.Data.CommandType.StoredProcedure;
                        sqlCommd.Parameters.AddWithValue("@CallingParty", SqlDbType.VarChar).Value = row.ItemArray[0].ToString();
                        sqlCommd.Parameters.AddWithValue("@OriginCauseValue", SqlDbType.VarChar).Value = row.ItemArray[1].ToString();
                        sqlCommd.Parameters.AddWithValue("@CalledParty", SqlDbType.VarChar).Value = row.ItemArray[2].ToString();
                        sqlCommd.Parameters.AddWithValue("@DestinationCauseValue", SqlDbType.VarChar).Value = row.ItemArray[3].ToString();
                        sqlCommd.Parameters.AddWithValue("@PeripheralCallKey", SqlDbType.VarChar).Value = row.ItemArray[4].ToString();

                        sqlCommd.ExecuteNonQuery();
                        sqlCommd.Dispose();
                        sqlConnection.Close();
                    }
                }
                _logger.LogInformation("All data inserted in to LCM table");
                _logger.LogInformation("CDR data inserted into the LCM table");

                rest = true;
            }
            catch (Exception ex)
            {
                rest = false;
                _logger.LogError($"DBErroR Failed Insert data to database : {ex}");
            }
            _logger.LogInformation("Outside InsertTxtDataToDatabase method");
            return rest;
        }


    }
}