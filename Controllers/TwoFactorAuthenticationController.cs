using System;
using System.Text.RegularExpressions; 
using System.Diagnostics.CodeAnalysis;
using Orchard.Localization;
using System.Web.Mvc;
using System.Web.Security;
using System.Collections.Generic;
using System.Linq;
using Orchard.Logging;
using Orchard.Mvc;
using Orchard.Mvc.Extensions;
using Orchard.Security;
using Orchard.Themes;
using Orchard.Users.Services;
using Orchard.ContentManagement;
using Orchard.Users.Models;
using Orchard.UI.Notify;
using Orchard.Users.Events;
using Orchard.Utility.Extensions;
using Orchard;
using Orchard.UI.Admin;
using EmeraldElements.TwoFactorAuthentication.ViewModels;
using EmeraldElements.TwoFactorAuthentication.Services;
using EmeraldElements.TwoFactorAuthentication.Models;
using Orchard.Messaging.Services;
using Orchard.Email.Services;
using Orchard.Environment.Features;

namespace EmeraldElements.TwoFactorAuthentication.Controllers {
    [HandleError, Themed]
    public class TwoFactorAuthenticationController : Controller {
        private readonly IAuthenticationService _authenticationService;
        private readonly IMembershipService _membershipService;
        private readonly IUserService _userService;
        private readonly IOrchardServices _orchardServices;
        private readonly IUserEventHandler _userEventHandler;
        private readonly ITwoFactorAuthenticator _twoFactorAuthenticator;
        private readonly INotifier _notifier;
        private readonly IMessageService _messageService;
        private readonly IFeatureManager _featureManager;

        public TwoFactorAuthenticationController(
            IAuthenticationService authenticationService, 
            IMembershipService membershipService,
            IUserService userService, 
            IOrchardServices orchardServices,
            IUserEventHandler userEventHandler,
            ITwoFactorAuthenticator twoFactorAuthenticator,
            INotifier notifier,
            IMessageService messageService,
            IFeatureManager featureManager) {
            _authenticationService = authenticationService;
            _membershipService = membershipService;
            _userService = userService;
            _orchardServices = orchardServices;
            _userEventHandler = userEventHandler;
            _twoFactorAuthenticator = twoFactorAuthenticator;
            _notifier = notifier;
            _messageService = messageService;
            _featureManager = featureManager;
            Logger = NullLogger.Instance;
            T = NullLocalizer.Instance;
        }

        public ILogger Logger { get; set; }
        public Localizer T { get; set; }

        ////Display provisioning image
        //[Admin]
        //public FileContentResult DisplayProvImg(string img)
        //{
        //    byte[] imgBytes = System.Text.Encoding.UTF8.GetBytes(img);
        //    return imgBytes != null ? new FileContentResult(imgBytes, "image/png") : null;
        //}

        [AlwaysAccessible]
        public ActionResult AccessDenied() {
            var returnUrl = Request.QueryString["ReturnUrl"];
            var currentUser = _authenticationService.GetAuthenticatedUser();

            if (currentUser == null) {
                Logger.Information("Access denied to anonymous request on {0}", returnUrl);
                var shape = _orchardServices.New.LogOn().Title(T("Access Denied").Text);
                return new ShapeResult(this, shape); 
            }

            //TODO: (erikpo) Add a setting for whether or not to log access denieds since these can fill up a database pretty fast from bots on a high traffic site
            //Suggestion: Could instead use the new AccessDenined IUserEventHandler method and let modules decide if they want to log this event?
            Logger.Information("Access denied to user #{0} '{1}' on {2}", currentUser.Id, currentUser.UserName, returnUrl);

            _userEventHandler.AccessDenied(currentUser);

            return View();
        }

        [AlwaysAccessible]
        public ActionResult LogOn(string returnUrl) {
            if (_authenticationService.GetAuthenticatedUser() != null)
                return this.RedirectLocal(returnUrl);

            Session["user"] = null;
            Session["rememberMe"] = false;
            Session["displayTFAChallenge"] = false;           

            var shape = _orchardServices.New.LogOn().Title(T("Log On").Text);
            return new ShapeResult(this, shape); 
        }

