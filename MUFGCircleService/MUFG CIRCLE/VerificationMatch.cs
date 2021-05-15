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
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using WinSCP;


namespace MUFGCircleService

{
    class VerificationMatch
    {
        private static IConfiguration _iconfiguration;
        DataTable sftpdata = new DataTable();
        DataTable reportData = new DataTable();
        DataTable insertreportData = new DataTable();
        DataTable reportFile = new DataTable();
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
        public VerificationMatch(ILogger<Worker> _logger)
        {
             this._logger = _logger;
            
             RetrievingFileFromMUFGServer();
            
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
            if (sftpdata != null)
            {
                _logger.LogInformation("Get SFTP Credenitals Successfully for MUFGCircle");
                foreach (DataRow details in sftpdata.Select("FileType='MUFGCircleAll'"))
                {
                    sftpServerIP = "sftp.mfsadmin.com";
                    sftpUserID = details["UserName"].ToString();
                    sftpPassword = details["Password"].ToString();
                    Int32.TryParse(details["Port"].ToString(), out sftpPort);
                    stringHostKey = details["HostKey"].ToString();
                    DestinationFilePath = FileDetails.DestinationFilePath; 
                   SourcefilePath =  "/confirmresponses/";  

                }
                filenames = FileName.Split(";");
            }
        }
        public void RetrievingFileFromMUFGServer()
        {
            _logger.LogInformation("Asking for SFTP Credenitals.");
            GetSFTPDetailsforverification();
            bool sessionOpenStatus = false;
            SessionOptions sessionOptionsSFTP = new SessionOptions
            {
                Protocol = Protocol.Sftp,
                HostName = sftpServerIP,
                UserName = sftpUserID,
                Password = sftpPassword,
                PortNumber = sftpPort,
                SshHostKeyFingerprint = stringHostKey
            };
            Session sessionSFTP = new Session();
            try
            {
                _logger.LogInformation("Trying to open the sftp session for Mufg");
                sessionSFTP.Open(sessionOptionsSFTP);
                sessionOpenStatus = true;

            }
            catch (Exception e)
            {
                _logger.LogError("Unable to open the session.");
                _logger.LogError("Check the Exception: " + e.ToString() + " ");
                sessionOpenStatus = false;
                _logger.LogError("Logging out...");
                return;
                _logger.LogError("Some exception occurs while creating session with server " + e.ToString());
            }
            if (sessionOpenStatus == true)
            {
                _logger.LogInformation("SFTP Session Opened Succesfully");
            }

           
            try
            {

                TransferOptions transferOptionsDownload = new TransferOptions();
                transferOptionsDownload.TransferMode = TransferMode.Binary;
                transferOptionsDownload.PreserveTimestamp = true;
                transferOptionsDownload.ResumeSupport.State = TransferResumeSupportState.Off;
                TransferOperationResult transferOperationResult;

                try
                {
                    _logger.LogInformation("Function to download file from server is called. ");
                    TransferOptions transferOptions = new TransferOptions();
                    transferOptions.TransferMode = TransferMode.Binary;


                    TransferOperationResult transferResult;
                    transferResult =
                    sessionSFTP.GetFiles("/confirmresponses/ConfirmationReport*.csv", @"Z:\Production\Blotter\SFTP\MUFG\ConfirmationReport\", false, transferOptions);

                    //sessionSFTP.GetFiles("/confirmresponses/ConfirmationReport*.csv", @"Z:\Vendors\Alliance Staging\MUFG Circle Testing\ConfirmationReport\", false, transferOptions);
                    _logger.LogInformation("status of downloading the files from the remote to local RV drive == " + transferResult.IsSuccess );
                }
                catch (Exception e)
                {
                    _logger.LogError("Exception occurred in downloading files from server " + e);
                }

            }

                catch (Exception ex)
                {
                    _logger.LogError("Exception occurred while processing directory: confirmresponses, exception is >> " + ex);

                }

            DirectoryInfo directory = new DirectoryInfo("Z:\\Production\\Blotter\\SFTP\\MUFG\\ConfirmationReport\\");

            //DirectoryInfo directory = new DirectoryInfo("Z:\\Vendors\\Alliance Staging\\MUFG Circle Testing\\ConfirmationReport\\");


            _logger.LogInformation(" Directory available is : " + directory.FullName + " No of files are: " + directory.GetFiles().Count());
            var files = directory.GetFiles();

            foreach (var file in files)
            {
                _logger.LogInformation("Started To Process file >> >> " + file);
                string filename = Path.GetFileNameWithoutExtension(file.Name);
                _logger.LogInformation("Started To Process file with file.name >> >> " + file.Name);

                var splits = filename.Split('_');
                string date = splits[1];
                string time = splits[2];

                UpdateTradesForTradeids(file.FullName);
                _logger.LogInformation("Started To move file ");
                string sourcepath = "/confirmresponses/" + filename + ".csv";
                string targetpath = "/confirmresponses/RVReadFiles/" + filename + ".csv";
                string localProcessedFilePath = "Z:\\Production\\Blotter\\SFTP\\MUFG\\ProcessedConfirmationReport";
             //   "Z:\\Vendors\\Alliance Staging\\MUFG Circle Testing\\ProcessedConfirmationReport\\";
            
                try
                {
                    sessionSFTP.MoveFile("/confirmresponses/" + filename + ".csv", "/confirmresponses/RVReadFiles/" + filename + ".csv");
                    _logger.LogInformation("File has been moved sucessfully from >> "+sourcepath + " to " +targetpath);
                    _logger.LogInformation("Trying to move processed file from local directory" + directory + filename + " to ProcessedFiles folder.");

                    File.Move(directory + "\\" + filename + ".csv", localProcessedFilePath + filename + ".csv");

                }
                catch (Exception e )
                {
                    _logger.LogError("Caught exception in moving file from " +sourcepath + " to " + targetpath + " for file >>"  + filename);

                }

            }
            sessionSFTP.Close();
        }

        public void UpdateTradesForTradeids(string fileName)
        {
            _logger.LogInformation("UpdateTradesForTradeids method called");
            string tradeIds = "";
            _logger.LogInformation("converting received file to datatable");
            try
            {
                _logger.LogInformation("File name to convert into datatable is " + fileName);

                DataTable reportFile = ExcelToDataSet.CreateDataTable(fileName).Tables[0];
                _logger.LogInformation("converted successfully");
                _logger.LogInformation("no of rows in a file are : " + reportFile.Rows.Count);
                foreach (DataRow row in reportFile.Rows)
                {
                    _logger.LogInformation("Entered into foreach block and Checking for matched as confirmation status for every row in a file.");
                    string confirmationstatus = row.Field<string>("ConfirmationStatus");
                    _logger.LogInformation("Checking for matched as confirmation status , confirmation status of row in a file is : " + confirmationstatus);

                    if (confirmationstatus.Equals("Matched") || confirmationstatus.Equals("Confirmed"))
                    {
                        _logger.LogInformation("Confirmation status of row in a file matched , fetching its id");
                        string id = row.Field<string>("ClientTradeId");
                        _logger.LogInformation("And id is >>" + id );
                        id = id.Substring(0, 6);
                        tradeIds += id + ",";
                        _logger.LogInformation("Ids in tradeid are >> " + tradeIds);
                    }
                }
                tradeIds = tradeIds.TrimEnd(',');
                _logger.LogInformation("Matched Ids in file: " + fileName + "tradeids are >> " + tradeIds);

            }
            catch (Exception e)
            {
                _logger.LogError("found an exception while converting into datatable" + e);
            }
          
            try
            {
                try
                {
                    _logger.LogInformation("Calling database update function for trade.trades and trade.additionaltradedatareport.");
                    string date = DateTime.Now.ToString();
                    DBRepository.UpdateVerificationStatus(EODModel.ConnectionString, tradeIds, date);
                    _logger.LogInformation("Database updated successfully");
                }
                catch (Exception e)
                {
                    _logger.LogError("Got exception while updating database for respective tradeids " + e);
                }

                
            }
            catch (Exception e)
            {
                _logger.LogError("Got exception while checking rows in a loop " + e);
            }
            
        }

        //public void RetrievingFileFromMUFGServer()
        //{
        //    _logger.LogInformation("Asking for SFTP Credenitals.");
        //    GetSFTPDetailsforverification();
        //    bool sessionOpenStatus = false;
        //    SessionOptions sessionOptionsSFTP = new SessionOptions
        //    {
        //        Protocol = Protocol.Sftp,
        //        HostName = sftpServerIP,
        //        UserName = sftpUserID,
        //        Password = sftpPassword,
        //        PortNumber = sftpPort,
        //        SshHostKeyFingerprint = stringHostKey
        //    };
        //    Session sessionSFTP = new Session();
        //    try
        //    {
        //        _logger.LogInformation("Trying to open the sftp session for Mufg");
        //        sessionSFTP.Open(sessionOptionsSFTP);
        //        _logger.LogInformation("sftp session has been opened for Mufg");
        //        sessionOpenStatus = true;

        //    }
        //    catch (Exception e)
        //    {
        //        _logger.LogError("Unable to open the session.");
        //        _logger.LogInformation("Check the Exception: " + e.ToString() + " ");
        //        sessionOpenStatus = false;
        //        _logger.LogInformation("Logging out...");
        //        return;
        //        _logger.LogError("Some exception occurs while creating session with server " + e.ToString());
        //    }
        //    if (sessionOpenStatus == true)
        //    {
        //        _logger.LogInformation("SFTP Session Opened Succesfully");
        //    }

        //    RemoteDirectoryInfo directory = sessionSFTP.ListDirectory(SourcefilePath);
        //    _logger.LogInformation("No of files in directory available are: " + directory.Files.Count);
        //    _logger.LogInformation("File name of remote directory is: " + directory);

        //    try
        //    {
        //        foreach (RemoteFileInfo file in directory.Files)
        //        {
        //            try
        //            {
        //                _logger.LogInformation("Started processing file." + file.Name);

        //                string filename = Path.GetFileNameWithoutExtension(file.Name);

        //                _logger.LogInformation(" calling comapring function for date time");

        //                GetDatafromSPToCompareMaxDateandTime();
        //                _logger.LogInformation(" called comapring function for date time and back to process files.");
        //                var splits = filename.Split('_');
        //                string date = splits[1];
        //                string time = splits[2];
        //                _logger.LogInformation(" Obtaining date and time for directory file : " + date + "and time is : " + time);
        //                try
        //                {
        //                    DateTime datecheck = Convert.ToDateTime(DateTime.ParseExact(date, "yyyyMMdd", CultureInfo.InvariantCulture));
        //                    _logger.LogInformation("Date of file is : " + datecheck);
        //                    string filedatefromsp = Convert.ToDateTime(reportData.Rows[0]["MaxCreateDate"]).ToString("yyyyMMdd");
        //                    DateTime fileDate = Convert.ToDateTime(DateTime.ParseExact(filedatefromsp, "yyyyMMdd", CultureInfo.InvariantCulture));
        //                    _logger.LogInformation("Date of file obtained from database is : " + fileDate);
        //                    _logger.LogInformation("Comparing both the dates");
        //                    try
        //                    {
        //                        if (datecheck == fileDate && Convert.ToInt64(time) == Convert.ToInt64(reportData.Rows[0]["MaxCreateTime"]))
        //                        {
        //                            _logger.LogInformation("Date and time matched that means no new file is generated to process.");
        //                            continue;
        //                        }

        //                        else if (datecheck >= fileDate && Convert.ToInt64(time) >= Convert.ToInt64(reportData.Rows[0]["MaxCreateTime"]))
        //                        {
        //                            _logger.LogInformation("There is new file, so processing.");
        //                            try
        //                            {
        //                                _logger.LogInformation("Calling function(UpdateTradesForTradeids) to obtain trade ids and update trades in database ");
        //                                _logger.LogInformation("file name " + file);//file name full /confirmresponses/ConfirmationReport_20210415_110005.csv
        //                                _logger.LogInformation("file name full " + file.FullName);//file name full /confirmresponses/ConfirmationReport_20210415_110005.csv
        //                                UpdateTradesForTradeids(file.FullName);
        //                                _logger.LogInformation("Calling the function to insert file details into database.");
        //                                insertdataIntoTable(filename, datecheck, time);
        //                            }

        //                            catch (Exception exception)
        //                            {
        //                                _logger.LogInformation("Unable to update database for file." + exception);
        //                                _logger.LogInformation("Logging out...");
        //                                return;
        //                            }
        //                        }
        //                    }
        //                    catch(Exception e)
        //                    {
        //                        _logger.LogError("Some error in comparing dates" + e);
        //                    }
        //                }
        //                catch (Exception e)
        //                {
        //                    _logger.LogInformation( "Exception occurred while processing data in file " + filename +e);
        //                }

        //            }
        //            catch (Exception ex)
        //            {
        //                _logger.LogError("Exception Occur : " + ex.ToString());

        //            }


        //        }

        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogInformation("Exception occurred while processing directory " + directory);

        //    }
        //}


      

        //public void insertdataIntoTable(string filename, DateTime date, string time)


        //{
        //    var Param1 = new SqlParameter("@filename", SqlDbType.NVarChar)
        //    {
        //        Value = filename

        //    };

        //    var Param2 = new SqlParameter("@CreateDate", SqlDbType.DateTime)
        //    {
        //        Value = date

        //    };
        //    var Param3 = new SqlParameter("@CreateTime", SqlDbType.Int)
        //    {
        //        Value = Convert.ToInt32(time)

        //    };
        //    var Param4 = new SqlParameter("@isInsert", SqlDbType.Bit)
        //    {
        //        Value = 1

        //    };
        //    var prms = new List<SqlParameter> { Param1, Param2, Param3, Param4 };

        //    try
        //    {
        //        insertreportData = DBRepository.FillUpDataSetFromSp(EODModel.ConnectionString, "[ExportData].[insertFileDetailsIntoMUFGCircleReport]", prms).Tables[0];
        //        _logger.LogInformation("inserted file details into database for file name : " + filename);
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogInformation("There is some exception occurred " +ex+ " while updating the database for file name  " + filename);
        //    }

        //}

      


        //readNupdateReportFile(file.FullName);

        //        try
        //        {
        //            DateTime datecheck = Convert.ToDateTime(DateTime.ParseExact(date, "yyyyMMdd", CultureInfo.InvariantCulture));
        //            string filedatefromsp = Convert.ToDateTime(reportData.Rows[0]["MaxCreateDate"]).ToString("yyyyMMdd");
        //            DateTime fileDate = Convert.ToDateTime(DateTime.ParseExact(filedatefromsp, "yyyyMMdd", CultureInfo.InvariantCulture));
        //            if (datecheck == fileDate && Convert.ToInt64(time) == Convert.ToInt64(reportData.Rows[0]["MaxCreateTime"]))
        //            {
        //                continue;
        //            }

        //            else if (datecheck >= fileDate && Convert.ToInt64(time) >= Convert.ToInt64(reportData.Rows[0]["MaxCreateTime"]))
        //            {

        //                // download file from server to rv server
        //                //read file update db(trade.trades and trade.blocktrades)
        //                //move to rv subfolder
        //                //readReportFile(file);

        //                insertdataIntoTable(filename, datecheck, time);


        //            }

        //        }
        //        catch (Exception e)
        //        {

        //        }
        //        // chnages 
        //    }

        //    tradeIds = tradeIds.TrimEnd(',');
        //    DBRepository.UpdateVerificationStatus(EODModel.ConnectionString, tradeIds);
        //}












        //public void readNupdateReportFile(string fileName)
        //{
        //    string filedata = "BlockClientTradeId,ClientTradeId,ConfirmationStatus \n";
        //    String replaceString = "Verified";
        //    DataTable reportFile = ExcelToDataSet.CreateDataTable(fileName).Tables[0];
        //    try
        //    {
        //        if (reportFile.Rows.Count != 0)
        //        {
        //            foreach (DataRow row in reportFile.Rows)
        //            {
        //                for (int i = 0; i < 3; i++)
        //                {
        //                    if (row[i].ToString().Equals("Matched"))
        //                        filedata += "Verified\n";
        //                    else
        //                        filedata += row[i] + ",";

        //                }
        //                string confirmationstatus = row.Field<string>("ConfirmationStatus");
        //                if (confirmationstatus.Equals("Matched"))
        //                {
        //                    string id = row.Field<string>("ClientTradeId");
        //                    tradeIds += id + ",";

        //                    row["ConfirmationStatus"] = "Verified";

        //                    string check = row.Field<string>("ConfirmationStatus");

        //                }

        //            }
        //            File.WriteAllText(fileName, filedata);
        //        }
        //    }
        //    catch (Exception e)
        //    {

        //    }
        //    }

       
        }
    }

   


                               
