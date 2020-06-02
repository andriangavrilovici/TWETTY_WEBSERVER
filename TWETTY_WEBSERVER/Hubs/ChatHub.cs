using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System;
using System.Data;
using System.Threading.Tasks;

namespace TWETTY_WEBSERVER
{
    [AuthorizeToken]
    public class ChatHub : Hub
    {
        #region Public Members

        // Logged users
        public readonly static ConnectionMapping<string> _connections =
            new ConnectionMapping<string>();

        #endregion

        #region Protected Members

        // User management
        protected UserManager<Users> _userManager;

        #endregion

        #region Private Members

        // Connection to the database
        private SqlConnection connection = new SqlConnection(appsettings.ConnectionSqlServer);

        // Execute any commands to the database
        private SqlCommand cmd;

        // Reading data from the database
        private SqlDataReader dataReader;

        // String for writing any command in the database
        private string CommandText = "";

        #endregion

        readonly ILogger<ChatHub> _log;

        #region Constructor

        public ChatHub(UserManager<Users> userManager, ILogger<ChatHub> log)
        {
            // User managemen
            _userManager = userManager;

            // Connection to the database
            connection = new SqlConnection(appsettings.ConnectionSqlServer);

            _log = log;
        }

        #endregion

        #region Send Message

        /// <summary>
        /// Send message from user to user
        /// </summary>
        /// <param name="messageApi">Message data</param>
        public async Task SendBy(MessageApiModel messageApi)
        {
            // The error message for a failed send message
            string ErrorMessage = "Ne pare rau, mesajul dmn. nu a fost trimis!";

            // Make sure we have all the data
            if (string.IsNullOrWhiteSpace(messageApi.SendBy_Email) ||
                string.IsNullOrWhiteSpace(messageApi.SendTo_Email) ||
                string.IsNullOrWhiteSpace(messageApi.Message) ||
                string.IsNullOrWhiteSpace(messageApi.MessageSentTime.ToString()))
            {
                await Clients.Caller.SendAsync("Error", ErrorMessage);
                return;
            }

            // We make sure that the sending user coincides
            if (!messageApi.SendBy_Email.Equals(Context.User.Identity.Name))
            {
                await Clients.Caller.SendAsync("Error", ErrorMessage);
                return;
            }

            // Get the data of the user sending the message
            var user_SendBy = await _userManager.FindByEmailAsync(messageApi.SendBy_Email);

            // Get the data of the user receiving the message
            var user_SendTo = await _userManager.FindByEmailAsync(messageApi.SendTo_Email);

            // We check if users exist
            if (user_SendBy == null || user_SendTo == null)
            {
                await Clients.Caller.SendAsync("Error", ErrorMessage);
                return;
            }

            // Save message in database
            try
            {
                CommandText = "INSERT INTO [dbo].[Messages] ([SendBy_Id], [SendTo_Id], [Message], [MessageSentTime]) " +
                "VALUES(@SendBy_Id, @SendTo_Id, @Message, @MessageSentTime)";
                cmd = new SqlCommand(CommandText, connection);
                cmd.Parameters.Add(
                "@SendBy_Id", SqlDbType.NVarChar).Value = user_SendBy.Id;
                cmd.Parameters.Add(
                "@SendTo_Id", SqlDbType.NVarChar).Value = user_SendTo.Id;
                cmd.Parameters.Add(
                "@Message", SqlDbType.NVarChar).Value = messageApi.Message;
                cmd.Parameters.Add(
                "@MessageSentTime", SqlDbType.DateTimeOffset).Value = messageApi.MessageSentTime;

                connection.Open();

                int k = cmd.ExecuteNonQuery();

                cmd.Parameters.Clear();

                connection.Close();

                if (k == 0)
                    await Clients.Caller.SendAsync("Error", ErrorMessage);
            }
            catch(Exception ex)
            {
                await Clients.Caller.SendAsync("Error", ErrorMessage);

                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ex.Message, Console.ForegroundColor);
                Console.ResetColor();
                _log.LogError(ex.Message);
            }

            // Send message to user if is online
            foreach (var connectionId in _connections.GetConnections(messageApi.SendTo_Email))
            {
                await Clients.Client(connectionId).SendAsync("SendBy",
                    new MessageApiModel
                    {
                        SendBy_Email = messageApi.SendBy_Email,
                        SendTo_Email = messageApi.SendTo_Email,
                        Message = messageApi.Message,
                        MessageSentTime = messageApi.MessageSentTime
                    });
            }
        }

        #endregion

        #region Friend Request / Response

