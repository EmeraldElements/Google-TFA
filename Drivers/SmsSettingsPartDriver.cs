using System;
using Orchard.ContentManagement;
using Orchard.ContentManagement.Drivers;
using Orchard.Localization;
using Orchard.Environment.Extensions;
using EmeraldElements.TwoFactorAuthentication.Models;

namespace EmeraldElements.TwoFactorAuthenticationDrivers {

    // We define a specific driver instead of using a TemplateFilterForRecord, because we need the model to be the part and not the record.
    // Thus the encryption/decryption will be done when accessing the part's property
    [OrchardFeature("EmeraldElements.TwoFactorAuthentication.Sms")]
    public class SmsSettingsPartDriver : ContentPartDriver<SmsSettingsPart> {
        private const string TemplateName = "Parts/SmsSettings";

        public SmsSettingsPartDriver() {
            T = NullLocalizer.Instance;
        }

        public Localizer T { get; set; }

        protected override string Prefix { get { return "SmsSettings"; } }

        protected override DriverResult Editor(SmsSettingsPart part, dynamic shapeHelper) {
            return ContentShape("Parts_SmsSettings_Edit",
                    () => shapeHelper.EditorTemplate(TemplateName: TemplateName, Model: part, Prefix: Prefix))
                    .OnGroup("sms");
        }

        protected override DriverResult Editor(SmsSettingsPart part, IUpdateModel updater, dynamic shapeHelper) {
            return ContentShape("Parts_SmsSettings_Edit", () => {
                    //var previousPassword = part.Password;
                    updater.TryUpdateModel(part, Prefix, null, null);

                    // restore password if the input is empty, meaning it has not been reseted
                    //if (string.IsNullOrEmpty(part.Password)) {
                    //    part.Password = previousPassword;
                    //}
                    return shapeHelper.EditorTemplate(TemplateName: TemplateName, Model: part, Prefix: Prefix);
                })
                .OnGroup("sms");
        }
    }
}