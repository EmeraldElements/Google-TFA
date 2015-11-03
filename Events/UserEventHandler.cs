using EmeraldElements.TwoFactorAuthentication.Models;
using Orchard;
using Orchard.ContentManagement;
using Orchard.Events;
using Orchard.Security;
using Orchard.Users.Events;
using System;

namespace EmeraldElements.TwoFactorAuthentication.Events {
    public class UserEventHandler : IUserEventHandler {
        private readonly IOrchardServices _services;

        public UserEventHandler(IOrchardServices services)
        {
            _services = services;
        }

        /// <summary>
        /// Called before a User is created
        /// </summary>
        public void Creating(UserContext context)
        {
            return;
        }

        /// <summary>
        /// Called after a user has been created
        /// </summary>
        public void Created(UserContext context)
        {
            return;
        }

        /// <summary>
        /// Called before a user has logged in
        /// </summary>
        public void LoggingIn(string userNameOrEmail, string password)
        {
            
            return;
        }

        /// <summary>
        /// Called after a user has logged in
        /// </summary>
        public void LoggedIn(IUser user)
        {
            //Switch the CanLoginWithoutTFA flag
            _services.WorkContext.CurrentUser.As<TwoFactorAuthenticationPart>().Record.CanLoginWithoutTFA = false;
            return;
        }

        /// <summary>
        /// Called when a login attempt failed
        /// </summary>
        public void LogInFailed(string userNameOrEmail, string password)
        {
            return;
        }

        /// <summary>
        /// Called when a user explicitly logs out (as opposed to one whose session cookie simply expires)
        /// </summary>
        public void LoggedOut(IUser user)
        {
            return;
        }

        /// <summary>
        /// Called when access is denied to a user
        /// </summary>
        public void AccessDenied(IUser user)
        {
            return;
        }

        /// <summary>
        /// Called after a user has changed password
        /// </summary>
        public void ChangedPassword(IUser user)
        {
            return;
        }

        /// <summary>
        /// Called after a user has confirmed their email address
        /// </summary>
        public void SentChallengeEmail(IUser user)
        {
            return;
        }

        /// <summary>
        /// Called after a user has confirmed their email address
        /// </summary>
        public void ConfirmedEmail(IUser user)
        {
            return;
        }

        /// <summary>
        /// Called after a user has been approved
        /// </summary>
        public void Approved(IUser user)
        {
            return;
        }
    }
}

