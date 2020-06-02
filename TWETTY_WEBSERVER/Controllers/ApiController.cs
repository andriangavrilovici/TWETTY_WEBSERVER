using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Newtonsoft.Json;
using Microsoft.Data.SqlClient;
using System.Data;
using Microsoft.Extensions.Logging;

namespace TWETTY_WEBSERVER
{
    [Authorize]
    public class ApiController : Controller
    {
        #region Protected Members

        protected ApplicationContext _context;
        protected UserManager<Users> _userManager;
        protected SignInManager<Users> _signInManager;
        protected SqlConnection connection = new SqlConnection(appsettings.ConnectionSqlServer);

        #endregion

        readonly ILogger<ApiController> _log;

        public ApiController(ApplicationContext context, UserManager<Users> userManager, 
                            SignInManager<Users> signInManager, ILogger<ApiController> log)
        {
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
            connection = new SqlConnection(appsettings.ConnectionSqlServer);
            _log = log;
        }

        #region Login/Register

        [AllowAnonymous]
        [Route(ApiRoutes.Register)]
        public async Task<string> RegisterAsync([FromBody]RegisterApiModel registerApi)
        {
            string errorMessage = "Nu sa putut crea.\nIntrodu toate datele.";

            // The error response for a failed login
            var errorResponse = new ApiResponse<RegisterResultApiModel>
            {
                // Set error message
                ErrorMessage = errorMessage
            };

            var user = new Users
            {
                FirstName = registerApi.FirstName,
                LastName = registerApi.LastName,
                Email = registerApi.Email,
                UserName = registerApi.Email
            };

            var result = await _userManager.CreateAsync(user, registerApi.Password);

            if (!result.Succeeded)
            {
                // Aggregate all errors into a single error string
                var error = result.Errors.ToList()
                              // Grab their description
                              .Select(f => f.Description)
                              // And combine them with a newline separator
                              .Aggregate((a, b) => $"{a}{Environment.NewLine}{b}");

                _log.LogError(error);
                // Return the failed response
                return JsonConvert.SerializeObject(new ApiResponse<RegisterResultApiModel>
                {

                    ErrorMessage = error
                });
            }

            var userIdentity = await _userManager.FindByEmailAsync(user.Email);

            // Add the administrator in friends
            try
            {
                connection.Open();
                SqlCommand cmd = connection.CreateCommand();
                string AddAdminQuerry = "INSERT INTO [dbo].[Friends] ([FriendRequestFlag], [RequestedBy_Id], [RequestedTo_Id]) " +
                "VALUES(1, @RequestedBy_Id, @RequestedTo_Id)";

                cmd = new SqlCommand(AddAdminQuerry, connection);

                cmd.Parameters.Add(
                "@RequestedBy_Id", SqlDbType.NVarChar).Value = _userManager.FindByNameAsync("Administrator").Result.Id;

                cmd.Parameters.Add(
                "@RequestedTo_Id", SqlDbType.NVarChar).Value = userIdentity.Id;

                int k = cmd.ExecuteNonQuery();

                connection.Close();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ex.Message, Console.ForegroundColor);
                Console.ResetColor();
                _log.LogError(ex.Message);
            }
            
            return JsonConvert.SerializeObject(new ApiResponse<RegisterResultApiModel>
            {
                Response = new RegisterResultApiModel
                {
                    FirstName = userIdentity.FirstName,
                    LastName = userIdentity.LastName,
                    Email = userIdentity.Email,
                    Token = userIdentity.GenerateJwtToken()
                }
            });
        }

        [AllowAnonymous]
        [Route(ApiRoutes.Login)]
        public async Task<string> LoginAsync([FromBody]LoginApiModel loginApi)
        {
            var isEmail = loginApi.Email.Contains("@");

            // Get the user details
            var user = isEmail ? 
                // Find by email
                await _userManager.FindByEmailAsync(loginApi.Email) : 
                // Find by username
                await _userManager.FindByNameAsync(loginApi.Email);


            if (user == null)
            {
                return JsonConvert.SerializeObject(new ApiResponse<UserProfileDetailsApiModel>
                {
                    // The message when we fail to login
                    ErrorMessage = "Utilizator nu a fost gasit!"
                });
            }

            var isValidPassword = await _userManager.CheckPasswordAsync(user, loginApi.Password);

            var signInPassword = await _signInManager.PasswordSignInAsync(user, loginApi.Password, false, false);
            
            if (!isValidPassword || !signInPassword.Succeeded)
            {
                return JsonConvert.SerializeObject(new ApiResponse<UserProfileDetailsApiModel>
                {
                    // The message when we fail to login
                    ErrorMessage = "Email-ul sau parola este incorecta"
                });
            }

            return JsonConvert.SerializeObject(new ApiResponse<UserProfileDetailsApiModel>
            {
                Response = new UserProfileDetailsApiModel
                {
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Email = user.Email,
                    Token = user.GenerateJwtToken()
                }
            });
        }
        
