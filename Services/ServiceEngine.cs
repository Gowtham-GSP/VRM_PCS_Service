using VRM_PCS_SERVICE.Interface;
using VRM_PCS_SERVICE.Module;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net.Http.Json;
using System.Reflection.Metadata.Ecma335;
using System.Runtime;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Timers;
using System.Xml.Linq;

namespace VRM_PCS_SERVICE.Services
{
    public class ServiceEngine : IServiceEngine
    {
        #region 

        private readonly ILogger<ServiceEngine> logger;
        private System.Timers.Timer PollTimer { get; set; }
        private int ScheduletimeForDelete {
            get
            {
                return _Options.Value.Appsettings.DeleteBackupFileEvery_day;
            }
        }
        private readonly IOptions<Configurations> _Options;
        private readonly IDBHelper _DBHelper;
        private DateTime DeleteScheduleTime { get; set; }
        private DateTime scheduledTime { get; set; }
        private bool isProcessedForDay = false;
        private int Interval
        {
            get
            {
                return (1000 * _Options.Value.Appsettings.PollIntervalInSeconds);
            }
        }
        
        #endregion

        public ServiceEngine(ILogger<ServiceEngine> _logger, IOptions<Configurations> Options, IDBHelper dBHelper)
        {
            logger= _logger;
            _Options = Options;
            _DBHelper = dBHelper;
            this.PollTimer = new System.Timers.Timer();
            _logger.LogInformation("VRM PCS service pollTimer interval {0} milliseconds", Interval);
            this.PollTimer.Elapsed += new ElapsedEventHandler(PollTimer_Elapsed);
            this.PollTimer.Interval = Interval;
            this.PollTimer.AutoReset = true;
            scheduledTime = DateTime.Now;
            DeleteScheduleTime = DateTime.Now.Date;
        }

        private void PollTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            //logger.LogInformation("PollTimer Stopped");
            this.PollTimer.Stop();
            try
            {
                lock (this)
                {
                    if (DateTime.Now >= scheduledTime)
                    {
                       
                        if (!isProcessedForDay)
                        {
                            isProcessedForDay = true;
                            logger.LogInformation("Inside PollTimer_Elapsed method");
                            bool isFilesExistandMoved = MoveFilesToNewFolder(_Options.Value.Appsettings.SourcePath, _Options.Value.Appsettings.DestinationPath);
                            if (isFilesExistandMoved)
                            {
                                logger.LogInformation($"Files are Exist and Moved To Destination folder");
                                bool isSavedToDB = ReadDataFromSourceFilePathAndSave();
                                if (isSavedToDB)
                                {
                                    logger.LogInformation("Process Completed Successfully [status : {0}]", isSavedToDB);
                                    DeleteFilesfromLocalFolder();
                                }
                                else
                                    logger.LogInformation("Process Not Completed [status : {0}]", isSavedToDB);
                            }
                            else
                            {
                                logger.LogInformation("No Files are Found to Process");
                            }
                            scheduledTime = DateTime.Now.AddMinutes(_Options.Value.Appsettings.ScheduleTimeinMin);
                            logger.LogInformation("Next scheduletime : {0}", scheduledTime.ToString());
                            logger.LogInformation("Outside PollTimer_Elapsed method");
                        }
                    }
                    else
                    {
                        isProcessedForDay = false;
                    }
                }
            }
            catch (Exception ex) 
            {
                logger.LogError(ex, "Exception: {0}", ex.Message);
            }
            this.PollTimer.Start();
          //  logger.LogInformation("PollTimer Started");
        }

        private void DeleteFilesfromLocalFolder()
        {

           // DateTime _scheduletime2 = DateTime.(Convert.ToString(ScheduletimeForDelete));
            if (DateTime.Now.Date >= DeleteScheduleTime.Date && _Options.Value.Appsettings.IsDeleteBackupfile)
            {
               
                logger.LogInformation("VRM_PCS_SERVICE - More than {0} days old files will be removed from the local folder ", _Options.Value.Appsettings.DeleteBackupFilesFromDaysAgo);
                DirectoryInfo yourRootDir = new DirectoryInfo(_Options.Value.Appsettings.DeleteFilePath);
                int count = 0;
                foreach (FileInfo file in yourRootDir.GetFiles())
                {
                    if (file.CreationTime < DateTime.Now.AddDays(-_Options.Value.Appsettings.DeleteBackupFilesFromDaysAgo))
                    {
                        file.Delete();
                        count++;
                    }
                }
                if (count > 0)
                {
                    logger.LogInformation("VRM_PCS_SERVICE - {0} files deleted from the local folder ", count);
                }
                else
                {
                    logger.LogInformation("VRM_PCS_SERVICE -There are no files to delete. ");
                }
                scheduledTime = DateTime.Now.AddDays(ScheduletimeForDelete);
                logger.LogInformation("VRM_PCS_Service - Next Schedule time for file delete : {0}", scheduledTime);
            }

        }


