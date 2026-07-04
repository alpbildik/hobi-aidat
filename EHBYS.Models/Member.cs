namespace EHBYS.Models;

public sealed class Member
{
    public int Id { get; set; }
    public int ParcelId { get; set; }
    public string FullName { get; set; } = "";
    public string TcNo { get; set; } = "";
    public string Address { get; set; } = "";
    public string Phone { get; set; } = "";
}
