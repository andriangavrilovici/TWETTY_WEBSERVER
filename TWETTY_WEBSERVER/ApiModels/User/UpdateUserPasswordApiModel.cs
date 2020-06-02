using System.Security;

namespace TWETTY_WEBSERVER
{
    /// <summary>
    /// The details to change for a Users Password from an API client call
    /// </summary>
    public class UpdateUserPasswordApiModel
    {
        #region Public Properties

        /// <summary>
        /// The user current password
        /// </summary>
        public string CurrentPassword { get; set; }

        /// <summary>
        /// The user new password
        /// </summary>
        public string NewPassword { get; set; }

        #endregion
    }
}
