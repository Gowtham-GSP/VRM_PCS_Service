using VRM_PCS_SERVICE.Module;
using System.Data;


namespace VRM_PCS_SERVICE.Services
{
   public interface IDBHelper
    {
        bool InsertTxtDataToCUSTOMDatabase(DataTable dataTable);
        DataTable GetLCMDataFromDB();
        bool InsertTxtDataToLCMDatabase(DataTable dataTable);
        bool DeleteCDRData();
        bool InsertTxtDataToLCMDatabase_SP(DataTable dataTable);
        bool InsertTxtDataToTable(InputReq inputreq);
        bool InsertIvrDetailsToDb(IvrRequest ivrCallRequest);
        bool InsertEmployeeDetailsToDb(EmployeeReq employeeReq);

    }
}
