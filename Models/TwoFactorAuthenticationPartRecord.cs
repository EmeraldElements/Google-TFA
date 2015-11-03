using Orchard.ContentManagement.Records;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace EmeraldElements.TwoFactorAuthentication.Models
{
    public class TwoFactorAuthenticationPartRecord : ContentPartRecord
    {
        public virtual bool EnableTFA { get; set; }

        public virtual byte[] SecretKey { get; set; }

        public virtual bool HasVerifiedKey { get; set; }

        public virtual bool CanLoginWithoutTFA { get; set; }

        public virtual bool EnableBackupSMS { get; set; }

        public virtual string BackupSMSNum { get; set; }
    }
}