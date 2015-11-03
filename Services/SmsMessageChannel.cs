using System;
using System.Collections.Generic;
using System.Configuration;
using System.Net;
using System.Net.Configuration;
using System.Net.Mail;
using System.Web.Mvc;
using Orchard.ContentManagement;
using Orchard.DisplayManagement;
using Orchard.Logging;
using Orchard.Email.Models;
using Orchard;
using Orchard.Environment.Extensions;
using EmeraldElements.TwoFactorAuthentication.Models;
using Twilio;

namespace EmeraldElements.TwoFactorAuthentication.Services {
    [OrchardFeature("EmeraldElements.TwoFactorAuthentication.Sms")]
    public class SmsMessageChannel : Component, ISmsChannel, IDisposable {
        private readonly SmsSettingsPart _smsSettings;
        private readonly IShapeFactory _shapeFactory;
        private readonly IShapeDisplay _shapeDisplay;
        private readonly Lazy<TwilioRestClient> _smsClientField;
        public static readonly string MessageType = "Sms";

        public SmsMessageChannel(
            IOrchardServices orchardServices,
            IShapeFactory shapeFactory,
            IShapeDisplay shapeDisplay) {

            _shapeFactory = shapeFactory;
            _shapeDisplay = shapeDisplay;

            _smsSettings = orchardServices.WorkContext.CurrentSite.As<SmsSettingsPart>();
            _smsClientField = new Lazy<TwilioRestClient>(CreateSmsClient);
        }

        public void Dispose() {
            if (!_smsClientField.IsValueCreated) {
                return;
            }

            //_smsClientField.Value.Dispose();
        }

        public void Process(IDictionary<string, object> parameters) {

            if (!_smsSettings.IsValid()) {
                return;
            }

            var smsMessage = new SmsMessage {
                Body = Read(parameters, "Body"),                
                Recipients = Read(parameters, "Recipients"),                
                From = _smsSettings.TwilioNumber
            };

            if (smsMessage.Recipients.Length == 0) {
                Logger.Error("Sms message doesn't have any recipients");
                return;
            }

            // Apply default Body alteration for SmsChannel.
            //var template = _shapeFactory.Create("Template_Sms_Wrapper", Arguments.From(new {
            //    Content = new MvcHtmlString(smsMessage.Body)
            //}));

            //var mailMessage = new MailMessage {
            //    Subject = smsMessage.Subject,
            //    Body = _shapeDisplay.Display(template),
            //    IsBodyHtml = true
            //};

            if (parameters.ContainsKey("Message")) {
                // A full message object is provided by the sender.

                var oldMessage = smsMessage;
                smsMessage = (SmsMessage)parameters["Message"];                

                if (String.IsNullOrWhiteSpace(smsMessage.Body)) {
                    smsMessage.Body = oldMessage.Body;
                    //mailMessage.IsBodyHtml = oldMessage.IsBodyHtml;
                }
            }

            try {

                if (String.IsNullOrWhiteSpace(smsMessage.From))
                {
                    // Take 'From' address from site settings
                    smsMessage.From = _smsSettings.TwilioNumber;                                       
                }                              

                foreach (var recipient in ParseRecipients(smsMessage.Recipients)) {
                    _smsClientField.Value.SendMessage(
                        smsMessage.From,
                        recipient,
                        smsMessage.Body
                    );
                }                

                //if (!String.IsNullOrWhiteSpace(smsMessage.From)) {
                //    mailMessage.From = new MailAddress(smsMessage.From);
                //}
                //else {
                //    // Take 'From' address from site settings or web.config.
                //    mailMessage.From = !String.IsNullOrWhiteSpace(_smtpSettings.Address)
                //        ? new MailAddress(_smtpSettings.Address)
                //        : new MailAddress(((SmtpSection)ConfigurationManager.GetSection("system.net/mailSettings/smtp")).From);
                //}

                //if (!String.IsNullOrWhiteSpace(smsMessage.ReplyTo)) {
                //    foreach (var recipient in ParseRecipients(smsMessage.ReplyTo)) {
                //        mailMessage.ReplyToList.Add(new MailAddress(recipient));
                //    }
                //}

                //_smsClientField.Value.Send(mailMessage);
            }
            catch (Exception e) {
                Logger.Error(e, "Could not send sms");
            }
        }

        private TwilioRestClient CreateSmsClient() {          
            var smsClient = new TwilioRestClient(_smsSettings.AccountSid, _smsSettings.AuthToken);
            
            return smsClient;
        }

        private string Read(IDictionary<string, object> dictionary, string key) {
            return dictionary.ContainsKey(key) ? dictionary[key] as string : null;
        }

        private IEnumerable<string> ParseRecipients(string recipients) {
            return recipients.Split(new[] {',', ';'}, StringSplitOptions.RemoveEmptyEntries);
        }
    }
}