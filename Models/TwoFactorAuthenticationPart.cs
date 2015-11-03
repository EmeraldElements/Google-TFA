using Orchard.ContentManagement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace EmeraldElements.TwoFactorAuthentication.Models
{
    public class TwoFactorAuthenticationPart : ContentPart<TwoFactorAuthenticationPartRecord>
    {

        public bool EnableTFA
        {
            get { return Record.EnableTFA; }
            set { Record.EnableTFA = value; }
        }

        public byte[] SecretKey
        {
            get { return Record.SecretKey; }
            set { Record.SecretKey = value; }
        }

        public bool HasVerifiedKey
        {
            get { return Record.HasVerifiedKey; }
            set { Record.HasVerifiedKey = value; }
        }

        public bool CanLoginWithoutTFA
        {
            get { return Record.CanLoginWithoutTFA; }
            set { Record.CanLoginWithoutTFA = value; }
        }

        public virtual bool EnableBackupSMS
        {
            get { return Record.EnableBackupSMS; }
            set { Record.EnableBackupSMS = value; }
        }

        public virtual string BackupSMSNum
        {
            get { return Record.BackupSMSNum; }
            set { Record.BackupSMSNum = value; }
        }
    }
}