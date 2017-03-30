using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Data.SqlClient;
using Newtonsoft.Json;

namespace sunset_api
{    
    public class DBAccess
    {        
        public SqlConnection conn = new SqlConnection(@"Data Source=.\WSSERVER;Initial Catalog=WeighingSystem;User id=sa;Password=rs232cv;");

        private System.Xml.XmlReader xmlData;

        public string Querry(string querry)
        {
            string results = string.Empty;
            conn.Open();

            SqlCommand command = new SqlCommand();
            command.Connection = conn;
            command.CommandText = querry;
            SqlDataReader reader = command.ExecuteReader();

            string jsonResult = string.Empty;
            while (reader.Read())
            {
                jsonResult += JsonConvert.SerializeObject(reader.ToString());
            }                       

            conn.Close();
            return jsonResult;
        }
     
    }
}
