using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Northwind.Services.EntityFramework.Entities;

[Table("Suppliers")]
public class Supplier
{
    [Key]
    [Column("SupplierID")]
    public int SupplierId { get; set; }

    [Column("CompanyName")]
    public string CompanyName { get; set; } = string.Empty;

    [Column("ContactName")]
    public string? ContactName { get; set; }

    [Column("ContactTitle")]
    public string? ContactTitle { get; set; }

    [Column("Address")]
    public string? Address { get; set; }

    [Column("City")]
    public string? City { get; set; }

    [Column("Region")]
    public string? Region { get; set; }

    [Column("PostalCode")]
    public string? PostalCode { get; set; }

    [Column("Phone")]
    public string? Phone { get; set; }

    [Column("Fax")]
    public string? Fax { get; set; }

    [Column("HomePage")]
    public string? HomePage { get; set; }

    public ICollection<Product> Products { get; init; } =
        [];
}