        [HttpPost, ActionName("LogOn")]
        [AlwaysAccessible]
        [ValidateInput(false)]
        [SuppressMessage("Microsoft.Design", "CA1054:UriParametersShouldNotBeStrings",
            Justification = "Needs to take same parameter type as Controller.Redirect()")]
        public ActionResult ValidateTFALogin(string tfacode, string returnUrl)
        {            
            var user = (IUser)Session["user"];  
            var userTFA = user.As<TwoFactorAuthenticationPart>();
            bool rememberMe = Session["rememberMe"] != null ? (bool)Session["rememberMe"] : false;
           
            ValidateTFA(user, tfacode);
            if (!ModelState.IsValid)
            {
                var shape = _orchardServices.New.LogOn().Title(T("Log On").Text);
                return new ShapeResult(this, shape);
            }               

            _authenticationService.SignIn(user, rememberMe);
            _userEventHandler.LoggedIn(user);

            return this.RedirectLocal(returnUrl);
        }

        [HttpPost]
        [AlwaysAccessible]
        [ValidateInput(false)]
        [SuppressMessage("Microsoft.Design", "CA1054:UriParametersShouldNotBeStrings",
            Justification = "Needs to take same parameter type as Controller.Redirect()")]
        public ActionResult CheckTFALogin(string userNameOrEmail, string password, string returnUrl, bool rememberMe = false)
        {
            _userEventHandler.LoggingIn(userNameOrEmail, password);

            var user = ValidateLogOn(userNameOrEmail, password);
            if (!ModelState.IsValid)
            {
                //var errors = ModelState.Keys.SelectMany(k => ModelState[k].Errors).Select(m => m.ErrorMessage).ToArray();
                var errorList = (from item in ModelState
                                 where item.Value.Errors.Any()
                                 select item.Value.Errors[0].ErrorMessage).ToList();
                return Json(new { error = true, errors = errorList });
            }

            //Handle TFA
            var userTFA = user.As<TwoFactorAuthenticationPart>();
            var settings = _orchardServices.WorkContext.CurrentSite.As<TwoFactorAuthenticationSettingsPart>();            

            if (!userTFA.CanLoginWithoutTFA && (settings.RequireTFA || userTFA.EnableTFA))
            {
                Session["user"] = user;
                Session["rememberMe"] = rememberMe;
                Session["displayTFAChallenge"] = true;                
                return Json(new { displayTFAChallenge = "True", error = false });                
            }         
   
            //Messages
            if (userTFA.CanLoginWithoutTFA && (settings.RequireTFA || userTFA.EnableTFA))
            {
                _notifier.Information(T("An admin has allowed you to login without Two-Factor Authentication."));

                if (settings.RequireTFA)
                    _notifier.Information(T("Two-Factor Authentication is required. Please ensure TFA has been set up prior to logging out."));
                else
                    _notifier.Information(T("Please disable or ensure correct setup of TFA prior to logging out."));

                //Some way of not hardcoding this???
                returnUrl = "/Admin/MyProfile";
            }      

            _authenticationService.SignIn(user, rememberMe);
            _userEventHandler.LoggedIn(user);
                      
            return Json(new { displayTFAChallenge = "False", returnUrl = returnUrl, error = false });
        }

        public ActionResult GenerateNewKey()
        {
            var userTFA = _orchardServices.WorkContext.CurrentUser.As<TwoFactorAuthenticationPart>();

            //Key
            var newKey = _twoFactorAuthenticator.GenerateKey();            

            userTFA.Record.SecretKey = newKey;
            userTFA.Record.HasVerifiedKey = false;

            var newKeyString = _twoFactorAuthenticator.EncodeKey(newKey);

            //QR_Code
            var identifier = _orchardServices.WorkContext.CurrentSite.SiteName + ":" + _orchardServices.WorkContext.CurrentUser.UserName;

            var newQRCode = _twoFactorAuthenticator.GenerateProvisioningImage(identifier, userTFA.SecretKey, 250, 250);
            var newQRCodeData = System.Convert.ToBase64String(newQRCode);

            return Json(new { newKey = newKeyString, newQRCode = newQRCodeData });
        }

