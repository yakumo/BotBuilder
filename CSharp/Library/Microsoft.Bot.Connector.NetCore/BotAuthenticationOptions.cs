using Microsoft.AspNetCore.Authentication;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Bot.Connector
{
    /// <summary>
    /// Options for <see cref="BotAuthenticationMiddleware"/>.
    /// </summary>
    public sealed class BotAuthenticationOptions
    {
        public BotAuthenticationOptions()
        {
        }

        /// <summary>
        /// The <see cref="ICredentialProvider"/> used for authentication.
        /// </summary>
        public ICredentialProvider CredentialProvider { set; get; }
    }
}
