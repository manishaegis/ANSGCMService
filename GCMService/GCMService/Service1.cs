using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Collections.Specialized;
using System.Configuration;
using System.Xml;
using System.Net;

namespace GCMService
{
    public partial class Service1 : ServiceBase
    {
        private string BasePath
        {
            get { return AppDomain.CurrentDomain.BaseDirectory; }
        }

        private string XMLFileName
        {
            get { return BasePath + "XMLFile.xml"; }
        }

        private string LogFileName
        {
            get { return BasePath + "ServiceLog.txt"; }
        }

        private Timer timer1 = null;
        public Service1()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            this.WriteToFile(string.Format("Service Started: {0}", DateTime.Now.ToString("dd/MM/yyyy hh:mm:ss tt")));
            CreateFiles();
            timer1 = new Timer();
            this.timer1.Interval = Convert.ToInt32(ConfigurationManager.AppSettings["Interval"]);
            this.timer1.Elapsed += new ElapsedEventHandler(this.timer1_Tick);
            timer1.Enabled = true;
        }

        private void timer1_Tick(object sender, ElapsedEventArgs e)
        {
            this.WriteToFile(string.Format("Simple Service Log: {0}", DateTime.Now.ToString("dd/MM/yyyy hh:mm:ss tt")));
            SendNotification();
        }

        protected override void OnStop()
        {
            timer1.Enabled = false;
            this.WriteToFile(string.Format("Service Stopped: {0}", DateTime.Now.ToString("dd/MM/yyyy hh:mm:ss tt")));
        }

        private void WriteToFile(string text)
        {
            using (StreamWriter writer = new StreamWriter(LogFileName, true))
            {
                writer.WriteLine(text);
                writer.Close();
            }
        }

        private void SendNotification()
        {
            try
            {
                UserManager userManager = new UserManager();
                DateTime LastTime = DateTime.MinValue;
                string LastTimeString = GetLastModifiedUserID();

                if (!string.IsNullOrEmpty(LastTimeString))
                {
                    LastTime = Convert.ToDateTime(LastTimeString);
                    LastTime = LastTime.AddSeconds(1);
                }

                bool sentToAll = false;
                DataTable userList = userManager.isSendToAll(LastTime);
                if (userList != null && userList.Rows.Count > 0)
                {
                    foreach (DataRow dr in userList.Rows)
                    {
                        sentToAll = true;
                        SetLastUserId(dr.ItemArray[12].ToString());
                    }
                }

                if (sentToAll)
                {
                    userList = userManager.GetAllUsers();
                    if (userList != null && userList.Rows.Count > 0)
                    {
                        foreach (DataRow dr in userList.Rows)
                        {
                            SendNotification(dr[2].ToString(), ConfigurationManager.AppSettings["MessageText"]);
                            this.WriteToFile(string.Format("Notification Sent to User:{0} at {1}", dr.ItemArray[0].ToString(), DateTime.Now.ToString("dd/MM/yyyy hh:mm:ss tt")));
                        }
                    }
                }
                else
                {
                    userList = userManager.GetNewUsers(LastTime);
                    if (userList != null && userList.Rows.Count > 0)
                    {
                        foreach (DataRow dr in userList.Rows)
                        {
                            SendNotification(dr[2].ToString(), ConfigurationManager.AppSettings["MessageText"]);
                            SetLastUserId(dr.ItemArray[3].ToString());
                            this.WriteToFile(string.Format("Notification Sent to User:{0} at {1}", dr.ItemArray[0].ToString(), DateTime.Now.ToString("dd/MM/yyyy hh:mm:ss tt")));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                this.WriteToFile(string.Format("Error = {0} /n {1}", ex.Message, ex.StackTrace));
            }
        }

        public string SendNotification(string deviceId, string message)
        {
            string GoogleAppID = ConfigurationManager.AppSettings["GoogleAppID"];
            var SENDER_ID = ConfigurationManager.AppSettings["SenderID"];

            var value = message;
            WebRequest tRequest;
            tRequest = WebRequest.Create("https://android.googleapis.com/gcm/send");
            tRequest.Method = "post";
            tRequest.ContentType = " application/x-www-form-urlencoded;charset=UTF-8";
            tRequest.Headers.Add(string.Format("Authorization: key={0}", GoogleAppID));

            tRequest.Headers.Add(string.Format("Sender: id={0}", SENDER_ID));

            string postData = "collapse_key=score_update&time_to_live=108&delay_while_idle=1&data.message=" + value + "&data.time=" + System.DateTime.Now.ToString() + "&registration_id=" + deviceId + "";
            Console.WriteLine(postData);
            Byte[] byteArray = Encoding.UTF8.GetBytes(postData);
            tRequest.ContentLength = byteArray.Length;

            Stream dataStream = tRequest.GetRequestStream();
            dataStream.Write(byteArray, 0, byteArray.Length);
            dataStream.Close();

            WebResponse tResponse = tRequest.GetResponse();

            dataStream = tResponse.GetResponseStream();

            StreamReader tReader = new StreamReader(dataStream);

            String sResponseFromServer = tReader.ReadToEnd();
            //this.WriteToFile(string.Format("Error = {0}", ex.Message));

            tReader.Close();
            dataStream.Close();
            tResponse.Close();
            return sResponseFromServer;
        }

        public void SetLastUserId(string date)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(XMLFileName);
            XmlNode node = doc.SelectSingleNode("/User/DateTime");
            node.InnerText = date;
            doc.Save(XMLFileName);
        }

        public string GetLastModifiedUserID()
        {
            string lastTime = string.Empty;
            using (XmlReader reader = XmlReader.Create(XMLFileName))
            {
                while (reader.Read())
                {
                    if (reader.IsStartElement())
                    {
                        switch (reader.Name.ToString())
                        {
                            case "DateTime":
                                lastTime = reader.ReadString();
                                break;
                        }
                    }
                }
            }
            return lastTime;
        }

        public void CreateFiles()
        {
            try
            {
                if (!File.Exists(LogFileName))
                    File.Create(LogFileName);
                if (!File.Exists(XMLFileName))
                {
                    string xml = "<?xml version='1.0' encoding='utf-8'?><User><DateTime>" + DateTime.Now.AddDays(-1).ToString() + "</DateTime></User>";
                    File.WriteAllText(XMLFileName, xml, Encoding.ASCII);
                }
            }
            catch (Exception ex)
            {
                WriteToFile("Create File Exception :- " + ex.Message);
            }
        }
    }
}
