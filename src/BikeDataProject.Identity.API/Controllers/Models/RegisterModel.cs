using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace BikeDataProject.Identity.API.Models.AccountViewModels
{
    public class RegisterModel
    {
        /// <summary>
        /// The functional name.
        /// </summary>
        [Required]
        [StringLength(20, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 3)]
        [Display(Name = "Username")]
        public string FunctionalName { get; set; }
        
        /// <summary>
        /// The email.
        /// </summary>
        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; }

        /// <summary>
        /// The password.
        /// </summary>
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }
        
        /// <summary>
        /// The url to use for email confirmation if an email address was given.
        /// </summary>
        public string ConfirmEmailUrl { get; set; }
    }
}
