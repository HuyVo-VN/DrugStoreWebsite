namespace DrugStoreWebSiteData.Application.Common;
public static class RoleConstants
{
    public const string Admin = "Admin";
    public const string Staff = "Staff";
    public const string Customer = "Customer";
    public const string ManagerRoles = $"{Admin},{Staff}";
    public const string CustomerRoles = $"{Customer}";
}