using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Data.SqlClient;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Collections;

namespace sunset_api
{    
    public class DBAccess
    {
        public SqlConnection conn;

        private int rowCount = 0;

        public DBAccess()
        {
            //if (Debugger.IsAttached)
                conn = new SqlConnection(@"Data Source=sunset.c8cr1ng5leql.us-east-1.rds.amazonaws.com,1433;Initial Catalog=TMWSunset_Live;User id=sunset;Password=sunsetruckit;MultipleActiveResultSets=True");
            //else
            //    conn = new SqlConnection(@"Data Source=SET-SQL01;Initial Catalog=TMWSunset_Live;User id=Ruckitadmin;Password=Sunset2017#;MultipleActiveResultSets=True");
        }
        public string QuerryJSON(string querry)
        {
            string results = string.Empty;
            bool keepGoing = true;
            bool endReader = true;

            try
            {
                conn.Open();
            }
            catch(Exception ex)
            { return ex.Message; }

            SqlCommand command = new SqlCommand();
            command.Connection = conn;
            command.CommandText = querry;
            SqlDataReader reader = command.ExecuteReader();

            ArrayList objs = new ArrayList();

            reader.Read();

            if (reader.HasRows)
            {
                while (keepGoing)
                {
                    string ord_number = reader["number"].ToString().Trim();

                    Order ord = new Order();
                    Driver dri = new Driver();
                    Truck tru = new Truck();
                    Start str = new Start();
                    End end = new End();

                    ord.number = reader["number"].ToString().Trim();
                    ord.status = reader["status"].ToString().Trim();
                    ord.description = reader["description"];
                    ord.weight = reader["weight"];
                    ord.ticket_number = reader["ticket_number"].ToString().Trim();

                    dri.phone_number = reader["phone_number"].ToString().Trim();
                    dri.first_name = reader["first_name"].ToString().Trim();
                    dri.last_name = reader["last_name"].ToString().Trim();
                    dri.license_number = reader["license_number"].ToString().Trim();
                    dri.zipcode = reader["zipcode"].ToString().Trim();
                    dri.state = reader["state"].ToString().Trim();
                    dri.id = reader["driver_id"].ToString().Trim();
                    dri.street_address = reader["street_address"].ToString().Trim();

                    tru.number = reader["driver_number"].ToString().Trim();
                    tru.license_plate = reader["license_plate"].ToString().Trim();

                    if (reader["type"].ToString().Trim() == "PUP")
                    {
                        str.weight = reader["stop_weight"];
                        str.type = reader["type"].ToString();
                        str.time = Convert.ToDateTime(reader["time"]);
                        str.zipcode = reader["stop_zipcode"].ToString().Trim();
                        str.address = reader["stop_address"].ToString().Trim();
                        str.order_number = reader["number"].ToString().Trim();
                        str.id = reader["stop_id"].ToString().Trim();
                    }

                    //GET DRP Stop regardless of date just equal to DRP and ord_hdrnumber = ord_number
                    SqlCommand drp_command = new SqlCommand();
                    drp_command.Connection = conn;
                    drp_command.CommandText = "SELECT stops.stp_type as type, stops.stp_address as stop_address, stops.stp_zipcode as stop_zipcode,stops.ord_hdrnumber as hdrnumber, " +
                                          "stops.stp_schdtearliest as time, stops.stp_number as stop_id, stops.stp_weight as stop_weight FROm stops " +
                                          "WHERE stops.stp_type = 'DRP' AND stops.ord_hdrnumber = '" + reader["number"].ToString().Trim() + "'";

                    SqlDataReader drp_reader = drp_command.ExecuteReader();
                    drp_reader.Read();

                    if (drp_reader.HasRows)
                    {
                        end.weight = drp_reader["stop_weight"];
                        end.type = drp_reader["type"].ToString();
                        end.time = Convert.ToDateTime(drp_reader["time"]);
                        end.zipcode = drp_reader["stop_zipcode"].ToString().Trim();
                        end.address = drp_reader["stop_address"].ToString().Trim();
                        end.order_number = drp_reader["hdrnumber"].ToString().Trim();
                        end.id = drp_reader["stop_id"].ToString().Trim();
                    }

                    endReader = reader.Read();

                    if (endReader == false)
                        keepGoing = false;

                    objs.Add(new Order
                    {
                        status = ord.status,
                        end = end,
                        description = ord.description,
                        weight = ord.weight,
                        driver = dri,
                        number = ord.number,
                        start = str,
                        truck = tru,
                        ticket_number = ord.ticket_number
                    });

                    rowCount++;
                }
            }

            var totalCount = rowCount;

            var paginationHeader = new
            {
                count = totalCount,
                next = "",
                results = objs,
                previous = ""
            };


            string json = JsonConvert.SerializeObject(paginationHeader, Formatting.Indented);

            conn.Close();
            return json;
        }