        /// <summary>
        /// Send friend request to user
        /// </summary>
        /// <param name="toEmail">This is the email of the user to whom we send request</param>
        public async Task SendFriendRequest(string toEmail)
        {
            // The error message for a failed send request
            string ErrorMessage = "Ne pare rau.\nCeva a mers incorect.";

            // Get the data of the user sending the request
            var user_SendBy = await _userManager.GetUserAsync(Context.User);

            // Get the data of the user receiving the request
            var user_SendTo = await _userManager.FindByEmailAsync(toEmail);

            // We check if users exist
            if (user_SendBy == null || user_SendTo == null)
            {
                await Clients.Caller.SendAsync("Error", ErrorMessage);
                return;
            }

            // Check if this friend is not already in the friends list
            try
            {
                CommandText = "SELECT [RequestedBy_Id] FROM [dbo].[Friends] WHERE [RequestedBy_Id] = @RequestedBy_Id AND [RequestedTo_Id] = @RequestedTo_Id";
                cmd = new SqlCommand(CommandText, connection);

                connection.Open();

                cmd.Parameters.Add(
                "@RequestedBy_Id", SqlDbType.NVarChar).Value = user_SendBy.Id;
                cmd.Parameters.Add(
                "@RequestedTo_Id", SqlDbType.NVarChar).Value = user_SendTo.Id;

                dataReader = cmd.ExecuteReader();

                if (dataReader.Read())
                {
                    await Clients.Caller.SendAsync("Error", "Aceasta persoana inca nu a raspuns!\nAsteptati.");
                    connection.Close();
                    return;
                }

                cmd.Parameters.Clear();
            }
            catch(Exception ex)
            {
                await Clients.Caller.SendAsync("Error", ErrorMessage);

                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ex.Message, Console.ForegroundColor);
                Console.ResetColor();
                _log.LogError(ex.Message);
            }

