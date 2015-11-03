using Orchard.Environment.Extensions;

namespace EmeraldElements.TwoFactorAuthentication.Models {
    [OrchardFeature("EmeraldElements.TwoFactorAuthentication.Sms")]
    public class SmsMessage {        
        public string Body { get; set; }
        public string Recipients { get; set; }        
        public string From { get; set; }
    }
}