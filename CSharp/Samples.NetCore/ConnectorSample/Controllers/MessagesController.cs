using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Bot.Connector;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace ConnectorSample.Controllers
{
    [Route("api/[controller]")]
    public class MessagesController : Controller
    {
        [HttpPost]
        public async Task<IActionResult> Post([FromBody]Activity activity)
        {
            if (activity.Type == "message")
            {
                var connector = new ConnectorClient(new Uri(activity.ServiceUrl));
                var reply = activity.CreateReply($"You sent {activity.Text} which was {activity.Text.Length} characters");
                await connector.Conversations.ReplyToActivityAsync(reply);
            }
            else
            {
                HandleSystemMessage(activity);
            }

            return this.Ok();
        }

        private void HandleSystemMessage(Activity activity)
        {
            
        }
    }
}
