using DailyRentalHomes.Domain.Enums;
using Microsoft.AspNetCore.Authorization;

namespace DailyRentalHomes.Api.Security;

public static class AuthorizationPolicies
{
    public const string AdminOnly = nameof(AdminOnly);
    public const string BrokerOrAdmin = nameof(BrokerOrAdmin);
    public const string CustomerOnly = nameof(CustomerOnly);

    public static void Configure(AuthorizationOptions options)
    {
        options.AddPolicy(AdminOnly, policy =>
            policy.RequireRole(nameof(UserRole.Admin)));

        options.AddPolicy(BrokerOrAdmin, policy =>
            policy.RequireRole(nameof(UserRole.Admin), nameof(UserRole.Broker)));

        options.AddPolicy(CustomerOnly, policy =>
            policy.RequireRole(nameof(UserRole.Customer)));
    }
}
