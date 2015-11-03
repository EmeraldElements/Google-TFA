using System.Collections.Generic;
using System.Net.Mail;
using System.Web.Mvc;
using Orchard.ContentManagement;
using Orchard.Localization;
using Orchard.UI.Admin.Notification;
using Orchard.UI.Notify;
using Orchard.Environment.Extensions;
using Orchard;
using EmeraldElements.TwoFactorAuthentication.Models;

namespace EmeraldElements.TwoFactorAuthentication {
    [OrchardFeature("EmeraldElements.TwoFactorAuthentication.Sms")]
    public class MissingSettingsBanner : INotificationProvider {
        private readonly IOrchardServices _orchardServices;

        public MissingSettingsBanner(IOrchardServices orchardServices) {
            _orchardServices = orchardServices;
            T = NullLocalizer.Instance;
        }

        public Localizer T { get; set; }

        public IEnumerable<NotifyEntry> GetNotifications() {
            var workContext = _orchardServices.WorkContext;
            var smsSettings = workContext.CurrentSite.As<SmsSettingsPart>();

            if (smsSettings == null || !smsSettings.IsValid()) {
                var urlHelper = new UrlHelper(workContext.HttpContext.Request.RequestContext);
                var url = urlHelper.Action("Sms", "Admin", new {Area = "Settings"});
                yield return new NotifyEntry {Message = T("The <a href=\"{0}\">SMS settings</a> need to be configured.", url), Type = NotifyType.Warning};
            }
        }
    }
}
