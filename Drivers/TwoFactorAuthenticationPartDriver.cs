using EmeraldElements.TwoFactorAuthentication.Models;
using EmeraldElements.TwoFactorAuthentication.Services;
using EmeraldElements.TwoFactorAuthentication.ViewModels;
using Orchard;
using Orchard.ContentManagement;
using Orchard.ContentManagement.Drivers;
using Orchard.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace EmeraldElements.TwoFactorAuthentication.Drivers
{
    public class TwoFactorAuthenticationPartDriver : ContentPartDriver<TwoFactorAuthenticationPart>
    {
        private const string TemplateName = "Parts/TwoFactorAuthentication";
        private readonly ITwoFactorAuthenticator _twoFactorAuthenticator;
        private readonly IOrchardServices _services;

        public TwoFactorAuthenticationPartDriver(ITwoFactorAuthenticator twoFactorAuthenticator, IOrchardServices services)
        {
            _twoFactorAuthenticator = twoFactorAuthenticator;
            _services = services;
            T = NullLocalizer.Instance;
        }

        public Localizer T { get; set; }

        protected override string Prefix
        {
            get { return "TFA"; }
        }

        //GET
        protected override DriverResult Editor(TwoFactorAuthenticationPart part, dynamic shapeHelper)
        {
            return ContentShape("Parts_TwoFactorAuthentication_Edit",
                     () => {
                         //Get settings
                         var siteTFASettings = _services.WorkContext.CurrentSite.As<TwoFactorAuthenticationSettingsPart>();
                         //Build the identifier
                         var identifier = _services.WorkContext.CurrentSite.SiteName + ":" + _services.WorkContext.CurrentUser.UserName;                         

                         //Build the TFA Model
                         var model = new TwoFactorAuthenticationViewModel()
                         {
                             //Helpers
                             PinToVerifyKey = null,
                             QRCode = _twoFactorAuthenticator.GenerateProvisioningImage(identifier, part.SecretKey, 250, 250),

                             //TFA - Site Settings
                             RequireTFA = siteTFASettings.RequireTFA,
                             SiteEnableBackupSMS = siteTFASettings.EnableBackupSMS,
                             RequireBackupSMS = siteTFASettings.RequireBackupSMS,

                             //TFA - Part
                             EnableTFA = part.EnableTFA,
                             SecretKey = _twoFactorAuthenticator.EncodeKey(part.SecretKey),
                             HasVerifiedKey = part.HasVerifiedKey,
                             CanLoginWithoutTFA = part.CanLoginWithoutTFA,
                             EnableBackupSMS = siteTFASettings.RequireBackupSMS ? siteTFASettings.RequireBackupSMS : part.EnableBackupSMS,                             
                             BackupSMSNum = part.BackupSMSNum
                         };

                         return shapeHelper.EditorTemplate(TemplateName: TemplateName, Model: model, Prefix: Prefix);
                     });
        }
        //POST
        protected override DriverResult Editor(TwoFactorAuthenticationPart part, IUpdateModel updater, dynamic shapeHelper)
        {
            return ContentShape("Parts_TwoFactorAuthentication_Edit",
                    () =>
                    {
                        //Get settings
                        var siteTFASettings = _services.WorkContext.CurrentSite.As<TwoFactorAuthenticationSettingsPart>();

                        //Handle update
                        var updateModel = BuildTwoFactorAuthenticationViewModel(part, siteTFASettings);
                        var isUserAdminArea = _services.WorkContext.HttpContext.Request.Url.AbsolutePath.Contains("Admin/Users");

                        if (updater.TryUpdateModel(updateModel, Prefix, null, null))
                        {
                            part.EnableTFA = updateModel.EnableTFA;
                            //part.SecretKey = updateModel.SecretKey;
                            part.HasVerifiedKey = part.HasVerifiedKey || updateModel.PinToVerifyKey != null && _twoFactorAuthenticator.IsValid(part.SecretKey, updateModel.PinToVerifyKey, 1);
                            part.CanLoginWithoutTFA = updateModel.CanLoginWithoutTFA;
                            part.EnableBackupSMS = updateModel.EnableBackupSMS;
                            part.BackupSMSNum = updateModel.BackupSMSNum;
                        }

                        //Have to handle the errors on verifying the pin directly so we can keep the textbox in the dom for when a new key is generated
                        //isUserAdminArea just makes sure we don't attempt to ensure verification of the key in the admin area for allowing one-time login
                        //TODO: This could be moved to the viewmodel as part of the validation...
                        if (!part.HasVerifiedKey && updateModel.PinToVerifyKey == null && !isUserAdminArea)
                            updater.AddModelError("PinToVerifyKey", T("The Verify Pin field is required"));

                        //Build model for view                        
                        //Build the identifier
                        var identifier = _services.WorkContext.CurrentSite.SiteName + ":" + _services.WorkContext.CurrentUser.UserName;                        

                        //Build the TFA Model
                        var model = new TwoFactorAuthenticationViewModel()
                        {
                            //Helpers
                            PinToVerifyKey = null,
                            QRCode = _twoFactorAuthenticator.GenerateProvisioningImage(identifier, part.SecretKey, 250, 250),

                            //TFA - Site Settings
                            RequireTFA = siteTFASettings.RequireTFA,
                            SiteEnableBackupSMS = siteTFASettings.EnableBackupSMS,
                            RequireBackupSMS = siteTFASettings.RequireBackupSMS,

                            //TFA - Part
                            EnableTFA = part.EnableTFA,
                            SecretKey = _twoFactorAuthenticator.EncodeKey(part.SecretKey),
                            HasVerifiedKey = part.HasVerifiedKey,
                            CanLoginWithoutTFA = part.CanLoginWithoutTFA,
                            EnableBackupSMS = siteTFASettings.RequireBackupSMS ? siteTFASettings.RequireBackupSMS : part.EnableBackupSMS,                        
                            BackupSMSNum = part.BackupSMSNum
                        };

                        return shapeHelper.EditorTemplate(TemplateName: TemplateName, Model: model, Prefix: Prefix);
                    });
        }
        private static TwoFactorAuthenticationViewModel BuildTwoFactorAuthenticationViewModel(TwoFactorAuthenticationPart tfaPart, TwoFactorAuthenticationSettingsPart tfaSettings)
        {
            return new TwoFactorAuthenticationViewModel
            {
                PinToVerifyKey = null,
                QRCode = null,
                EnableTFA = tfaPart.EnableTFA,
                HasVerifiedKey = tfaPart.HasVerifiedKey,
                CanLoginWithoutTFA = tfaPart.CanLoginWithoutTFA,
                EnableBackupSMS = tfaPart.EnableBackupSMS,
                BackupSMSNum = tfaPart.BackupSMSNum,
                SiteEnableBackupSMS = tfaSettings.EnableBackupSMS,
                RequireBackupSMS = tfaSettings.RequireBackupSMS
            };
        }
    }
}