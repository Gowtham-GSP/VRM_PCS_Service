using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VRM_PCS_SERVICE.Services
{
    public class AppSetting
    {
        public int PollIntervalInSeconds { get; set; }
        public string SourcePath { get; set; }
        public string DestinationPath { get; set; }
        public string BackUpPath { get; set; }
        public FileProcess[] FileProcess { get; set; }
        public int ModifiedFileTimeMins { get; set; }
        public int ScheduleTimeinMin { get; set; }
        public string sharedPathUserName { get; set; }
        public string sharedPathPassword { get; set; }
        public string sharedPathDomain { get; set; }
        public string sharedPathLogOnNumber { get; set; }
        public string FileName { get; set; }
        public string CDRTableName { get; set; }
        public string LCMTableName { get; set; }
        public bool IsDeleteBackupfile { get; set; }
        public int DeleteBackupFilesFromDaysAgo { get; set; }
        public string DeleteFilePath { get; set; }
        public bool FilterNullColumnsRowData { get; set; }
        public string FilterNullColCheckTxtFileNames { get; set; }
        public bool IsDeleteCDRDataFromDB { get; set; }
        public bool IsBulkCopy { get; set; }
        public int DeleteBackupFileEvery_day { get; set; }
    }

    public class Configurations
    {
        public const string SectionName = "Configuration";
        public AppSetting Appsettings { get; set; }
        public ConnectionString connectionString { get; set; }
    }
    public class ConnectionString
    {
        public string DbConnectionString { get; set; }
        public string DBConnectionString { get;set; }
    }

    public class FileProcess
    {
        public string FileColumnNames { get; set; }
        public string DBColumnNames { get; set; }
        public string LCMColumnNames { get; set; }
    }












}
