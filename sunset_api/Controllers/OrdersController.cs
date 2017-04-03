using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Data.SqlClient;

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
            /*
             SELECT orderheader.ord_number AS orderheader_ord_number, orderheader.ord_status AS orderheader_ord_status, orderheader.ord_totalweight AS orderheader_ord_totalweight, orderheader.ord_refnum AS orderheader_ord_refnum, orderheader.ord_description AS orderheader_ord_description, orderheader.ord_driver1 AS orderheader_ord_driver1, orderheader.ord_tractor AS orderheader_ord_tractor 
             FROM orderheader JOIN stops ON orderheader.ord_number = stops.ord_hdrnumber AND stops.stp_type = %(stp_type_1)s 
             WHERE orderheader.ord_status = %(ord_status_1)s AND stops.stp_schdtearliest > %(stp_schdtearliest_1)s AND stops.stp_schdtearliest < %(stp_schdtearliest_2)s
            */
            DBAccess db = new DBAccess();

            if (status.Contains("\""))
            {
                status = status.Replace("\"", "");
            }

            string query = "SELECT orderheader.ord_number AS orderheader_ord_number, orderheader.ord_status AS orderheader_ord_status, orderheader.ord_totalweight AS orderheader_ord_totalweight," +
                           "orderheader.ord_refnum AS orderheader_ord_refnum, orderheader.ord_description AS orderheader_ord_description, orderheader.ord_driver1 AS orderheader_ord_driver1," +
                           "orderheader.ord_tractor AS orderheader_ord_tractor FROM orderheader JOIN stops ON orderheader.ord_number = stops.ord_hdrnumber " +
                           "WHERE orderheader.ord_status = '" + status + "' AND stops.stp_schdtearliest > '" + start.ToString() + "' AND stops.stp_schdtearliest < '" + end.ToString() + "'";

            result = db.QuerryJSON(query);

            return result;
        }


        //// GET api/orders/5922079/complete/?weight=22.34&ticket_number=1938429
        //// GET api/orders/<order-id>/complete/?weight=22.34&ticket_number=1938429
        [HttpGet("{id}/complete")]
        public IActionResult Get(int id, string weight, string ticket_number)
        {
            //Writes 'CMP' to the orderheader.ord_status column, weight to orderheader.ord_weight, ticket number to orderheader.ord_refnum
            //And also writes the weight to the stops.stp_weight where the stops.ord_hdrnumber is the order - id and the stops.stp_type = DRP
            //Returns nothing, 200 OK

            if (weight.Contains("\""))
            {
                weight = weight.Replace("\"", "");
            }

            if (ticket_number.Contains("\""))
            {
                ticket_number = ticket_number.Replace("\"", "");
            }

            DBAccess db = new DBAccess();

            SqlCommand comm = new SqlCommand("UPDATE orderheader SET ord_status = 'CMP', ord_totalweight = @weight, ord_refnum = @ticket_number WHERE orderheader.ord_number = @orderid");
            comm.Parameters.Add(new SqlParameter("weight", weight));
            comm.Parameters.Add(new SqlParameter("ticket_number", Convert.ToString(ticket_number)));
            comm.Parameters.Add(new SqlParameter("orderid", Convert.ToString(id)));
            db.Update(comm);

            comm = new SqlCommand("UPDATE stops SET stp_weight = @weight WHERE ord_hdrnumber = @orderid AND stp_type = 'DRP'");
            comm.Parameters.Add(new SqlParameter("weight", weight));
            comm.Parameters.Add(new SqlParameter("orderid", Convert.ToString(id)));
            db.Update(comm);

            //db.Update("UPDATE orderheader SET ord_status = 'CMP', ord_totalweight = " + weight + ", ord_refnum = '" + ticket_number + "' WHERE orderheader.ord_number = '" + id.ToString() + "'");
            //db.Update("UPDATE stops SET stp_weight = " + weight + " WHERE ord_hdrnumber = '" + id.ToString() + "' AND stp_type = 'DRP'");

            return Ok();
        }
    }
}