        public ActionResult SendBackupPin()
        {
            var user = (IUser)Session["user"];
            if (user == null)
                return new EmptyResult();

            var userTFA = user.As<TwoFactorAuthenticationPart>();
            var pin = _twoFactorAuthenticator.GeneratePin(userTFA.SecretKey);
            var siteTFA = _orchardServices.WorkContext.CurrentSite.As<TwoFactorAuthenticationSettingsPart>();            
            var sendMethod = "";
            
            if (siteTFA.EnableBackupSMS && userTFA.EnableBackupSMS && !String.IsNullOrEmpty(userTFA.BackupSMSNum))
            {
                //Send SMS
                sendMethod = "sms: " + userTFA.BackupSMSNum;
                SendBackupSMS(userTFA.BackupSMSNum, pin);
            }
            else if (!String.IsNullOrEmpty(user.Email))
            {
                //Send email
                sendMethod = "email: " + user.Email;
                SendBackupEmail(user.Email, pin);
            }
            else
            {
                var errorList = new List<LocalizedString>();
                errorList.Add(T("Error sending backup pin."));
                return Json(new { error = true, errors = errorList });
            }

            var infoList = new List<LocalizedString>();
            infoList.Add(T("Backup Pin sent via {0}", sendMethod));
            return Json(new { error = false, infos = infoList});
        }

        private void SendBackupSMS(string userPhone, string pin)
        {
            var parameters = new Dictionary<string, object> {                
                {"Body", "Temporary Pin: " + pin},
                {"Recipients", userPhone}        
            };
           
            _messageService.Send(SmsMessageChannel.MessageType, parameters);
        }

        private void SendBackupEmail(string userEmail, string pin)
        {                     
            var parameters = new Dictionary<string, object> {
                {"Subject", "Two-Factor Authentication"},
                {"Body", "Temporary Pin: " + pin},
                {"Recipients", userEmail}        
            };

            _messageService.Send(SmtpMessageChannel.MessageType, parameters);                      
        }

        # region Unused Methods
        //public ActionResult LogOff(string returnUrl) {
        //    var wasLoggedInUser = _authenticationService.GetAuthenticatedUser();
        //    _authenticationService.SignOut();
        //    if (wasLoggedInUser != null)
        //        _userEventHandler.LoggedOut(wasLoggedInUser);
        //    return this.RedirectLocal(returnUrl);
        //}

        //int MinPasswordLength {
        //    get {
        //        return _membershipService.GetSettings().MinRequiredPasswordLength;
        //    }
        //}

        //[AlwaysAccessible]
        //public ActionResult Register() {
        //    // ensure users can register
        //    var registrationSettings = _orchardServices.WorkContext.CurrentSite.As<RegistrationSettingsPart>();
        //    if ( !registrationSettings.UsersCanRegister ) {
        //        return HttpNotFound();
        //    }

        //    ViewData["PasswordLength"] = MinPasswordLength;

        //    var shape = _orchardServices.New.Register();
        //    return new ShapeResult(this, shape); 
        //}

        //[HttpPost]
        //[AlwaysAccessible]
        //[ValidateInput(false)]
        //public ActionResult Register(string userName, string email, string password, string confirmPassword, string returnUrl = null) {
        //    // ensure users can register
        //    var registrationSettings = _orchardServices.WorkContext.CurrentSite.As<RegistrationSettingsPart>();
        //    if ( !registrationSettings.UsersCanRegister ) {
        //        return HttpNotFound();
        //    }

        //    ViewData["PasswordLength"] = MinPasswordLength;

