namespace EHBYS.Models;

public static class PermissionMatrix
{
    public static bool CanEditParcels => Session.Role is UserRole.Admin or UserRole.Muhasebe;
    public static bool CanDeleteData => Session.Role == UserRole.Admin;
    public static bool CanViewReports => Session.Role is UserRole.Admin or UserRole.Muhasebe;
    public static bool CanTakePayment => Session.Role is UserRole.Admin or UserRole.Muhasebe;
}
