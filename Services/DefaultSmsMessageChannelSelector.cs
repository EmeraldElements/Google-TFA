using Orchard;
using Orchard.Environment.Extensions;
using Orchard.Messaging.Services;

namespace EmeraldElements.TwoFactorAuthentication.Services {
    [OrchardFeature("EmeraldElements.TwoFactorAuthentication.Sms")]
    public class DefaultSmsMessageChannelSelector : Component, IMessageChannelSelector {
        private readonly IWorkContextAccessor _workContextAccessor;
        public const string ChannelName = "Sms";

        public DefaultSmsMessageChannelSelector(IWorkContextAccessor workContextAccessor) {
            _workContextAccessor = workContextAccessor;
        }

        public MessageChannelSelectorResult GetChannel(string messageType, object payload) {
            if (messageType == "Sms") {
                var workContext = _workContextAccessor.GetContext();
                return new MessageChannelSelectorResult {
                    Priority = 50,
                    MessageChannel = () => workContext.Resolve<ISmsChannel>()
                };
            }

            return null;
        }
    }
}
