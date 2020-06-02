namespace TWETTY_WEBSERVER
{
    public static class ApiRoutes
    {
        public const string GetFriends = "api/friends";

        public const string GetMessage = "api/get/message";

        #region Login / Register
        // The route to the Login Api method
        public const string Login = "api/login";

        // The route to the Register Api method
        public const string Register = "api/register";

        #endregion

        #region User Profile

        // The route to the GetUserProfile Api method
        public const string GetUserProfile = "api/user/profile";

        // The route to the UpdateUserProfile Api method
        public const string UpdateUserProfile = "api/user/profile/update";

        // The route to the UpdateUserPassword Api method
        public const string UpdateUserPassword = "api/user/password/update";

        #endregion

        #region Contacts

        // The route to the SearchUsers Api method
        public const string SearchUsers = "api/users/search";

        #endregion
    }
}
