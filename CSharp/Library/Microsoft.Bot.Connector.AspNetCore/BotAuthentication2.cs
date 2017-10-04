using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Bot.Connector
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public class BotAuthentication : ActionFilterAttribute
    {
        /// <summary>
        /// Microsoft AppId for the bot 
        /// </summary>
        /// <remarks>
        /// Needs to be used with MicrosoftAppPassword.  Ignored if CredentialProviderType is specified.
        /// </remarks>
        public string MicrosoftAppId { get; set; }

        /// <summary>
        /// Microsoft AppPassword for the bot (needs to be used with MicrosoftAppId)
        /// </summary>
        /// <remarks>
        /// Needs to be used with MicrosoftAppId. Ignored if CredentialProviderType is specified.
        /// </remarks>
        public string MicrosoftAppPassword { get; set; }

        public string AuthenticationScheme { get; set; } = JwtBearerDefaults.AuthenticationScheme;

        public bool DisableEmulatorTokens { get; set; }

        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var activities = GetActivities(context);

            foreach (var activity in activities)
            {
                MicrosoftAppCredentials.TrustServiceUrl(activity.ServiceUrl);
            }

            string appId = MicrosoftAppId ?? Environment.GetEnvironmentVariable(MicrosoftAppCredentials.MicrosoftAppIdKey, EnvironmentVariableTarget.Process);
            string pass = MicrosoftAppPassword ?? Environment.GetEnvironmentVariable(MicrosoftAppCredentials.MicrosoftAppPasswordKey, EnvironmentVariableTarget.Process);
            if (String.IsNullOrWhiteSpace(appId) || String.IsNullOrWhiteSpace(pass))
            {
                context.Result = new UnauthorizedResult();
            }
            else
            {

                string token = String.Empty;
                string authorization = context.HttpContext.Request.Headers["Authorization"];
                token = authorization?.Substring("Bearer ".Length).Trim();

                var authenticator = new BotAuthenticator(MicrosoftAppId, MicrosoftAppPassword);
                var authorized = await authenticator.TryAuthenticateAsync(AuthenticationScheme, token, CancellationToken.None);
                if (!authorized.Authenticated)
                {
                    context.Result = new UnauthorizedResult();
                }
            }

            await base.OnActionExecutionAsync(context, next);
        }

        private IList<Activity> GetActivities(ActionExecutingContext actionContext)
        {
            var activties = actionContext.ActionArguments.Select(t => t.Value).OfType<Activity>().ToList();
            if (activties.Any())
            {
                return activties;
            }
            else
            {
                var objects =
                    actionContext.ActionArguments.Where(t => t.Value is JObject || t.Value is JArray)
                        .Select(t => t.Value).ToArray();
                if (objects.Any())
                {
                    activties = new List<Activity>();
                    foreach (var obj in objects)
                    {
                        activties.AddRange((obj is JObject) ? new Activity[] { ((JObject)obj).ToObject<Activity>() } : ((JArray)obj).ToObject<Activity[]>());
                    }
                }
            }
            return activties;
        }
    }
}
