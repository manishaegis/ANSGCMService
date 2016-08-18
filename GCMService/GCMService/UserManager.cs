using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GCMService
{
    public class UserManager
    {
        private SqlConnection con = null;
        public UserManager()
        {
            con = new SqlConnection(ConfigurationManager.ConnectionStrings["MyConnectionString"].ConnectionString);
        }

        public DataTable GetNewUsers(DateTime LastModifiedTime)
        {
            if (LastModifiedTime != DateTime.MinValue)
            {
                return this.ExecuteSelectCommand(string.Format(@"SELECT 
                                                                    DISTINCT tblUserGcm.ID, tblUserGcm.ClientID, tblUserGcm.GCMId, 
                                                                    DATEADD(MILLISECOND,-DATEPART(MILLISECOND,tblTrade.TradeTimestamp),tblTrade.TradeTimestamp) AS TradeTimestamp, 
                                                                    Buy_Sell_Message 
                                                                FROM tblTrade 
                                                                    INNER JOIN tblUserGcm ON tblUserGcm.ClientID = tblTrade.Client 
                                                                WHERE 
                                                                    TradeTimestamp >= '{0}' AND 
                                                                    Buy_Sell_Message IN('B','S') 
                                                                ORDER BY TradeTimestamp", LastModifiedTime.ToString()), CommandType.Text);
            }
            else
            {
                return this.ExecuteSelectCommand(string.Format(@"SELECT 
                                                                    DISTINCT tblUserGcm.ID, tblUserGcm.ClientID, tblUserGcm.GCMId, 
                                                                    DATEADD(MILLISECOND,-DATEPART(MILLISECOND,tblTrade.TradeTimestamp),tblTrade.TradeTimestamp) AS TradeTimestamp, 
                                                                    Buy_Sell_Message 
                                                                FROM tblTrade 
                                                                    INNER JOIN tblUserGcm ON tblUserGcm.ClientID = tblTrade.Client 
                                                                WHERE 
                                                                    TradeTimestamp IS NOT NULL AND 
                                                                    Buy_Sell_Message IN('B','S') 
                                                                ORDER BY TradeTimestamp"), CommandType.Text);
            }
        }

        public DataTable isSendToAll(DateTime LastModifiedTime)
        {
            DataTable dt = new DataTable();
            if (LastModifiedTime != DateTime.MinValue)
            {
                dt = this.ExecuteSelectCommand(string.Format(@"SELECT 
                                                                            TOP 1 *
                                                                        FROM tblTrade 
                                                                        WHERE 
                                                                            TradeTimestamp >= '{0}' AND 
                                                                            Buy_Sell_Message IN('M') 
                                                                        ORDER BY TradeTimestamp", LastModifiedTime.ToString()), CommandType.Text);
            }
            else
            {
                dt = this.ExecuteSelectCommand(string.Format(@"SELECT 
                                                                            TOP 1 *
                                                                        FROM tblTrade 
                                                                        WHERE 
                                                                            Buy_Sell_Message IN('M') 
                                                                        ORDER BY TradeTimestamp", LastModifiedTime.ToString()), CommandType.Text);
            }
            return dt;
        }

        public DataTable GetAllUsers()
        {
            return this.ExecuteSelectCommand(string.Format(@"SELECT * FROM tblUserGcm"), CommandType.Text);
        }

        private DataTable ExecuteSelectCommand(string CommandName, CommandType cmdType)
        {
            SqlCommand cmd = null;
            DataTable table = new DataTable();

            cmd = con.CreateCommand();

            cmd.CommandType = cmdType;
            cmd.CommandText = CommandName;

            try
            {
                con.Open();

                SqlDataAdapter da = null;
                using (da = new SqlDataAdapter(cmd))
                {
                    da.Fill(table);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                cmd.Dispose();
                cmd = null;
                con.Close();
            }

            return table;
        }
    }
}
