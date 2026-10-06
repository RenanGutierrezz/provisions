namespace RWMS.Models.Domain;

public class CustomerProduct
{
    public int Id { get; set; }
    public string CustomerId { get; set; } = string.Empty;
    public int ProductId { get; set; }
    public int? ParLevel { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ApplicationUser Customer { get; set; } = null!;
    public Product Product { get; set; } = null!;
}