        //    if (ValidateRegistration(userName, email, password, confirmPassword)) {
        //        // Attempt to register the user
        //        // No need to report this to IUserEventHandler because _membershipService does that for us
        //        var user = _membershipService.CreateUser(new CreateUserParams(userName, password, email, null, null, false));

        //        if (user != null) {
        //            if ( user.As<UserPart>().EmailStatus == UserStatus.Pending ) {
        //                var siteUrl = _orchardServices.WorkContext.CurrentSite.BaseUrl;
        //                if(String.IsNullOrWhiteSpace(siteUrl)) {
        //                    siteUrl = HttpContext.Request.ToRootUrlString();
        //                }

        //                _userService.SendChallengeEmail(user.As<UserPart>(), nonce => Url.MakeAbsolute(Url.Action("ChallengeEmail", "Account", new {Area = "Orchard.Users", nonce = nonce}), siteUrl));

        //                _userEventHandler.SentChallengeEmail(user);
        //                return RedirectToAction("ChallengeEmailSent");
        //            }

        //            if (user.As<UserPart>().RegistrationStatus == UserStatus.Pending) {
        //                return RedirectToAction("RegistrationPending");
        //            }

        //            _authenticationService.SignIn(user, false /* createPersistentCookie */);
        //            return this.RedirectLocal(returnUrl);
        //        }
                
        //        ModelState.AddModelError("_FORM", T(ErrorCodeToString(/*createStatus*/MembershipCreateStatus.ProviderError)));
        //    }

        //    // If we got this far, something failed, redisplay form
        //    var shape = _orchardServices.New.Register();
        //    return new ShapeResult(this, shape); 
        //}

        //[AlwaysAccessible]
        //public ActionResult RequestLostPassword() {
        //    // ensure users can request lost password
        //    var registrationSettings = _orchardServices.WorkContext.CurrentSite.As<RegistrationSettingsPart>();
        //    if ( !registrationSettings.EnableLostPassword ) {
        //        return HttpNotFound();
        //    }

        //    return View();
        //}

        //[HttpPost]
        //[AlwaysAccessible]
        //public ActionResult RequestLostPassword(string username) {
        //    // ensure users can request lost password
        //    var registrationSettings = _orchardServices.WorkContext.CurrentSite.As<RegistrationSettingsPart>();
        //    if ( !registrationSettings.EnableLostPassword ) {
        //        return HttpNotFound();
        //    }

        //    if(String.IsNullOrWhiteSpace(username)){
        //        ModelState.AddModelError("userNameOrEmail", T("Invalid username or E-mail."));
        //        return View();
        //    }

        //    var siteUrl = _orchardServices.WorkContext.CurrentSite.BaseUrl;
        //    if (String.IsNullOrWhiteSpace(siteUrl)) {
        //        siteUrl = HttpContext.Request.ToRootUrlString();
        //    }

        //    _userService.SendLostPasswordEmail(username, nonce => Url.MakeAbsolute(Url.Action("LostPassword", "Account", new { Area = "Orchard.Users", nonce = nonce }), siteUrl));

        //    _orchardServices.Notifier.Information(T("Check your e-mail for the confirmation link."));
            
        //    return RedirectToAction("LogOn");
        //}

        //[Authorize]
        //[AlwaysAccessible]
        //public ActionResult ChangePassword() {
        //    ViewData["PasswordLength"] = MinPasswordLength;

        //    return View();
        //}

        //[Authorize]
        //[HttpPost]
        //[AlwaysAccessible]
        //[ValidateInput(false)]
        //[SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes",
        //    Justification = "Exceptions result in password not being changed.")]
        //public ActionResult ChangePassword(string currentPassword, string newPassword, string confirmPassword) {
        //    ViewData["PasswordLength"] = MinPasswordLength;

        //    if ( !ValidateChangePassword(currentPassword, newPassword, confirmPassword) ) {
        //        return View();
        //    }

        //    try {
        //        var validated = _membershipService.ValidateUser(User.Identity.Name, currentPassword);

