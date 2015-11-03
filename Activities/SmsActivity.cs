using System.Collections.Generic;
using Orchard.Email.Services;
using Orchard.Environment.Extensions;
using Orchard.Events;
using Orchard.Localization;
using Orchard.Messaging.Services;
using Orchard.Workflows.Models;
using Orchard.Workflows.Services;
using EmeraldElements.TwoFactorAuthentication.Services;

namespace EmeraldElements.TwoFactorAuthentication.Activities {
    public interface IJobsQueueService : IEventHandler {
        void Enqueue(string message, object parameters, int priority);
    }

    [OrchardFeature("EmeraldElements.TwoFactorAuthentication.SmsWorkflows")]
    public class SmsActivity : Task {
        private readonly IMessageService _messageService;
        private readonly IJobsQueueService _jobsQueueService;

        public SmsActivity(
            IMessageService messageService,
            IJobsQueueService jobsQueueService
            ) {
            _messageService = messageService;
            _jobsQueueService = jobsQueueService;
            T = NullLocalizer.Instance;
        }

        public Localizer T { get; set; }

        public override IEnumerable<LocalizedString> GetPossibleOutcomes(WorkflowContext workflowContext, ActivityContext activityContext) {
            return new[] { T("Done") };
        }

        public override string Form {
            get {
                return "SmsActivity";
            }
        }

        public override LocalizedString Category {
            get { return T("Messaging"); }
        }

        public override string Name {
            get { return "SendSms"; }
        }

        public override LocalizedString Description {
            get { return T("Sends an sms message to a specific user."); }
        }

        public override IEnumerable<LocalizedString> Execute(WorkflowContext workflowContext, ActivityContext activityContext) {
            var body = activityContext.GetState<string>("Body");            
            var recipients = activityContext.GetState<string>("Recipients");            

            var parameters = new Dictionary<string, object> {                
                {"Body", body},
                {"Recipients", recipients}
            };

            var queued = activityContext.GetState<bool>("Queued");

            if (!queued) {
                _messageService.Send(SmsMessageChannel.MessageType, parameters);
            }
            else {
                var priority = activityContext.GetState<int>("Priority");
                _jobsQueueService.Enqueue("IMessageService.Send", new { type = SmsMessageChannel.MessageType, parameters = parameters }, priority);
            }

            yield return T("Done");
        }
    }
}