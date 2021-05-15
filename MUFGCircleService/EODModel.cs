using System;
using System.Collections.Generic;
using System.Text;

namespace MUFGCircleService
{
    public static class EODModel
    {
        public static string ConnectionString { get; set; }
    }
    public class MUFGTriggerDetails
    {
        public static string TradeFileInterval { get; set; }
    }
public class MUFGCircleEmailDetails
{
    public static string Name { get; set; }
    public static string Host { get; set; }
    public static string Port { get; set; }
    public static string From { get; set; }
    public static string FromPassword { get; set; }
    public static string To { get; set; }
    public static string Priority { get; set; }
    public static string EnableSsl { get; set; }
    public static string IsBodyHtml { get; set; }
}
    public class FileDetails
    {
        public static string DestinationFilePath { get; set; }
    }



}
