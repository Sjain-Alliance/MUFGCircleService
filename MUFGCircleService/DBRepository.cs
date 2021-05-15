using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;

namespace MUFGCircleService
{
    public static class DBRepository
    {
        public static DataSet FillUpDataSetFromSp(string ConnectionString, string storedProc, List<SqlParameter> addParams = null)
       {
            var ds = new DataSet();
            string strTableNames = "";
            using (var connection = new SqlConnection(ConnectionString))
            {
                var da = new SqlDataAdapter(storedProc, connection)
                {
                    SelectCommand =
                    {
                        CommandType = CommandType.StoredProcedure,
                        CommandTimeout = 5000
                    }
                };
                if (addParams != null)
                {
                    foreach (var p in addParams)
                    {
                        da.SelectCommand.Parameters.Add(p);
                    }
                }

                var param = new SqlParameter("@tableNames", SqlDbType.NVarChar,-1)
                {
                    Direction = ParameterDirection.Output
                };
                da.SelectCommand.Parameters.Add(param);
                da.Fill(ds);
                strTableNames = da.SelectCommand.Parameters["@tableNames"].Value.ToString();
                connection.Close();
            }

            var tableNames = strTableNames.Split(',');

            for (var i = 0; i < tableNames.Length; i++)
            {
                var name = tableNames[i].Trim().Replace(" ", "");
                ds.Tables[i].TableName = name;
            }
            return ds;

        }


        public static void UpdateTradeWithSFTPStatus(string ConnectionString, int[] TradeIds, string AdapterType)
        {
            using (var connection = new SqlConnection(ConnectionString))
            {
                connection.Open();
                foreach (int tradeid in TradeIds)
                {
                    DataTable dt = new DataTable();
                    using (SqlCommand cmd = new SqlCommand($"Select * from ExportData.TradeTransferActivity  WITH (NOLOCK) where TradeId = {tradeid}", connection))
                    {
                        cmd.CommandType = CommandType.Text;
                        cmd.CommandTimeout = 120;
                        using (SqlDataAdapter sda = new SqlDataAdapter(cmd))
                        {
                            sda.Fill(dt);
                        }
                        
                       if (AdapterType.Equals("MUFGCircle"))
                        {
                            if (dt.Rows.Count > 0)
                            {

                                for (int i = 0; i < dt.Rows.Count; i++)
                                {
                                    SqlDataAdapter adap = new SqlDataAdapter();
                                    string sql = "update ExportData.TradeTransferActivity set MUFGCircle_LastSFTP = '" + DateTime.UtcNow + "', ModifiedBy = 1013, ModifiedOn = '" + DateTime.UtcNow + "' where TradeId = " + tradeid;
                                    SqlCommand sqlCommand = new SqlCommand(sql, connection);
                                    adap.InsertCommand = new SqlCommand(sql, connection);
                                    adap.InsertCommand.ExecuteNonQuery();
                                }
                            }
                            else
                            {
                                SqlDataAdapter adap = new SqlDataAdapter();
                                string sql = "insert into ExportData.TradeTransferActivity(TradeId, MUFGCircle_LastSFTP, ModifiedBy, ModifiedOn) values(" + tradeid + ",'" + DateTime.UtcNow + "', 1013,'" + DateTime.UtcNow + "')";
                                SqlCommand sqlCommand = new SqlCommand(sql, connection);
                                adap.InsertCommand = new SqlCommand(sql, connection);
                                adap.InsertCommand.ExecuteNonQuery();
                            }

                        }
                    }
                }
            }
        }

        public static void UpdateSFTPDateTimeStamp(string ConnectionString, string AdminName, string FileType, string issftp)
        {
            using (var connection = new SqlConnection(ConnectionString))
            {
                connection.Open();
                SqlDataAdapter adap = new SqlDataAdapter();
                string sql = "insert into ExportData.FileTransferActivity(LastSFTPTime, SFTPStatus, UserId, AdminId, FileType) values('" + DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) + "'," + issftp + ", 1013,(SELECT AdminId FROM ExportData.TradeFileAdmin WHERE AdminName = '" + AdminName + "'),'" + FileType + "')";
                SqlCommand sqlCommand = new SqlCommand(sql, connection);
                adap.InsertCommand = new SqlCommand(sql, connection);
                adap.InsertCommand.ExecuteNonQuery();
            }
        }

        public static void UpdateVerificationStatus(string ConnectionString, string Tradeid, string date)
        {
            using (var connection = new SqlConnection(ConnectionString))
            {
                connection.Open();
                SqlDataAdapter adap = new SqlDataAdapter();
                string sql = "update trade.trades set VerifiedById = 1013  , VerifiedOn = '"+ date +"' where id in ( " + Tradeid  + "); " + "update trade.AdditionalTradeDataReport set IsMUFGMatched = 1 where tradeid in (" + Tradeid + ")";
                SqlCommand sqlCommand = new SqlCommand(sql, connection);
                adap.InsertCommand = new SqlCommand(sql, connection);
                adap.InsertCommand.ExecuteNonQuery();
            }
        }
    }
}
