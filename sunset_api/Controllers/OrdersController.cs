using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Data.SqlClient;
using System.Data;

namespace sunset_api.Controllers
{
    [Route("api/[controller]")]
    public class OrdersController : Controller
    {
        private string result = string.Empty;

        // GET api/orders
        [HttpGet]
        public IEnumerable<string> Get()
        {
            return new string[] { "Sunset", "Trucking" };
        }

        //// Get started order based on ID
        //// GET api/orders/<order-id>/started
        //// GET api/orders/5922079/started

        [HttpGet("{id}/started")]
        public IActionResult Get(int id)
        {
            //Writes a single column on a table.  Write 'STD' to the orderheader.ord_status column   where the orderheader.ord_number == the id in the GET url
            //Returns nothing, 200 OK

            DBAccess db = new DBAccess();
            SqlCommand comm = new SqlCommand("UPDATE orderheader SET ord_status = 'STD' WHERE orderheader.ord_number = @orderid");
            comm.Parameters.AddWithValue("orderid", Convert.ToString(id));
            db.Update(comm);

            //db.Update("UPDATE orderheader SET ord_status = 'STD' WHERE orderheader.ord_number = '" + id.ToString() + "'");

            return Ok();
        }

        //// GET api/orders/byStatus?status="PLN"&start=2015-03-06T19:07:46Z&end=2017-03-07T23:07:46Z
        [HttpGet("byStatus")]
        public string Get(string status, DateTime start, DateTime end)
        {
            //Querry DB for data and return JSON
            DBAccess db = new DBAccess();

            if (status.Contains("\""))
            {
                status = status.Replace("\"", "");
            }

            //Get all planned (PLN) order numbers that have a PUP stop within the given time range. 
            string sQuery = "SELECT stops.ord_hdrnumber as number FROM stops " +
                            "JOIN orderheader ON orderheader.ord_hdrnumber = stops.ord_hdrnumber " +
                            "WHERE orderheader.ord_status = '" + status + "' AND stops.stp_type = 'PUP' AND stops.stp_schdtearliest > '" + start.ToUniversalTime().ToString() + "' AND stops.stp_schdtearliest < '" + end.ToUniversalTime().ToString() + "'";

            result = db.QuerryJSON(sQuery);

            return result;
        }


