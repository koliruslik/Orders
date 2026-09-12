using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Northwind.Services.EntityFramework.Entities;

[Table("Categories")]
public class Category
{
    [Key]
    [Column("CategoryID")]
    public long Id { get; set; }

    [Column("CategoryName")]
    public string CategoryName { get; set; } = string.Empty;

    [Column("Description")]
    public string? Description { get; set; } = string.Empty;

    public ICollection<Product> Products { get; init; } =
        [];
}
