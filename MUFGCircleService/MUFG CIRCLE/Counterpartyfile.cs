using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Text.RegularExpressions;

namespace MUFGCircleService
{
    class Counterpartyfile
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

        public Counterpartyfile(ILogger<Worker> _logger)
        {
            this._logger = _logger;


            DataSet ds = DBRepository.FillUpDataSetFromSp(EODModel.ConnectionString, "[ExportData].[counterpartylist]", null);
            if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
            {
                this._logger.LogInformation("DataSet is not Blank for MUFG Trade File");
                CheckForSpecialCharacter(ds);
                string csv = String.Empty;

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
                string mufgCircleFileName = filenames[0] + "_" + "Bond_" + DateTime.Now.ToString("yyyyMMdd") + "_" + DateTime.Now.ToString("hhmmss") + ".csv";

                File.WriteAllText(mufgCircleFileName, csv);

                File.Move(mufgCircleFileName, "Z:" + "\\" );
            }
        }


        public void CheckForSpecialCharacter(DataSet ds)
        {
            for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
            {
                string assetdescriptionvalue = ds.Tables[0].Rows[i][0].ToString();
                string replacedstring = Regex.Replace(assetdescriptionvalue, @"[^0-9a-zA-Z:./ ]+", "");
                ds.Tables[0].Rows[i][0] = replacedstring.Trim();
            }
        }
    }
}
