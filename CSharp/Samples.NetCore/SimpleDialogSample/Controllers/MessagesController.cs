using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Builder.Dialogs;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace SimpleDialogSample.Controllers
{
    [Route("api/[controller]")]
    public class MessagesController : Controller
    {
        // POST api/values
        [HttpPost]
        public async Task<IActionResult> Post([FromBody]Activity activity)
        {
            if(activity.Type == ActivityTypes.Message){
                await Conversation.SendAsync(activity,()=>{
                    
                })
            }else{
                HandleSystemMessage(activity);
            }
            return this.Ok();
        }

        private void HandleSystemMessage(Activity activity)
        {
        }
    }
}
