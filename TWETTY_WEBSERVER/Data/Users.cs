using Microsoft.AspNetCore.Identity;

namespace TWETTY_WEBSERVER
{
    public class Users : IdentityUser
    {
        /// <summary>
        /// The users first name
        /// </summary>
        public string FirstName { get; set; }

        /// <summary>
        /// The users last name
        /// </summary>
        public string LastName { get; set; }
    }
}
