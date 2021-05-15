using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Text.RegularExpressions;

namespace MUFGCircleService
{
    class SFTPMUFGBondFiles
    {


        DataTable dtSFTPCred = new DataTable();
        string sftpServerIP = "";
        string sftpUserID = "";
        string sftpPassword = "";
        string stringToPath = "";
        string stringHostKey = "";
        string FileName = "";
        string FilePath = "";
        string[] filenames = new string[2];
        int sftpPort = 0;



        private readonly ILogger<Worker> _logger;

        public SFTPMUFGBondFiles(ILogger<Worker> _logger)
        {
            this._logger = _logger;

            try
            {
                DataSet ds = DBRepository.FillUpDataSetFromSp(EODModel.ConnectionString, "[ExportData].[getMUFGCircleAdaptorTrades]", null);
                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                {
                    this._logger.LogInformation("DataSet is not Blank for MUFG Trade File");
                    CheckForSpecialCharacter(ds);
                    GetSFTPDetails();
                    ExportMUFGCircleFiles(ds);
                }
                else
                {
                    this._logger.LogInformation("DataSet is Blank for MUFG Trade Files and we don't have to send them the file for now. So suppressing the File Generation");
                }
            }
            catch (Exception ex)
            {
                EmailSetup.SendEmail(MUFGCircleEmailDetails.Name, MUFGCircleEmailDetails.From, MUFGCircleEmailDetails.FromPassword, MUFGCircleEmailDetails.Host, MUFGCircleEmailDetails.Port,
                    MUFGCircleEmailDetails.EnableSsl, MUFGCircleEmailDetails.IsBodyHtml, MUFGCircleEmailDetails.To, "AutoAlert: MUFG circle files SFTP", "Exception Occur " + ex.ToString());
                _logger.LogError("Exception Occurs " + ex.ToString());
            }
        }



        public void GetSFTPDetails()
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

            dtSFTPCred = DBRepository.FillUpDataSetFromSp(EODModel.ConnectionString, "[ExportData].[ExtractSFTPDetails]", prms).Tables[0];
            _logger.LogInformation("Get SFTP Credenitals Successfully for MUFGCircle");
            if (dtSFTPCred != null)
            {
                foreach (var details in dtSFTPCred.Select("FileType='MUFGCircleAll'"))
                {
                    sftpServerIP = details["IPAddress"].ToString();
                    sftpUserID = details["UserName"].ToString();
                    sftpPassword = details["Password"].ToString();
                    Int32.TryParse(details["Port"].ToString(), out sftpPort);
                    stringToPath = details["DestinationPath"].ToString();
                    stringHostKey = details["HostKey"].ToString();
                    FileName = details["FileName"].ToString();
                    FilePath = details["FilePath"].ToString();

                }
                filenames = FileName.Split(";");
            }
        }

        public void ExportMUFGCircleFiles(DataSet ds)
        {
            string csv = String.Empty;
            string errorstring = "";
            DataTable dtMUFGCircle = ds.Tables[0];
            int i = 0;
            while (i < dtMUFGCircle.Columns.Count)
            {
                csv += dtMUFGCircle.Columns[i].ToString().Replace(" ", "") + ",";
                i++;
            }

            csv += "\r\n";

            for (int row = 0; row < dtMUFGCircle.Rows.Count; row++)
            {
                i = 0;
                while (i < dtMUFGCircle.Columns.Count)
                {
                    csv += dtMUFGCircle.Rows[row][i].ToString() + ",";
                    i++;
                }
                csv += "\r\n";
            }

            if (!Directory.Exists(FilePath))
                Directory.CreateDirectory(FilePath);
           
            string mufgCircleFileName = filenames[0] + "_" + DateTime.Now.ToString("yyyyMMdd") + "_" + DateTime.Now.ToString("hhmmss") + ".csv";
            string completeMUFGCircleFileName = FilePath + mufgCircleFileName;
            File.WriteAllText(completeMUFGCircleFileName, csv);
            string filenameposted = mufgCircleFileName;
            var PostPath = Path.Combine(FilePath + "\\Post");
            if (!Directory.Exists(PostPath))
                Directory.CreateDirectory(PostPath);
            try
            {
                if (WinSFTP.SFTPHelper(sftpServerIP, sftpUserID, sftpPassword, sftpPort, stringHostKey, stringToPath + filenameposted, completeMUFGCircleFileName, ref errorstring))
                {
                    UpdateMUFGCircleSFTPStatus(ds);
                    if (!System.IO.File.Exists(PostPath + "\\" + filenameposted))
                    {
                        _logger.LogInformation("File Transferred Successfully to Admin");
                        File.Move(completeMUFGCircleFileName, PostPath + "\\" + filenameposted);
                        _logger.LogInformation("MUFG Circle File has been moved to Posted Directory");
                        //EmailSetup.SendEmail(MUFGCircleEmailDetails.Name, MUFGCircleEmailDetails.From, MUFGCircleEmailDetails.FromPassword, MUFGCircleEmailDetails.Host, MUFGCircleEmailDetails.Port, MUFGCircleEmailDetails.EnableSsl, MUFGCircleEmailDetails.IsBodyHtml, MUFGCircleEmailDetails.To, "AutoAlert: MUFG Circle SFTP", "MUFG Circle File Successfully Transferred to Admin");

                    }
                }
            }
            catch (Exception exError)
            {
                EmailSetup.SendEmail(MUFGCircleEmailDetails.Name, MUFGCircleEmailDetails.From, MUFGCircleEmailDetails.FromPassword, MUFGCircleEmailDetails.Host,
                    MUFGCircleEmailDetails.Port, MUFGCircleEmailDetails.EnableSsl, MUFGCircleEmailDetails.IsBodyHtml, MUFGCircleEmailDetails.To, "AutoAlert: MUFG Circle SFTP", "Exception Occur " + exError);
                _logger.LogError("Exception Occur " + exError.ToString() + "MovedPath : " + PostPath + "\\" + filenameposted);
            }

        }
        public void CheckForSpecialCharacter(DataSet ds)
        {
            for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
            {
                string assetdescriptionvalue = ds.Tables[0].Rows[i][31].ToString();
                string replacedstring = Regex.Replace(assetdescriptionvalue, @"[^0-9a-zA-Z:,./ ]+", "");
                ds.Tables[0].Rows[i][31] = replacedstring.Trim();
            }
        }
        public void UpdateMUFGCircleSFTPStatus(DataSet ds)
        {
            string fld = "Fx";
            int[] exportTradeIds = new int[ds.Tables[0].Rows.Count];

            for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
            {
                Int32.TryParse((ds.Tables[0].Rows[i]["ClientTradeId"].ToString()), out int tradeid);
                exportTradeIds[i] = tradeid;
            }
            try
            {
                _logger.LogInformation("MUFG Circle Tradeids Update Started");
                DBRepository.UpdateTradeWithSFTPStatus(EODModel.ConnectionString, exportTradeIds, "MUFGCircle");
                DBRepository.UpdateSFTPDateTimeStamp(EODModel.ConnectionString, "MUFGCircle", fld, 1 + "");
                _logger.LogInformation("MUFG Circle Tradeid's SFTPtime updated :");
            }
            catch (Exception e)
            {
                _logger.LogInformation("There are some issues :");
            }
        }
    }
}
