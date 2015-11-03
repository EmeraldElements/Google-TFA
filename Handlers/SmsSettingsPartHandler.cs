using System;
using System.Text;
using JetBrains.Annotations;
using Orchard.ContentManagement;
using Orchard.ContentManagement.Handlers;
using Orchard.Localization;
using Orchard.Logging;
using Orchard.Security;
using System.Configuration;
using EmeraldElements.TwoFactorAuthentication.Models;
using Orchard.Environment.Extensions;

namespace EmeraldElements.TwoFactorAuthentication.Handlers {
    [UsedImplicitly]
    [OrchardFeature("EmeraldElements.TwoFactorAuthentication.Sms")]
    public class SmsSettingsPartHandler : ContentHandler {
        private readonly IEncryptionService _encryptionService;

        public SmsSettingsPartHandler(IEncryptionService encryptionService) {
            T = NullLocalizer.Instance;
            Logger = NullLogger.Instance;

            _encryptionService = encryptionService;
            Filters.Add(new ActivatingFilter<SmsSettingsPart>("Site"));

            OnLoaded<SmsSettingsPart>(LazyLoadHandlers);

            OnInitializing<SmsSettingsPart>((context, part) => {
                //part.Port = 25;
                //part.RequireCredentials = false;
                //part.EnableSsl = false;
            });
        }

        public new ILogger Logger { get; set; }

        void LazyLoadHandlers(LoadContentContext context, SmsSettingsPart part) {
            //part.PasswordField.Getter(() => {
            //    try {
            //        var encryptedPassword = part.Retrieve(x => x.Password);
            //        return String.IsNullOrWhiteSpace(encryptedPassword) ? String.Empty : Encoding.UTF8.GetString(_encryptionService.Decode(Convert.FromBase64String(encryptedPassword)));
            //    }
            //    catch {
            //        Logger.Error("The email password could not be decrypted. It might be corrupted, try to reset it.");
            //        return null;
            //    }
            //});

            //part.PasswordField.Setter(value => {
            //    var encryptedPassword = String.IsNullOrWhiteSpace(value) ? String.Empty : Convert.ToBase64String(_encryptionService.Encode(Encoding.UTF8.GetBytes(value)));
            //    part.Store(x => x.Password, encryptedPassword);
            //});

            //part.AddressPlaceholderField.Loader(value => (string)((dynamic)ConfigurationManager.GetSection("system.net/mailSettings/smtp")).From);
            part.TwilioNumberPlaceholderField.Loader(value => "YYY-YYY-YYYY");
        }

        public Localizer T { get; set; }

        protected override void GetItemMetadata(GetContentItemMetadataContext context) {
            if (context.ContentItem.ContentType != "Site")
                return;
            base.GetItemMetadata(context);
            context.Metadata.EditorGroupInfo.Add(new GroupInfo(T("Sms")));
        }
    }
}