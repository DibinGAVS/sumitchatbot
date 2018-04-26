using System;
using System.Configuration;
using System.Threading.Tasks;

using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Luis;
using Microsoft.Bot.Builder.Luis.Models;
using RestSharp;
using Newtonsoft.Json.Linq;
using System.Text;
using AdaptiveCards;
using System.Collections.Generic;
using Microsoft.Bot.Connector;
using AdaptiveCards.Rendering.Config;

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
            string Edelman_TopFiveIssues_ServiceURL = "https://gavel.gavstech.com/v3/customers/edelman/heatMapTickets?fromDate=2018-04-11T00:00:00Z&size=5&toDate=2018-04-11T23:59:59Z";
            var client = new RestClient(Edelman_TopFiveIssues_ServiceURL);
            var request = new RestRequest(Method.GET);
            request.AddHeader("user-key", UserKey);
            request.AddHeader("Session-Token", sessionToken);
            IRestResponse response = client.Execute(request);
            return response.Content;
        }

        public string GetEdelmanCSAT(string sessionToken)
        {
            string EdelmanServiceURL = "https://gavel.gavstech.com/v3/customers/edelman/csat-metrics?fromDate=2018-04-11T00:00:00Z&toDate=2018-04-11T23:59:59Z";
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
            int inprogress = (int)TicketResult["inprogress"];
            int pending = (int)TicketResult["pending"];
            int newticket = (int)TicketResult["new"];
            //string status = "Sure. As of now, we have" + " " + newticket+" "+ "New Tickets, " + " " + inprogress +" "+ "In progress tickets and " + " " + pending +" "+ "Pending Tickets.";

            //await context.SayAsync(status, status, new MessageOptions() { InputHint = Connector.InputHints.ExpectingInput });
            var message = context.MakeMessage();
            var attachment = GetAdaptiveCard();
            message.Attachments.Add(attachment);
            await context.PostAsync(message);
        }

        [LuisIntent("BLHCOpportunities")]
        public async Task EdelmanBLHCOpportunitiesIntent(IDialogContext context, LuisResult result)
        {
            var SessionToken = GetSession();
            string BLHCOpportunities = GetEdelmanOpportunities(SessionToken);
            JObject BLHCOpportunitiesResult = JObject.Parse(BLHCOpportunities);
            int open = (int)BLHCOpportunitiesResult["open"];
            string status = "New Opportunities for BLH is" + " " + open + ".";


            //var response = context.MakeMessage();
            //response.Text = status;
            //response.Speak = status;
            //response.InputHint = Connector.InputHints.ExpectingInput;
            //await context.PostAsync(response);

            await context.SayAsync(status, status, new MessageOptions() { InputHint = Connector.InputHints.ExpectingInput });
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
            int TotalOpenticket = unAssigned + assigned;
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
            string status = "Well, for now, you have " + " " + critical + " " + "critical tickets. Ask me later or check Gavel portal to stay updated on this.";
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
            

            #region Card One
            AdaptiveCard cardone = new AdaptiveCard()
            {
                BackgroundImage = "https://edelmangavelbot.azurewebsites.net/Images/ic_background_02.png",
                Body = new List<CardElement>()
              {
                  new Container()
                    {

                 Items = new List<CardElement>()
                    {
                    new ColumnSet()
                    {
                    Columns = new List<Column>()
                    {
                                    new Column()
                                    {
                                         Size =ColumnSize.Stretch,
                                        Items = new List<CardElement>()
                                        {
                                            new Image()
                                            {
                                                Url = "https://edelmangavelbot.azurewebsites.net/Images/ic_single_app.png",
                                                Size = ImageSize.Large,
                                                Style = ImageStyle.Normal
                                            }
                                        }
                                    },
                                     new Column()
                                    {
                                        Items = new List<CardElement>()
                                        {
                                             new Image()
                                            {
                                                Url = "https://edelmangavelbot.azurewebsites.net/Images/ic_alert_large.png",
                                                Size = ImageSize.Auto,
                                                Style = ImageStyle.Normal
                                            },
                                        }
                                     },
                                    new Column()
                                    {
                                        Items = new List<CardElement>()
                                        {
                                            new TextBlock()
                                            {
                                                Text =  "20 Alerts",
                                                Speak = "20 Alerts",
                                                Weight = TextWeight.Normal,
                                                HorizontalAlignment=HorizontalAlignment.Center,
                                                Size=TextSize.Normal,
                                                Wrap = true,
                                                Color=TextColor.Light,
                                                Separation=SeparationStyle.Strong,
                                            },
                                        }
                                    },
                                    new Column()
                                    {
                                        Separation=SeparationStyle.Strong,
                                        Items= new List<CardElement>()
                                        {
                                            new Image()
                                            {
                                                Url = "https://edelmangavelbot.azurewebsites.net/Images/ic_correlation.png",
                                                Size = ImageSize.Auto,
                                                Style = ImageStyle.Person,
                                            }
                                        }
                                    },
                                    new Column()
                                    {
                                        Items= new List<CardElement>()
                                        {
                                             new TextBlock()
                                            {
                                                Text = "12 Correlation",
                                                Speak= "12 Correlation",
                                                Weight = TextWeight.Normal,
                                                HorizontalAlignment=HorizontalAlignment.Center,
                                                Size=TextSize.Normal,
                                                Wrap = true,
                                                Color=TextColor.Light
                                            }
                                        }
                                    }
                                }
                             },
                 //   new ColumnSet()
                 //   {
                 //   Columns = new List<Column>()
                 //   {
                 //               new Column()
                 //                   {
                 //                       Size =ColumnSize.Stretch,
                 //                       Items = new List<CardElement>()
                 //                       {
                 //                           new Image()
                 //                           {
                 //                               Url = "",
                 //                               Size = ImageSize.Auto,
                 //                               Style = ImageStyle.Normal,
                 //                               HorizontalAlignment=HorizontalAlignment.Center
                 //                           }
                 //                       }
                 //                   },
                 //                new Column()
                 //                   {
                 //                       Items = new List<CardElement>()
                 //                       {
                 //                            new TextBlock()
                 //                           {
                 //                               Text =  "",
                 //                               Weight = TextWeight.Normal,
                 //                               HorizontalAlignment=HorizontalAlignment.Left,
                 //                               Size=TextSize.ExtraLarge,
                 //                               Wrap = true,
                 //                               Color=TextColor.Light,
                 //                           }
                 //                       }
                 //                   }
                 //   }
                 //   },
                 //   new ColumnSet()
                 //   {
                 //   Columns = new List<Column>()
                 //   {
                 //               new Column()
                 //                   {
                 //                      Size =ColumnSize.Stretch,
                 //                       Items = new List<CardElement>()
                 //                       {
                 //                           new Image()
                 //                           {
                 //                               Url = "",
                 //                               Size = ImageSize.Auto,
                 //                               Style = ImageStyle.Normal,
                 //                               HorizontalAlignment=HorizontalAlignment.Center
                 //                           }
                 //                       }
                 //                   },
                 //                new Column()
                 //                   {
                 //                       Items = new List<CardElement>()
                 //                       {
                 //                            new TextBlock()
                 //                           {
                 //                               Text =  "",
                 //                               Weight = TextWeight.Normal,
                 //                               HorizontalAlignment=HorizontalAlignment.Left,
                 //                               Size=TextSize.ExtraLarge,
                 //                               Wrap = true,
                 //                               Color=TextColor.Light,
                 //                           }
                 //                       }
                 //                   }
                 //   }
                 //   },
                 //   new ColumnSet()
                 //   {
                 //   Columns = new List<Column>()
                 //   {
                 //               new Column()
                 //                   {
                 //                       Size =ColumnSize.Stretch,
                 //                       Items = new List<CardElement>()
                 //                       {
                 //                           new Image()
                 //                           {
                 //                               Url = "",
                 //                               Size = ImageSize.Auto,
                 //                               Style = ImageStyle.Normal,
                 //                               HorizontalAlignment=HorizontalAlignment.Center

                 //                           }

                 //                       }
                 //                   },
                 //                new Column()
                 //                   {
                 //                       Items = new List<CardElement>()
                 //                       {
                 //                            new TextBlock()
                 //                           {
                 //                               Text =  "",
                 //                               Weight = TextWeight.Normal,
                 //                               HorizontalAlignment=HorizontalAlignment.Left,
                 //                               Size=TextSize.ExtraLarge,
                 //                               Wrap = true,
                 //                               Color=TextColor.Light,
                 //                           }
                 //                       }
                 //                   }
                 //   }
                 //   },
                 //   new ColumnSet()
                 //   {

                 //   Columns = new List<Column>()
                 //   {
                 //               new Column()
                 //                   {
                 //                       Size =ColumnSize.Stretch,
                 //                       Items = new List<CardElement>()
                 //                       {
                 //                           new Image()
                 //                           {
                 //                               Url = "",
                 //                               Size = ImageSize.Auto,
                 //                               Style = ImageStyle.Normal,
                 //                               HorizontalAlignment=HorizontalAlignment.Center
                 //                           }
                 //                       }
                 //                   },
                 //                new Column()
                 //                   {
                 //                       Items = new List<CardElement>()
                 //                       {
                 //                            new TextBlock()
                 //                           {
                 //                               Text =  "",
                 //                               Weight = TextWeight.Normal,
                 //                               HorizontalAlignment=HorizontalAlignment.Left,
                 //                               Size=TextSize.ExtraLarge,
                 //                               Wrap = true,
                 //                               Color=TextColor.Light,
                 //                           }
                 //                       }
                 //                   }
                 //   }
                 //   },
                 //// third column

                 //   new ColumnSet()
                 //   {
                 //   Columns = new List<Column>()
                 //   {
                 //            new Column()
                 //                   {
                 //                       Items = new List<CardElement>()
                 //                       {
                 //                               new Image()
                 //                           {
                 //                               Url = "https://edelmangavelbot.azurewebsites.net/Images/ic_apm_healthy.png",
                 //                               Size = ImageSize.Small,
                 //                               Style = ImageStyle.Normal,
                 //                           },
                 //                           new TextBlock()
                 //                           {
                 //                               Type="TextBlock",
                 //                               Text = "APM",
                 //                               Weight = TextWeight.Normal,
                 //                               Size=TextSize.Normal,
                 //                               Wrap = true,
                 //                               Color=TextColor.Light
                 //                           }

                 //                       }
                 //                   },
                 //            new Column()
                 //                   {
                 //                       Separation=SeparationStyle.Strong,
                 //                       Items = new List<CardElement>()
                 //                       {
                 //                               new Image()
                 //                           {
                 //                               Url = "https://edelmangavelbot.azurewebsites.net/Images/ic_server_up.png",
                 //                               Size = ImageSize.Small,
                 //                               Style = ImageStyle.Normal,

                 //                           },
                 //                           new TextBlock()
                 //                           {
                 //                               Type="TextBlock",
                 //                               Text = "Server",
                 //                               Weight = TextWeight.Normal,
                 //                               Size=TextSize.Normal,
                 //                               Color=TextColor.Light,
                 //                               Wrap = true,
                 //                           }

                 //                       }
                 //                   },
                 //            new Column()
                 //                   {
                 //                       Separation=SeparationStyle.Strong,
                 //                       Items = new List<CardElement>()
                 //                       {
                 //                               new Image()
                 //                           {
                 //                               Url = "https://edelmangavelbot.azurewebsites.net/Images/ic_database_down.png",
                 //                               Size = ImageSize.Small,
                 //                               Style = ImageStyle.Normal,

                 //                           },
                 //                           new TextBlock()
                 //                           {
                 //                               Type="TextBlock",
                 //                               Text = "Database",
                 //                               Weight = TextWeight.Normal,
                 //                               Size=TextSize.Normal,
                 //                               Color=TextColor.Light,
                 //                               Wrap = true,
                 //                           }

                 //                       }
                 //                   },
                 //            new Column()
                 //                   {
                 //                       Separation=SeparationStyle.Strong,
                 //                       Items = new List<CardElement>()
                 //                       {
                 //                               new Image()
                 //                           {
                 //                               Url = "https://edelmangavelbot.azurewebsites.net/Images/ic_network_up.png",
                 //                               Size = ImageSize.Small,
                 //                               Style = ImageStyle.Normal,

                 //                           },
                 //                           new TextBlock()
                 //                           {
                 //                               Type="TextBlock",
                 //                               Text = "Network",
                 //                               Weight = TextWeight.Normal,
                 //                               Size=TextSize.Normal,
                 //                               Wrap = true,
                 //                               Color=TextColor.Light
                 //                           }

                 //                       }
                 //                   },
                 //            new Column()
                 //                   {
                 //                       Separation=SeparationStyle.Strong,
                 //                       Items = new List<CardElement>()
                 //                       {
                 //                               new Image()
                 //                           {
                 //                               Url = "https://edelmangavelbot.azurewebsites.net/Images/ic_storage_down.png",
                 //                               Size = ImageSize.Small,
                 //                               Style = ImageStyle.Normal,

                 //                           },
                 //                           new TextBlock()
                 //                           {
                 //                               Type="TextBlock",
                 //                               Text = "Storage",
                 //                               Weight = TextWeight.Normal,
                 //                               Size=TextSize.Normal,
                 //                               Color=TextColor.Light,
                 //                               Wrap = true,
                 //                           }

                 //                       }
                 //                   },

                 //           }

                 //        },
                 //   new ColumnSet()
                 //   {
                 //   Columns = new List<Column>()
                 //   {
                 //               new Column()
                 //                   {
                 //                      Size =ColumnSize.Stretch,
                 //                       Items = new List<CardElement>()
                 //                       {
                 //                           new Image()
                 //                           {
                 //                               Url = "",
                 //                               Size = ImageSize.Auto,
                 //                               Style = ImageStyle.Normal,
                 //                               HorizontalAlignment=HorizontalAlignment.Center
                 //                           }

                 //                       }
                 //                   },
                 //                new Column()
                 //                   {
                 //                       Items = new List<CardElement>()
                 //                       {
                 //                            new TextBlock()
                 //                           {
                 //                               Text =  "",
                 //                               Weight = TextWeight.Normal,
                 //                               HorizontalAlignment=HorizontalAlignment.Left,
                 //                               Size=TextSize.ExtraLarge,
                 //                               Wrap = true,
                 //                               Color=TextColor.Light,
                 //                           }
                 //                       }
                 //                   }
                 //   }
                 //   },
                    //new ColumnSet()
                    //{
                    //Columns = new List<Column>()
                    //            {
                    //            new Column()
                    //                {
                    //                   Size =ColumnSize.Stretch,
                    //                    Items = new List<CardElement>()
                    //                    {
                    //                        new Image()
                    //                        {
                    //                            Url = "",
                    //                            Size = ImageSize.Auto,
                    //                            Style = ImageStyle.Normal,
                    //                            HorizontalAlignment=HorizontalAlignment.Center
                    //                        }
                    //                    }
                    //                },
                    //             new Column()
                    //                {
                    //                    Items = new List<CardElement>()
                    //                    {
                    //                         new TextBlock()
                    //                        {
                    //                            Text =  "",
                    //                            Weight = TextWeight.Normal,
                    //                            HorizontalAlignment=HorizontalAlignment.Left,
                    //                            Size=TextSize.ExtraLarge,
                    //                            Wrap = true,
                    //                            Color=TextColor.Light,
                    //                        }
                    //                    }
                    //                }
                    //}
                    //},

                   }
                }
                }
            };

            Attachment attachment = new Attachment()
            {
                ContentType = AdaptiveCard.ContentType,
                Content = cardone
            };
            #endregion
            var reply = context.MakeMessage();
            reply.Attachments.Add(attachment);

            await context.PostAsync(reply);


           // AdaptiveCard card = new AdaptiveCard()
           // {
           //     Title= "Hello!</s><s>Are you looking for a flight or a hotel?",
           //     Speak= "Hello!</s><s>Are you looking for a flight or a hotel?",
           //     Body = new List<CardElement>()
           //     {
           //         new Container()
           //         {
                       
           //             Items = new List<CardElement>()
           //             {
           //                 new ColumnSet()
           //                 {
           //                     Columns = new List<Column>()
           //                     {
           //                         new Column()
           //                         {
           //                             Size = ColumnSize.Auto,
           //                             Items = new List<CardElement>()
           //                             {
           //                                 new Image()
           //                                 {
           //                                     Url = "https://placeholdit.imgix.net/~text?txtsize=65&txt=Adaptive+Cards&w=300&h=300",
           //                                     Size = ImageSize.Medium,
           //                                     Style = ImageStyle.Person
           //                                 }
           //                             }
           //                         },
           //                         new Column()
           //                         {
           //                             Size = ColumnSize.Stretch,
           //                             Items = new List<CardElement>()
           //                             {
           //                                 new TextBlock()
           //                                 {
           //                                     Text =  "Hello!",
           //                                     Speak="Hello",
           //                                     Weight = TextWeight.Bolder,
           //                                     IsSubtle = true
           //                                 },
           //                                 new TextBlock()
           //                                 {
           //                                     Text = "Are you looking for sub Content?",
           //                                     Wrap = true,
           //                                     Speak="Are you looking for sub Content?"

           //                                 }
           //                             }
           //                         }
           //                     }
           //                 }
           //             }
           //         }
           //     },
               
           // };

           // Attachment attachment = new Attachment()
           // {
           //     ContentType = AdaptiveCard.ContentType,
           //     Content = card
           // };

           //// var reply = context.MakeMessage();
           // reply.Attachments.Add(attachment);

           // await context.PostAsync(reply);

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
            AdaptiveCard card = new AdaptiveCard()
            {
                Title = "Hello!</s><s>Are you looking for a flight or a hotel?",
                Speak = "Hello!</s><s>Are you looking for a flight or a hotel?",
                BackgroundImage= "https://edelmangavelbot.azurewebsites.net/Images/ic_background_02.png",
                Body = new List<CardElement>()
                 {
                    
                     new Container()
                     {

                         Items = new List<CardElement>()
                         {
                             new ColumnSet()
                             {
                                 Columns = new List<Column>()
                                 {
                                     new Column()
                                     {
                                         Size = ColumnSize.Auto,
                                         Items = new List<CardElement>()
                                         {
                                             new Image()
                                             {
                                                 Url = "https://placeholdit.imgix.net/~text?txtsize=65&txt=Adaptive+Cards&w=300&h=300",
                                                 Size = ImageSize.Medium,
                                                 Style = ImageStyle.Person
                                             }
                                         }
                                     },
                                     new Column()
                                     {
                                         Size = ColumnSize.Stretch,
                                         Items = new List<CardElement>()
                                         {
                                             new TextBlock()
                                             {
                                                 Text =  "Hello!",
                                                 Speak="Hello",
                                                 Weight = TextWeight.Bolder,
                                                 IsSubtle = true
                                             },
                                             new TextBlock()
                                             {
                                                 Text = "Are you looking for sub Content?",
                                                 Wrap = true,
                                                 Speak="Are you looking for sub Content?"

                                             }
                                         }
                                     }
                                 }
                             }
                         }
                     }
                 },

            };

            Attachment attachment = new Attachment()
            {
                ContentType = AdaptiveCard.ContentType,
                Content = card
            };

            var reply = context.MakeMessage();
            reply.Attachments.Add(attachment);

            await context.PostAsync(reply);
        }

        [LuisIntent("Cancel")]
        public async Task CancelIntent(IDialogContext context, LuisResult result)
        {
            await context.SayAsync("Have a good day.", "Sure. Have a good day!", new MessageOptions() { InputHint = Connector.InputHints.AcceptingInput });
        }

        [LuisIntent("Help")]
        public async Task HelpIntent(IDialogContext context, LuisResult result)
        {
            string Text = "Ok.";
            string Speak = "My creator is working on that. Could you query about Ticket status?";
            await context.SayAsync(Text, Speak, new MessageOptions() { InputHint = Connector.InputHints.ExpectingInput });
        }

       
        private static Attachment GetAdaptiveCard()
        {
            var Adaptcard = new AdaptiveCard
            {
                BackgroundImage = "https://edelmangavelbot.azurewebsites.net/Images/img_background_01.png",
                Title = "Over All Applciation Health",
                Speak = "Over All Applciation Health",
                Version="1.0",
                Body = new List<CardElement>()
                {
                new Container()
                    {

                 Items = new List<CardElement>()
                    {
                new ColumnSet()
                {
                    Columns = new List<Column>()
                    {
                        new Column()
                        {
                            Size = ColumnSize.Stretch,
                            Items = new List<CardElement>()
                            {
                                new Image()
                                {
                                    Url = " ",
                                    Size = ImageSize.Auto,
                                    Style = ImageStyle.Normal,
                                    HorizontalAlignment = HorizontalAlignment.Right
                                }
                            }
                        },
                        new Column()
                        {
                            Size = ColumnSize.Stretch,
                            Items = new List<CardElement>()
                            {
                                new TextBlock()
                                {
                                    Text = "",
                                    Size = TextSize.Normal,
                                    Weight = TextWeight.Normal,
                                    HorizontalAlignment = HorizontalAlignment.Right
                                }
                            }
                        }
                    }
                },
                new ColumnSet()
                {
                    Columns = new List<Column>()
                    {
                        new Column()
                        {
                            Size = ColumnSize.Stretch,
                            Items = new List<CardElement>()
                            {
                                new Image()

                                {
                                    Url = " ",
                                    Size = ImageSize.Auto,
                                    Style = ImageStyle.Normal,
                                    HorizontalAlignment = HorizontalAlignment.Right
                                }
                            }
                        },
                        new Column()
                        {
                            Size = ColumnSize.Stretch,
                            Items = new List<CardElement>()
                            {
                                new TextBlock()

                                {
                                  Text = "",
                                    Size = TextSize.Normal,
                                    Weight = TextWeight.Normal,
                                    HorizontalAlignment = HorizontalAlignment.Right
                                }
                            }
                        }
                    }
                },
                new ColumnSet()
                {
                    Columns = new List<Column>()
                    {
                        new Column()
                        {
                            Size = ColumnSize.Stretch,
                            Items = new List<CardElement>()
                            {
                                new Image()

                                {
                                    Url = " ",
                                    Size = ImageSize.Auto,
                                    Style = ImageStyle.Normal,
                                    HorizontalAlignment = HorizontalAlignment.Right
                                }
                            }
                        },
                        new Column()
                        {
                            Size = ColumnSize.Stretch,
                            Items = new List<CardElement>()
                            {
                                new TextBlock()

                                {
                                  Text = "",
                                    Size = TextSize.Normal,
                                    Weight = TextWeight.Normal,
                                    HorizontalAlignment = HorizontalAlignment.Right
                                }
                            }
                        }
                    }
                },
                //First Column
                new ColumnSet()
                    {
                        Columns = new List<Column>()
                        {
                        new Column()
                            {
                            Size = "40"  ,
                            Items = new List<CardElement>()
                                {
                                new Image()
                                    {
                                    Url = "https://edelmangavelbot.azurewebsites.net/Images/ic_application.png",
                                    Size = ImageSize.Large,
                                    Style = ImageStyle.Normal,
                                    HorizontalAlignment = HorizontalAlignment.Center
                                    }
                                }
                            },
                        new Column()
                            {
                                Size = "25",
                                Style = ContainerStyle.Normal,

                                Items = new List<CardElement>()
                                {
                                     new Image()
                                    {
                                    Url = "https://edelmangavelbot.azurewebsites.net/Images/ic_risk.png",
                                    Size = ImageSize.Large,
                                    Style = ImageStyle.Normal,
                                    HorizontalAlignment = HorizontalAlignment.Center
                                    },
                                new TextBlock()
                                    {
                                    Text =  "15",
                                    Weight = TextWeight.Bolder,
                                    Size = TextSize.ExtraLarge,
                                    IsSubtle = true,
                                      Color = TextColor.Attention,
                                    HorizontalAlignment = HorizontalAlignment.Center,
                                    Wrap = true
                                    }
                                ,
                                new TextBlock()
                                    {
                                    Text = "Applications",
                                    Speak= "15 Applications",
                                    Size = TextSize.Small,
                                    Color = TextColor.Light,
                                    Wrap = true,
                                     HorizontalAlignment = HorizontalAlignment.Center,
                                       Separation= SeparationStyle.None
                                    }
                                }
                            },
                        new Column()
                            {
                                Size = "15",
                                 Style = ContainerStyle.Normal,
                                 Separation = SeparationStyle.Strong,
                                Items = new List<CardElement>()
                                {
                                     new Image()
                                    {
                                    Url = "https://edelmangavelbot.azurewebsites.net/Images/ic_alert.png",
                                    Size = ImageSize.Large,
                                    Style = ImageStyle.Person,
                                    HorizontalAlignment = HorizontalAlignment.Center
                                    },
                                new TextBlock()
                                    {
                                    Text =  "8",
                                    Weight = TextWeight.Bolder,
                                    Size = TextSize.ExtraLarge,  Color = TextColor.Warning,
                                    IsSubtle = true,
                                    HorizontalAlignment = HorizontalAlignment.Center,
                                    Wrap = true
                                    }
                                ,
                                new TextBlock()
                                    {
                                    Text = "Risk",
                                    Speak= "8 Risk",
                                    Size = TextSize.Small,
                                    Color = TextColor.Light,
                                    Wrap = true,
                                     HorizontalAlignment = HorizontalAlignment.Center,
                                       Separation= SeparationStyle.None
                                    }
                                }
                            },new Column()
                            {
                                Size = "20",
                                Separation = SeparationStyle.Strong,
                                Items = new List<CardElement>()
                                {
                                     new Image()
                                    {
                                    Url = "https://edelmangavelbot.azurewebsites.net/Images/ic_healthy.png",
                                    Size = ImageSize.Large,
                                    Style = ImageStyle.Normal,
                                    HorizontalAlignment = HorizontalAlignment.Center
                                    },
                                new TextBlock()
                                    {
                                    Text =  "2",
                                    Weight = TextWeight.Bolder,
                                    Size = TextSize.ExtraLarge,  Color = TextColor.Good,
                                    IsSubtle = true,
                                    HorizontalAlignment = HorizontalAlignment.Center,
                                    Wrap = true
                                    }
                                ,
                                new TextBlock()
                                    {
                                    Text = "Warning",
                                    Speak= "2 Warning",
                                    Size = TextSize.Small,
                                    Color = TextColor.Light,
                                    Wrap = true,
                                     HorizontalAlignment = HorizontalAlignment.Center,
                                       Separation= SeparationStyle.None
                                    }
                                }
                            }

                        }
                    }
                ,
                new ColumnSet()
                {
                    Columns = new List<Column>()
                    {
                        new Column()
                        {
                            Size = ColumnSize.Stretch,
                            Items = new List<CardElement>()
                            {
                                new Image()

                                {
                                    Url = " ",
                                    Size = ImageSize.Auto,
                                    Style = ImageStyle.Normal,
                                    HorizontalAlignment = HorizontalAlignment.Right
                                }
                            }
                        },
                        new Column()
                        {
                            Size = ColumnSize.Stretch,
                            Items = new List<CardElement>()
                            {
                                new TextBlock()

                                {
                                  Text = "",
                                    Size = TextSize.Normal,
                                    Weight = TextWeight.Normal,
                                    HorizontalAlignment = HorizontalAlignment.Right
                                }
                            }
                        }
                    }
                 }
                ,
                new ColumnSet()
                {
                    Columns = new List<Column>()
                    {
                        new Column()
                        {
                            Size = ColumnSize.Stretch,
                            Items = new List<CardElement>()
                            {
                                new Image()

                                {
                                    Url = " ",
                                    Size = ImageSize.Auto,
                                    Style = ImageStyle.Normal,
                                    HorizontalAlignment = HorizontalAlignment.Right
                                }
                            }
                        },
                        new Column()
                        {
                            Size = ColumnSize.Stretch,
                            Items = new List<CardElement>()
                            {
                                new TextBlock()

                                {
                                  Text = "",
                                   Size = TextSize.Normal,
                                    Weight = TextWeight.Normal,
                                    HorizontalAlignment = HorizontalAlignment.Right
                                }
                            }
                        }
                    }
                 },
                new ColumnSet()
                {
                    Columns = new List<Column>()
                    {
                        new Column()
                        {
                            Size = ColumnSize.Stretch,
                            Items = new List<CardElement>()
                            {
                                new Image()

                                {
                                    Url = " ",
                                    Size = ImageSize.Auto,
                                    Style = ImageStyle.Normal,
                                    HorizontalAlignment = HorizontalAlignment.Right
                                }
                            }
                        },
                        new Column()
                        {
                            Size = ColumnSize.Stretch,
                            Items = new List<CardElement>()
                            {
                                new TextBlock()

                                {
                                  Text = "",
                                    Size = TextSize.Normal,
                                    Weight = TextWeight.Normal,
                                    HorizontalAlignment = HorizontalAlignment.Right
                                }
                            }
                        }
                    }
                 },
                new ColumnSet()
                {
                    Columns = new List<Column>()
                    {
                        new Column()
                        {
                            Size = ColumnSize.Stretch,
                            Items = new List<CardElement>()
                            {
                                new Image()

                                {
                                    Url = " ",
                                    Size = ImageSize.Auto,
                                    Style = ImageStyle.Normal,
                                    HorizontalAlignment = HorizontalAlignment.Right
                                }
                            }
                        },
                        new Column()
                        {
                            Size = ColumnSize.Stretch,
                            Items = new List<CardElement>()
                            {
                                new TextBlock()

                               {
                                  Text = "",
                                    Size = TextSize.Normal,
                                    Weight = TextWeight.Normal,
                                    HorizontalAlignment = HorizontalAlignment.Right
                                }
                            }
                        }
                    }
                 },
                //Second ColumnSet
                new ColumnSet()
                {
                    Columns = new List<Column>()
                    {
                        new Column()
                        {
                            Size = "10",
                            Items = new List<CardElement>()
                            {
                                new Image()

                                {
                                    Url = "https://edelmangavelbot.azurewebsites.net/Images/ic_risk.png",
                                    Size = ImageSize.Auto,
                                    Style = ImageStyle.Normal,
                                    HorizontalAlignment = HorizontalAlignment.Right
                                }
                            }
                        },
                         new Column()
                        {
                            Size = "30",

                            Items = new List<CardElement>()
                            {
                                new TextBlock()
                                {
                                    Text =  "Riskfort",
                                    HorizontalAlignment = HorizontalAlignment.Left,
                                    Color = TextColor.Light,
                                    Wrap = false
                                }
                            }
                        },
                         new Column()
                        {
                            Size = "5",
                            Items = new List<CardElement>()
                            {
                                new Image()

                                {
                                    Url = "",
                                    Size = ImageSize.Auto,
                                    Style = ImageStyle.Normal,
                                    HorizontalAlignment = HorizontalAlignment.Right
                                }
                            }
                        },
                         new Column()
                        {
                            Size = "10",
                            Items = new List<CardElement>()
                            {
                                new Image()

                                {
                                    Url = "https://edelmangavelbot.azurewebsites.net/Images/ic_alert.png",
                                    Size = ImageSize.Auto,
                                    Style = ImageStyle.Normal,
                                    HorizontalAlignment = HorizontalAlignment.Right
                                }
                            }
                         }, new Column()
                        {
                            Size = "30",
                            Items = new List<CardElement>()
                            {
                                new TextBlock()
                                {
                                    Text =  "iView",
                                    HorizontalAlignment = HorizontalAlignment.Left,
                                    Color = TextColor.Light,
                                    Wrap = false
                                }
                            }
                        },
                         new Column()
                        {
                            Size = "5",
                            Items = new List<CardElement>()
                            {
                                new Image()

                                {
                                    Url = "",
                                    Size = ImageSize.Auto,
                                    Style = ImageStyle.Normal,
                                    HorizontalAlignment = HorizontalAlignment.Right
                                }
                            }
                        },
                          new Column()
                        {
                            Size = "10",
                            Items = new List<CardElement>()
                            {
                                new Image()

                                {
                                    Url = "https://edelmangavelbot.azurewebsites.net/Images/ic_healthy.png",
                                    Size = ImageSize.Auto,
                                    Style = ImageStyle.Normal,
                                    HorizontalAlignment = HorizontalAlignment.Right
                                }
                            }
                          },
                          new Column()
                        {
                            Size = "30",
                            Items = new List<CardElement>()
                            {
                                new TextBlock()
                                {
                                    Text =  "WEBFORT",
                                    HorizontalAlignment = HorizontalAlignment.Left,
                                    Color = TextColor.Light,
                                    Wrap = false
                                }
                            }
                        }
                        }
                    },
               // //Third Columnset
               // new ColumnSet()
               // {
               //     Columns = new List<Column>()
               //     {
               //         new Column()
               //         {
               //             Size = "10",
               //             Items = new List<CardElement>()
               //             {
               //                 new Image()

               //                 {
               //                     Url = "https://edelmangavelbot.azurewebsites.net/Images/ic_risk.png",
               //                     Size = ImageSize.Auto,
               //                     Style = ImageStyle.Normal,
               //                     HorizontalAlignment = HorizontalAlignment.Right
               //                 }
               //             }
               //         },
               //          new Column()
               //         {
               //             Size = "30",
               //             Items = new List<CardElement>()
               //             {
               //                 new TextBlock()
               //                 {
               //                     Text =  "IMPS",
               //                     HorizontalAlignment = HorizontalAlignment.Left,
               //                     Color = TextColor.Light,
               //                     Wrap = false
               //                 }
               //             }
               //         },
               //          new Column()
               //         {
               //             Size = "5",
               //             Items = new List<CardElement>()
               //             {
               //                 new Image()

               //                 {
               //                     Url = "",
               //                     Size = ImageSize.Auto,
               //                     Style = ImageStyle.Normal,
               //                     HorizontalAlignment = HorizontalAlignment.Right
               //                 }
               //             }
               //         },
               //          new Column()
               //         {
               //             Size = "10",
               //             Items = new List<CardElement>()
               //             {
               //                 new Image()

               //                 {
               //                     Url = "https://edelmangavelbot.azurewebsites.net/Images/ic_alert.png",
               //                     Size = ImageSize.Auto,
               //                     Style = ImageStyle.Normal,
               //                     HorizontalAlignment = HorizontalAlignment.Right
               //                 }
               //             }
               //          }, new Column()
               //         {
               //             Size = "30",
               //             Items = new List<CardElement>()
               //             {
               //                 new TextBlock()
               //                 {
               //                     Text =  "3DSecure",
               //                     HorizontalAlignment = HorizontalAlignment.Left,
               //                     Color = TextColor.Light,
               //                     Wrap = false
               //                 }
               //             }
               //         },
               //          new Column()
               //         {
               //             Size = "5",
               //             Items = new List<CardElement>()
               //             {
               //                 new Image()

               //                 {
               //                     Url = "",
               //                     Size = ImageSize.Auto,
               //                     Style = ImageStyle.Normal,
               //                     HorizontalAlignment = HorizontalAlignment.Right
               //                 }
               //             }
               //         },
               //           new Column()
               //         {
               //             Size = "10",
               //             Items = new List<CardElement>()
               //             {
               //                 new Image()

               //                 {
               //                     Url = "https://edelmangavelbot.azurewebsites.net/Images/ic_healthy.png",
               //                     Size = ImageSize.Auto,
               //                     Style = ImageStyle.Normal,
               //                     HorizontalAlignment = HorizontalAlignment.Right
               //                 }
               //             }
               //           },
               //           new Column()
               //         {
               //             Size = "30",
               //             Items = new List<CardElement>()
               //             {
               //                 new TextBlock()
               //                 {
               //                     Text =  "iCore",
               //                     HorizontalAlignment = HorizontalAlignment.Left,
               //                     Color = TextColor.Light,
               //                     Wrap = false
               //                 }
               //             }
               //         }
               //         }
               //     },
               // //Fourth Columnset
               // new ColumnSet()
               // {
               //     Columns = new List<Column>()
               //     {
               //         new Column()
               //         {
               //             Size = "10",
               //             Items = new List<CardElement>()
               //             {
               //                 new Image()

               //                 {
               //                     Url = "https://edelmangavelbot.azurewebsites.net/Images/ic_risk.png",
               //                     Size = ImageSize.Auto,
               //                     Style = ImageStyle.Normal,
               //                     HorizontalAlignment = HorizontalAlignment.Right
               //                 }
               //             }
               //         },
               //          new Column()
               //         {
               //             Size = "30",
               //             Items = new List<CardElement>()
               //             {
               //                 new TextBlock()
               //                 {
               //                     Text =  "DBP",
               //                     HorizontalAlignment = HorizontalAlignment.Left,
               //                     Color = TextColor.Light,
               //                     Wrap = false
               //                 }
               //             }
               //         },
               //          new Column()
               //         {
               //             Size = "5",
               //             Items = new List<CardElement>()
               //             {
               //                 new Image()

               //                 {
               //                     Url = "",
               //                     Size = ImageSize.Auto,
               //                     Style = ImageStyle.Normal,
               //                     HorizontalAlignment = HorizontalAlignment.Right
               //                 }
               //             }
               //         },
               //          new Column()
               //         {
               //             Size = "10",
               //             Items = new List<CardElement>()
               //             {
               //                 new Image()

               //                 {
               //                     Url = "https://edelmangavelbot.azurewebsites.net/Images/ic_alert.png",
               //                     Size = ImageSize.Auto,
               //                     Style = ImageStyle.Normal,
               //                     HorizontalAlignment = HorizontalAlignment.Right
               //                 }
               //             }
               //          }, new Column()
               //         {
               //             Size = "30",
               //             Items = new List<CardElement>()
               //             {
               //                 new TextBlock()
               //                 {
               //                     Text =  "iLoans",
               //                     HorizontalAlignment = HorizontalAlignment.Left,
               //                     Color = TextColor.Light,
               //                     Wrap = false
               //                 }
               //             }
               //         },
               //          new Column()
               //         {
               //             Size = "5",
               //             Items = new List<CardElement>()
               //             {
               //                 new Image()

               //                 {
               //                     Url = "",
               //                     Size = ImageSize.Auto,
               //                     Style = ImageStyle.Normal,
               //                     HorizontalAlignment = HorizontalAlignment.Right
               //                 }
               //             }
               //         },
               //           new Column()
               //         {
               //             Size = "10",
               //             Items = new List<CardElement>()
               //             {
               //                 new Image()

               //                 {
               //                     Url = "https://edelmangavelbot.azurewebsites.net/Images/ic_healthy.png",
               //                     Size = ImageSize.Auto,
               //                     Style = ImageStyle.Normal,
               //                     HorizontalAlignment = HorizontalAlignment.Right
               //                 }
               //             }
               //           },
               //           new Column()
               //         {
               //             Size = "30",
               //             Items = new List<CardElement>()
               //             {
               //                 new TextBlock()
               //                 {
               //                     Text =  "iMobile",
               //                     HorizontalAlignment = HorizontalAlignment.Left,
               //                     Color = TextColor.Light,
               //                     Wrap = false
               //                 }
               //             }
               //         }
               //         }
               //     },
               // // Empty ColumnSet
               // new ColumnSet()
               //{
               //     Columns = new List<Column>()
               //     {
               //         new Column()
               //         {
               //             Size = ColumnSize.Stretch,
               //             Items = new List<CardElement>()
               //             {
               //                 new Image()

               //                 {
               //                     Url = " ",
               //                     Size = ImageSize.Auto,
               //                     Style = ImageStyle.Normal,
               //                     HorizontalAlignment = HorizontalAlignment.Right
               //                 }
               //             }
               //         },
               //         new Column()
               //         {
               //             Size = ColumnSize.Stretch,
               //             Items = new List<CardElement>()
               //             {
               //                 new TextBlock()

               //                 {
               //                   Text = "",
               //                     Size = TextSize.Normal,
               //                     Weight = TextWeight.Normal,
               //                     HorizontalAlignment = HorizontalAlignment.Right
               //                 }
               //             }
               //         }
               //     }
               //  },
               // new ColumnSet()
               // {
               //     Columns = new List<Column>()
               //     {
               //         new Column()
               //         {
               //             Size = ColumnSize.Stretch,
               //             Items = new List<CardElement>()
               //             {
               //                 new Image()

               //                 {
               //                     Url = " ",
               //                     Size = ImageSize.Auto,
               //                     Style = ImageStyle.Normal,
               //                     HorizontalAlignment = HorizontalAlignment.Right
               //                 }
               //             }
               //         },
               //         new Column()
               //         {
               //             Size = ColumnSize.Stretch,
               //             Items = new List<CardElement>()
               //             {
               //                 new TextBlock()

               //                 {
               //                   Text = "",
               //                    Size = TextSize.Normal,
               //                     Weight = TextWeight.Normal,
               //                     HorizontalAlignment = HorizontalAlignment.Right
               //                 }
               //             }
               //         }
               //     }
               //  },
               // new ColumnSet()
               // {
               //     Columns = new List<Column>()
               //     {
               //         new Column()
               //         {
               //             Size = ColumnSize.Stretch,
               //             Items = new List<CardElement>()
               //             {
               //                 new Image()

               //                 {
               //                     Url = " ",
               //                     Size = ImageSize.Auto,
               //                     Style = ImageStyle.Normal,
               //                     HorizontalAlignment = HorizontalAlignment.Right
               //                 }
               //             }
               //         },
               //         new Column()
               //         {
               //             Size = ColumnSize.Stretch,
               //             Items = new List<CardElement>()
               //             {
               //                 new TextBlock()

               //                 {
               //                   Text = "",
               //                     Size = TextSize.Normal,
               //                     Weight = TextWeight.Normal,
               //                     HorizontalAlignment = HorizontalAlignment.Right
               //                 }
               //             }
               //         }
               //     }
               //  }
            }
                 }
                }
            };
            // Create the attachment.

            Attachment attachment = new Attachment()
            {
                ContentType = AdaptiveCard.ContentType,
                Content = Adaptcard
            };
            return attachment;
        }

        #endregion

    }
}