using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace EmeraldElements.TwoFactorAuthentication.ViewModels
{
    public class TwoFactorAuthenticationViewModel : IValidatableObject
    {
        //Helpers        
        public string PinToVerifyKey { get; set; }

        public byte[] QRCode { get; set; }

        //Site Settings
        public bool RequireTFA { get; set; }

        public bool SiteEnableBackupSMS { get; set; }

        public bool RequireBackupSMS { get; set; }

        //TFA Part
        public bool EnableTFA { get; set; }

        public string SecretKey { get; set; }

        public bool HasVerifiedKey { get; set; }

        public bool CanLoginWithoutTFA { get; set; }

        public bool EnableBackupSMS { get; set; }

        public string BackupSMSNum { get; set; }

        //Validation
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (this.RequireBackupSMS)
            {
                if (!this.EnableBackupSMS)
                {
                    yield return new ValidationResult("Backup SMS is required.");
                }

                if (string.IsNullOrEmpty(this.BackupSMSNum))
                {
                    yield return new ValidationResult("Backup SMS Number is required.");
                }
            }
        }
    }
}