        private bool MoveFilesToNewFolder(string sourcepath,string destinationPath) 
        {
            bool result = false;
            string filename = string.Empty;

            try
            {
                logger.LogInformation($"Inside MoveFilesToNewFolder method : sourcepath : {@sourcepath},destinationPath: {@destinationPath}");

                if (!Directory.Exists(destinationPath))
                {
                    Directory.CreateDirectory(Path.Combine(destinationPath));
                }
                string username = _Options.Value.Appsettings.sharedPathUserName;
                string password = _Options.Value.Appsettings.sharedPathPassword;
                string domain = _Options.Value.Appsettings.sharedPathDomain;
                if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password) && !string.IsNullOrEmpty(domain))
                {
                    int logonNum = int.Parse(_Options.Value.Appsettings.sharedPathLogOnNumber);


                    IntPtr token = IntPtr.Zero;
                    var success = LogonUser(username, domain, password,
                        logonNum, 0, ref token);

                    if (success)
                    {
                        //using (System.Security.Principal.WindowsImpersonationContext person = new WindowsIdentity(token).Impersonate())
                        //{
                        //    //if (File.Exists(sourceLocalPath))
                        //    //{
                        //    //    File.Copy(sourceLocalPath, destinationDialerPath, true);
                        //    //}
                        //}
                    }
                }
                DirectoryInfo DirInfo = new DirectoryInfo(sourcepath);

