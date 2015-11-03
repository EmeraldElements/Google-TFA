using EmeraldElements.TwoFactorAuthentication.Models;
using Orchard.ContentManagement;
using Orchard.ContentManagement.Drivers;
using Orchard.Environment.Features;
using Orchard.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace EmeraldElements.TwoFactorAuthentication.Drivers
{
    public class TwoFactorAuthenticationSettingsPartDriver : ContentPartDriver<TwoFactorAuthenticationSettingsPart>
    {
        private readonly IFeatureManager _featureManager;
        private const string TemplateName = "Parts/TwoFactorAuthenticationSettings";

        public TwoFactorAuthenticationSettingsPartDriver(IFeatureManager featureManager)
        {
            T = NullLocalizer.Instance;
            _featureManager = featureManager;
        }

        public Localizer T { get; set; }

        protected override string Prefix { get { return "TFASettings"; } }

        protected override DriverResult Editor(TwoFactorAuthenticationSettingsPart part, dynamic shapeHelper)
        {
            return ContentShape("Parts_TwoFactorAuthenticationSettings_Edit",
                    () => {

                        part.SMSFeatureEnabled = _featureManager.GetEnabledFeatures().Any(x => x.Id == "EmeraldElements.TwoFactorAuthentication.Sms");
                        if (!part.SMSFeatureEnabled)
                        {
                            part.EnableBackupSMS = false;
                            part.RequireBackupSMS = false;
                        }

                        return shapeHelper.EditorTemplate(TemplateName: TemplateName, Model: part, Prefix: Prefix);
                    })
                        .OnGroup("Two-Factor Authentication");
        }

        protected override DriverResult Editor(TwoFactorAuthenticationSettingsPart part, IUpdateModel updater, dynamic shapeHelper)
        {
            return ContentShape("Parts_TwoFactorAuthenticationSettings_Edit", () => {                
                updater.TryUpdateModel(part, Prefix, null, null);

                part.SMSFeatureEnabled = _featureManager.GetEnabledFeatures().Any(x => x.Id == "EmeraldElements.TwoFactorAuthentication.Sms");
                if (!part.SMSFeatureEnabled)
                {
                    part.EnableBackupSMS = false;
                    part.RequireBackupSMS = false;
                }

                return shapeHelper.EditorTemplate(TemplateName: TemplateName, Model: part, Prefix: Prefix);
            })
                .OnGroup("Two-Factor Authentication");
        }
    }
}