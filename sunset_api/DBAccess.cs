using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Data.SqlClient;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Collections;
using System.Data;

namespace sunset_api
{
    public class DBAccess
    {
        public SqlConnection conn;

        private int rowCount = 0;

        public DBAccess()
        {
            //Connection strings for the test server (aws) and sunset server(set-sql01)
            if (Debugger.IsAttached)
                conn = new SqlConnection(@"Data Source=sunset.c8cr1ng5leql.us-east-1.rds.amazonaws.com,1433;Initial Catalog=TMWSunset_Live;User id=sunset;Password=sunsetruckit;");
            else
                conn = new SqlConnection(@"Data Source=SET-SQL01;Initial Catalog=tmwlive;User id=Ruckitadmin;Password=Sunset2017#;");
            // Sunset test DB connection: conn = new SqlConnection(@"Data Source=SET-SQL01;Initial Catalog=TMWSunset_Live;User id=Ruckitadmin;Password=Sunset2017#;");
        }
        public string QuerryJSON(string querry)
        {
            string results = string.Empty;
            string fullQuerry = string.Empty;

            try
            {
                conn.Open();
            }
            catch (Exception ex)
            { return ex.Message; }

            SqlCommand command = new SqlCommand();
            command.Connection = conn;
            command.CommandText = querry;
            using (SqlDataReader reader = command.ExecuteReader())
            {
                try
                {
                    //Loop through all order numberas and add to array.  Later to be used to build main querry. 
                    ArrayList order_hdrs = new ArrayList();

                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            order_hdrs.Add(reader["number"].ToString().Trim());
                        }

                        string qOrders = string.Empty;

                        foreach (string s in order_hdrs)
                        {
                            qOrders += "'" + s + "',";
                        }

                        if (qOrders.EndsWith(","))
                            qOrders = qOrders.Remove(qOrders.Length - 1);

                        //build querry based on planned order number with PUP stops in date range to get additional data an DRP stops.
                        fullQuerry = "SELECT orderheader.ord_number AS number, orderheader.ord_status AS status, orderheader.ord_totalweight AS weight, orderheader.ord_refnum AS ticket_number," +
                       "orderheader.ord_description AS description,orderheader.ord_driver1 AS driver, orderheader.ord_tractor AS truck,orderheader.ord_revtype2 AS market, orderheader.ord_billto as customer, " +
                       "manpowerprofile.mpp_firstname as first_name,manpowerprofile.mpp_lastname as last_name,manpowerprofile.mpp_currentphone as phone_number,manpowerprofile.mpp_id as driver_id," +
                       "manpowerprofile.mpp_licensenumber as license_number, manpowerprofile.mpp_address1 as street_address, manpowerprofile.mpp_state as state, manpowerprofile.mpp_zip as zipcode," +
                       "tractorprofile.trc_number as driver_number, tractorprofile.trc_licnum as license_plate," +
                       "stops.stp_type as type, stops.stp_address as stop_address, stops.stp_zipcode as stop_zipcode, stops.stp_schdtearliest as time, stops.stp_number as stop_id, stops.stp_comment as stop_comment, " +
                       "stops.stp_weight as stop_weight, stops.cmp_name as stop_consignee, (SELECT cty_name FROM city WHERE cty_code = stops.stp_city) as stop_city, stops.stp_state as stop_state " +
                       "FROM orderheader JOIN stops ON orderheader.ord_number = stops.ord_hdrnumber " +
                       "JOIN manpowerprofile ON orderheader.ord_driver1 = manpowerprofile.mpp_id " +
                       "JOIN tractorprofile ON orderheader.ord_tractor = tractorprofile.trc_number " +
                       "WHERE orderheader.ord_number in (" + qOrders + ") ORDER BY orderheader.ord_number";
                    }
                }
                finally
                {
                    if (reader != null) { reader.Dispose(); }
                }
            }

            command.CommandText = fullQuerry;

