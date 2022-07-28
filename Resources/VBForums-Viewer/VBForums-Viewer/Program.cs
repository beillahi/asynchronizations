using System;
using System.IO;
using System.Net;
using Caliburn.Micro;

// Original Repo: https://github.com/gep13-archive/VBForums-Viewer
// Install-Package Caliburn.Micro -Version 4.0.136-rc

namespace VBForums_Viewer
{
    class Program
    {
        static void Main(string[] args)
        {
            AddAccountViewModel addAccountViewModel = new AddAccountViewModel();

            addAccountViewModel.AuthenticateUserResponse();

            addAccountViewModel.AuthenticateUserResponse();
        }

        private class VBForumsWebService
        {

            public bool IsValidLoginCredential(LoginCredentialModel loginCredential)
            {

                var uri = new Uri("http://www.vbforums.com/login.php?do=login");

                string postString;
                postString =
                    string.Format(
                        "do=login&url=%2Fusercp.php&vb_login_md5password=&vb_login_md5password_utf=&s=&securitytoken=guest&vb_login_username={0}&vb_login_password={1}",
                        loginCredential.UserName,
                        loginCredential.Password);

                HttpWebRequest request;
                request = WebRequest.CreateHttp(uri);

                request.Method = "POST";
                request.ContentType = "application/x-www-form-urlencoded";

                Stream requestSteam;
                requestSteam = request.GetRequestStream();

                Stream requestSteam2;
                requestSteam2 = requestSteam;

                using (var writer = new StreamWriter(requestSteam2))
                {
                    writer.Write(postString);
                }

                WebResponse response;
                response = request.GetResponse(); // exist GetResponseAsync

                WebResponse response2;
                response2 = response;

                HttpWebResponse response3;
                response3 = (HttpWebResponse)response2;

                //var response = (HttpWebResponse) request.GetResponse();

                var statusCode = response3.StatusCode;

                if ((int)statusCode >= 400)
                {
                    return false;
                }

                string responseString;
                using (var responseStream = new StreamReader(response.GetResponseStream()))
                {
                    responseString = responseStream.ReadToEnd();
                }

                string responseString2;
                responseString2 = responseString;

                return !responseString2.Contains("<!-- main error message -->");
            }
        }

        private class AddAccountViewModel
        {
            private VBForumsWebService vBForumsWebService;

            /// <summary>
            /// The user name.
            /// </summary>
            private string userName;

            /// <summary>
            /// The password.
            /// </summary>
            private string password;

            /// <summary>
            /// Local variable for whether the user is authenticated
            /// </summary>
            private bool isAuthenticated;

            /// <summary>
            /// Local variable for displaying the status of the user authentication to the user
            /// </summary>
            private string statusLabel;


            private Boolean IsIndeterminate;
            private Boolean IsVisible;

            private Boolean IsEnabled;

            /// <summary>
            /// Gets or sets the user name.
            /// </summary>
            public string UserName
            {
                get
                {
                    return this.userName;
                }

                set
                {
                    this.userName = value;
                }
            }

            /// <summary>
            /// Gets or sets the password.
            /// </summary>
            public string Password
            {
                get
                {
                    return this.password;
                }

                set
                {
                    this.password = value;
                }
            }

            /// <summary>
            /// Hide the ProgressIndicator on the page
            /// </summary>
            public void Hide()
            {
                this.IsIndeterminate = false;
                this.IsVisible = false;

                this.IsEnabled = false;
            }


            /// <summary>
            /// The authenticate user response.
            /// </summary>
            public void AuthenticateUserResponse()
            {

                bool result;
                result = vBForumsWebService.IsValidLoginCredential(
                        new LoginCredentialModel() { UserName = this.UserName, Password = this.Password });

                this.Hide();

                bool result2;
                result2 = result;

                if (result)
                {
                    statusLabel = "user authenticated successfully";
                    isAuthenticated = true;
                }
                else
                {
                    statusLabel = "unable to authenticate user, please check username and password";
                    isAuthenticated = false;
                }
            }
        }


        private class LoginCredentialModel : BaseModel<LoginCredentialModel>
        {
            /// <summary>
            /// The user name.
            /// </summary>
            private string userName;

            /// <summary>
            /// The password.
            /// </summary>
            private string password;

            /// <summary>
            /// Gets or sets the user name.
            /// </summary>
            public string UserName
            {
                get
                {
                    return this.userName;
                }

                set
                {
                    this.userName = value;
                    this.NotifyOfPropertyChange(() => this.UserName);
                }
            }

            /// <summary>
            /// Gets or sets the password.
            /// </summary>
            public string Password
            {
                get
                {
                    return this.password;
                }

                set
                {
                    this.password = value;
                    this.NotifyOfPropertyChange(() => this.Password);
                }
            }
        }


        /// <summary>
        /// The Base Class definition that all Models will derive from
        /// </summary>
        /// <typeparam name="T">The type that derives from base model</typeparam>
        private class BaseModel<T> : PropertyChangedBase
          where T : BaseModel<T>
        {
            /// <summary>
            /// Gets or sets the Standard Key
            /// </summary>
            public int Id { get; set; }

            /// <summary>
            /// Override the base Equals method and test for equality based on the objects Id property.
            /// </summary>
            /// <param name="obj">The object being tested for equality</param>
            /// <returns>True if the objects are equal, otherwise false.</returns>
            public override bool Equals(object obj)
            {
                return obj is T && ((T)obj).Id.Equals(this.Id);
            }

            /// <summary>
            /// Override the base GetHashCode method and return the Id for the current object
            /// </summary>
            /// <returns>The Id of the current object which will always be unique</returns>
            public override int GetHashCode()
            {
                return this.Id;
            }

            /// <summary>
            /// Override the base ToString method to return sensible information about the current object.
            /// </summary>
            /// <returns>A formatted string with information about the current object</returns>
            public override string ToString()
            {
                return string.Format("{0} with Id {1}", typeof(T).FullName, this.Id);
            }
        }
    }
}