        #endregion

        #region User Profile

        /// <summary>
        /// Returns the users profile details based on the authenticated user
        /// </summary>
        /// <returns></returns>
        [Route(ApiRoutes.GetUserProfile)]
        [AuthorizeToken]
        public async Task<string> GetUserProfileAsync()
        {
            _log.LogWarning("GetUserProfileAsync - " + HttpContext.User.Identity.Name);
            // Get user claims
            var user = await _userManager.GetUserAsync(HttpContext.User);
            
            // If we have no user...
            if (user == null)
                // Return error
                return JsonConvert.SerializeObject(new ApiResponse<UserProfileDetailsApiModel>()
                {
                    // TODO: Localization
                    ErrorMessage = "Utilizator nu a fost gasit"
                });

            return JsonConvert.SerializeObject(new ApiResponse<UserProfileDetailsApiModel>
            {
                // Pass back the user details
                Response = new UserProfileDetailsApiModel
                {
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Email = user.Email,
                }
            });
        }

        /// <summary>
        /// Returns the users update profile details 
        /// </summary>
        /// <returns></returns>
        [Route(ApiRoutes.UpdateUserProfile)]
        [AuthorizeToken]
        public async Task<string> UpdateUserProfieAsync([FromBody] UpdateUserProfileApiModel updateProfile)
        {
            _log.LogWarning("UpdateUserProfileAsync - " + HttpContext.User.Identity.Name);
            // The message when we fail to login
            var errorMessage = "Nu ati introdus datele corect!";

            // The error response for a failed login
            var errorResponse = new ApiResponse<string>
            {
                // Set error message
                ErrorMessage = errorMessage
            };

            // Get user claims
            var user = await _userManager.GetUserAsync(HttpContext.User);

            // Make sure the user exists
            if (user == null)
            {
                // Return error
                errorResponse.ErrorMessage = "Utilizator nu a fost gasit!";
                return JsonConvert.SerializeObject(errorResponse);
            }

            // Make sure users match via email
            if (!user.Email.Equals(updateProfile.CurrentEmail))
            {
                errorResponse.ErrorMessage = "Utilizator nu a fost gasit";
                return JsonConvert.SerializeObject(errorResponse);
            }


            // Update new user data
            // Check if your email needs updating
            if (!string.IsNullOrWhiteSpace(updateProfile.NewEmail))
            {
                // Get user with this new email
                var userFound = await _userManager.FindByEmailAsync(updateProfile.NewEmail);

                // Check if there is a user with such an email
                if (userFound != null)
                {
                    // Return error
                    errorResponse.ErrorMessage = "Astfel de email exista deja!";
                    return JsonConvert.SerializeObject(errorResponse);
                }

                // Updates the email
                user.Email = updateProfile.NewEmail;
                
                // Updates the user name
                user.UserName = updateProfile.NewEmail;
            }

            // Check if your first name needs updating
            if (!string.IsNullOrWhiteSpace(updateProfile.FirstName))
                // Updates the first name
                user.FirstName = updateProfile.FirstName;
            
            // Check if your last name needs updating
            if (!string.IsNullOrWhiteSpace(updateProfile.LastName))
                // Updates the last name
                user.LastName = updateProfile.LastName;

            // Update the new user data in the database
            var result = await _userManager.UpdateAsync(user);

            // Find the user with the updated data
            var userIdentity = await _userManager.FindByEmailAsync(user.Email);

            // Check if the data was not successfully updated or the user was not found
            if (!result.Succeeded || userIdentity == null)
            {
                errorResponse.ErrorMessage = "Nu sa putut actualiza datele.";
                return JsonConvert.SerializeObject(errorResponse);
            }

            // If the data has been updated and the user has been found returns a token
            return JsonConvert.SerializeObject(new ApiResponse<string>
            {
                // Send the user's token
                Response = userIdentity.GenerateJwtToken()
            });
        }

