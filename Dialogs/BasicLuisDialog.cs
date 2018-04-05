using System;
using System.Configuration;
using System.Threading.Tasks;

using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Luis;
using Microsoft.Bot.Builder.Luis.Models;
using RestSharp;
using Newtonsoft.Json.Linq;

namespace GavelChatbot
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
        /// Get session token based on the user-key.
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
            var TicketStatus = GetTicketStatus(sessionToken);
            return TicketStatus;
        }

        /// <summary>
        /// Get ticket status based on the user-key and session-token input.
        /// </summary>
        /// <param name="sessionToken"></param>
        /// <returns></returns>
        public string GetTicketStatus(string sessionToken)
        {
            string TicketStatusURL = ConfigurationManager.AppSettings["TicketStatusServiceURL"];
            var client = new RestClient(TicketStatusURL);
            var request = new RestRequest(Method.GET);
            request.AddHeader("user-key", UserKey);
            request.AddHeader("Session-Token", sessionToken);
            IRestResponse response = client.Execute(request);
            return response.Content;
        }

        [LuisIntent("CurrentTicketStatus")]
        public async Task CurrentTicketStatusIntent(IDialogContext context, LuisResult result)
        {
            var TicketStatus = GetSession();
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
    }
}