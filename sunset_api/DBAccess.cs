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
        public SqlConnection conn = new SqlConnection(@"Data Source=sunset.c8cr1ng5leql.us-east-1.rds.amazonaws.com,1433;Initial Catalog=TMWSunset_Live;User id=sunset;Password=sunsetruckit;");

        public string QuerryJSON(string querry)
        {
            string results = string.Empty;
            conn.Open();

            SqlCommand command = new SqlCommand();
            command.Connection = conn;
            command.CommandText = querry;
            SqlDataReader reader = command.ExecuteReader();

            var r = Serialize(reader);
            string json = JsonConvert.SerializeObject(r, Formatting.Indented);

            conn.Close();
            return json;
        }

        public void Update(string querry)
        {
            string results = string.Empty;
            conn.Open();

            SqlCommand command = new SqlCommand();
            command.Connection = conn;
            command.CommandText = querry;
            command.ExecuteNonQuery();

            conn.Close();
        }

        private IEnumerable<Dictionary<string, object>> Serialize(SqlDataReader reader)
        {
            var results = new List<Dictionary<string, object>>();
            var cols = new List<string>();
            for (var i = 0; i < reader.FieldCount; i++)
                cols.Add(reader.GetName(i));

            while (reader.Read())
                results.Add(SerializeRow(cols, reader));

            return results;
        }
        private Dictionary<string, object> SerializeRow(IEnumerable<string> cols,SqlDataReader reader)
        {
            var result = new Dictionary<string, object>();
            foreach (var col in cols)
                result.Add(col, reader[col]);
            return result;
        }

    }
}
