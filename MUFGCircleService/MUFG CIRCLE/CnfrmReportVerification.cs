using CsvHelper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using WinSCP;


namespace MUFGCircleService
{
    class CnfrmReportVerification
    {
        private static IConfiguration _iconfiguration;
        DataTable sftpdata = new DataTable();
        DataTable reportData = new DataTable();
        DataTable insertreportData = new DataTable();
        string sftpServerIP = "";
        string sftpUserID = "";
        string sftpPassword = "";
        string stringToPath = "";
        string stringHostKey = "";
        string FileName = "";
        string DestinationFilePath = "";
        string SourcefilePath = "";
        string[] filenames = new string[2];
        int sftpPort = 0;

        private readonly ILogger<Worker> _logger;
        public CnfrmReportVerification(ILogger<Worker> _logger)
        {
            this._logger = _logger;
            GetSFTPDetailsforverification();
           
        }
        public void GetSFTPDetailsforverification()
        {
            var Param1 = new SqlParameter("@AdminName", SqlDbType.NVarChar)
            {
                Value = "MUFGCircle"

            };
            var Param2 = new SqlParameter("@FileType", SqlDbType.NVarChar)
            {
                Value = "All"
            };
            var prms = new List<SqlParameter> { Param1, Param2 };

            sftpdata = DBRepository.FillUpDataSetFromSp(EODModel.ConnectionString, "[ExportData].[ExtractSFTPDetails]", prms).Tables[0];
            _logger.LogInformation("Get SFTP Credenitals Successfully for MUFGCircle");
            reportData = DBRepository.FillUpDataSetFromSp(EODModel.ConnectionString, "[exportdata].[insertFileDetailsIntoMUFGCircleReport]").Tables[0];
            _logger.LogInformation("Get reportdata from sp");
            if (sftpdata != null)
            {
                foreach (DataRow details in sftpdata.Select("FileType='MUFGCircleAll'"))
                {
                    sftpServerIP = "sftp.mfsadmin.com";//details["IPAddress"].ToString();
                    sftpUserID = details["UserName"].ToString();
                    sftpPassword = details["Password"].ToString();
                    Int32.TryParse(details["Port"].ToString(), out sftpPort);
                    stringToPath = details["DestinationPath"].ToString();
                    stringHostKey = details["HostKey"].ToString();
                    FileName = details["FileName"].ToString();
                    DestinationFilePath = details["FilePath"].ToString();
                    SourcefilePath = "Z:\reports"; //ask thomas";
                }

                filenames = FileName.Split(";");
            }
        }


    }
}