        /// <summary>
        /// Returns the users update profile details 
        /// </summary>
        /// <returns></returns>
        [Route(ApiRoutes.UpdateUserPassword)]
        [AuthorizeToken]
        public async Task<string> UpdateUserPasswordAsync([FromBody] UpdateUserPasswordApiModel updatePassword)
        {
            _log.LogWarning("UpdateUserPasswordAsync - " + HttpContext.User.Identity.Name);
            // Get user claims
            var user = await _userManager.GetUserAsync(HttpContext.User);

            // If we have no user...
            if (user == null)
            {
                // Return error
                return JsonConvert.SerializeObject(new ApiResponse
                {
                    ErrorMessage = "Utilizator nu a fost gasit!"
                });
            }
             
            // Attempt to commit changes to data store
            var result = await _userManager.ChangePasswordAsync(user, updatePassword.CurrentPassword, updatePassword.NewPassword);
            
            if (!result.Succeeded)
            {
                // Aggregate all errors into a single error string
                var error = result.Errors.ToList()
                              // Grab their description
                              .Select(f => f.Description)
                              // And combine them with a newline separator
                              .Aggregate((a, b) => $"{a}{Environment.NewLine}{b}");

                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(error);
                Console.ResetColor();
                _log.LogError(error);

                // Return the failed response
                return JsonConvert.SerializeObject(new ApiResponse
                {
                    ErrorMessage = error
                });
            }

            
            return JsonConvert.SerializeObject(new ApiResponse());
            
        }
        #endregion

        #region Contact

        #region Search Users

        [AllowAnonymous]
        [Route(ApiRoutes.SearchUsers)]
        public async Task<string> SearchUsersAsync([FromBody]SearchUserApiModel model)
        {
            // Create a found user variable
            var foundUser = default(Users);

            // If we have a email...
            if (!string.IsNullOrWhiteSpace(model.Email))
                // Find the user by email
                foundUser = await _userManager.FindByEmailAsync(model.Email);
            
            // If we found a user...
            if (foundUser != null)
            {
                // Return that users details
                return JsonConvert.SerializeObject(new ApiResponse<SearchUsersResultsApiModel>
                {
                    Response = new SearchUsersResultsApiModel
                        {
                            new SearchUserApiModel
                            {
                                Email = foundUser.Email,
                                FirstName = foundUser.FirstName,
                                LastName = foundUser.LastName,
                            }
                        }
                });
            }
            var firstOrLastNameMissing = string.IsNullOrWhiteSpace(model?.FirstName) || string.IsNullOrWhiteSpace(model?.LastName);

            var results = new SearchUsersResultsApiModel();

            // If we have a first and last name...
            if (!firstOrLastNameMissing)
            {
                // Search for users...
                var foundUsers = _userManager.Users.Where(u =>
                                    // With the same first name
                                    u.FirstName == model.FirstName ||
                                    // And same last name
                                    u.LastName == model.LastName)
                                    // And for now, limit to 100 results
                                    // TODO: Add pagination
                                    .Take(100);

                // If we found any users...
                if (foundUsers.Any())
                {
                    // Add each users details
                    results.AddRange(foundUsers.Select(u => new SearchUserApiModel
                    {
                        Email = u.Email,
                        FirstName = u.FirstName,
                        LastName = u.LastName
                    }));
                }
            }

            // Return the results
            return JsonConvert.SerializeObject(new ApiResponse<SearchUsersResultsApiModel>
            {
                Response = results
            });
        }

            #endregion

            #region Friends

