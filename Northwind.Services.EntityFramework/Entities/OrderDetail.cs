using System.ComponentModel.DataAnnotations.Schema;

namespace Northwind.Services.EntityFramework.Entities;

[Table("OrderDetails")]
public class OrderDetail
{
    [Column("OrderID")]
    public long OrderId { get; set; }

    [Column("ProductID")]
    public long ProductId { get; set; }

    [Column("UnitPrice")]
    public decimal UnitPrice { get; set; }

    [Column("Quantity")]
    public long Quantity { get; set; }

    [Column("Discount")]
    public double Discount { get; set; }

    public Order Order { get; set; } = null!;

    public Product Product { get; set; } = null!;
}
