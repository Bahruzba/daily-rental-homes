using DailyRentalHomes.Api.Security;
using DailyRentalHomes.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;

namespace DailyRentalHomes.Tests;

public sealed class AuthorizationPoliciesTests
{
    [Fact]
    public void Configure_RegistersExpectedRolePolicies()
    {
        var options = new AuthorizationOptions();

        AuthorizationPolicies.Configure(options);

        AssertRoles(options, AuthorizationPolicies.AdminOnly, nameof(UserRole.Admin));
        AssertRoles(options, AuthorizationPolicies.BrokerOrAdmin, nameof(UserRole.Admin), nameof(UserRole.Broker));
        AssertRoles(options, AuthorizationPolicies.CustomerOnly, nameof(UserRole.Customer));
    }

    private static void AssertRoles(AuthorizationOptions options, string policyName, params string[] roles)
    {
        var policy = options.GetPolicy(policyName);
        Assert.NotNull(policy);

        var requirement = Assert.Single(policy.Requirements.OfType<RolesAuthorizationRequirement>());
        Assert.Equal(roles.Order(), requirement.AllowedRoles.Order());
    }
}
