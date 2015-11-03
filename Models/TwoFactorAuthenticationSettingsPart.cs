using Orchard.ContentManagement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace EmeraldElements.TwoFactorAuthentication.Models
{
    public class TwoFactorAuthenticationSettingsPart : ContentPart
    {
        public bool RequireTFA
        {
            get { return this.Retrieve(x => x.RequireTFA); }
            set { this.Store(x => x.RequireTFA, value); }
        }

        public bool SMSFeatureEnabled
        {
            get { return this.Retrieve(x => x.SMSFeatureEnabled); }
            set { this.Store(x => x.SMSFeatureEnabled, value); }
        }

        public bool EnableBackupSMS
        {
            get { return this.Retrieve(x => x.EnableBackupSMS); }
            set { this.Store(x => x.EnableBackupSMS, value); }
        }

        public bool RequireBackupSMS
        {
            get { return this.Retrieve(x => x.RequireBackupSMS); }
            set { this.Store(x => x.RequireBackupSMS, value); }
        }
    }
}