            // Save the friend request in the database
            try
            {
                CommandText = "INSERT INTO [dbo].[Friends] ([FriendRequestFlag], [RequestedBy_Id], [RequestedTo_Id]) " +
                "VALUES(0, @RequestedBy_Id, @RequestedTo_Id)";

                cmd = new SqlCommand(CommandText, connection);

                cmd.Parameters.Add(
                "@RequestedBy_Id", SqlDbType.NVarChar).Value = user_SendBy.Id;

                cmd.Parameters.Add(
                "@RequestedTo_Id", SqlDbType.NVarChar).Value = user_SendTo.Id;

                int k = cmd.ExecuteNonQuery();

                connection.Close();

                if (k == 0)
                    await Clients.Caller.SendAsync("Error", ErrorMessage);
            }catch(Exception ex)
            {
                await Clients.Caller.SendAsync("Error", ErrorMessage);
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ex.Message, Console.ForegroundColor);
                Console.ResetColor();
                _log.LogError(ex.Message);
            }

            // Send friend request to user if online
            foreach (var connectionId in _connections.GetConnections(toEmail))
            {
                await Clients.Client(connectionId).SendAsync("FriendRequest",
                   new FriendApiModel
                   {
                       Email = user_SendBy.Email,
                       FirstName = user_SendBy.FirstName,
                       LastName = user_SendBy.LastName,
                       Status = true
                   });
            }
        }

        /// <summary>
        /// Send the answer to the friend request
        /// </summary>
        /// <param name="Response">Email to send and response</param>
        public async Task SendFriendResponse(string Response)
        {
            // The error message for a failed send response
            string ErrorMessage = "Ne pare rau.\nCeva a mers incorect.";
            // The e-mail to whom we send the answer
            string toEmail = "";
            // Response to the request
            bool responseFlag = false;

            // Set email and response
            try
            {
                // We separate the answer and the email
                string[] words = Response.Split(new char[] { ' ' });
                // We set the email to whom we send
                toEmail = words[0];
                // We set the answer
                responseFlag = Convert.ToBoolean(words[1]);
            }
            // We find the error and display
            catch (Exception ex)
            {
                await Clients.Caller.SendAsync("Error", ErrorMessage);
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ex.Message, Console.ForegroundColor);
                Console.ResetColor();
                _log.LogError(ex.Message);
                return;
            }

            // Check if the send email is not null or empty
            if (string.IsNullOrWhiteSpace(toEmail))
            {
                await Clients.Caller.SendAsync("Error", ErrorMessage);
                return;
            }

            // Get the data of the user sending the response
            var user_SendBy = await _userManager.GetUserAsync(Context.User);
            // Get the data of the user receiving the response
            var user_SendTo = await _userManager.FindByEmailAsync(toEmail);

            // We check if users exist
            if (user_SendBy == null || user_SendTo == null)
            {
                await Clients.Caller.SendAsync("Error", ErrorMessage);
                return;
            }
            // Set the answer or delete the request from the database
            try
            {
                CommandText = "UPDATE [dbo].[Friends] SET [FriendRequestFlag] = 1 " +
                "WHERE [RequestedBy_Id] = @RequestedTo_Id AND [RequestedTo_Id] = @RequestedBy_Id";

                if (!responseFlag)
                {
                    CommandText = "DELETE FROM [dbo].[Friends] WHERE [FriendRequestFlag] = 0 AND " +
                        "[RequestedBy_Id] = @RequestedTo_Id AND [RequestedTo_Id] = @RequestedBy_Id";
                }

                cmd = new SqlCommand(CommandText, connection);

                cmd.Parameters.Add(
                "@RequestedBy_Id", SqlDbType.NVarChar).Value = user_SendBy.Id;
                cmd.Parameters.Add(
                "@RequestedTo_Id", SqlDbType.NVarChar).Value = user_SendTo.Id;

                connection.Open();

                int k = cmd.ExecuteNonQuery();

                connection.Close();

                if (k == 0)
                {
                    await Clients.Caller.SendAsync("Error", ErrorMessage);
                    return;
                }
            }
            // We find the error and display
            catch (Exception ex)
            {
                await Clients.Caller.SendAsync("Error", ErrorMessage);
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ex.Message, Console.ForegroundColor);
                Console.ResetColor();
                _log.LogError(ex.Message);
            }

            // Check to see if the answer is negative
            if (!responseFlag) return;

            // Send friend response to user if online
            foreach (var connectionId in _connections.GetConnections(toEmail))
            {
                await Clients.Client(connectionId).SendAsync("FriendResponse",
                    new FriendApiModel
                    {
                        Email = user_SendBy.Email,
                        FirstName = user_SendBy.FirstName,
                        LastName = user_SendBy.LastName,
                        Status = true
                    });
            }
        }

        #endregion

        #region Send User Update

        /// <summary>
        /// Send all user update
        /// </summary>
        [AuthorizeToken]
        public async Task UpdateUserProfile(UpdateUserProfileApiModel update)
        {
            _log.LogWarning("UpdateUserProfile_SignalR - " + Context.User.Identity.Name);
            var user = await _userManager.GetUserAsync(Context.User);

            if (user == null) return;

            if (!string.IsNullOrWhiteSpace(update.NewEmail))
            {
                var userFound = await _userManager.FindByEmailAsync(update.NewEmail);
                if (userFound == null || user != userFound) return;
            }

            // Send to everyone that this user is online
            await Clients.Others.SendAsync("UpdateFriend", update);
        }

        #endregion

        #region Connecting / Disconnecting

        /// <summary>
        /// The user connection
        /// </summary>
        [AuthorizeToken]
        public override async Task OnConnectedAsync()
        {
            _log.LogWarning("OnConnectedAsync - " + Context.User.Identity.Name);
            // We obtain the data of the current user
            var user = await _userManager.GetUserAsync(Context.User);

            // We obtain the email of the current user
            string name = Context.User.Identity.Name;

            // We send a notification to the connected user
            await Clients.Caller.SendAsync("Notify", $"Bun venit {user.FirstName} :)");
            
            // Add user for ConnectionMapping
            _connections.Add(name, Context.ConnectionId);

            // Send to everyone that this user is online
            await Clients.Others.SendAsync("UserOnline", name);
            
            // Send the user all requests for friendship
            try
            {
                CommandText = "SELECT [RequestedBy_Id] FROM [dbo].[Friends] WHERE [RequestedTo_Id] = @MyId AND [FriendRequestFlag] = 0";
                cmd = new SqlCommand(CommandText, connection);

                cmd.Parameters.Add(
                "@MyId", SqlDbType.NVarChar).Value = user.Id;

                connection.Open();

                dataReader = cmd.ExecuteReader();

                while (dataReader.Read())
                {
                    var user_SendBy = await _userManager.FindByIdAsync(dataReader["RequestedBy_Id"].ToString());

                    bool Status = false;

                    foreach (var connectionEmail in _connections.GetOnline())
                        if (connectionEmail.Equals(user_SendBy.Email))
                        {
                            Status = true;
                            break;
                        }

                    await Clients.Caller.SendAsync("FriendRequest",
                        new FriendApiModel
                        {
                            Email = user_SendBy.Email,
                            FirstName = user_SendBy.FirstName,
                            LastName = user_SendBy.LastName,
                            Status = Status
                        });
                }

                connection.Close();
            }catch (Exception ex)
            {
                await Clients.Caller.SendAsync("Error", "Ne pare rau!\nCeva a mers incorect.");
                
                Console.ForegroundColor = ConsoleColor.Red; 
                Console.WriteLine(ex.Message, Console.ForegroundColor);
                Console.ResetColor();
                _log.LogError(ex.Message);
            }

            // We create the user connection
            await base.OnConnectedAsync();
        }

        /// <summary>
        /// The user disconnecting
        /// </summary>
        public override async Task OnDisconnectedAsync(Exception exception)
        {
            string name = Context.User.Identity.Name;

            // Remove user from ConnectionMapping
            _connections.Remove(name, Context.ConnectionId);

            // Send to everyone that this user is offline
            await Clients.Others.SendAsync("UserOffline", name);

            // Disconnecting the user
            await base.OnDisconnectedAsync(exception);
        }

        #endregion
    }
}
