#region LICENSE

/*
    
    Copyright(c) Voat, Inc.

    This file is part of Voat.

    This source file is subject to version 3 of the GPL license,
    that is bundled with this package in the file LICENSE, and is
    available online at http://www.gnu.org/licenses/gpl-3.0.txt;
    you may not use this file except in compliance with the License.

    Software distributed under the License is distributed on an
    "AS IS" basis, WITHOUT WARRANTY OF ANY KIND, either express
    or implied. See the License for the specific language governing
    rights and limitations under the License.

    All Rights Reserved.

*/

#endregion LICENSE

using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Builder;
using System;
using Voat.Configuration;
using Voat.Domain.Models;
using Voat.Utilities;
using Microsoft.AspNetCore.Routing.Constraints;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Voat.Data.Models;
using System.Net;
using Microsoft.AspNetCore.Authorization;
using Voat.Data;
using Voat.UI;
using Voat.Controllers;
using Voat.Domain.Command;
using Voat.Domain.Query;
using Voat.Common;
using Voat.Http.Filters;
using Voat.Http;
using Voat.IO.Email;
using System.Linq;

//Github repo: https://github.com/voat/voat

namespace Voat
{
    public class RouteConfig
    {
        public static async Task MainAsync(string[] args)
        {
            VoatUserManager userManager = new VoatUserManager();
            SignInManager<VoatIdentityUser> signInManager = null;

            AccountController accountController;
            accountController = new AccountController(userManager, signInManager);

            Task<ActionResult> asyncVal1;
            asyncVal1 = accountController.ToggleNightMode();

            Task<JsonResult> asyncVal2;
            asyncVal2 = accountController.CheckUsernameAvailability();

            ActionResult val1;
            val1 = await asyncVal1;

            JsonResult val2;
            val2 = await asyncVal2;

        }
    }

    public class AccountController : BaseController
    {
        private readonly VoatUserManager _userManager;
        private readonly SignInManager<VoatIdentityUser> _signInManager;
        private int _statuscode;

        public AccountController(
            VoatUserManager userManager,
            SignInManager<VoatIdentityUser> signInManager
            )
        {
            _userManager = userManager;
            _signInManager = signInManager;

            UserManager = _userManager;
        }

        public VoatUserManager UserManager { get; private set; }

        // POST: /Account/ToggleNightMode
        [Authorize]
        public async Task<ActionResult> ToggleNightMode()
        {
            var q = new QueryUserPreferences().SetUserContext(User);

            Task<Domain.Models.UserPreference> taskPreferences;
            taskPreferences =  q.ExecuteAsync();
            Domain.Models.UserPreference lpreferences;
            lpreferences = await taskPreferences;

            UserPreferenceUpdate newPreferences;
            newPreferences = new Domain.Models.UserPreferenceUpdate();

            Domain.Models.UserPreference preferences;
            preferences = lpreferences;
            newPreferences.NightMode = !preferences.NightMode;

            var cmd = new UpdateUserPreferencesCommand(newPreferences).SetUserContext(User);
            Task<CommandResponse> taskResult;
            taskResult = cmd.Execute();
            CommandResponse result;
            result = await taskResult;

            string newTheme = newPreferences.NightMode.Value ? "dark" : "light";

            UserHelper.SetUserStylePreferenceCookie(HttpContext, newTheme);
            _statuscode = 200;
            Response.StatusCode = _statuscode;
            return Json("Toggled Night Mode" /* CORE_PORT: Removed , JsonRequestBehavior.AllowGet */);
        }      
        

        public async Task<JsonResult> CheckUsernameAvailability()
        {
            //CORE_PORT: Ported correctly?
            //var userNameToCheck = Request.Params["userName"];
            var userNameToCheck = Request.Form["userName"].FirstOrDefault();

            if (userNameToCheck == null)
            {
                Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return Json("A username parameter is required for this function." /* CORE_PORT: Removed , JsonRequestBehavior.AllowGet */);
            }

            // check username availability
            Task<VoatIdentityUser> taskUserNameAvailable;
            taskUserNameAvailable = UserManager.FindByNameAsync(userNameToCheck);
            VoatIdentityUser luserNameAvailable;
            luserNameAvailable = await taskUserNameAvailable;

            VoatIdentityUser userNameAvailable;
            userNameAvailable = luserNameAvailable;
            if (userNameAvailable == null)
            {
                _statuscode = 200;
                Response.StatusCode = _statuscode;
                var response = new UsernameAvailabilityResponse
                {
                    Available = true
                };

                return Json(response /* CORE_PORT: Removed , JsonRequestBehavior.AllowGet */);
            }
            else
            {
                _statuscode = (int)HttpStatusCode.BadRequest;
                Response.StatusCode = _statuscode;
                _statuscode = 200;
                Response.StatusCode = _statuscode;
                var response = new UsernameAvailabilityResponse
                {
                    Available = false
                };
                return Json(response /* CORE_PORT: Removed , JsonRequestBehavior.AllowGet */);
            }
        }

        private class UsernameAvailabilityResponse
        {
            public bool Available { get; set; }
        }
    }
}
