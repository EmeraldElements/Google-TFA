using System.Configuration;
using System.Net.Configuration;
using Orchard.ContentManagement;
using System;
using Orchard.ContentManagement.Utilities;
using Orchard.Environment.Extensions;
using System.ComponentModel.DataAnnotations;

namespace EmeraldElements.TwoFactorAuthentication.Models {
    [OrchardFeature("EmeraldElements.TwoFactorAuthentication.Sms")]
    public class SmsSettingsPart : ContentPart {
        [Required]
        public string TwilioNumber {
            get { return this.Retrieve(x => x.TwilioNumber); }
            set { this.Store(x => x.TwilioNumber, value); }
        }

        private readonly LazyField<string> _twilioNumberPlaceholder = new LazyField<string>();
        internal LazyField<string> TwilioNumberPlaceholderField { get { return _twilioNumberPlaceholder; } }
        public string TwilioNumberPlaceholder { get { return _twilioNumberPlaceholder.Value; } }

        [Required]
        public string AuthToken {
            get { return this.Retrieve(x => x.AuthToken); }
            set { this.Store(x => x.AuthToken, value); }
        }

        [Required]
        public string AccountSid {
            get { return this.Retrieve(x => x.AccountSid); }
            set { this.Store(x => x.AccountSid, value); }
        }

        public bool IsValid() {          
            if (String.IsNullOrWhiteSpace(TwilioNumber)) {
                return false;
            }

            if (String.IsNullOrWhiteSpace(AuthToken)) {
                return false;
            }

            if (String.IsNullOrWhiteSpace(AccountSid))
            {
                return false;
            }

            return true;
        }
    }
}