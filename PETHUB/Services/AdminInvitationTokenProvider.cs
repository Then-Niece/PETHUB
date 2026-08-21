using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace PETHUB.Services
{
    public class AdminInvitationTokenProvider<TUser>
        : DataProtectorTokenProvider<TUser>
        where TUser : class
    {
        public AdminInvitationTokenProvider(
            IDataProtectionProvider dataProtectionProvider,
            IOptions<AdminInvitationTokenProviderOptions> options,
            ILogger<DataProtectorTokenProvider<TUser>> logger)
            : base(dataProtectionProvider, options, logger)
        {
        }
    }

    public class AdminInvitationTokenProviderOptions
        : DataProtectionTokenProviderOptions
    {
        public AdminInvitationTokenProviderOptions()
        {
            Name = "PETHUBAdminInvitationTokenProvider";
            TokenLifespan = TimeSpan.FromHours(1);
        }
    }
}