        //// GET api/orders/5922079/complete/?weight=22.34&ticket_number=1938429
        //// GET api/orders/<order-id>/complete/?weight=22.34&ticket_number=1938429
        [HttpGet("{id}/complete")]
        public IActionResult Get(int id, string weight, string ticket_number, DateTime completion_date)
        {
            //Writes 'CMP' to the orderheader.ord_status column, weight to orderheader.ord_weight, ticket number to orderheader.ord_refnum
            //And also writes the weight to the stops.stp_weight where the stops.ord_hdrnumber is the order - id and the stops.stp_type = DRP
            //Returns nothing, 200 OK

            string mov_number = string.Empty;

            if (weight.Contains("\""))
            {
                weight = weight.Replace("\"", "");
            }

            if (ticket_number.Contains("\""))
            {
                ticket_number = ticket_number.Replace("\"", "");
            }

            DBAccess db = new DBAccess();

            SqlCommand comm = new SqlCommand("SELECT mov_number FROM orderheader WHERE orderheader.ord_number = @orderid");
            comm.Parameters.Add(new SqlParameter("orderid", Convert.ToString(id)));
            mov_number = Convert.ToString(db.SelectColumn(comm));

            if (mov_number != string.Empty && mov_number != DBNull.Value.ToString())
            {
                comm = new SqlCommand("UPDATE orderheader SET ord_status = 'CMP', ord_totalweight = @weight, ord_refnum = @ticket_number, ord_completiondate = @completiondate WHERE orderheader.ord_number = @orderid");
                comm.Parameters.Add(new SqlParameter("weight", weight));
                comm.Parameters.Add(new SqlParameter("ticket_number", Convert.ToString(ticket_number)));
                comm.Parameters.Add(new SqlParameter("orderid", Convert.ToString(id)));

                if (completion_date == DateTime.MinValue)
                    comm.Parameters.Add(new SqlParameter("completiondate", DateTime.Now.ToString()));
                else
                    comm.Parameters.Add(new SqlParameter("completiondate", completion_date.ToUniversalTime().ToString()));

                db.Update(comm);

                comm = new SqlCommand("UPDATE stops SET stp_weight = @weight, stp_refnum = @ticket_number WHERE ord_hdrnumber = @orderid AND stp_type = 'DRP'");
                comm.Parameters.Add(new SqlParameter("weight", weight));
                comm.Parameters.Add(new SqlParameter("ticket_number", Convert.ToString(ticket_number)));
                comm.Parameters.Add(new SqlParameter("orderid", Convert.ToString(id)));
                db.Update(comm);

                comm = new SqlCommand("UPDATE stops SET stp_reftype = 'TI#', stp_departure_status = 'DNE', stp_status = 'DNE' WHERE mov_number = @mov_number AND stp_event = 'BMT'");
                comm.Parameters.Add(new SqlParameter("mov_number", mov_number));
                db.Update(comm);

                System.Threading.Thread.Sleep(100);

                comm = new SqlCommand("UPDATE stops SET stp_reftype = 'TI#', stp_departure_status = 'DNE', stp_status = 'DNE' WHERE mov_number = @mov_number AND stp_event = 'LLD'");
                comm.Parameters.Add(new SqlParameter("mov_number", mov_number));
                db.Update(comm);

                System.Threading.Thread.Sleep(100);

                comm = new SqlCommand("UPDATE stops SET stp_reftype = 'TI#', stp_departure_status = 'DNE', stp_status = 'DNE' WHERE mov_number = @mov_number AND stp_event = 'LUL'");
                comm.Parameters.Add(new SqlParameter("mov_number", mov_number));
                db.Update(comm);

                comm = new SqlCommand("UPDATE referencenumber SET ref_number = @ticket_number, ref_type = 'TI#' WHERE ord_hdrnumber = @orderid");
                comm.Parameters.Add(new SqlParameter("ticket_number", Convert.ToString(ticket_number)));
                comm.Parameters.Add(new SqlParameter("orderid", Convert.ToString(id)));
                db.Update(comm);

                int exist = 0;

                comm = new SqlCommand("SELECT Count(*) FROM referencenumber WHERE ord_hdrnumber = @orderid AND ref_table = 'orderheader' ");
                comm.Parameters.Add(new SqlParameter("orderid", Convert.ToString(id)));
                exist = db.GetCount(comm);

                if (exist <= 0)
                {
                    comm = new SqlCommand("INSERT INTO referencenumber (ref_tablekey, ref_type, ref_number, ref_typedesc, ref_sequence, ord_hdrnumber, ref_table) " +
                            "select @orderid, r.ref_type, r.ref_number, r.ref_typedesc, r.ref_sequence, r.ord_hdrnumber, 'orderheader' from " +
                            "referencenumber r where ord_hdrnumber = @orderid");
                    comm.Parameters.Add(new SqlParameter("orderid", Convert.ToString(id)));
                    db.Update(comm);
                }                  

                comm = new SqlCommand("update_move");
                comm.CommandType = System.Data.CommandType.StoredProcedure;
                comm.Parameters.AddWithValue("@mov", SqlDbType.Int).Value = mov_number;
                db.Update(comm);

                comm = new SqlCommand("update_ord");
                comm.CommandType = System.Data.CommandType.StoredProcedure;
                comm.Parameters.AddWithValue("@mov", SqlDbType.Int).Value = mov_number;
                comm.Parameters.AddWithValue("@invwhen", SqlDbType.VarChar).Value = DBNull.Value;
                comm.Parameters.AddWithValue("@date_presentation", SqlDbType.SmallInt).Value = DBNull.Value;
                db.Update(comm);
            }
            //db.Update("UPDATE orderheader SET ord_status = 'CMP', ord_totalweight = " + weight + ", ord_refnum = '" + ticket_number + "' WHERE orderheader.ord_number = '" + id.ToString() + "'");
            //db.Update("UPDATE stops SET stp_weight = " + weight + " WHERE ord_hdrnumber = '" + id.ToString() + "' AND stp_type = 'DRP'");

            return Ok();
        }
    }
}
