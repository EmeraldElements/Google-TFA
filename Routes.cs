using System.Collections.Generic;
using System.Web.Mvc;
using System.Web.Routing;
using Orchard.Mvc.Routes;

namespace EmeraldElements.TwoFactorAuthentication {
    public class Routes : IRouteProvider {
        public void GetRoutes(ICollection<RouteDescriptor> routes) {
            foreach (var routeDescriptor in GetRoutes())
                routes.Add(routeDescriptor);
        }

        public IEnumerable<RouteDescriptor> GetRoutes() {
            return new[] {
                new RouteDescriptor {
                    Priority = 5,
                    Route = new Route(
                        "Users/Account/LogOn",
                        new RouteValueDictionary {
                            {"area", "EmeraldElements.TwoFactorAuthentication"},
                            {"controller", "TwoFactorAuthentication"},
                            {"action", "LogOn"}
                        },
                        new RouteValueDictionary(),
                        new RouteValueDictionary {
                            {"area", "EmeraldElements.TwoFactorAuthentication"}
                        },
                        new MvcRouteHandler())
                },
                new RouteDescriptor {
                    Priority = 5,
                    Route = new Route(
                        "Users/Account/ValidateTFA",
                        new RouteValueDictionary {
                            {"area", "EmeraldElements.TwoFactorAuthentication"},
                            {"controller", "TwoFactorAuthentication"},
                            {"action", "ValidateTFALogin"}
                        },
                        new RouteValueDictionary(),
                        new RouteValueDictionary {
                            {"area", "EmeraldElements.TwoFactorAuthentication"}
                        },
                        new MvcRouteHandler())
                },
                new RouteDescriptor {
                    Priority = 5,
                    Route = new Route(
                        "Admin/MyProfile",
                        new RouteValueDictionary {
                            {"area", "EmeraldElements.TwoFactorAuthentication"},
                            {"controller", "Profile"},
                            {"action", "Index"}
                        },
                        new RouteValueDictionary(),
                        new RouteValueDictionary {
                            {"area", "EmeraldElements.TwoFactorAuthentication"}
                        },
                        new MvcRouteHandler())
                }
            };
        }
    }
}