        //        if ( validated != null ) {
        //            _membershipService.SetPassword(validated, newPassword);
        //            _userEventHandler.ChangedPassword(validated);
        //            return RedirectToAction("ChangePasswordSuccess");
        //        }
                
        //        ModelState.AddModelError("_FORM",
        //                                 T("The current password is incorrect or the new password is invalid."));
        //        return ChangePassword();
        //    } catch {
        //        ModelState.AddModelError("_FORM", T("The current password is incorrect or the new password is invalid."));
        //        return ChangePassword();
        //    }
        //}

        //public ActionResult LostPassword(string nonce) {
        //    if ( _userService.ValidateLostPassword(nonce) == null ) {
        //        return RedirectToAction("LogOn");
        //    }

        //    ViewData["PasswordLength"] = MinPasswordLength;

        //    return View();
        //}

        //[HttpPost]
        //[ValidateInput(false)]
        //public ActionResult LostPassword(string nonce, string newPassword, string confirmPassword) {
        //    IUser user;
        //    if ( (user = _userService.ValidateLostPassword(nonce)) == null ) {
        //        return Redirect("~/");
        //    }

        //    ViewData["PasswordLength"] = MinPasswordLength;

        //    if (newPassword == null || newPassword.Length < MinPasswordLength) {
        //        ModelState.AddModelError("newPassword", T("You must specify a new password of {0} or more characters.", MinPasswordLength));
        //    }

        //    if (!String.Equals(newPassword, confirmPassword, StringComparison.Ordinal)) {
        //        ModelState.AddModelError("_FORM", T("The new password and confirmation password do not match."));
        //    }

        //    if (!ModelState.IsValid) {
        //        return View();
        //    }

        //    _membershipService.SetPassword(user, newPassword);

        //    _userEventHandler.ChangedPassword(user);

        //    return RedirectToAction("ChangePasswordSuccess");
        //}

        //public ActionResult ChangePasswordSuccess() {
        //    return View();
        //}

        //public ActionResult RegistrationPending() {
        //    return View();
        //}

        //public ActionResult ChallengeEmailSent() {
        //    return View();
        //}

        //public ActionResult ChallengeEmailSuccess() {
        //    return View();
        //}

        //public ActionResult ChallengeEmailFail() {
        //    return View();
        //}

        //public ActionResult ChallengeEmail(string nonce) {
        //    var user = _userService.ValidateChallenge(nonce);

        //    if ( user != null ) {
        //        _userEventHandler.ConfirmedEmail(user);

        //        return RedirectToAction("ChallengeEmailSuccess");
        //    }

        //    return RedirectToAction("ChallengeEmailFail");
        //}
        #endregion

        #region Validation Methods
        //private bool ValidateChangePassword(string currentPassword, string newPassword, string confirmPassword) {
        //    if ( String.IsNullOrEmpty(currentPassword) ) {
        //        ModelState.AddModelError("currentPassword", T("You must specify a current password."));
        //    }
        //    if ( newPassword == null || newPassword.Length < MinPasswordLength ) {
        //        ModelState.AddModelError("newPassword", T("You must specify a new password of {0} or more characters.", MinPasswordLength));
        //    }

        //    if ( !String.Equals(newPassword, confirmPassword, StringComparison.Ordinal) ) {
        //        ModelState.AddModelError("_FORM", T("The new password and confirmation password do not match."));
        //    }

        //    return ModelState.IsValid;
        //}


        private IUser ValidateLogOn(string userNameOrEmail, string password) {
            bool validate = true;

            if (String.IsNullOrEmpty(userNameOrEmail)) {
                ModelState.AddModelError("userNameOrEmail", T("You must specify a username or e-mail."));
                validate = false;
            }
            if (String.IsNullOrEmpty(password)) {
                ModelState.AddModelError("password", T("You must specify a password."));
                validate = false;
            }

            if (!validate)
                return null;

            var user = _membershipService.ValidateUser(userNameOrEmail, password);
            if (user == null) {
                _userEventHandler.LogInFailed(userNameOrEmail, password);
                ModelState.AddModelError("_FORM", T("The username or e-mail or password provided is incorrect."));
            }

            return user;
        }

