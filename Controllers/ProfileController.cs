using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web.Mvc;
using System.Web.Routing;
using Orchard.ContentManagement;
using Orchard.Core.Settings.Models;
using Orchard.DisplayManagement;
using Orchard.Localization;
using Orchard.Mvc;
using Orchard.Security;
using Orchard.UI.Notify;
using Orchard.Users.Events;
using Orchard.Users.Models;
using Orchard.Users.Services;
using Orchard.Users.ViewModels;
using Orchard.Mvc.Extensions;
using System;
using Orchard.Settings;
using Orchard.UI.Navigation;
using Orchard.Utility.Extensions;
using Orchard;
using Orchard.UI.Admin;
using Orchard.Environment.Extensions;

namespace EmeraldElements.TwoFactorAuthentication.Controllers {
    [Admin]
    [ValidateInput(false)]
    [OrchardFeature("EmeraldElements.TwoFactorAuthentication.UserProfile")]
    public class ProfileController : Controller, IUpdateModel {
        private readonly IMembershipService _membershipService;
        private readonly IUserService _userService;
        private readonly IUserEventHandler _userEventHandlers;
        private readonly ISiteService _siteService;

        public ProfileController(
            IOrchardServices services,
            IMembershipService membershipService,
            IUserService userService,
            IShapeFactory shapeFactory,
            IUserEventHandler userEventHandlers,
            ISiteService siteService) {
            Services = services;
            _membershipService = membershipService;
            _userService = userService;
            _userEventHandlers = userEventHandlers;
            _siteService = siteService;

            T = NullLocalizer.Instance;
            Shape = shapeFactory;
        }

        dynamic Shape { get; set; }
        public IOrchardServices Services { get; set; }
        public Localizer T { get; set; }        

        public ActionResult Index() {
            var id = Services.WorkContext.CurrentUser.Id;
            var user = Services.ContentManager.Get<UserPart>(id);
            var editor = Shape.EditorTemplate(TemplateName: "Parts/User.Edit", Model: new UserEditViewModel {User = user}, Prefix: null);
            editor.Metadata.Position = "2";
            var model = Services.ContentManager.BuildEditor(user);        
            model.Content.Add(editor);

            ////Remove UserRoles
            //foreach (var item in model.Content.Items)
            //{
            //    if (item.Prefix == "UserRoles")
            //    {
            //        model.Content.Items.Remove(item);
            //        break;
            //    }
            //}

            return View(model);
        }

        [HttpPost, ActionName("Index")]
        public ActionResult IndexPost() {
            var id = Services.WorkContext.CurrentUser.Id;
            var user = Services.ContentManager.Get<UserPart>(id, VersionOptions.DraftRequired);
            string previousName = user.UserName;

            var model = Services.ContentManager.UpdateEditor(user, this);

            var editModel = new UserEditViewModel {User = user};
            if (TryUpdateModel(editModel)) {
                if (!_userService.VerifyUserUnicity(id, editModel.UserName, editModel.Email)) {
                    AddModelError("NotUniqueUserName", T("User with that username and/or email already exists."));
                }
                else if (!Regex.IsMatch(editModel.Email ?? "", UserPart.EmailPattern, RegexOptions.IgnoreCase)) {
                    // http://haacked.com/archive/2007/08/21/i-knew-how-to-validate-an-email-address-until-i.aspx    
                    ModelState.AddModelError("Email", T("You must specify a valid email address."));
                }
                else {
                    // also update the Super user if this is the renamed account
                    if (String.Equals(Services.WorkContext.CurrentSite.SuperUser, previousName, StringComparison.Ordinal)) {
                        _siteService.GetSiteSettings().As<SiteSettingsPart>().SuperUser = editModel.UserName;
                    }

                    user.NormalizedUserName = editModel.UserName.ToLowerInvariant();
                }
            }

            if (!ModelState.IsValid) {
                Services.TransactionManager.Cancel();

                var editor = Shape.EditorTemplate(TemplateName: "Parts/User.Edit", Model: editModel, Prefix: null);
                editor.Metadata.Position = "2";
                model.Content.Add(editor);

                ////Remove UserRoles
                //foreach (var item in model.Content.Items)
                //{
                //    if (item.Prefix == "UserRoles")
                //    {
                //        model.Content.Items.Remove(item);
                //        break;
                //    }
                //}

                return View(model);
            }

            Services.ContentManager.Publish(user.ContentItem);

            Services.Notifier.Information(T("User information updated"));
            return RedirectToAction("Index");
        }

        bool IUpdateModel.TryUpdateModel<TModel>(TModel model, string prefix, string[] includeProperties, string[] excludeProperties) {
            return TryUpdateModel(model, prefix, includeProperties, excludeProperties);
        }

        public void AddModelError(string key, LocalizedString errorMessage) {
            ModelState.AddModelError(key, errorMessage.ToString());
        }
    }

}
