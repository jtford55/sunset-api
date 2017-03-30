using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace sunset_api.Controllers
{
    [Route("api/[controller]")]
    public class OrdersController : Controller
    {
        // GET api/orders
        [HttpGet]
        public IEnumerable<string> Get()
        {
            return new string[] { "Sunset", "Trucking" };
        }

        //// Get started order based on ID
        //// GET api/orders/<order-id>/started
        [HttpGet("{id}/started")]
        public string Get(int id)
        {
            //Writes a single column on a table.  Write 'STD' to the orderheader.ord_status column   where the orderheader.ord_number == the id in the GET url
            //Returns nothing, 200 OK

            return "Started Order: " + id;
        }

        //// GET api/orders/byStatus?status="PLN"&start=2017-03-06T19:07:46Z&end=2017-03-07T23:07:46Z
        [HttpGet("byStatus")]
        public string Get(string status, DateTime start, DateTime end)
        {
            //Querry DB for data and return JSON

            return status + " " + start.ToString() + " " + end.ToString() ;
        }


        //// GET api/orders/<order-id>/complete/?weight="22.34"&ticket_number="1938429"
        [HttpGet("{id}/complete")]
        public string Get(int id, string weight, string ticket_number)
        {
            //Writes 'CMP' to the orderheader.ord_status column, weight to orderheader.ord_weight, ticket number to orderheader.ord_refnum
            //And also writes the weight to the stops.stp_weight where the stops.ord_hdrnumber is the order - id and the stops.stp_type = DRP
            //Returns nothing, 200 OK

            return id.ToString() + " "  + weight + " " + ticket_number;
        }
    }
}
