using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BikeDataProject.Identity.API.Services
{
    /// <summary>
    /// Abstract definition of a service to send emails.
    /// </summary>
    public interface IEmailSender
    {
        /// <summary>
        /// Sends an email confirmation email.
        /// </summary>
        /// <param name="email">The email address.</param>
        /// <param name="confirmationLink">The confirmation link.</param>
        /// <returns></returns>
        Task SendConfirmAsync(string email, string confirmationLink);
        
        /// <summary>
        /// Sends a fitbit confirmation email.
        /// </summary>
        /// <param name="email">The email address.</param>
        /// <param name="confirmationLink">The confirmation link.</param>
        /// <returns></returns>
        Task SendFitbitConfirmAsync(string email, string confirmationLink);
    }
}
