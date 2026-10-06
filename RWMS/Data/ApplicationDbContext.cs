using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using RWMS.Models.Domain;

namespace RWMS.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, string>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // Domain DbSets
    // Each DbSet tells EF Core: "there is a table for this entity"
    public DbSet<Product> Products { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }
    // Customer entity removed — Orders now reference ApplicationUser directly
    public DbSet<Delivery> Deliveries { get; set; }
    public DbSet<SupplyItem> SupplyItems { get; set; }
    public DbSet<Invite> Invites { get; set; }
    public DbSet<CustomerProduct> CustomerProducts { get; set; }
    public DbSet<AccountRequest> AccountRequests { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Identity table names
        builder.Entity<ApplicationUser>().ToTable("Users");
        builder.Entity<ApplicationRole>().ToTable("Roles");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserRole<string>>().ToTable("UserRoles");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserClaim<string>>().ToTable("UserClaims");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserLogin<string>>().ToTable("UserLogins");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityRoleClaim<string>>().ToTable("RoleClaims");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserToken<string>>().ToTable("UserTokens");

        // Product
        builder.Entity<Product>(entity =>
        {
            entity.ToTable("Products");

            entity.Property(p => p.Name)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(p => p.Price)
                .HasColumnType("decimal(18,2)");

            entity.Property(p => p.Unit)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(p => p.Category)
                .HasMaxLength(100);
        });

        // ApplicationUser business fields
        builder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(u => u.CompanyName).HasMaxLength(200);
            entity.Property(u => u.Address).HasMaxLength(500);
            entity.Property(u => u.Notes).HasMaxLength(1000);
        });

        // Delivery
        builder.Entity<Delivery>(entity =>
        {
            entity.ToTable("Deliveries");
            entity.Property(d => d.Notes).HasMaxLength(1000);

            // SetNull: removing an account clears their driver assignment, does not delete the delivery
            entity.HasOne(d => d.Driver)
                .WithMany()
                .HasForeignKey(d => d.DriverId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Order
        builder.Entity<Order>(entity =>
        {
            entity.ToTable("Orders");

            entity.Property(o => o.TotalAmount)
                .HasColumnType("decimal(18,2)");

            entity.Property(o => o.Notes)
                .HasMaxLength(500);

            // Restrict: deleting a user must not delete their order history
            entity.HasOne(o => o.Customer)
                .WithMany(u => u.Orders)
                .HasForeignKey(o => o.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            // SetNull: deleting a delivery unassigns the orders, does not delete them
            entity.HasOne(o => o.Delivery)
                .WithMany(d => d.Orders)
                .HasForeignKey(o => o.DeliveryId)
                .OnDelete(DeleteBehavior.SetNull);

            // Store OrderStatus enum as a string in the DB ("Pending", "Accepted", etc.)
            // This makes the database readable without needing a lookup table
            entity.Property(o => o.Status)
                .HasConversion<string>();
        });

        // OrderItem
        builder.Entity<OrderItem>(entity =>
        {
            entity.ToTable("OrderItems");

            entity.Property(oi => oi.UnitPrice)
                .HasColumnType("decimal(18,2)");

            entity.Property(oi => oi.Status)
                .HasConversion<string>();

            // OrderItem belongs to an Order
            // Cascade: deleting an order deletes its line items (they have no meaning alone)
            entity.HasOne(oi => oi.Order)
                .WithMany(o => o.OrderItems)
                .HasForeignKey(oi => oi.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            // OrderItem references a Product
            // Restrict: deleting a product must NOT delete order history
            entity.HasOne(oi => oi.Product)
                .WithMany(p => p.OrderItems)
                .HasForeignKey(oi => oi.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Invite
        builder.Entity<Invite>(entity =>
        {
            entity.ToTable("Invites");

            entity.Property(i => i.TokenHash).IsRequired().HasMaxLength(64);
            entity.Property(i => i.Email).IsRequired().HasMaxLength(256);
            entity.Property(i => i.Role).IsRequired().HasMaxLength(50);

            entity.HasIndex(i => i.TokenHash).IsUnique();
        });

        // CustomerProduct
        builder.Entity<CustomerProduct>(entity =>
        {
            entity.ToTable("CustomerProducts");

            entity.HasIndex(cp => new { cp.CustomerId, cp.ProductId }).IsUnique();

            entity.HasOne(cp => cp.Customer)
                .WithMany(u => u.CustomerProducts)
                .HasForeignKey(cp => cp.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(cp => cp.Product)
                .WithMany(p => p.CustomerProducts)
                .HasForeignKey(cp => cp.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // AccountRequest
        builder.Entity<AccountRequest>(entity =>
        {
            entity.ToTable("AccountRequests");

            entity.Property(r => r.FirstName).IsRequired().HasMaxLength(100);
            entity.Property(r => r.LastName).IsRequired().HasMaxLength(100);
            entity.Property(r => r.CompanyName).HasMaxLength(200);
            entity.Property(r => r.Email).IsRequired().HasMaxLength(256);
            entity.Property(r => r.Phone).HasMaxLength(30);
            entity.Property(r => r.Message).HasMaxLength(1000);

            entity.Property(r => r.Status).HasConversion<string>();

            entity.HasOne(r => r.ReviewedBy)
                .WithMany()
                .HasForeignKey(r => r.ReviewedById)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // SupplyItem
        builder.Entity<SupplyItem>(entity =>
        {
            entity.ToTable("SupplyItems");

            entity.Property(s => s.Name)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(s => s.Unit)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(s => s.Notes)
                .HasMaxLength(500);

            entity.Property(s => s.UnitCost)
                .HasColumnType("decimal(18,2)");

        });
    }
}
