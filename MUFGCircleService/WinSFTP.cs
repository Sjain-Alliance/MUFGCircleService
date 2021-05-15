using System;
using WinSCP;

namespace MUFGCircleService
{
    public static class WinSFTP
    {
        public static bool SFTPHelper(string sftpServerIP, string sftpUserID, string sftpPassword, int sftpPort, string sftpHostKey, string sftpToPath, string sourcefile,ref string error)
        {
            if (sftpServerIP.Equals("7.7.7.7"))
            {
                return true;
            }
            try
            {
                SessionOptions sessionOptionsSFTP = new SessionOptions
                {
                    Protocol = Protocol.Sftp,
                    HostName = sftpServerIP,
                    UserName = sftpUserID,
                    Password = sftpPassword,
                    PortNumber = sftpPort,
                    SshHostKeyFingerprint = sftpHostKey
                };

                Session sessionSFTP = new Session();

                sessionSFTP.Open(sessionOptionsSFTP);

                TransferOptions transferOptionsSFTP = new TransferOptions();
                transferOptionsSFTP.TransferMode = TransferMode.Binary;
                transferOptionsSFTP.PreserveTimestamp = false;
                transferOptionsSFTP.ResumeSupport.State = TransferResumeSupportState.Off;
                TransferOperationResult transferOperationResultSFTP;

                transferOperationResultSFTP = sessionSFTP.PutFiles(sourcefile, sftpToPath, false, transferOptionsSFTP);

                transferOperationResultSFTP.Check();


                if (transferOperationResultSFTP.IsSuccess)
                {
                    sessionSFTP.Close();
                    return true;
                }
                else
                {
                    sessionSFTP.Close();
                    return false;
                }

            }
            catch (Exception ex)
            {
                error = ex.ToString();
                return false;
            }
        }

    }
}
