namespace Reflect.ProviderDashboard.Web.Infrastructure;

public static class ProviderAuthorization
{
    public const string ProviderRole = "Mental_Health_Provider";
    public const string PolicyName = "ProviderOnly";

    /// <summary>Manages people and assignments. Does not by itself open patient charts.</summary>
    public const string AdminRole = "Facility_Admin";
    public const string AdminPolicyName = "FacilityAdminOnly";

    /// <summary>Set on accounts created from an invite, so hosted sign-in can tell them from stray accounts.</summary>
    public const string InvitedClaim = "invited_by";
}
