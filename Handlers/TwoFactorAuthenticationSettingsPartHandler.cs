using EmeraldElements.TwoFactorAuthentication.Models;
using JetBrains.Annotations;
using Orchard.ContentManagement;
using Orchard.ContentManagement.Handlers;
using Orchard.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace EmeraldElements.TwoFactorAuthentication.Handlers
{
    [UsedImplicitly]
    public class TwoFactorAuthenticationSettingsPartHandler : ContentHandler
    {
        public TwoFactorAuthenticationSettingsPartHandler()
        {
            T = NullLocalizer.Instance;
            Filters.Add(new ActivatingFilter<TwoFactorAuthenticationSettingsPart>("Site"));
            //Filters.Add(new TemplateFilterForPart<TwoFactorAuthenticationSettingsPart>("TFASettings", "Parts/TwoFactorAuthenticationSettings", "Two-Factor Authentication"));
        }

        public Localizer T { get; set; }

        protected override void GetItemMetadata(GetContentItemMetadataContext context)
        {
            if (context.ContentItem.ContentType != "Site")
                return;
            base.GetItemMetadata(context);
            context.Metadata.EditorGroupInfo.Add(new GroupInfo(T("Two-Factor Authentication")));
        }
    }
}