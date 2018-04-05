using System;
using System.Configuration;
using System.Threading.Tasks;

using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Luis;
using Microsoft.Bot.Builder.Luis.Models;
using RestSharp;
using Newtonsoft.Json.Linq;
using System.Web.Script.Serialization;
using Newtonsoft.Json;
using LuisBot.Model;
using System.Text;

namespace Microsoft.Bot.Sample.LuisBot
{
    // For more information about this template visit http://aka.ms/azurebots-csharp-luis
    [Serializable]
    public class BasicLuisDialog : LuisDialog<object>
    {
        public BasicLuisDialog() : base(new LuisService(new LuisModelAttribute(
            ConfigurationManager.AppSettings["LuisAppId"], 
            ConfigurationManager.AppSettings["LuisAPIKey"], 
            domain: ConfigurationManager.AppSettings["LuisAPIHostName"])))
        {
        }

        public string UserKey = ConfigurationManager.AppSettings["UserKey"];

        /// <summary>
        /// Get session token to authenticated
        /// </summary>
        /// <returns></returns>
        public string GetSession()
        {
            string SessionURL = ConfigurationManager.AppSettings["SessionServiceURL"];
            var client = new RestClient(SessionURL);
            var request = new RestRequest(Method.GET);
            request.AddHeader("user-key", UserKey);
            IRestResponse response = client.Execute(request);
            var sessionJson = response.Content;
            JObject ParsedObject = JObject.Parse(sessionJson);
            string sessionToken = (string)ParsedObject["sessionToken"];
            return sessionToken;
        }

        /// <summary>
        /// Get Ticket Status.
        /// </summary>
        /// <param name="sessionToken"></param>
        /// <returns></returns>
        public string GetTicketStatus(string sessionToken)
        {
            string TicketStatusURL = ConfigurationManager.AppSettings["EdelmanTicketStatus_ServiceURL"];
            var client = new RestClient(TicketStatusURL);
            var request = new RestRequest(Method.GET);
            request.AddHeader("user-key", UserKey);
            request.AddHeader("Session-Token", sessionToken);
            IRestResponse response = client.Execute(request);
            return response.Content;
        }

        /// <summary>
        /// Get CSAT Ticket Status
        /// </summary>
        /// <param name="sessionToken"></param>
        /// <returns></returns>
        public string GetEdelmanTopFiveIssues(string sessionToken)
        {
            string Edelman_TopFiveIssues_ServiceURL = "https://gavel.gavstech.com/v3/customers/edelman/heatMapTickets?fromDate=2018-04-05T00:00:00Z&size=5&toDate=2018-04-05T23:59:59Z";
            var client = new RestClient(Edelman_TopFiveIssues_ServiceURL);
            var request = new RestRequest(Method.GET);
            request.AddHeader("user-key", UserKey);
            request.AddHeader("Session-Token", sessionToken);
            IRestResponse response = client.Execute(request);
            return response.Content;
        }

        public string GetEdelmanCSAT(string sessionToken)
        {
            string EdelmanServiceURL = "https://gavel.gavstech.com/v3/customers/edelman/csat-metrics?fromDate=2018-04-05T00:00:00Z&toDate=2018-04-05T23:59:59Z";
            var client = new RestClient(EdelmanServiceURL);
            var request = new RestRequest(Method.GET);
            request.AddHeader("user-key", UserKey);
            request.AddHeader("Session-Token", sessionToken);
            IRestResponse response = client.Execute(request);
            return response.Content;
        }

        #region -- Intent--

        [LuisIntent("EdelmanTicketStatus")]
        public async Task EdelmanTicketStatusIntent(IDialogContext context, LuisResult result)
        {
            var SessionToken = GetSession();
            string TicketStatus = GetTicketStatus(SessionToken);
            JObject TicketResult = JObject.Parse(TicketStatus);
            int unAssigned =(int)TicketResult["unAssigned"];
            int assigned = (int)TicketResult["assigned"];
            int inprogress = (int)TicketResult["inprogress"];
            int pending = (int)TicketResult["pending"];
            int closed = (int)TicketResult["closed"];
            int brokenTickets = (int)TicketResult["brokenTickets"];
            int lostTickets = (int)TicketResult["lostTickets"];
            int newticket =(int)TicketResult["new"];
            int critical = (int)TicketResult["critical"];
            int assignedToMe = (int)TicketResult["assignedToMe"];
            int happyCustomers = (int)TicketResult["happyCutomers"];
            int responseBreach = (int)TicketResult["responseBreach"];
            int resolutionBreach = (int)TicketResult["resolutionBreach"];
            string status = "The current status of Edelman ticket status are as follows," + " " + "Unassigned " + " " + unAssigned + "," + " " + "Assigned" + " " + assigned + "," + " " + "In progress" + " " + inprogress + "," + " " + "Pending" + " " + pending + "," + " " + "Closed" + " " + closed + "," + " " + "Broken Tickets" + " " + brokenTickets + "," + " " + "Lost Tickets" + " " + lostTickets + ","+" "+ "New"+" " + newticket +"," + " " + "Critical" + " " + critical + "," + " " + "Assigned To Me" + " " + assignedToMe + "," + " " + "Happy Customers" + " " + happyCustomers + "," + " " + "Response Breach" + " " + responseBreach + "," + " " + "Resolution Breach" + " " + resolutionBreach + ".";
            await context.SayAsync(text: status, speak: status);
        }

