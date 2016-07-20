﻿using DiningLUISNS;
using Microsoft.Bot.Connector;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web.Http;
using Microsoft.Bot.Builder.Dialogs;


namespace msftbot
{
    [BotAuthentication]
    public class MessagesController : ApiController
    {
        public async Task<HttpResponseMessage> Post([FromBody]Activity activity)
        {
            if (activity.Type == "message")
            {
                // This is new to V3
                ConnectorClient connector = new ConnectorClient(new Uri(activity.ServiceUrl));

                #region LUIS
                string diningoption=String.Empty;
                Luis diLUIS = await GetEntityFromLUIS(activity.Text);
                
                if (diLUIS.intents.Count() > 0 /*&& diLUIS.entities.Count() > 0*/)
                {
                    switch (diLUIS.intents[0].intent)
                    {
                        case "list-all-cafe": //find-food is an intent from LUIS
                            diningoption = await GetAllCafes();
                            break;

                        case "find-food": //find-food is an intent from LUIS
                            diningoption = await GetCafeForItem(diLUIS.entities[0].entity);
                            break;

                        // change this back to GetMenu if test does not work out
                        case "find-menu": //find-food is an intent from LUIS
                            diningoption = await GetCafeMenu(diLUIS.entities[0].entity);
                            
                            //Implements echo bot when dining option fails
                            if (diningoption==null)
                                await Conversation.SendAsync(activity, () => new FindMenuDialog());
                            break;

                        default:
                            diningoption = "Sorry, I am not getting you...";
                            break;
                    }
                }
                else
                {
                    diningoption = "Sorry, I am not getting you...";
                }
                #endregion               
                
                Activity reply = activity.CreateReply(diningoption);
                if (diningoption != null)
                    await connector.Conversations.ReplyToActivityAsync(reply);
            }
            else
            {
                HandleSystemMessage(activity);
            }
            var response = Request.CreateResponse(HttpStatusCode.OK);
            return response;
        }


        [Serializable]
        public class FindMenuDialog : IDialog<object>
        {
            protected int count = 1;

            public async Task StartAsync(IDialogContext context)
            {

                context.Wait(MessageReceivedAsync);
            }

            public virtual async Task MessageReceivedAsync(IDialogContext context, IAwaitable<IMessageActivity> argument)
            {

                var message = await argument;

                if (message.Text == "reset")
                {

                    PromptDialog.Confirm(

                        context,

                        AfterResetAsync,

                        "Are you sure you want to reset the count?",

                        "Didn't get that!",

                        promptStyle: PromptStyle.None);

                }

                else
                {

                    await context.PostAsync(string.Format("Sorry, that dining location doesn't exist.", this.count++, message.Text));
                    context.Wait(MessageReceivedAsync);

                }



            }

            public async Task AfterResetAsync(IDialogContext context, IAwaitable<bool> argument)
            {
                var confirm = await argument;
                if (confirm)
                {
                    this.count = 1;
                    await context.PostAsync("Reset count.");
                }

                else
                {
                    await context.PostAsync("Did not reset count.");
                }
                
                context.Wait(MessageReceivedAsync);
                

            }
        }

        private async Task<string> GetAllCafes()
        {
            // Get authentication token from authentication.cs
            Authentication auth = new Authentication();
            string authtoken = await auth.GetAuthHeader();

            // Get JSON – List of all Cafes
            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authtoken);
            HttpResponseMessage response = await httpClient.GetAsync("https://msrefdiningint.azurewebsites.net/api/v1/cafes");
            response.EnsureSuccessStatusCode();
            string responseBody = await response.Content.ReadAsStringAsync();

            // Convert JSON to list
            List<Cafe> allCafeList = JsonConvert.DeserializeObject<List<Cafe>>(responseBody);

            // Format list
            string allcafes = string.Empty;
            foreach (var item in allCafeList)
            {
                allcafes += "[" + item.CafeName + "](https://microsoft.sharepoint.com/sites/refweb/Pages/Dining-Menus.aspx?cafe=" + item.CafeName + ")" + "\n\n";
            }

            // Return list
            return allcafes;
        }

        private async Task<string> GetCafeForItem(string dining)
        {
            // String café - empty string will be populating from json response.
            string cafe = string.Empty;            

            // Get authentication token from authentication.cs
            Authentication auth = new Authentication();
            string authtoken = await auth.GetAuthHeader();

            // Get cafe from refdinign API
            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authtoken);                        
            HttpResponseMessage response = await httpClient.GetAsync("https://msrefdiningint.azurewebsites.net/api/v1/cafe/Name/" + dining);
            response.EnsureSuccessStatusCode();
            string responseBody = await response.Content.ReadAsStringAsync();

