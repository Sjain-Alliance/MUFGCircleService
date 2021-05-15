using System;
using System.Collections.Generic;
using System.Configuration;
using System.Net;
using System.Net.Mail;
using System.Text;


namespace MUFGCircleService
{
    public static class EmailSetup
    {
        public static void SendEMailGmail(string from, string Recipient, string subject, string body)
        {
            var message = new MailMessage();
            var smtp = new SmtpClient();
            message.From = new MailAddress(from);
            //foreach (var to in toAddresses)
            //{
            //    message.To.Add(to);
            //}
            foreach (var recipentaddress in Recipient.Split(','))
            {
                message.To.Add(new MailAddress(recipentaddress, ""));
            }
            message.Subject = subject;
            message.IsBodyHtml = false;
            message.Body = body;
            smtp.Port = int.Parse(MUFGCircleEmailDetails.Port);
            smtp.Host = MUFGCircleEmailDetails.Host;
            smtp.UseDefaultCredentials = false;
            smtp.Credentials = new NetworkCredential(from, MUFGCircleEmailDetails.FromPassword);
            smtp.EnableSsl = true;
            try
            {
                smtp.Send(message);
            }
            catch(Exception e)
            {

            }
            

        }


        public static void SendEmail(string SenderName, string SenderId, string SenderPassword, string SmtpServerHost, string SmtpServerPort, string EnableSsl, string IsBodyHtml, string Recipient, string emailSubject, string emailBody)
        {
            MailMessage message = new MailMessage();
            message.Subject = emailSubject;

            message.Body = emailBody;
            message.BodyEncoding = Encoding.UTF8;
            message.From = new MailAddress(SenderId, SenderName);
            foreach (var recipentaddress in Recipient.Split(','))
            {
                message.To.Add(new MailAddress(recipentaddress, ""));
            }

            message.Priority = MailPriority.Normal;
            message.IsBodyHtml = Convert.ToBoolean(IsBodyHtml);



            SmtpClient client = new SmtpClient(SmtpServerHost, int.Parse(SmtpServerPort))
            {
                EnableSsl = Convert.ToBoolean(EnableSsl),
                UseDefaultCredentials = true
            };
            try
            {
                client.Send(message);
            }
            catch (SmtpException ex)
            {
            }
        }

    }
}
