namespace RWMS.Models.Domain;

public class Delivery
{
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public string? DriverId { get; set; }
    public ApplicationUser? Driver { get; set; }
    public string? Notes { get; set; }
    public bool IsComplete { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Order> Orders { get; set; } = new List<Order>();
}