            // De-serialize response into list of objects with type cafe (cafe.cs). 
            List<Cafe> list = JsonConvert.DeserializeObject<List<Cafe>>(responseBody);
            
            // Populate string with cafe’s. 
            foreach (var item in list)
            {
                cafe += item.CafeName + "\n\n";                
            }

            // Return list
            return cafe;
        }

        private async Task<string> GetCafeMenu(string location)
        {
            
            // Get authentication token from authentication.cs
            Authentication auth = new Authentication();
            string authtoken = await auth.GetAuthHeader();

            // Get JSON – List of all Cafes
            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authtoken);
            HttpResponseMessage ResponseAllCafe = await httpClient.GetAsync("https://msrefdiningint.azurewebsites.net/api/v1/cafes");
            ResponseAllCafe.EnsureSuccessStatusCode();
            string RespnseBodyAllCafe = await ResponseAllCafe.Content.ReadAsStringAsync();

            // Convert JSON to list
            List<Cafe> allCafeList = JsonConvert.DeserializeObject<List<Cafe>>(RespnseBodyAllCafe);

            var buildingid =
                from n in allCafeList
                where n.CafeName.Equals(location, StringComparison.OrdinalIgnoreCase)
                select n;

            string newid = string.Empty;

            foreach (Cafe item in buildingid)
            {
                newid = item.BuildingId.ToString();
            }

            // Get the day of the week (1 – 5) for use in API URI. 
            DateTime day = DateTime.Now;
            int today = (int)day.DayOfWeek;

            // String menu - empty string will be populating from json response.
            string menu = string.Empty;

            try
            {

                //Get JSON – Cafe menu
                HttpResponseMessage response = await httpClient.GetAsync("https://msrefdiningint.azurewebsites.net/api/v1/menus/building/" + newid + "/weekday/" + today);
                response.EnsureSuccessStatusCode();
                string responseBody = await response.Content.ReadAsStringAsync();

                // Convert JSON to list
                List<CafeMenu> list = JsonConvert.DeserializeObject<List<CafeMenu>>(responseBody);

                // Format header – URL to café menu of dining site
                menu += "#[Cafe " + location + "](https://microsoft.sharepoint.com/sites/refweb/Pages/Dining-Menus.aspx?cafe=Café " + location + ")" + "\n\n";

                // Populate string with menu item description - convert to LINQ query
                foreach (var item in list)
                {
                    menu += "**" + item.Name + "** \n\n";

                    foreach (var item2 in item.CafeItems)
                    {
                        menu += "- " + item2.Name + "\n\n";
                    }
                }
            }
            catch
            {
                menu = null;
            }

            // Return list
            return menu;
        }

        private async Task<Luis> GetEntityFromLUIS(string Query)
        {
            Query = Uri.EscapeDataString(Query);
            Luis Data = new Luis();
            using (HttpClient client = new HttpClient())
            {
                string RequestURI = "https://api.projectoxford.ai/luis/v1/application?id=f11f7c0a-e4b1-47a3-9842-e825dc6b9922&subscription-key=daaf89e73e87447a9d5c45e24c23dbde&q=" + Query;
                HttpResponseMessage msg = await client.GetAsync(RequestURI);

                if (msg.IsSuccessStatusCode)
                {
                    var JsonDataResponse = await msg.Content.ReadAsStringAsync();
                    Data = JsonConvert.DeserializeObject<Luis>(JsonDataResponse);
                }
            }

            return Data;
        }

        private Activity HandleSystemMessage(Activity message)
        {
            if (message.Type == ActivityTypes.DeleteUserData)
            {
                // Implement user deletion here
                // If we handle user deletion, return a real message
            }
            else if (message.Type == ActivityTypes.ConversationUpdate)
            {
                // Handle conversation state changes, like members being added and removed
                // Use Activity.MembersAdded and Activity.MembersRemoved and Activity.Action for info
                // Not available in all channels
            }
            else if (message.Type == ActivityTypes.ContactRelationUpdate)
            {
                // Handle add/remove from contact lists
                // Activity.From + Activity.Action represent what happened
            }
            else if (message.Type == ActivityTypes.Typing)
            {
                // Handle knowing tha the user is typing
            }
            else if (message.Type == ActivityTypes.Ping)
            {
            }

            return null;
        }
    }
}