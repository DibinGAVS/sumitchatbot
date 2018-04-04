using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LuisBot.GavelEntity
{
    public class TicketStatusModel
    {
        public string unAssigned { get; set; }
        public string assigned { get; set; }
        public string inprogress { get; set; }
        public string pending { get; set; }
        public string closed { get; set; }
        public string brokenTickets { get; set; }
        public string lostTickets { get; set; }
        public string newticket  { get; set; }
        public string critical { get; set; }
        public string assignedToMe { get; set; }
        public string happyCutomers { get; set; }
        public string responseBreach { get; set; }
        public string resolutionBreach { get; set; }
    }
}