                var modifiedfiles = from file in DirInfo.EnumerateFiles() 
                                   // where file.LastWriteTime < DateTime.Now.AddMinutes(-(_Options.Value.Appsettings.ModifiedFileTimeMins)) 
                                    //&& (file.Name.StartsWith(_Options.Value.Appsettings.FileName.ToUpper()) || file.Name.StartsWith(_Options.Value.Appsettings.FileName.ToLower()))
                                    select file;
                if (modifiedfiles.Any() && modifiedfiles != null)
                {
                    foreach (var file in modifiedfiles)
                    {
                        try
                        {
                            File.Move(file.FullName, destinationPath + file.Name);
                            filename = file.Name;
                            logger.LogInformation("File: {0} moved to destination folder successfully", filename);
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex, "Exception: {0}", ex.Message);
                            logger.LogInformation("Moving File: {0} to destination folder failed : {1}", filename, sourcepath);
                            continue;
                        }
                    }                   
                    result = true;
                }
                else
                {                  
                    logger.LogInformation("No Files exists to move destination folder");
                    result = false;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Exception: {0}", ex.Message);
                logger.LogInformation("Moving File: {0} to destination folder failed : {1}", filename, sourcepath);
                result = false;
            }
            logger.LogInformation($"Outside MoveFilesToNewFolder method : sourcepath : {@sourcepath},destinationPath: {@destinationPath}");

            return result;
        }

        private bool ReadDataFromSourceFilePathAndSave()
        {
            logger.LogInformation($"Inside ReadDataFromSourceFilePathAndSave method");
            DirectoryInfo DirMovedInfo = new DirectoryInfo(_Options.Value.Appsettings.DestinationPath);
            var movedfiles = from file in DirMovedInfo.EnumerateFiles() select file;

            bool isInserted = false;
            
            try
            {
                foreach (var filedetails in movedfiles)
                {
                  var st =  filedetails.ToString();
                            //(filedetails, this);
                    DataTable datatbl = new DataTable();
                    FileInfo fileinfo = new FileInfo(_Options.Value.Appsettings.DestinationPath + filedetails.Name);
                    if (File.Exists(fileinfo.FullName))
                    {
                        //GetDataFromtxt(fileinfo.FullName);

                        //IvrRequest inputobj = ReadDataIVRCallDetails(fileinfo.FullName);
                         EmployeeReq inputobj = ReadEmployeeData(fileinfo.FullName);
                    
                        //datatbl = Filetodatatable(filedetails);

                        if (inputobj != null)
                        {
                           logger.LogInformation("Read data from Destination path : {0} and row count : {1}", fileinfo.FullName, datatbl.Rows.Count);

                            //isInserted = _DBHelper.InsertIvrDetailsToDb(inputobj);
                            isInserted = _DBHelper.InsertEmployeeDetailsToDb(inputobj);
                           
                            if (isInserted)
                            {
                                logger.LogInformation("Date inserted into staging table in the custom db");

                                
                                if (!Directory.Exists(_Options.Value.Appsettings.BackUpPath))
                                {
                                    logger.LogInformation("Files directory is not found : {0}", _Options.Value.Appsettings.BackUpPath);
                                    Directory.CreateDirectory(Path.Combine(_Options.Value.Appsettings.BackUpPath));
                                }
                                File.Move(filedetails.FullName, _Options.Value.Appsettings.BackUpPath + filedetails.Name);
                            }
                            
                            logger.LogInformation("Data Read & Saved for the file : {0} was completed", fileinfo.FullName);
                        }
                        else
                        {
                            logger.LogInformation("No data found in the file : {0}", fileinfo.FullName);
                        }
                        
                    }
                    else
                    {
                        logger.LogInformation("Files are not Available in the source path : {0}", fileinfo.FullName);
                        isInserted = false;
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Exception: {0}", ex.Message);
                logger.LogInformation("Error Occurred at ReadDataFromSourceFilePathAndSave method");
                isInserted = false;
            }
            logger.LogInformation($"Outside ReadDataFromSourceFilePathAndSave method");

            return isInserted;
        }
        private DataTable GetOutboundDataFromDB()
        {
            return _DBHelper.GetLCMDataFromDB();
        }


        private void GetDataFromtxt(string filepath)
        {
            string filePath = "distivrrequest.txt";

            try
            {
                string fileContent = File.ReadAllText(filepath);
                InputReq myData = JsonSerializer.Deserialize<InputReq>(fileContent);
              
                _DBHelper.InsertTxtDataToTable(myData);
                Console.WriteLine($"id: {myData.Agentid}");
                Console.WriteLine($"AgentName: {myData.AgentName}");
                Console.WriteLine($"Campaignid: {myData.Campaignid}");
            }
            catch (FileNotFoundException)
            {
                Console.WriteLine("File not found.");
            }
            catch (JsonException)
            {
                Console.WriteLine("Error deserializing JSON.");
            }
          //  return null;
        }


        private IvrRequest ReadDataIVRCallDetails(string filepath)
        {
            //  string filePath = "distivrcallrequest.txt";
            IvrRequest IvrData = null;
            try
            {
                string fileContent = File.ReadAllText(filepath);
                if(!string.IsNullOrEmpty(fileContent))
                {
                    IvrData = JsonSerializer.Deserialize<IvrRequest>(fileContent);
                }
                else
                {
                    Console.WriteLine("File not found.");
                }
                
                //_DBHelper.InsertIvrDetailsToDb(IvrData);
                Console.WriteLine($"conversationID: {IvrData.conversationID}",$"time: {IvrData.time}", $"callId: {IvrData.callId}", $"ani: {IvrData.ani}", $"dnis: {IvrData.dnis}", $"callType: {IvrData.callType}", $"routerCallKey: {IvrData.routerCallKey}", $"routerCallKeyDay: {IvrData.routerCallKeyDay}", $"consent: {IvrData.consent}", $"rating: {IvrData.rating}");              
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error deserializing JSON : {0}", ex);
            }
             return IvrData;
        }
//------------------------------------------
       private EmployeeReq ReadEmployeeData(string filepath)
         {
            EmployeeReq EmpData = null;
            try
            {
                string fileContent = File.ReadAllText(filepath);
                if(!string.IsNullOrEmpty(fileContent))
                {
                    EmpData = JsonSerializer.Deserialize<EmployeeReq>(fileContent);
                }
                else
                {
                    Console.WriteLine("File not found");
                }
                Console.WriteLine($"userId:{EmpData.userId}", $"obTitleName : {EmpData.jobTitleName}", $"firstName : {EmpData.firstName}", $"lastName :{EmpData.lastName}", $"employeeCode : {EmpData.employeeCode}", $"region:{EmpData.region}", $"phoneNumbe : {EmpData.phoneNumber}", $"emailAddress :{EmpData.emailAddress}", $"InActive : {EmpData.InActive}");
            }
            catch(Exception ex)
            {
                Console.WriteLine("Error deserializing JSON : {0}", ex);
            }
            return EmpData;
        }
        

 //---------------------------------------     

        private DataTable Filetodatatable(FileInfo fileInformation)
        {
            logger.LogInformation($"Inside Filetodatatable method for file - {fileInformation.Name}");

            DataTable dtTable = new DataTable();
            
            try
            {
                Dictionary<string, int> colHeaderIndexList = new Dictionary<string, int>();
                Regex CSVParser = new Regex(",(?=(?:[^\"]*\"[^\"]*\")*(?![^\"]*\"))");
                FileProcess _fileprocess = _Options.Value.Appsettings.FileProcess[0];
                var configcolumn = _fileprocess.FileColumnNames.ToUpper().Split(",");
                InputReq inpt = new InputReq();
                var nullCheckColNames = _Options.Value.Appsettings.FilterNullColCheckTxtFileNames.ToUpper().Split(",").ToList();
                using (StreamReader sr = new StreamReader(_Options.Value.Appsettings.DestinationPath + fileInformation.Name))
                {
                    string[] headers = sr.ReadLine().Split(',');
                    for (int i = 0; i <= headers.Length - 1; i++)
                    {
                        for (int j = 0; j < configcolumn.Length; j++)
                        {
                            if (headers[i].ToUpper().Contains(configcolumn[j]) && !dtTable.Columns.Contains(configcolumn[j]))
                            {
                                colHeaderIndexList.Add(configcolumn[j], i);
                                dtTable.Columns.Add(configcolumn[j]);
                            }
                        }
                    }                  
                    while (!sr.EndOfStream)
                    {
                        try
                        {
                            string[] rows = CSVParser.Split(sr.ReadLine());
                            //inpt.Agentid = rows[0]["Agentid"]
                            DataRow dr = dtTable.NewRow();
                            int index = 0;
                            foreach (var indexValue in colHeaderIndexList)
                            {                             
                                if (!string.IsNullOrEmpty(rows[indexValue.Value]))
                                {
                                    dr[index] = rows[indexValue.Value].Replace("\"", string.Empty);
                                }
                                index++;
                            }
                            dtTable.Rows.Add(dr);
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex, "Exception: {0}", ex.Message);
                            logger.LogInformation("Error Occurred at Reading data line: {0}", Convert.ToString(sr.ReadLine()));
                        }
                    }                    
                }
                dtTable.Rows.RemoveAt(0);
                if (dtTable.Rows.Count > 0 && dtTable != null && _Options.Value.Appsettings.FilterNullColumnsRowData)
                {
                    List<DataRow> toDeleteRowList = new List<DataRow>();
                    foreach (string chkNullcol in nullCheckColNames)
                    {                     
                        if (dtTable.Rows.Cast<DataRow>().Any(x => string.IsNullOrEmpty(x.Field<string>(chkNullcol))))
                        {
                            logger.LogInformation($"Null data available for the column check: {chkNullcol}");
                            foreach (DataRow dr in dtTable.Rows.Cast<DataRow>().Where(x => string.IsNullOrEmpty(x.Field<string>(chkNullcol))))
                            {
                                toDeleteRowList.Add(dr);
                            }                                                                            
                        }
                        else
                        {
                            logger.LogInformation($"No Null data availability for the column check: {chkNullcol}");
                        }
                    }
                   
                    if (toDeleteRowList.Count > 0 && toDeleteRowList != null)
                    {
                        logger.LogInformation($"Removed the below line d" +
                            $"ata due to null availability for the specified columns:");
                        foreach (DataRow dr in toDeleteRowList)
                        {
                            string logData = string.Empty;
                            for (int i = 0; i < dr.ItemArray.Count(); i++)
                            {
                                logData += $"column: {dr.Table.Columns[i]} - {dr.ItemArray[i]} , ";                               
                            }
                            logger.LogInformation($"{logData}");
                            dtTable.Rows.Remove(dr);
                        }
                        dtTable.AcceptChanges();
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Exception: {0}", ex.Message);
                logger.LogInformation("Error Occurred at Filetodatatable method");
            }
            logger.LogInformation($"Outside Filetodatatable method for file - {fileInformation.Name}");
            return dtTable;
        }

        private bool InsertRowDataToDB(DataTable dataTable)
        {
            if(_Options.Value.Appsettings.IsDeleteCDRDataFromDB)
            {
                _DBHelper.DeleteCDRData();
            }
            return _DBHelper.InsertTxtDataToCUSTOMDatabase(dataTable);
        }

        public void Dispose()
        {
            this.PollTimer?.Dispose();
            logger.LogInformation("Service Disposed");
        }

        public void Start()
        {
            this.PollTimer.Start();
            logger.LogInformation("PollTimer Started");
        }

        public void Stop()
        {
            this.PollTimer.Stop();
            logger.LogInformation("PollTimer Stopped");
         
        }

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool LogonUser(string lpszUsername, string lpszDomain, string lpszPassword,
int dwLogonType, int dwLogonProvider, ref IntPtr phToken);

        [DllImport("kernel32.dll")]
        private static extern Boolean CloseHandle(IntPtr hObject);
    }
   
}
