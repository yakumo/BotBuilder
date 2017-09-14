using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Bot.Connector
{
    /// <summary>
    /// Extension methods to add BotAuthentication capabilities to an HTTP application pipeline.
    /// </summary>
    public static class BotAuthenticationAppBuilderExtensions
    {
        public static IApplicationBuilder UseBotAuthentication(this IApplicationBuilder app, IConfiguration configuration)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            return app.UseBotAuthentication(
                configuration.GetSection(MicrosoftAppCredentials.MicrosoftAppIdKey)?.Value,
                configuration.GetSection(MicrosoftAppCredentials.MicrosoftAppPasswordKey)?.Value);
        }

        public static IApplicationBuilder UseBotAuthentication(this IApplicationBuilder app, string microsoftAppId, string microsoftAppPassword)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            return app.UseBotAuthentication(new StaticCredentialProvider(microsoftAppId, microsoftAppPassword));
        }

        public static IApplicationBuilder UseBotAuthentication(this IApplicationBuilder app, ICredentialProvider credentialProvider)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            var options = new BotAuthenticationOptions
            {
                CredentialProvider = credentialProvider
            };

            return app.UseMiddleware<BotAuthenticationMiddleware>(Options.Create(options));
        }

        public static IApplicationBuilder UseBotAuthentication(this IApplicationBuilder app, BotAuthenticationOptions options)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            return app.UseMiddleware<BotAuthenticationMiddleware>(Options.Create(options));
        }
    }


    /// <summary>
    /// Bot authentication middleware.
    /// </summary>
    public sealed class BotAuthenticationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IOptions<BotAuthenticationOptions> _options;

        public BotAuthenticationMiddleware(RequestDelegate next, IOptions<BotAuthenticationOptions> options)
        {
            _next = next;
            _options = options;
        }

        public Task Invoke(HttpContext context)
        {
            var b = new BotAuthenticator(_options.Value.CredentialProvider);
            return this._next(context);
        }
    }
}
