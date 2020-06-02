namespace TWETTY_WEBSERVER
{
    public static class appsettings
    {
        // Data base connection
        public static string ConnectionSqlServer { get; } = "Server=*****;Database=****; MultipleActiveResultSets=true; User Id=****; Password=****";
		
        public static string Jwt_Issuer { get; } = "twetty-chat_app";
        public static string Jwt_Audience { get; } = "twetty-chat_app";
        public static string Jwt_SecretKey { get; } = "ThisIsMytwettyChatSecretKey";
    }
}
