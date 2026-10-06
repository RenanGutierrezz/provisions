namespace RWMS.Models.Domain;

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public string Unit { get; set; } = string.Empty;
    public string? Category { get; set; }

    // Never hard-deleted — past orders reference products by ID
    public bool IsActive { get; set; } = true;
    public DateTime? DeletedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    public ICollection<CustomerProduct> CustomerProducts { get; set; } = new List<CustomerProduct>();
}