        [Route(ApiRoutes.GetFriends)]
        [AuthorizeToken]
        public async Task<string> GetFriendsAsync()
        {
            _log.LogWarning("GetFriendsAsync - " + HttpContext.User.Identity.Name);
            // Get user claims
            var user = await _userManager.GetUserAsync(HttpContext.User);

            var errorMessage = "Utilizator nu a fost gasit";

            var errorResponse = new ApiResponse<FriendsResultsApiModel>
            {
                // Set error message
                ErrorMessage = errorMessage
            };

            // If we have no user...
            if (user == null)
                // Return error
                return JsonConvert.SerializeObject(errorResponse);

            var results = new FriendsResultsApiModel();

            // Get from database
            try
            {
                connection.Open();
                SqlCommand cmd = connection.CreateCommand();

                cmd.Parameters.Add(
                "@CurrentUserId", SqlDbType.NVarChar).Value = user.Id;

                cmd.CommandText = "SELECT [dbo].[AspNetUsers].[Email], [dbo].[AspNetUsers].[FirstName], [dbo].[AspNetUsers].[LastName] " +
                "FROM [dbo].[Friends] INNER JOIN [dbo].[AspNetUsers] ON ( ([dbo].[Friends].[RequestedBy_Id] = [dbo].[AspNetUsers].[Id] AND " +
                "[dbo].[Friends].[RequestedTo_Id] = @CurrentUserId) OR ([dbo].[Friends].[RequestedTo_Id] = [dbo].[AspNetUsers].[Id] AND " +
                "[dbo].[Friends].[RequestedBy_Id] = @CurrentUserId) ) " +
                "WHERE [dbo].[Friends].[FriendRequestFlag] = 1";
                SqlDataReader dataReader = cmd.ExecuteReader();
                while (dataReader.Read())
                {
                    results.Add(
                        new FriendApiModel()
                        {
                            Email = dataReader["Email"].ToString(),
                            FirstName = dataReader["FirstName"].ToString(),
                            LastName = dataReader["LastName"].ToString(),
                            Status = ChatHub._connections.GetOnline().Contains(dataReader["Email"].ToString())
                        });

                }
                connection.Close();
            }
            catch(Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ex.Message);
                Console.ResetColor();

                errorResponse.ErrorMessage = ex.Message;
                _log.LogError(ex.Message);
                return JsonConvert.SerializeObject(errorResponse);
            }
            
            // Return the results
            return JsonConvert.SerializeObject(new ApiResponse<FriendsResultsApiModel>
            {
                Response = results
            });
        }
            #endregion

        #endregion

        #region Get Messages

        [Route(ApiRoutes.GetMessage)]
        [AuthorizeToken]
        public async Task<string> GetMessageAsync()
        {
            _log.LogWarning("GetMessageAsync - " + HttpContext.User.Identity.Name);
            // Get user claims
            var user = await _userManager.GetUserAsync(HttpContext.User);

            var errorMessage = "Utilizator nu a fost gasit";
            var errorResponse = new ApiResponse<GetMessagesApiModels>
            {
                // Set error message
                ErrorMessage = errorMessage
            };

            // If we have no user...
            if (user == null)
                // Return error
                return JsonConvert.SerializeObject(errorResponse);

            var results = new GetMessagesApiModels();

            // Get from database
            try
            {
                connection.Open();

                SqlCommand cmd = connection.CreateCommand();

                cmd.Parameters.Add(
                "@CurrentUserId", SqlDbType.NVarChar).Value = user.Id;

                cmd.CommandText = "SELECT [dbo].[AspNetUsers].[Email], [dbo].[Messages].[SendTo_Id], [dbo].[Messages].[Message], [dbo].[Messages].[MessageSentTime] " +
                "FROM [dbo].[AspNetUsers] INNER JOIN [dbo].[Messages] ON [dbo].[AspNetUsers].[Id] = [dbo].[Messages].[SendBy_Id] " +
                "WHERE [dbo].[Messages].[SendBy_Id] = @CurrentUserId OR [dbo].[Messages].[SendTo_Id] = @CurrentUserId";

                SqlDataReader dataReader = cmd.ExecuteReader();
                
                while (dataReader.Read())
                {
                    var send_toEmail = _userManager.FindByIdAsync(dataReader["SendTo_Id"].ToString()).Result.Email;
                    results.Add(
                        new MessageApiModel()
                        {
                            SendBy_Email = dataReader["Email"].ToString(),
                            SendTo_Email = send_toEmail,
                            Message = dataReader["Message"].ToString(),
                            MessageSentTime = DateTimeOffset.Parse(dataReader["MessageSentTime"].ToString())
                        });

                }

                connection.Close();
            }
            catch(Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ex.Message);
                Console.ResetColor();
                _log.LogError(ex.Message);

                errorResponse.ErrorMessage = "Nu sa sa putut extrage mesajele!";
                return JsonConvert.SerializeObject(errorResponse);
            }
            
            // Return the results
            return JsonConvert.SerializeObject(new ApiResponse<GetMessagesApiModels>
            {
                Response = results
            });
        }

        #endregion

    }
}