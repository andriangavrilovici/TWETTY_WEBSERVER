namespace TWETTY_WEBSERVER
{
    public class UserProfileDetailsApiModel
    {
        public string Id { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }

        // The authentication token used to stay authenticated through future requests
        /// <remarks>The Token is only provided when called from the login methods</remarks>
        public string Token { get; set; }

        #region Constructor
        public UserProfileDetailsApiModel() { }
        #endregion

        #region Public Helper Methods

        /// <summary>
        /// Creates a new <see cref="LoginCredentialsDataModel"/>
        /// from this model
        /// </summary>
        /// <returns></returns>
        public LoginCredentialsDataModel ToLoginCredentialsDataModel()
        {
            return new LoginCredentialsDataModel
            {
                Email = Email,
                FirstName = FirstName,
                LastName = LastName,
                Token = Token
            };
        }

        #endregion
    }
}
