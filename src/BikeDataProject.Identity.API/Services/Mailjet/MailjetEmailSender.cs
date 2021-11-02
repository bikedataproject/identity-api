using System.Threading.Tasks;
using Mailjet.Client;
using Mailjet.Client.Resources;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace BikeDataProject.Identity.API.Services.Mailjet
{
    /// <summary>
    /// Sends emails using mailjet.
    /// </summary>
    public class MailjetEmailSender : IEmailSender
    {
        private readonly MailjetConfiguration _configuration;
        private readonly ILogger<MailjetEmailSender> _logger;

        /// <summary>
        /// Creates a new email sender.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        /// <param name="logger">The logger.</param>
        public MailjetEmailSender(MailjetConfiguration configuration, ILogger<MailjetEmailSender> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        private MailjetClient CreateClient()
        {
            return new MailjetClient(
                this._configuration.ApiKey,
                this._configuration.ApiSecret);
        }

        /// <inhertidoc/>
        public async Task SendConfirmAsync(string email, string confirmationLink)
        {
            
        }

        /// <inhertidoc/>
        public async Task SendFitbitConfirmAsync(string email, string confirmationLink)
        {
            var request = new MailjetRequest
                {
                    Resource = SendV31.Resource,
                }
                .Property(Send.Messages, new JArray
                {
                    new JObject
                    {
                        {
                            "To",
                            new JArray
                            {
                                new JObject
                                {
                                    {
                                        "Email",
                                        email
                                    }
                                }
                            }
                        },
                        {
                            "TemplateID",
                            this._configuration.FitbitTemplateId
                        },
                        {
                            "TemplateLanguage",
                            true
                        },
                        {
                            "Variables",

                            new JObject
                            {
                                {
                                    "confirmation_link",
                                    confirmationLink
                                }
                            }
                        }
                    }
                });
            var client = this.CreateClient();
            var response = await client.PostAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to send email: {StatusCode} - {ErrorInfo}",
                    response.StatusCode, response.GetErrorInfo());
            }
        }
    }
}