        [LuisIntent("EdelmanOnHoldTickets")]
        public async Task EdelmanOnHoldTicketsIntent(IDialogContext context, LuisResult result)
        {
            var SessionToken = GetSession();
            string EdelmanOnHoldTicket = GetTicketStatus(SessionToken);
            JObject EdelmanOnHoldResult = JObject.Parse(EdelmanOnHoldTicket);
            int pending = (int)EdelmanOnHoldResult["pending"];
            string status = "Edelman Onhold Ticket is" + " " + pending;
            await context.SayAsync(text: status, speak: status);
        }

        [LuisIntent("EdelmanOpenTickets")]
        public async Task EdelmanOpenTicketsIntent(IDialogContext context, LuisResult result)
        {
            var SessionToken = GetSession();
            string OpenTickets = GetTicketStatus(SessionToken);
            JObject OpenTicketsResult = JObject.Parse(OpenTickets);
            int unAssigned = (int)OpenTicketsResult["unAssigned"];
            int assigned = (int)OpenTicketsResult["assigned"];
            int TotalOpenticket= unAssigned + assigned;
            string status = "Open Ticket is" + " " + TotalOpenticket +" "+ "UnAssigned" + " " + unAssigned + "," + " " + "Assigned" + " " + assigned;
            await context.SayAsync(text: status, speak: status);
        }

        [LuisIntent("EdelmanCriticalTickets")]
        public async Task EdelmanCriticalTicketsIntent(IDialogContext context, LuisResult result)
        {
            var SessionToken = GetSession();
            string CriticalTicket = GetTicketStatus(SessionToken);
            JObject CriticalTicketResult = JObject.Parse(CriticalTicket);
            int critical = (int)CriticalTicketResult["critical"];
            string status = "Edelman Critical Ticket is" + " "+ critical;
            await context.SayAsync(text: status, speak: status);
        }
        [LuisIntent("EdelmanBreachStatus")]
        public async Task EdelmanBreachStatusIntent(IDialogContext context, LuisResult result)
        {
            var SessionToken = GetSession();
            string EdelmanBreachStatusTicket = GetTicketStatus(SessionToken);
            JObject EdelmanBreachStatusResult = JObject.Parse(EdelmanBreachStatusTicket);
            int responseBreach = (int)EdelmanBreachStatusResult["responseBreach"];
            int resolutionBreach = (int)EdelmanBreachStatusResult["resolutionBreach"];
            string status = "Response about to Breach" +" " + responseBreach + "," +" "+ "Resolution about to  Breach" + " "+ resolutionBreach;
            await context.SayAsync(text: status, speak: status);
        }
        [LuisIntent("EdelmanCSAT")]
        public async Task EdelmanCSATIntent(IDialogContext context, LuisResult result)
        {
            var SessionToken = GetSession();
            string CSATStatus = GetEdelmanCSAT(SessionToken);
            string HappyCustomers = GetTicketStatus(SessionToken);
            JObject CSATResult = JObject.Parse(CSATStatus);
            JObject HappyCustomerResult = JObject.Parse(HappyCustomers);
            int Positive = (int)CSATResult["positive"];
            int Negative = (int)CSATResult["negative"];
            int Neutral = (int)CSATResult["neutral"];
            int happyCustomer = (int)HappyCustomerResult["happyCutomers"];
            string status = "Edelman CSAT," + " " + "Positive " + " " + Positive + "," + " " + "Negative" + " " + Negative + "," + " " + "Neutral" + " " + Neutral+","+" "+ "Happy Customers"+" "+ happyCustomer+"%";
            await context.SayAsync(text: status, speak: status);
        }

        [LuisIntent("EdelmanTopFiveIssues")]
        public async Task EdelmanTopFiveIssuesIntent(IDialogContext context, LuisResult result)
        {
            var SessionToken = GetSession();
            string TopFiveIssue = GetEdelmanTopFiveIssues(SessionToken);
            JArray parsedArray = JArray.Parse(TopFiveIssue);
            StringBuilder amountMsg = new StringBuilder();
            string propertyValue = string.Empty;
            foreach (JObject parsedObject in parsedArray)
            {
                propertyValue = (string)parsedObject["key"];
                amountMsg.AppendFormat(propertyValue +", ");
            }
            string status = "Edelman Top Five Issues are," + " " + amountMsg;
            await context.SayAsync(text: status, speak: status);
        }

        [LuisIntent("None")]
        public async Task NoneIntent(IDialogContext context, LuisResult result)
        {
            await context.SayAsync(text: "Sorry i dont know that. Try saying, Tell me the ticket status.", speak: "Sorry i dont know that. Try saying, Tell me the ticket status.");
        }

        [LuisIntent("Greeting")]
        public async Task GreetingIntent(IDialogContext context, LuisResult result)
        {
            await context.SayAsync(text: "Hey!", speak: "Hey!");
        }

        [LuisIntent("Cancel")]
        public async Task CancelIntent(IDialogContext context, LuisResult result)
        {
            await context.SayAsync(text: "Okay cancelling", speak: "Okay cancelling.");
        }

        [LuisIntent("Help")]
        public async Task HelpIntent(IDialogContext context, LuisResult result)
        {
            await context.SayAsync(text: "Ok.", speak: "My creator is working on that. Could you query about Ticket status?");
        }


        #endregion
    }
}