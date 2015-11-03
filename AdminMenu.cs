using Orchard.Environment.Extensions;
using Orchard.Localization;
using Orchard.Security;
using Orchard.UI.Navigation;

namespace EmeraldElements.TwoFactorAuthentication {
    [OrchardFeature("EmeraldElements.TwoFactorAuthentication.UserProfile")]
    public class AdminMenu : INavigationProvider {
        public Localizer T { get; set; }
        public string MenuName { get { return "admin"; } }

        public void GetNavigation(NavigationBuilder builder) {
            builder.AddImageSet("myprofile")                
                    .Add(T("My Profile"), "50.0", item => item.Action("Index", "Profile", new { area = "EmeraldElements.TwoFactorAuthentication" }));                                           
        }
    }
}
