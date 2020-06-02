
namespace TWETTY_WEBSERVER
{
    /// <summary>
 /// The credentials for an API client to log into the server and receive a token back
 /// </summary>
    public class LoginApiModel
    {
        #region Public Properties

        /// <summary>
        /// The users email
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// The user password
        /// </summary>
        public string Password { get; set; }

        #endregion

        #region Constructor

        /// <summary>
        /// Default constructor
        /// </summary>
        public LoginApiModel()
        {

        }

        #endregion
    }
}
