using System;
using System.Collections.Generic;
using System.Web.Mvc;
using Orchard.ContentManagement;
using Orchard.Localization;
using Orchard.Logging;
using Orchard.UI.Admin;
using Orchard.Environment.Extensions;
using EmeraldElements.TwoFactorAuthentication.Services;
using EmeraldElements.TwoFactorAuthentication.Models;

namespace Orchard.Email.Controllers {
    [Admin]
    [OrchardFeature("EmeraldElements.TwoFactorAuthentication.Sms")]
    public class SmsController : Controller {
        private readonly ISmsChannel _smsChannel;
        private readonly IOrchardServices _orchardServices;

        public SmsController(ISmsChannel smsChannel, IOrchardServices orchardServices) {
            _smsChannel = smsChannel;
            _orchardServices = orchardServices;
            T = NullLocalizer.Instance;
        }

        public Localizer T { get; set; }

        [HttpPost]
        public ActionResult TestSettings(TestSmsSettings testSettings) {
            ILogger logger = null;
            try {
                var fakeLogger = new FakeLogger();
                var smsChannelComponent = _smsChannel as Component;
                if (smsChannelComponent != null) {
                    logger = smsChannelComponent.Logger;
                    smsChannelComponent.Logger = fakeLogger;
                }

                // Temporarily update settings so that the test will actually use the specified host, port, etc.
                var smsSettings = _orchardServices.WorkContext.CurrentSite.As<SmsSettingsPart>();

                smsSettings.AccountSid = testSettings.AccountSid;
                smsSettings.AuthToken = testSettings.AuthToken;
                smsSettings.TwilioNumber = testSettings.From;                

                if (!smsSettings.IsValid()) {
                    fakeLogger.Error("Invalid settings.");
                }
                else {
                    _smsChannel.Process(new Dictionary<string, object> {
                        {"Recipients", testSettings.To},                        
                        {"Body", testSettings.Body}                    
                    });
                }

                if (!String.IsNullOrEmpty(fakeLogger.Message)) {
                    return Json(new { error = fakeLogger.Message });
                }

                return Json(new {status = T("Message sent.").Text});
            }
            catch (Exception e) {
                return Json(new {error = e.Message});
            }
            finally {
                var smsChannelComponent = _smsChannel as Component;
                if (smsChannelComponent != null) {
                    smsChannelComponent.Logger = logger;
                }

                // Undo the temporarily changed smtp settings.
                _orchardServices.TransactionManager.Cancel();
            }
        }

        private class FakeLogger : ILogger {
            public string Message { get; set; }

            public bool IsEnabled(LogLevel level) {
                return true;
            }

            public void Log(LogLevel level, Exception exception, string format, params object[] args) {
                Message = exception == null ? format : exception.Message;
            }
        }

        public class TestSmsSettings {            
            public string AccountSid { get; set; }
            public string AuthToken { get; set; }
            public string From { get; set; }            
            public string To { get; set; }            
            public string Body { get; set; }
        }
    }
}