            if (fullQuerry != string.Empty)
            {
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    ArrayList objs = new ArrayList();

                    //loop thorugh all order with DRP and PUP stops and build JSON response.
                    while (reader.Read())
                    {
                        string ord_number = reader["number"].ToString().Trim();

                        //Create objects 
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
                        ord.market = reader["market"].ToString().Trim();
                        ord.customer = reader["customer"].ToString().Trim();

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

                        bool keepGoing = true;
                        bool pupHit = false;
                        bool drpHit = false;

                        //Loop through results to write the corrispoding Stops PUP and DRP  there will be 1 of each per order. 
                        while (keepGoing)
                        {
                            if (ord_number == reader["number"].ToString().Trim())
                            {
                                if (reader["type"].ToString().Trim() == "PUP")
                                {
                                    str.weight = reader["stop_weight"].ToString() == string.Empty ? null : reader["stop_weight"];
                                    str.type = reader["type"].ToString();
                                    str.time = Convert.ToDateTime(reader["time"]);
                                    str.zipcode = reader["stop_zipcode"].ToString().Trim();
                                    str.address = reader["stop_address"].ToString().Trim();
                                    str.order_number = reader["number"].ToString().Trim();
                                    str.id = reader["stop_id"].ToString().Trim();
                                    str.consignee = reader["stop_consignee"].ToString().Trim();
                                    str.city = reader["stop_city"].ToString().Trim();
                                    str.state = reader["stop_state"].ToString().Trim();
                                    str.comment = reader["stop_comment"].ToString().Trim();

                                    pupHit = true;
                                }
                                else if (reader["type"].ToString().Trim() == "DRP")
                                {
                                    end.weight = reader["stop_weight"].ToString() == string.Empty ? null : reader["stop_weight"];
                                    end.type = reader["type"].ToString();
                                    end.time = Convert.ToDateTime(reader["time"]);
                                    end.zipcode = reader["stop_zipcode"].ToString().Trim();
                                    end.address = reader["stop_address"].ToString().Trim();
                                    end.order_number = reader["number"].ToString().Trim();
                                    end.id = reader["stop_id"].ToString().Trim();
                                    end.consignee = reader["stop_consignee"].ToString().Trim();
                                    end.city = reader["stop_city"].ToString().Trim();
                                    end.state = reader["stop_state"].ToString().Trim();
                                    end.comment = reader["stop_comment"].ToString().Trim();

                                    drpHit = true;
                                }
                            }

                            if (drpHit && pupHit)
                                keepGoing = false;
                            else
                                reader.Read();
                        }

                        //Add Order to Arraylist that will later be used to create JSON object. 
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
                            ticket_number = ord.ticket_number,
                            market = ord.market,
                            customer = ord.customer
                        });

                        rowCount++;
                    }

                    var totalCount = rowCount;

                    var pagination = new
                    {
                        count = totalCount,
                        next = "",
                        results = objs,
                        previous = ""
                    };

                    //Create JSON object out of pagination object that contains row count, and results. 
                    string json = JsonConvert.SerializeObject(pagination, Formatting.Indented);

                    conn.Close();
                    return json;
                }
            }


            //Return empty...no results.
            var empty = new
            {
                count = 0,
                next = "",
                results = new ArrayList(),
                previous = ""
            };
            string emptyJSON = JsonConvert.SerializeObject(empty, Formatting.Indented);
            conn.Close();
            return emptyJSON;
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

        public int Update(SqlCommand command)
        {
            int ret = 0;

            conn.Open();
            command.Connection = conn;
            ret = command.ExecuteNonQuery();
            conn.Close();

            return ret;
        }

        public int GetCount(SqlCommand command)
        {
            int ret = 0;

            conn.Open();
            command.Connection = conn;
            ret = Convert.ToInt32(command.ExecuteScalar());
            conn.Close();

            return ret;
        }

        public object SelectColumn(SqlCommand command)
        {
            object obj = null;

            conn.Open();
            command.Connection = conn;
            obj = command.ExecuteScalar();
            conn.Close();

            return obj;
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
        private Dictionary<string, object> SerializeRow(IEnumerable<string> cols, SqlDataReader reader)
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
        public string consignee { get; set; }
        public string city { get; set; }
        public string state { get; set; }
        public string comment { get; set; }
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
        public string consignee { get; set; }
        public string city { get; set; }
        public string state { get; set; }
        public string comment { get; set; }
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
        public object market { get; set; }
        public object customer { get; set; }
    }
}
