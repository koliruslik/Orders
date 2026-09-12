using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Northwind.Services.EntityFramework.Entities;

[Table("Shippers")]
public class Shipper
{
    [Key]
    [Column("ShipperID")]
    public int ShipperId { get; set; }

    [Column("CompanyName")]
    public string CompanyName { get; set; } = string.Empty;

    [Column("Phone")]
    public string? Phone { get; set; }

    public ICollection<Order> Orders { get; init; } =
        [];
}
