
namespace TWETTY_WEBSERVER
{
    /// <summary>
    /// The credentials for an API client to register on the server 
    /// </summary>
    public class RegisterApiModel
    {
        #region Public Properties
        /// <summary>
        /// The users email
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// The users first name
        /// </summary>
        public string FirstName { get; set; }

        /// <summary>
        /// The users last name
        /// </summary>
        public string LastName { get; set; }

        /// <summary>
        /// The user password
        /// </summary>
        public string Password { get; set; }

        #endregion

        #region Constructor

        /// <summary>
        /// Default constructor
        /// </summary>
        public RegisterApiModel()
        {

        }

        #endregion
    }
}
