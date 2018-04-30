using System;
using System.Configuration;
using System.Threading.Tasks;

using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Luis;
using Microsoft.Bot.Builder.Luis.Models;
using RestSharp;
using Newtonsoft.Json.Linq;
using AdaptiveCards;
using System.Collections.Generic;
using Microsoft.Bot.Connector;

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
            var attachment = GetAdaptiveCard();
            var message = context.MakeMessage();
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
            var attachment = HealthCardApplication();
            var message = context.MakeMessage();
            message.Attachments.Add(attachment);
            await context.PostAsync(message);
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

            #region DB server Cards
            AdaptiveCard DbServerCard = new AdaptiveCard()
            {
                BackgroundImage = "https://edelmangavelbot.azurewebsites.net/Images/ic_background_05.png",
                Title = "Health of db servers",
                Speak = "<s>Health of db servers.</s>",
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

                        },

                    }
                }
                } },
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

                        },

                    }
                },
                new ColumnSet()
                {
                    Columns = new List<Column>()
                    {
                        new Column()
                        {
                            Size = ColumnSize.Stretch,

                        },
                        new Column()
                        {
                            Size = ColumnSize.Stretch,

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

                        },
                        new Column()
                        {
                            Size = ColumnSize.Stretch,

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
                                    Url = "https://edelmangavelbot.azurewebsites.net/Images/ic_server.png",
                                    Size = ImageSize.Auto,
                                    Style = ImageStyle.Normal,
                                    HorizontalAlignment = HorizontalAlignment.Center,
                                    }
                                }
                            },
                        new Column()
                            {
                                Size = "20",
                                Style = ContainerStyle.Normal,

                                Items = new List<CardElement>()
                                {
                                   
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
                                    Text = "Server",
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
                                Size = "20",
                                 Style = ContainerStyle.Normal,
                                 Separation = SeparationStyle.Strong,
                                Items = new List<CardElement>()
                                {

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
                                    Text = "Healthy",
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
                                   
                                new TextBlock()
                                    {
                                    Text =  "21",
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

                        },
                        new Column()
                        {
                            Size = ColumnSize.Stretch,

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

                        },
                        new Column()
                        {
                            Size = ColumnSize.Stretch,

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

                        },
                        new Column()
                        {
                            Size = ColumnSize.Stretch,

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

                        },
                        new Column()
                        {
                            Size = ColumnSize.Stretch,

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

                        },
                        new Column()
                        {
                            Size = ColumnSize.Stretch,

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

                        },
                        new Column()
                        {
                            Size = ColumnSize.Stretch,

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
                                    Url = "https://edelmangavelbot.azurewebsites.net/Images/ic_alert.png",
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
                                    Text =  "IMPS",
                                    HorizontalAlignment = HorizontalAlignment.Left,
                                    Color = TextColor.Light,
                                    Wrap = false
                                }
                            }
                        },
                         new Column()
                        {
                            Size = "5"

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
                                    Text =  "iCore India",
                                    HorizontalAlignment = HorizontalAlignment.Left,
                                    Color = TextColor.Light,
                                    Wrap = false
                                }
                            }
                        },
                         new Column()
                        {
                            Size = "5"

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
                          },
                          new Column()
                        {
                            Size = "30",
                            Items = new List<CardElement>()
                            {
                                new TextBlock()
                                {
                                    Text =  "iCards Online 4",
                                    HorizontalAlignment = HorizontalAlignment.Left,
                                    Color = TextColor.Light,
                                    Wrap = true
                                }
                            }
                        }
                        }
                    },
                //Third Columnset
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
                                    Url = "https://edelmangavelbot.azurewebsites.net/Images/ic_alert.png",
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
                            Size = "5"

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
                            Size = "5"

                        },
                          new Column()
                        {
                            Size = "10",
                          },
                          new Column()
                        {
                            Size = "30",
                        }
                        }
                    },
             

                // Empty ColumnSet
                new ColumnSet()
               {
                    Columns = new List<Column>()
                    {
                        new Column()
                        {
                            Size = ColumnSize.Stretch,

                        },
                        new Column()
                        {
                            Size = ColumnSize.Stretch,

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

                        },
                        new Column()
                        {
                            Size = ColumnSize.Stretch,

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

                        },
                        new Column()
                        {
                            Size = ColumnSize.Stretch,

                        }
                    }
                 }
            }
                 }
                }
            };
            // Create the attachment.

            #endregion
            Attachment attachment = new Attachment()
            {
                ContentType = AdaptiveCard.ContentType,
                Content = DbServerCard
            };
            var reply = context.MakeMessage();
            reply.Attachments.Add(attachment);

            await context.PostAsync(reply);
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
                Title = "Riskfort Health Status",
                Speak = "<s>Riskfort Health Status</s>",
                Body = new List<CardElement>()
              {
                  new Container()
                    {
                      Speak="Container",
                 Items = new List<CardElement>()
                    {
                     // Empty Column Sets
                      new ColumnSet()
                {
                    Columns = new List<Column>()
                    {
                        new Column()
                        {
                            Size = ColumnSize.Stretch,

                        },

                    }
                },
                new ColumnSet()
                {
                    Columns = new List<Column>()
                    {
                        new Column()
                        {
                            Size = ColumnSize.Stretch,

                        },
                        new Column()
                        {
                            Size = ColumnSize.Stretch,

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

                        },
                        new Column()
                        {
                            Size = ColumnSize.Stretch,

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

                        },
                        new Column()
                        {
                            Size = ColumnSize.Stretch,

                        }
                    }
                },
                    // First Column
                   new ColumnSet()
                    {
                    Columns = new List<Column>()
                    {
                             new Column()
                                    {
                                        Items = new List<CardElement>()
                                        {
                                            new Image()
                                            {
                                                Url = "https://edelmangavelbot.azurewebsites.net/Images/ic_healthy.png",
                                                Size = ImageSize.Small,
                                                HorizontalAlignment=HorizontalAlignment.Right
                                            }
                                        }
                                    },
                                 new Column()
                                    {
                                        Items = new List<CardElement>()
                                        {
                                             new TextBlock()
                                            {
                                                Text =  "Riskfort",
                                                Weight = TextWeight.Normal,
                                                HorizontalAlignment=HorizontalAlignment.Left,
                                                Size=TextSize.ExtraLarge,
                                                Wrap = true,
                                                Color=TextColor.Light,
                                            }
                                        }
                                    }
                    }
                    },
                   //Empty Columnset
                    new ColumnSet()
                    {
                    Columns = new List<Column>()
                    {
                                new Column()
                                    {
                                        Size =ColumnSize.Stretch,
                                    },
                    }
                    },                    
                    // Second Column
                    new ColumnSet()
                    {
                    Columns = new List<Column>()
                    {
                                    new Column()
                                    {
                                         Size ="30",
                                        Items = new List<CardElement>()
                                        {
                                            new Image()
                                            {
                                                Url = "https://edelmangavelbot.azurewebsites.net/Images/ic_single_app.png",
                                                Size = ImageSize.Auto,
                                            }
                                        }
                                    },
                                     new Column()
                                    {
                                         Size="10",
                                        Items = new List<CardElement>()
                                        {
                                             new Image()
                                            {
                                                Url = "https://edelmangavelbot.azurewebsites.net/Images/ic_alert_large.png",
                                                Size = ImageSize.Medium,
                                                HorizontalAlignment=HorizontalAlignment.Right
                                            },
                                        }
                                     },
                                    new Column()
                                    {
                                        Size="25",
                                        Items = new List<CardElement>()
                                        {
                                            new TextBlock()
                                            {
                                                Text =  "20 Alerts",
                                                Weight = TextWeight.Normal,
                                                HorizontalAlignment=HorizontalAlignment.Left,
                                                Size=TextSize.Normal,
                                                Wrap = true,
                                                Color=TextColor.Light,
                                            },
                                        }
                                    },
                                    new Column()
                                    {
                                        Separation=SeparationStyle.Strong,
                                        Size="10",
                                        Items= new List<CardElement>()
                                        {
                                            new Image()
                                            {
                                                Url = "https://edelmangavelbot.azurewebsites.net/Images/ic_correlation.png",
                                                Size = ImageSize.Medium,
                                                HorizontalAlignment=HorizontalAlignment.Right
                                            }
                                        }
                                    },
                                    new Column()
                                    {
                                        Size="25",
                                        Items= new List<CardElement>()
                                        {
                                             new TextBlock()
                                            {
                                                Text = "12 Correlation",
                                                Weight = TextWeight.Normal,
                                                HorizontalAlignment=HorizontalAlignment.Left,
                                                Size=TextSize.Normal,
                                                Wrap = true,
                                                Color=TextColor.Light
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
                                        Size =ColumnSize.Stretch,

                                    },

                    }
                    },
                    new ColumnSet()
                    {
                    Columns = new List<Column>()
                    {
                                new Column()
                                    {
                                       Size =ColumnSize.Stretch,

                                    },

                    }
                    },
                    new ColumnSet()
                    {
                    Columns = new List<Column>()
                    {
                                new Column()
                                    {
                                        Size =ColumnSize.Stretch,

                                    },

                    }
                    },
                    new ColumnSet()
                    {

                    Columns = new List<Column>()
                    {
                                new Column()
                                    {
                                        Size =ColumnSize.Stretch,

                                    },

                    }
                    },
                    new ColumnSet()
                    {
                    Columns = new List<Column>()
                    {
                                new Column()
                                    {
                                        Size =ColumnSize.Stretch,
                                    },
                    }
                    },
                 // third column
                 
                    new ColumnSet()
                    {
                    Columns = new List<Column>()
                    {

                             new Column()
                                    {
                                 Size="20",
                                        Items = new List<CardElement>()
                                        {
                                                new Image()
                                            {
                                                Url = "https://edelmangavelbot.azurewebsites.net/Images/ic_apm_healthy.png",
                                                Size = ImageSize.Medium,
                                                Style = ImageStyle.Normal,
                                                HorizontalAlignment = HorizontalAlignment.Center
                                            },
                                            new TextBlock()
                                            {
                                                Type="TextBlock",
                                                Text = "APM",
                                                Weight = TextWeight.Normal,
                                                Size=TextSize.Normal,
                                                Wrap = true,
                                                Color=TextColor.Light,
                                                HorizontalAlignment = HorizontalAlignment.Center
                                            }

                                        }
                                    },
                             //new Column()
                             //{
                             //    Separation=SeparationStyle.Strong,
                             //},
                             new Column()
                                    {
                                 Size="20",
                                  Separation=SeparationStyle.Strong,
                                        Items = new List<CardElement>()
                                        {
                                                new Image()
                                            {
                                                Url = "https://edelmangavelbot.azurewebsites.net/Images/ic_server_up.png",
                                                Size = ImageSize.Medium,
                                                Style = ImageStyle.Normal,
                                                HorizontalAlignment = HorizontalAlignment.Center

                                            },
                                            new TextBlock()
                                            {
                                                Type="TextBlock",
                                                Text = "Server",
                                                Weight = TextWeight.Normal,
                                                Size=TextSize.Normal,
                                                Color=TextColor.Light,
                                                Wrap = true,
                                                HorizontalAlignment = HorizontalAlignment.Center
                                            }

                                        }
                                    },
                             //new Column()
                             //{
                             //     Separation=SeparationStyle.Strong,
                             //},
                             new Column()
                                    {
                                 Size="20",
                                  Separation=SeparationStyle.Strong,
                                        Items = new List<CardElement>()
                                        {
                                                new Image()
                                            {
                                                Url = "https://edelmangavelbot.azurewebsites.net/Images/ic_database_down.png",
                                                Size = ImageSize.Medium,
                                                Style = ImageStyle.Normal,
                                                 HorizontalAlignment = HorizontalAlignment.Center

                                            },
                                            new TextBlock()
                                            {
                                                Type="TextBlock",
                                                Text = "Database",
                                                Weight = TextWeight.Normal,
                                                Size=TextSize.Normal,
                                                Color=TextColor.Light,
                                                Wrap = true,
                                                 HorizontalAlignment = HorizontalAlignment.Center
                                            }

                                        }
                                    },
                             //  new Column()
                             //{
                             //     Separation=SeparationStyle.Strong,
                             //},
                             new Column()
                                    {
                                 Size="20",
                                  Separation=SeparationStyle.Strong,
                                        Items = new List<CardElement>()
                                        {
                                                new Image()
                                            {
                                                Url = "https://edelmangavelbot.azurewebsites.net/Images/ic_network_up.png",
                                                Size = ImageSize.Medium,
                                                Style = ImageStyle.Normal,
                                                 HorizontalAlignment = HorizontalAlignment.Center
                                            },
                                            new TextBlock()
                                            {
                                                Type="TextBlock",
                                                Text = "Network",
                                                Weight = TextWeight.Normal,
                                                Size=TextSize.Normal,
                                                Wrap = true,
                                                Color=TextColor.Light,
                                                 HorizontalAlignment = HorizontalAlignment.Center
                                            }

                                        }
                                    },
                             //new Column()
                             //{
                             //     Separation=SeparationStyle.Strong,
                             //},
                             new Column()
                                    {
                                 Size="20",
                                  Separation=SeparationStyle.Strong,
                                        Items = new List<CardElement>()
                                        {
                                                new Image()
                                            {
                                                Url = "https://edelmangavelbot.azurewebsites.net/Images/ic_storage_down.png",
                                                Size = ImageSize.Medium,
                                                Style = ImageStyle.Normal,
                                                HorizontalAlignment = HorizontalAlignment.Center
                                            },
                                            new TextBlock()
                                            {
                                                Type="TextBlock",
                                                Text = "Storage",
                                                Weight = TextWeight.Normal,
                                                Size=TextSize.Normal,
                                                Color=TextColor.Light,
                                                Wrap = true,
                                                HorizontalAlignment = HorizontalAlignment.Center
                                            }

                                        }
                                    },

                            }

                         },
                    new ColumnSet()
                    {
                    Columns = new List<Column>()
                    {
                                new Column()
                                    {
                                       Size =ColumnSize.Stretch,

                                    },

                    }
                    },
                    new ColumnSet()
                    {
                    Columns = new List<Column>()
                                {
                                new Column()
                                    {
                                       Size =ColumnSize.Stretch,

                                    },

                    }
                    },
                     new ColumnSet()
                    {
                    Columns = new List<Column>()
                                {
                                new Column()
                                    {
                                       Size =ColumnSize.Stretch,

                                    },

                    }
                    },
                      new ColumnSet()
                    {
                    Columns = new List<Column>()
                                {
                                new Column()
                                    {
                                       Size =ColumnSize.Stretch,

                                    },

                    }
                    },
                    //   new ColumnSet()
                    //{
                    //Columns = new List<Column>()
                    //            {
                    //            new Column()
                    //                {
                    //                   Size =ColumnSize.Stretch,

                    //                },

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
                Speak = "<s>Over All Applciation Health.</s>",
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

                        },

                    }
                }
                } },
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

                        },

                    }
                },
                new ColumnSet()
                {
                    Columns = new List<Column>()
                    {
                        new Column()
                        {
                            Size = ColumnSize.Stretch,

                        },
                        new Column()
                        {
                            Size = ColumnSize.Stretch,

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

                        },
                        new Column()
                        {
                            Size = ColumnSize.Stretch,

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
                                    Size = ImageSize.Auto,
                                    Style = ImageStyle.Normal,
                                    HorizontalAlignment = HorizontalAlignment.Center,
                                   
                                    }
                                }
                            },
                        new Column()
                            {
                                Size = "20",
                                Style = ContainerStyle.Normal,

                                Items = new List<CardElement>()
                                {
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
                                Size = "20",
                                 Style = ContainerStyle.Normal,
                                 Separation = SeparationStyle.Strong,
                                Items = new List<CardElement>()
                                {
                                 
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

                        },
                        new Column()
                        {
                            Size = ColumnSize.Stretch,

                        }
                    }
                 }
                ,
                //new ColumnSet()
                //{
                //    Columns = new List<Column>()
                //    {
                //        new Column()
                //        {
                //            Size = ColumnSize.Stretch,

                //        },
                //        new Column()
                //        {
                //            Size = ColumnSize.Stretch,

                //        }
                //    }
                // },
                new ColumnSet()
                {
                    Columns = new List<Column>()
                    {
                        new Column()
                        {
                            Size = ColumnSize.Stretch,

                        },
                        new Column()
                        {
                            Size = ColumnSize.Stretch,

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

                        },
                        new Column()
                        {
                            Size = ColumnSize.Stretch,

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
                            Size = "5"

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
                            Size = "5"

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
                //Third Columnset
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
                                    Text =  "IMPS",
                                    HorizontalAlignment = HorizontalAlignment.Left,
                                    Color = TextColor.Light,
                                    Wrap = false
                                }
                            }
                        },
                         new Column()
                        {
                            Size = "5"

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
                                    Text =  "3DSecure",
                                    HorizontalAlignment = HorizontalAlignment.Left,
                                    Color = TextColor.Light,
                                    Wrap = false
                                }
                            }
                        },
                         new Column()
                        {
                            Size = "5"

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
                                    Text =  "iCore",
                                    HorizontalAlignment = HorizontalAlignment.Left,
                                    Color = TextColor.Light,
                                    Wrap = false
                                }
                            }
                        }
                        }
                    },
                //Fourth Columnset
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
                                    Text =  "DBP",
                                    HorizontalAlignment = HorizontalAlignment.Left,
                                    Color = TextColor.Light,
                                    Wrap = false
                                }
                            }
                        },
                         new Column()
                        {
                            Size = "5"

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
                                    Text =  "iLoans",
                                    HorizontalAlignment = HorizontalAlignment.Left,
                                    Color = TextColor.Light,
                                    Wrap = false
                                }
                            }
                        },
                         new Column()
                        {
                            Size = "5"

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
                                    Text =  "iMobile",
                                    HorizontalAlignment = HorizontalAlignment.Left,
                                    Color = TextColor.Light,
                                    Wrap = false
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
                            Size = "10",
                            Items = new List<CardElement>()
                            {
                                new AdaptiveCards.Image()

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
                                    Text =  "DBBank",
                                    HorizontalAlignment = HorizontalAlignment.Left,
                                    Color = TextColor.Light,
                                    Wrap = false
                                }
                            }
                        },
                         new Column()
                        {
                            Size = "5",

                        },
                         new Column()
                        {
                            Size = "10",
                            Items = new List<CardElement>()
                            {
                                new AdaptiveCards.Image()

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
                                    Text =  "IAI",
                                    HorizontalAlignment = HorizontalAlignment.Left,
                                    Color = TextColor.Light,
                                    Wrap = false
                                }
                            }
                        },
                         new Column()
                        {
                            Size = "5",

                        },
                          new Column()
                        {
                            Size = "10",
                            Items = new List<CardElement>()
                            {
                                new AdaptiveCards.Image()

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
                                    Text =  "EAI",
                                    HorizontalAlignment = HorizontalAlignment.Left,
                                    Color = TextColor.Light,
                                    Wrap = false
                                }
                            }
                        }
                        }
                    },
                //Sixth Columnset
                new ColumnSet()
                {
                    Columns = new List<Column>()
                    {
                        new Column()
                        {
                            Size = "10",
                            Items = new List<CardElement>()
                            {
                                new AdaptiveCards.Image()

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
                                    Text =  "iCore India",
                                    HorizontalAlignment = HorizontalAlignment.Left,
                                    Color = TextColor.Light,
                                    Wrap = false
                                }
                            }
                        },
                         new Column()
                        {
                            Size = "5",

                        },
                         new Column()
                        {
                            Size = "10",
                            Items = new List<CardElement>()
                            {
                                new AdaptiveCards.Image()

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
                                    Text =  "SMS Gateway",
                                    HorizontalAlignment = HorizontalAlignment.Left,
                                    Color = TextColor.Light,
                                    Wrap = false
                                }
                            }
                        },
                         new Column()
                        {
                            Size = "5",

                        },
                          new Column()
                        {
                            Size = "10",
                            Items = new List<CardElement>()
                            {
                                new AdaptiveCards.Image()

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
                                    Text =  "iCards Online",
                                    HorizontalAlignment = HorizontalAlignment.Left,
                                    Color = TextColor.Light,
                                    Wrap = false
                                }
                            }
                        }
                        }
                    },


                // Empty ColumnSet
                new ColumnSet()
               {
                    Columns = new List<Column>()
                    {
                        new Column()
                        {
                            Size = ColumnSize.Stretch,

                        },
                        new Column()
                        {
                            Size = ColumnSize.Stretch,

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

                        },
                        new Column()
                        {
                            Size = ColumnSize.Stretch,

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

                        },
                        new Column()
                        {
                            Size = ColumnSize.Stretch,

                        }
                    }
                 }
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


        private Attachment HealthCardApplication()
        {
            var Adaptcard = new AdaptiveCard
            {
                Type = "AdaptiveCard",
                Version = "1.0",
                FallbackText = "",
                MinVersion = "0.5",
                BackgroundImage = "https://edelmangavelbot.azurewebsites.net/Images/ic_background_05.png",
                Title = "Over All Applciation Health",
                Speak = "Over All Applciation Health.",
                Body = new List<CardElement>()
                {
                //Empty Columnset
                new ColumnSet()
        {
            Columns = new List<Column>()
                    {
                        new Column()
                        {
                            Size = ColumnSize.Stretch,
                            
                        },
                        new Column()
                        {
                            Size = ColumnSize.Stretch,
                           
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
                            
                        },
                        new Column()
                        {
                            Size = ColumnSize.Stretch,
                            
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
                           
                        },
                        new Column()
                        {
                            Size = ColumnSize.Stretch,
                          
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
                            
                        },
                        new Column()
                        {
                            Size = ColumnSize.Stretch,
                            
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
                            Size ="30",
                            Items = new List<CardElement>()
                                {
                                new AdaptiveCards.Image()
                                    {
                                    Url = "https://edelmangavelbot.azurewebsites.net/Images/ic_server.png",
                                    Size = ImageSize.Auto,
                                    Style = ImageStyle.Normal,
                                    HorizontalAlignment = HorizontalAlignment.Center
                                    },
                                      }
                                },
                                new Column()
                                {
                                    Size="15",
                                Items = new List<CardElement>()
                                {
                                            new AdaptiveCards.Image()
                                    {
                                    Url = "https://edelmangavelbot.azurewebsites.net/Images/ic_healthy.png",
                                    Size = ImageSize.Small,
                                    Style = ImageStyle.Normal,
                                    HorizontalAlignment = HorizontalAlignment.Right
                                    },
                                 new AdaptiveCards.Image()
                                    {
                                    Url = "https://edelmangavelbot.azurewebsites.net/Images/ic_storage.png",
                                   Size = ImageSize.Medium,
                                    Style = ImageStyle.Normal,
                                    HorizontalAlignment = HorizontalAlignment.Right
                                    },

                                        }
                                    },
                                    new Column()
                                    {
                                        Size = "60",
                                        Items = new List<CardElement>()
                                        {
                                            new TextBlock()
                                {
                                    Text ="Riskfort",
                                    Size = TextSize.ExtraLarge,
                                    Color = TextColor.Light,
                                    HorizontalAlignment = HorizontalAlignment.Left,
                                },
                                             new TextBlock()
                                {
                                    Text ="Transactions",
                                    Size = TextSize.ExtraLarge,
                                    Color = TextColor.Light,
                                    HorizontalAlignment = HorizontalAlignment.Left,
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
                            
                        },
                        new Column()
                        {
                            Size = ColumnSize.Stretch,
                           
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
                           
                        },
                        new Column()
                        {
                            Size = ColumnSize.Stretch,
                           
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
                           
                        },
                        new Column()
                        {
                            Size = ColumnSize.Stretch,
                           
                        }
                    }
                },
                //Second Column
                new ColumnSet()
        {
            Columns = new List<Column>()
                        {
                    new Column()
                    {
                        Items = new List<CardElement>()
                        {
                                     new AdaptiveCards.Image()
                                    {
                                    Url = "https://edelmangavelbot.azurewebsites.net/Images/ic_healthy.png",
                                    Size = ImageSize.Small,
                                    Style = ImageStyle.Normal,
                                    HorizontalAlignment = HorizontalAlignment.Right
                                    },
                                 new AdaptiveCards.Image()
                                    {
                                    Url = "https://edelmangavelbot.azurewebsites.net/Images/ic_risk.png",
                                    Size = ImageSize.Medium,
                                    Style = ImageStyle.Normal,
                                    HorizontalAlignment = HorizontalAlignment.Right
                                    },
                                   new AdaptiveCards.Image()
                                    {
                                    Url = "https://edelmangavelbot.azurewebsites.net/Images/ic_healthy.png",
                                    Size = ImageSize.Small,
                                    Style = ImageStyle.Normal,
                                    HorizontalAlignment = HorizontalAlignment.Right
                                    },
                                 new AdaptiveCards.Image()
                                    {
                                    Url = "https://edelmangavelbot.azurewebsites.net/Images/ic_healthy.png",
                                    Size = ImageSize.Medium,
                                    Style = ImageStyle.Normal,
                                    HorizontalAlignment = HorizontalAlignment.Right
                                    },
                                   new AdaptiveCards.Image()
                                    {
                                    Url = "https://edelmangavelbot.azurewebsites.net/Images/ic_healthy.png",
                                    Size = ImageSize.Medium,
                                    Style = ImageStyle.Normal,
                                    HorizontalAlignment = HorizontalAlignment.Right
                                    },
                        }
                    },
                     new Column()
                    {
                        Items = new List<CardElement>()
                        {
                                     new TextBlock()
                                    {
                                    Text = "HYD3DSAPP04",
                                    Size = TextSize.Large,
                                    HorizontalAlignment = HorizontalAlignment.Left,
                                    Color = TextColor.Light
                                    },
                                  new TextBlock()
                                    {
                                    Text = "hydpgweb053|X",
                                    Size = TextSize.Large,
                                    HorizontalAlignment = HorizontalAlignment.Left,
                                    Color = TextColor.Light
                                    },
                                     new TextBlock()
                                    {
                                    Text = "hyd3dsweb05|_Total",
                                    Size = TextSize.Large,
                                    HorizontalAlignment = HorizontalAlignment.Left,
                                    Color = TextColor.Light
                                    },
                                    new TextBlock()
                                    {
                                    Text = "HYD3DSAPP04",
                                    Size = TextSize.Large,
                                    HorizontalAlignment = HorizontalAlignment.Left,
                                    Color = TextColor.Light
                                    },
                                      new TextBlock()
                                    {
                                    Text = "hydpgweb053|X",
                                    Size = TextSize.Large,
                                    HorizontalAlignment = HorizontalAlignment.Left,
                                    Color = TextColor.Light
                                    },
                        }
                    },
                     new Column()
                    {
                         Separation =SeparationStyle.Strong,
                        Items = new List<CardElement>()
                        {
                                     new AdaptiveCards.Image()
                                    {
                                    Url = "https://edelmangavelbot.azurewebsites.net/Images/ic_healthy.png",
                                    Size = ImageSize.Small,
                                    Style = ImageStyle.Normal,
                                    HorizontalAlignment = HorizontalAlignment.Right
                                    },
                                 new AdaptiveCards.Image()
                                    {
                                    Url = "https://edelmangavelbot.azurewebsites.net/Images/ic_risk.png",
                                    Size = ImageSize.Medium,
                                    Style = ImageStyle.Normal,
                                    HorizontalAlignment = HorizontalAlignment.Right
                                    },
                                   new AdaptiveCards.Image()
                                    {
                                    Url = "https://edelmangavelbot.azurewebsites.net/Images/ic_healthy.png",
                                    Size = ImageSize.Small,
                                    Style = ImageStyle.Normal,
                                    HorizontalAlignment = HorizontalAlignment.Right
                                    },
                                 new AdaptiveCards.Image()
                                    {
                                    Url = "https://edelmangavelbot.azurewebsites.net/Images/ic_risk.png",
                                    Size = ImageSize.Medium,
                                    Style = ImageStyle.Normal,
                                    HorizontalAlignment = HorizontalAlignment.Right
                                    },
                                   new AdaptiveCards.Image()
                                    {
                                    Url = "https://edelmangavelbot.azurewebsites.net/Images/ic_risk.png",
                                    Size = ImageSize.Medium,
                                    Style = ImageStyle.Normal,
                                    HorizontalAlignment = HorizontalAlignment.Right
                                    },
                        }
                    },
                        new Column()
                    {
                        Items = new List<CardElement>()
                        {
                                     new TextBlock()
                                    {
                                    Text = "HYD3DSAPP04",
                                    Size = TextSize.Large,
                                    HorizontalAlignment = HorizontalAlignment.Left,
                                    Color = TextColor.Light
                                    },
                                  new TextBlock()
                                    {
                                    Text = "hydpgweb053|X",
                                    Size = TextSize.Large,
                                    HorizontalAlignment = HorizontalAlignment.Left,
                                    Color = TextColor.Light
                                    },
                                     new TextBlock()
                                    {
                                    Text = "hyd3dsweb05|_Total",
                                    Size = TextSize.Large,
                                    HorizontalAlignment = HorizontalAlignment.Left,
                                    Color = TextColor.Light
                                    },
                                    new TextBlock()
                                    {
                                    Text = "HYD3DSAPP04",
                                    Size = TextSize.Large,
                                    HorizontalAlignment = HorizontalAlignment.Left,
                                    Color = TextColor.Light
                                    },
                                      new TextBlock()
                                    {
                                    Text = "hydpgweb053|X",
                                    Size = TextSize.Large,
                                    HorizontalAlignment = HorizontalAlignment.Left,
                                    Color = TextColor.Light
                                    },
                        }
                    },

                     }
                },
                new ColumnSet()
        {
            Columns = new List<Column>()
                    {
                        new Column()
                        {
                            Size = ColumnSize.Stretch,
                            
                        },
                        new Column()
                        {
                            Size = ColumnSize.Stretch,
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
                        },
                        new Column()
                        {
                            Size = ColumnSize.Stretch,
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
                        },
                        new Column()
                        {
                            Size = ColumnSize.Stretch,
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
                        },
                        new Column()
                        {
                            Size = ColumnSize.Stretch,                           
                        }
                    }
                 },
                    
                }
};
Attachment attachment = new Attachment()
{
    ContentType = AdaptiveCard.ContentType,
    Content = Adaptcard
};

            //replyToConversation.Attachments.Add(attachment);
            return attachment;

        }
    }
}