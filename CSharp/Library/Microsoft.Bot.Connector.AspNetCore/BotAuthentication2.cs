using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Bot.Connector
{
    /// <summary>
    /// Bot authenticate for RECEIVED Activity
    /// </summary>
    /// <example>
    /// [BotAuthentication]
    /// [HttpPost]
    /// public async Task Post([FromBody]Activity activity)
    /// </example>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public class BotAuthentication : AuthorizeAttribute, IActionFilter
    {
        public BotAuthentication()
        {
            AuthenticationSchemes = BotAuthenticationAppBuilderExtensions.DefaultBotAuthenticationScheme;
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
        }

        public void OnActionExecuting(ActionExecutingContext context)
        {
            var activities = GetActivities(context);

            foreach (var activity in activities)
            {
                MicrosoftAppCredentials.TrustServiceUrl(activity.ServiceUrl);
            }
        }

        public static IList<Activity> GetActivities(ActionExecutingContext actionContext)
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

    /// <summary>
    /// Extension methods to add BotAuthentication capabilities to an HTTP service collection.
    /// </summary>
    /// <example>
    /// public void ConfigureServices(IServiceCollection services)
    /// {
    ///     services.AddAuthentication().AddBot("{AppId}", "{AppPassword}");
    /// }
    /// </example>
    public static class BotAuthenticationAppBuilderExtensions
    {
        public const string DefaultBotAuthenticationScheme = "Bot";
        public const string BotClaim = "Bot";

        public static AuthenticationBuilder AddBot(this AuthenticationBuilder authBuilder, string microsoftAppId = null, string microsoftAppPassword = null)
        {
            if (String.IsNullOrWhiteSpace(microsoftAppId))
            {
                microsoftAppId = Environment.GetEnvironmentVariable(MicrosoftAppCredentials.MicrosoftAppIdKey, EnvironmentVariableTarget.Process);
            }
            if (String.IsNullOrWhiteSpace(microsoftAppPassword))
            {
                microsoftAppPassword = Environment.GetEnvironmentVariable(MicrosoftAppCredentials.MicrosoftAppPasswordKey, EnvironmentVariableTarget.Process);
            }

            return authBuilder.AddBot(new StaticCredentialProvider(microsoftAppId, microsoftAppPassword));
        }

        public static AuthenticationBuilder AddBot(this AuthenticationBuilder authBuilder, ICredentialProvider credentialProvider)
        {
            return authBuilder.AddBot(new BotAuthenticationOptions(credentialProvider));
        }

        public static AuthenticationBuilder AddBot(this AuthenticationBuilder authBuilder, BotAuthenticationOptions options)
        {
            authBuilder.Services.AddSingleton(typeof(BotAuthenticationOptions), options);
            var ret = authBuilder.AddScheme<BotAuthenticationOptions, BotAuthenticationHandler>(BotAuthenticationAppBuilderExtensions.DefaultBotAuthenticationScheme, (conf) =>
            {
                conf.CredentialProvider = options.CredentialProvider;
                conf.OpenIdConfiguration = options.OpenIdConfiguration;
                conf.DisableEmulatorTokens = options.DisableEmulatorTokens;
                conf.SaveToken = options.SaveToken;
            });
            return ret;
        }
    }

    /// <summary>
    /// Uses Authentication for Bot Connector
    /// </summary>
    public interface IBotAuthenticationOptions
    {
        ICredentialProvider CredentialProvider { set; get; }
        string OpenIdConfiguration { set; get; }
        bool DisableEmulatorTokens { set; get; }
        bool SaveToken { set; get; }
    }

    /// <summary>
    /// implementation to Authentication for Bot Connector
    /// </summary>
    public class BotAuthenticationOptions : AuthenticationSchemeOptions, IBotAuthenticationOptions
    {
        public BotAuthenticationOptions()
        {
        }

        public BotAuthenticationOptions(ICredentialProvider credentialProvider)
        {
            CredentialProvider = credentialProvider;
        }

        /// <summary>
        /// The <see cref="ICredentialProvider"/> used for authentication.
        /// </summary>
        public ICredentialProvider CredentialProvider { set; get; }

        /// <summary>
        /// The OpenId configuation.
        /// </summary>
        public string OpenIdConfiguration { set; get; } = JwtConfig.ToBotFromChannelOpenIdMetadataUrl;

        /// <summary>
        /// Flag indicating if emulator tokens should be disabled.
        /// </summary>
        public bool DisableEmulatorTokens { set; get; } = false;

        /// <summary>
        /// Flag indicating if <see cref="BotAuthenticationHandler"/> should be stored in 
        /// the returned <see cref="Microsoft.AspNetCore.Authentication.AuthenticationTicket"/>. 
        /// </summary>
        public bool SaveToken { set; get; } = true;
    }

    /// <summary>
    /// Authentication Handler
    /// </summary>
    public class BotAuthenticationHandler : AuthenticationHandler<BotAuthenticationOptions>
    {
        public BotAuthenticationHandler(IOptionsMonitor<BotAuthenticationOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock) : base(options, logger, encoder, clock)
        {
        }

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (await Options.CredentialProvider.IsAuthenticationDisabledAsync())
            {
                var principal = new ClaimsPrincipal(new ClaimsIdentity(new Claim[] { new Claim(ClaimTypes.Role, BotAuthenticationAppBuilderExtensions.BotClaim) }));
                return AuthenticateResult.Success(new AuthenticationTicket(principal, new AuthenticationProperties(), JwtBearerDefaults.AuthenticationScheme));
            }

            string token = null;

            string authorization = Request.Headers["Authorization"];
            token = authorization?.Substring("Bearer ".Length).Trim();

            // If no token found, no further work possible
            // and Authentication is not disabled fail
            if (string.IsNullOrEmpty(token))
            {
                return AuthenticateResult.Fail("No JwtToken is present and BotAuthentication is enabled!");
            }

            var authenticator = new BotAuthenticator(Options.CredentialProvider, Options.OpenIdConfiguration, Options.DisableEmulatorTokens);
            var identityToken = await authenticator.TryAuthenticateAsync(JwtBearerDefaults.AuthenticationScheme, token, CancellationToken.None);

            if (identityToken.Authenticated)
            {
                identityToken.Identity.AddClaim(new Claim(ClaimTypes.Role, BotAuthenticationAppBuilderExtensions.BotClaim));
                var principal = new ClaimsPrincipal(identityToken.Identity);
                var ticket = new AuthenticationTicket(principal, new AuthenticationProperties(), JwtBearerDefaults.AuthenticationScheme);
                Context.User = principal;

                if (Options.SaveToken)
                {
                    ticket.Properties.StoreTokens(new[]
                            {
                                new AuthenticationToken { Name = "access_token", Value = token }
                            });
                }

                return AuthenticateResult.Success(ticket);
            }
            else
            {
                return AuthenticateResult.Fail($"Failed to authenticate JwtToken {token}");
            }
        }
    }
}
