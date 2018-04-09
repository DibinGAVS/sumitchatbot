using System;
using System.Configuration;
using System.Threading.Tasks;

using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Luis;
using Microsoft.Bot.Builder.Luis.Models;
using RestSharp;
using Newtonsoft.Json.Linq;
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


        #region -- API Methods --
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
            string Edelman_TopFiveIssues_ServiceURL = "https://gavel.gavstech.com/v3/customers/edelman/heatMapTickets?fromDate=2018-04-06T00:00:00Z&size=5&toDate=2018-04-06T23:59:59Z";
            var client = new RestClient(Edelman_TopFiveIssues_ServiceURL);
            var request = new RestRequest(Method.GET);
            request.AddHeader("user-key", UserKey);
            request.AddHeader("Session-Token", sessionToken);
            IRestResponse response = client.Execute(request);
            return response.Content;
        }

        public string GetEdelmanCSAT(string sessionToken)
        {
            string EdelmanServiceURL = "https://gavel.gavstech.com/v3/customers/edelman/csat-metrics?fromDate=2018-04-06T00:00:00Z&toDate=2018-04-06T23:59:59Z";
            var client = new RestClient(EdelmanServiceURL);
            var request = new RestRequest(Method.GET);
            request.AddHeader("user-key", UserKey);
            request.AddHeader("Session-Token", sessionToken);
            IRestResponse response = client.Execute(request);
            return response.Content;
        }

        public string GetEdelmanOpportunities(string sessionToken)
        {
            string EdelmanServiceURL = "https://gavel.gavstech.com/v3/opportunities/customers/blhc/status/open?order=asc&size=9&sort=ExpectedOccurrenceDate&start=0&status=open";
            var client = new RestClient(EdelmanServiceURL);
            var request = new RestRequest(Method.GET);
            request.AddHeader("user-key", UserKey);
            request.AddHeader("Session-Token", sessionToken);
            IRestResponse response = client.Execute(request);
            return response.Content;
        }

        #endregion

        #region -- Intent--

        [LuisIntent("EdelmanTicketStatus")]
        public async Task EdelmanTicketStatusIntent(IDialogContext context, LuisResult result)
        {
            var SessionToken = GetSession();
            string TicketStatus = GetTicketStatus(SessionToken);
            JObject TicketResult = JObject.Parse(TicketStatus);
            //int unAssigned =(int)TicketResult["unAssigned"];
            //int assigned = (int)TicketResult["assigned"];
            int inprogress = (int)TicketResult["inprogress"];
            int pending = (int)TicketResult["pending"];
           // int closed = (int)TicketResult["closed"];
           // int brokenTickets = (int)TicketResult["brokenTickets"];
           // int lostTickets = (int)TicketResult["lostTickets"];
            int newticket =(int)TicketResult["new"];
            // int critical = (int)TicketResult["critical"];
            // int assignedToMe = (int)TicketResult["assignedToMe"];
            // int happyCustomers = (int)TicketResult["happyCutomers"];
            // int responseBreach = (int)TicketResult["responseBreach"];
            // int resolutionBreach = (int)TicketResult["resolutionBreach"];
            string status = "Sure. As of now, we have" + " " + newticket+" "+ "New Tickets, " + " " + inprogress +" "+ "In progress tickets and " + " " + pending +" "+ "Pending Tickets.";

            await context.SayAsync(status, status, new MessageOptions() { InputHint = Connector.InputHints.ExpectingInput });
        }

        [LuisIntent("BLHCOpportunities")]
        public async Task EdelmanBLHCOpportunitiesIntent(IDialogContext context, LuisResult result)
        {
            var SessionToken = GetSession();
            string BLHCOpportunities = GetEdelmanOpportunities(SessionToken);
            JObject BLHCOpportunitiesResult = JObject.Parse(BLHCOpportunities);
            int open = (int)BLHCOpportunitiesResult["open"];
            string status = "New Opportunities for BLH is" + " " + open +".";


            //var response = context.MakeMessage();
            //response.Text = status;
            //response.Speak = status;
            //response.InputHint = Connector.InputHints.ExpectingInput;
            //await context.PostAsync(response);

            await context.SayAsync(status, status, new MessageOptions() { InputHint = Connector.InputHints.ExpectingInput });
            //await context.SayAsync(text: status, speak: status);
        }
        [LuisIntent("EdelmanOnHoldTickets")]
        public async Task EdelmanOnHoldTicketsIntent(IDialogContext context, LuisResult result)
        {
            var SessionToken = GetSession();
            string EdelmanOnHoldTicket = GetTicketStatus(SessionToken);
            JObject EdelmanOnHoldResult = JObject.Parse(EdelmanOnHoldTicket);
            int pending = (int)EdelmanOnHoldResult["pending"];
            string status = "Currently, there are" + " " + pending + " " + "tickets which are On hold.";
            await context.SayAsync(status, status, new MessageOptions() { InputHint = Connector.InputHints.ExpectingInput });
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
            string status = "Right now, I could see there are" + " " + TotalOpenticket + " " + "open tickets, in which" + " " + assigned + " " + "are assigned to the engineers and " + " " + unAssigned + " " + "are not.";
            await context.SayAsync(status, status, new MessageOptions() { InputHint = Connector.InputHints.ExpectingInput });
        }

        [LuisIntent("EdelmanCriticalTickets")]
        public async Task EdelmanCriticalTicketsIntent(IDialogContext context, LuisResult result)
        {
            var SessionToken = GetSession();
            string CriticalTicket = GetTicketStatus(SessionToken);
            JObject CriticalTicketResult = JObject.Parse(CriticalTicket);
            int critical = (int)CriticalTicketResult["critical"];
            string status = "Well, for now, you have "+ " "+ critical +" " +"critical tickets. Ask me later or check Gavel portal to stay updated on this.";
            await context.SayAsync(status, status, new MessageOptions() { InputHint = Connector.InputHints.ExpectingInput });
        }
        [LuisIntent("EdelmanBreachStatus")]
        public async Task EdelmanBreachStatusIntent(IDialogContext context, LuisResult result)
        {
            var SessionToken = GetSession();
            string EdelmanBreachStatusTicket = GetTicketStatus(SessionToken);
            JObject EdelmanBreachStatusResult = JObject.Parse(EdelmanBreachStatusTicket);
            int responseBreach = (int)EdelmanBreachStatusResult["responseBreach"];
            int resolutionBreach = (int)EdelmanBreachStatusResult["resolutionBreach"];
            string status = "Okay. Looks like you have" + " " + responseBreach + " " + "resolutions which are about to breach. And as far responses are concerned, you have " + " " + resolutionBreach + " " + "to breach.";
            await context.SayAsync(status, status, new MessageOptions() { InputHint = Connector.InputHints.ExpectingInput });
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
            string status = "I could see that you have" + " " + Positive + " " + "positives, " + " " + Negative + " " + "negatives and" + " " + Neutral + " " + " neutral ratings, which makes your C Sat score a" + " " + happyCustomer + "%.";
            await context.SayAsync(status, status, new MessageOptions() { InputHint = Connector.InputHints.ExpectingInput });
        }

        [LuisIntent("EdelmanTopFiveIssues")]
        public async Task EdelmanTopFiveIssuesIntent(IDialogContext context, LuisResult result)
        {
            var SessionToken = GetSession();
            string TopFiveIssue = GetEdelmanTopFiveIssues(SessionToken);
            JArray parsedArray = JArray.Parse(TopFiveIssue);
            StringBuilder TopFive = new StringBuilder();
            string propertyValue = string.Empty;
            foreach (JObject parsedObject in parsedArray)
            {
                propertyValue = (string)parsedObject["key"];
                TopFive.AppendFormat(propertyValue +", ");
            }
            string status = "By looking at the Heatmap of Edelman, I could see that your top 5 issues are" + " " + TopFive;
            await context.SayAsync(status, status, new MessageOptions() { InputHint = Connector.InputHints.ExpectingInput });
        }

        [LuisIntent("None")]
        public async Task NoneIntent(IDialogContext context, LuisResult result)
        {
            string status = "Sorry i dont know that. Try saying, Tell me the ticket status.";
            await context.SayAsync(status, status, new MessageOptions() { InputHint = Connector.InputHints.ExpectingInput });
        }

        [LuisIntent("Greeting")]
        public async Task GreetingIntent(IDialogContext context, LuisResult result)
        {
            string status = "Hey!";
            await context.SayAsync(status, status, new MessageOptions() { InputHint = Connector.InputHints.ExpectingInput });
        }

        [LuisIntent("Cancel")]
        public async Task CancelIntent(IDialogContext context, LuisResult result)
        {
            string status = "Okay cancelling";
            await context.SayAsync(status, status, new MessageOptions() { InputHint = Connector.InputHints.ExpectingInput });
        }

        [LuisIntent("Help")]
        public async Task HelpIntent(IDialogContext context, LuisResult result)
        {
            string Text = "Ok.";
            string Speak = "My creator is working on that. Could you query about Ticket status?";
            await context.SayAsync(Text, Speak, new MessageOptions() { InputHint = Connector.InputHints.ExpectingInput });
        }


        #endregion
    }
}