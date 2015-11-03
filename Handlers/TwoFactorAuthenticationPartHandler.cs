using EmeraldElements.TwoFactorAuthentication.Models;
using EmeraldElements.TwoFactorAuthentication.Services;
using Orchard.ContentManagement.Handlers;
using Orchard.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace EmeraldElements.TwoFactorAuthentication.Handlers
{
    public class TwoFactorAuthenticationPartHandler : ContentHandler
    {
        private readonly ITwoFactorAuthenticator _twoFactorAuthenticator;

        public TwoFactorAuthenticationPartHandler(IRepository<TwoFactorAuthenticationPartRecord> repository, ITwoFactorAuthenticator twoFactorAuthenticator)
        {
            _twoFactorAuthenticator = twoFactorAuthenticator;

            Filters.Add(StorageFilter.For(repository));
            OnInitializing<TwoFactorAuthenticationPart>((context, part) => {
                if (part.SecretKey == null)
                {
                    part.SecretKey = _twoFactorAuthenticator.GenerateKey();
                }
            });
        }
    }
}