        public void Update(string querry)
        {
            conn.Open();

            SqlCommand command = new SqlCommand();
            command.Connection = conn;
            command.CommandText = querry;            
            command.ExecuteNonQuery();

            conn.Close();
        }

        public void Update(SqlCommand command)
        {
            conn.Open();
            command.Connection = conn;
            int check = command.ExecuteNonQuery();
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

            rowCount++;
            return result;
        }

    }

    public class End
    {
        public object weight { get; set; }
        public string type { get; set; }
        public DateTime time { get; set; }
        public string zipcode { get; set; }
        public string address { get; set; }
        public string order_number { get; set; }
        public string id { get; set; }
    }

    public class Start
    {
        public object weight { get; set; }
        public string type { get; set; }
        public DateTime time { get; set; }
        public string zipcode { get; set; }
        public string address { get; set; }
        public string order_number { get; set; }
        public string id { get; set; }
    }

    public class Driver
    {
        public string phone_number { get; set; }
        public string first_name { get; set; }
        public string last_name { get; set; }
        public object license_number { get; set; }
        public object zipcode { get; set; }
        public object state { get; set; }
        public string id { get; set; }
        public object street_address { get; set; }
    }

    public class Truck
    {
        public string number { get; set; }
        public object license_plate { get; set; }
    }

    public class Order
    {
        public string status { get; set; }
        public End end { get; set; }
        public Start start { get; set; }
        public object description { get; set; }
        public object weight { get; set; }
        public Driver driver { get; set; }
        public string number { get; set; }
        public Truck truck { get; set; }
        public object ticket_number { get; set; }
    }
}


/*while (reader.Read())
{
    objs.Add(new Order {
        status = reader["status"].ToString().Trim(),
        end = new End
        {
            weight = reader["stop_weight"],
            type = reader["type"].ToString(),
            time = Convert.ToDateTime(reader["time"]),
            zipcode = reader["stop_zipcode"].ToString().Trim(),
            address = reader["stop_address"].ToString().Trim(),
            order_number = reader["number"].ToString().Trim(),
            id = reader["stop_id"].ToString().Trim()
        },
        description = reader["description"],
        weight = reader["weight"],
        driver = new Driver
        {
            phone_number = reader["phone_number"].ToString().Trim(),
            first_name = reader["first_name"].ToString().Trim(),
            last_name = reader["last_name"].ToString().Trim(),
            license_number = reader["license_number"].ToString().Trim(),
            zipcode = reader["zipcode"].ToString().Trim(),
            state = reader["state"].ToString().Trim(),
            id = reader["driver_id"].ToString().Trim(),
            street_address = reader["street_address"].ToString().Trim()
        },
        number = reader["number"].ToString().Trim(),
        start = new Start
        {
            weight = reader["stop_weight"],
            type = reader["type"].ToString(),
            time = Convert.ToDateTime(reader["time"]),
            zipcode = reader["stop_zipcode"].ToString().Trim(),
            address = reader["stop_address"].ToString().Trim(),
            order_number = reader["number"].ToString().Trim(),
            id = reader["stop_id"].ToString().Trim()
        },
        truck = new Truck
        {
            number = reader["number"].ToString().Trim(),
            license_plate = reader["license_plate"].ToString().Trim()
        },
        ticket_number = reader["ticket_number"].ToString().Trim()
    });

    rowCount++;
}*/