        private void ValidateTFA(IUser user, string tfaCode)
        {   
            //TODO: need null check on user??? Should it ever reach here with the user as null???
            var userTFA = user.As<TwoFactorAuthenticationPart>();
            var isValidCode = _twoFactorAuthenticator.IsValid(userTFA.SecretKey, tfaCode, 1);

            if (!isValidCode)
            {
                ModelState.AddModelError("tfacode", T("Invalid TFA Code."));
            }

            //return isValidCode;
        }

        //private bool ValidateRegistration(string userName, string email, string password, string confirmPassword) {
        //    bool validate = true;

        //    if (String.IsNullOrEmpty(userName)) {
        //        ModelState.AddModelError("username", T("You must specify a username."));
        //        validate = false;
        //    }
        //    else {
        //        if (userName.Length >= 255) {
        //            ModelState.AddModelError("username", T("The username you provided is too long."));
        //            validate = false;
        //        }
        //    }

        //    if (String.IsNullOrEmpty(email)) {
        //        ModelState.AddModelError("email", T("You must specify an email address."));
        //        validate = false;
        //    }
        //    else if (email.Length >= 255) {
        //        ModelState.AddModelError("email", T("The email address you provided is too long."));
        //        validate = false;
        //    }
        //    else if (!Regex.IsMatch(email, UserPart.EmailPattern, RegexOptions.IgnoreCase)) {
        //        // http://haacked.com/archive/2007/08/21/i-knew-how-to-validate-an-email-address-until-i.aspx    
        //        ModelState.AddModelError("email", T("You must specify a valid email address."));
        //        validate = false;
        //    }

        //    if (!validate)
        //        return false;

        //    if (!_userService.VerifyUserUnicity(userName, email)) {
        //        ModelState.AddModelError("userExists", T("User with that username and/or email already exists."));
        //    }
        //    if (password == null || password.Length < MinPasswordLength) {
        //        ModelState.AddModelError("password", T("You must specify a password of {0} or more characters.", MinPasswordLength));
        //    }
        //    if (!String.Equals(password, confirmPassword, StringComparison.Ordinal)) {
        //        ModelState.AddModelError("_FORM", T("The new password and confirmation password do not match."));
        //    }
        //    return ModelState.IsValid;
        //}

        private static string ErrorCodeToString(MembershipCreateStatus createStatus) {
            // See http://msdn.microsoft.com/en-us/library/system.web.security.membershipcreatestatus.aspx for
            // a full list of status codes.
            switch (createStatus) {
                case MembershipCreateStatus.DuplicateUserName:
                    return "Username already exists. Please enter a different user name.";

                case MembershipCreateStatus.DuplicateEmail:
                    return "A username for that e-mail address already exists. Please enter a different e-mail address.";

                case MembershipCreateStatus.InvalidPassword:
                    return "The password provided is invalid. Please enter a valid password value.";

                case MembershipCreateStatus.InvalidEmail:
                    return "The e-mail address provided is invalid. Please check the value and try again.";

                case MembershipCreateStatus.InvalidAnswer:
                    return "The password retrieval answer provided is invalid. Please check the value and try again.";

                case MembershipCreateStatus.InvalidQuestion:
                    return "The password retrieval question provided is invalid. Please check the value and try again.";

                case MembershipCreateStatus.InvalidUserName:
                    return "The user name provided is invalid. Please check the value and try again.";

                case MembershipCreateStatus.ProviderError:
                    return
                        "The authentication provider returned an error. Please verify your entry and try again. If the problem persists, please contact your system administrator.";

                case MembershipCreateStatus.UserRejected:
                    return
                        "The user creation request has been canceled. Please verify your entry and try again. If the problem persists, please contact your system administrator.";

                default:
                    return
                        "An unknown error occurred. Please verify your entry and try again. If the problem persists, please contact your system administrator.";
            }
        }

        #endregion
    }
}