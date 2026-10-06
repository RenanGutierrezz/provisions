using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using RWMS.Data;
using RWMS.Extensions;
using RWMS.Models.Domain;
using RWMS.Models.Enums;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Don't reveal the server technology in response headers
builder.WebHost.ConfigureKestrel(o => o.AddServerHeader = false);

var culture = new System.Globalization.CultureInfo("en-US");
System.Globalization.CultureInfo.DefaultThreadCurrentCulture = culture;
System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = culture;

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    var conn = builder.Configuration.GetConnectionString("DefaultConnection")!;
    options.UseSqlServer(conn);
});

builder.Services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 8;

    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;

    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedAccount = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = "tm_session";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
        ? CookieSecurePolicy.SameAsRequest
        : CookieSecurePolicy.Always;
    options.Cookie.SameSite = builder.Environment.IsDevelopment()
        ? SameSiteMode.Lax
        : SameSiteMode.Strict;
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Home/Error/403";
});

builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("login", limiter =>
    {
        limiter.PermitLimit = 5;
        limiter.Window = TimeSpan.FromMinutes(15);
        limiter.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiter.QueueLimit = 0;
    });
    options.RejectionStatusCode = 429;
});

builder.Services.AddAuthorizationBuilder()
    .AddPolicy("OwnerAccess", policy => policy.RequireRole("Owner"))
    .AddPolicy("ManagerAccess", policy => policy.RequireRole("Owner", "Manager"))
    .AddPolicy("CustomerAccess", policy => policy.RequireRole("Customer"));

var keysFolder = Path.Combine(builder.Environment.ContentRootPath, "DataProtectionKeys");
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(keysFolder))
    .SetApplicationName("RWMS");

builder.Services.AddApplicationServices();

builder.Services.AddHsts(options =>
{
    options.MaxAge = TimeSpan.FromDays(365);
    options.IncludeSubDomains = true;
    options.Preload = true;
});

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

var app = builder.Build();

if (app.Environment.IsProduction())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await db.Database.EnsureCreatedAsync();
}

await SeedRolesAsync(app);

if (app.Environment.IsDevelopment())
{
    await SeedDemoAccountsAsync(app);
    await SeedDemoDataAsync(app);
    await SeedDevDataAsync(app);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.Use(async (context, next) =>
{
    var h = context.Response.Headers;
    h.Append("X-Content-Type-Options", "nosniff");
    h.Append("X-Frame-Options", "DENY");
    h.Append("X-Permitted-Cross-Domain-Policies", "none");
    h.Append("Referrer-Policy", "strict-origin-when-cross-origin");
    h.Append("Permissions-Policy", "camera=(), microphone=(), geolocation=()");
    h.Remove("X-Powered-By");
    h.Append("Content-Security-Policy",
        "default-src 'self'; " +
        "script-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net; " +
        "style-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net https://fonts.googleapis.com; " +
        "font-src 'self' https://cdn.jsdelivr.net https://fonts.gstatic.com; " +
        "img-src 'self' data:; " +
        "frame-ancestors 'none'; " +
        "form-action 'self';");
    await next();
});

app.UseRateLimiter();

app.UseStatusCodePagesWithReExecute("/Home/Error/{0}");
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapRazorPages();

app.Run();

static async Task SeedDevDataAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var config = app.Configuration.GetSection("DevSeed");

    var managerEmail = config["ManagerEmail"]!;
    if (await userManager.FindByEmailAsync(managerEmail) is null)
    {
        var manager = new ApplicationUser
        {
            UserName = managerEmail,
            Email = managerEmail,
            FirstName = "Sarah",
            LastName = "Mitchell",
            IsActive = true
        };
        var r = await userManager.CreateAsync(manager, config["ManagerPassword"]!);
        if (r.Succeeded) await userManager.AddToRoleAsync(manager, "Manager");
    }

    if (!db.Products.Any())
    {
        db.Products.AddRange(
            new Product { Name = "Russet Potatoes (20kg)",     Price = 24.00m,  Unit = "sack",  Category = "Produce" },
            new Product { Name = "Roma Tomatoes (10kg)",       Price = 32.00m,  Unit = "case",  Category = "Produce" },

            new Product { Name = "Large Eggs (15 doz)",        Price = 58.00m,  Unit = "case",  Category = "Dairy & Eggs" },
            new Product { Name = "Unsalted Butter (10kg)",     Price = 96.00m,  Unit = "case",  Category = "Dairy & Eggs" },

            new Product { Name = "Chicken Breast (10kg)",      Price = 112.00m, Unit = "case",  Category = "Meat & Deli" },
            new Product { Name = "Ground Beef 80/20 (10kg)",   Price = 98.00m,  Unit = "case",  Category = "Meat & Deli" },

            new Product { Name = "All-Purpose Flour (20kg)",   Price = 26.00m,  Unit = "sack",  Category = "Dry Goods" },
            new Product { Name = "Canola Oil (16L)",           Price = 42.00m,  Unit = "box",   Category = "Dry Goods" },

            new Product { Name = "Orange Juice (12x1L)",       Price = 38.00m,  Unit = "case",  Category = "Beverages" },
            new Product { Name = "Cola (24x355ml)",            Price = 22.00m,  Unit = "case",  Category = "Beverages" },

            new Product { Name = "Takeout Containers (500ct)", Price = 64.00m,  Unit = "case",  Category = "Disposables" },
            new Product { Name = "Nitrile Gloves (10x100)",    Price = 52.00m,  Unit = "case",  Category = "Disposables" }
        );
        await db.SaveChangesAsync();
    }

    if (!db.SupplyItems.Any())
    {
        db.SupplyItems.AddRange(
            new SupplyItem { Name = "Pallet Wrap (18in roll)",         QuantityNeeded = 24, Unit = "roll" },
            new SupplyItem { Name = "Corrugated Boxes (bundle/25)",    QuantityNeeded = 30, Unit = "bundle" },
            new SupplyItem { Name = "Packing Tape (case/36)",          QuantityNeeded = 10, Unit = "case" },
            new SupplyItem { Name = "Shipping Labels (roll/500)",      QuantityNeeded = 12, Unit = "roll" },
            new SupplyItem { Name = "Bin Labels (roll/1000)",          QuantityNeeded =  6, Unit = "roll" },
            new SupplyItem { Name = "Gel Ice Packs (case/48)",         QuantityNeeded = 20, Unit = "case" },
            new SupplyItem { Name = "Insulated Pallet Covers",         QuantityNeeded =  8, Unit = "each" },
            new SupplyItem { Name = "Probe Thermometers",              QuantityNeeded =  6, Unit = "each" },
            new SupplyItem { Name = "Cooler Blankets",                 QuantityNeeded = 10, Unit = "each" },
            new SupplyItem { Name = "Load Straps (10-pack)",           QuantityNeeded =  6, Unit = "pack" },
            new SupplyItem { Name = "Hand Truck Tires",                QuantityNeeded =  4, Unit = "each" },
            new SupplyItem { Name = "Windshield Washer Fluid (4L)",    QuantityNeeded = 12, Unit = "jug" },
            new SupplyItem { Name = "Nitrile Gloves (box/100)",        QuantityNeeded = 15, Unit = "box" },
            new SupplyItem { Name = "Food-Safe Sanitizer (4L)",        QuantityNeeded =  8, Unit = "jug" },
            new SupplyItem { Name = "Dish Soap (4L jug)",              QuantityNeeded = 10, Unit = "jug" },
            new SupplyItem { Name = "Paper Towels (case/12)",          QuantityNeeded =  6, Unit = "case" },
            new SupplyItem { Name = "Thermal Printer Rolls (case/24)", QuantityNeeded =  8, Unit = "case" },
            new SupplyItem { Name = "Invoice Paper (box/2500)",        QuantityNeeded =  5, Unit = "box" },
            new SupplyItem { Name = "Printer Toner",                   QuantityNeeded =  4, Unit = "each" }
        );
        await db.SaveChangesAsync();
    }

}

static async Task SeedDemoDataAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

    if (!db.Products.Any())
    {
        db.Products.AddRange(
            new Product { Name = "Russet Potatoes (20kg)",     Price = 24.00m,  Unit = "sack",  Category = "Produce" },
            new Product { Name = "Roma Tomatoes (10kg)",       Price = 32.00m,  Unit = "case",  Category = "Produce" },

            new Product { Name = "Large Eggs (15 doz)",        Price = 58.00m,  Unit = "case",  Category = "Dairy & Eggs" },
            new Product { Name = "Unsalted Butter (10kg)",     Price = 96.00m,  Unit = "case",  Category = "Dairy & Eggs" },

            new Product { Name = "Chicken Breast (10kg)",      Price = 112.00m, Unit = "case",  Category = "Meat & Deli" },
            new Product { Name = "Ground Beef 80/20 (10kg)",   Price = 98.00m,  Unit = "case",  Category = "Meat & Deli" },

            new Product { Name = "All-Purpose Flour (20kg)",   Price = 26.00m,  Unit = "sack",  Category = "Dry Goods" },
            new Product { Name = "Canola Oil (16L)",           Price = 42.00m,  Unit = "box",   Category = "Dry Goods" },

            new Product { Name = "Orange Juice (12x1L)",       Price = 38.00m,  Unit = "case",  Category = "Beverages" },
            new Product { Name = "Cola (24x355ml)",            Price = 22.00m,  Unit = "case",  Category = "Beverages" },

            new Product { Name = "Takeout Containers (500ct)", Price = 64.00m,  Unit = "case",  Category = "Disposables" },
            new Product { Name = "Nitrile Gloves (10x100)",    Price = 52.00m,  Unit = "case",  Category = "Disposables" }
        );
        await db.SaveChangesAsync();
    }

    // Seed demo client accounts (replacing old Customer entity)
    var clientSeeds = new[]
    {
        (Email: "orders@harbourbistro.ca",       First: "Maya",   Last: "Chen",    Company: "Harbour Bistro", Phone: "416-555-0121", Address: "142 King St W, Toronto, ON"),
        (Email: "hello@oakwoodcafe.ca",   First: "Daniel", Last: "Okafor",    Company: "Oakwood Cafe",      Phone: "416-555-0188", Address: "87 College St, Toronto, ON"),
        (Email: "kitchen@lakeviewgrill.ca",        First: "Sofia",  Last: "Reyes", Company: "Lakeview Grill",    Phone: "647-555-0134", Address: "310 Front St W, Toronto, ON"),
        (Email: "purchasing@birchwoodhotel.ca", First: "Marcus", Last: "Webb",  Company: "Birchwood Hotel",    Phone: "416-555-0199", Address: "55 Yonge St, Toronto, ON"),
        (Email: "orders@copperkettle.ca",     First: "Elena",  Last: "Novak",  Company: "Copper Kettle Catering",        Phone: "647-555-0162", Address: "228 Bloor St W, Toronto, ON"),
        (Email: "bakery@sunrisebakery.ca",      First: "Omar",   Last: "Haddad",   Company: "Sunrise Bakery",   Phone: "416-555-0177", Address: "91 Queen St E, Toronto, ON"),
        (Email: "deli@greenfielddeli.ca",     First: "Anna",   Last: "Kowalski",  Company: "Greenfield Deli",      Phone: "647-555-0145", Address: "403 Spadina Ave, Toronto, ON"),
        (Email: "bar@riversidepub.ca",    First: "Paul",   Last: "Dubois",    Company: "Riverside Pub",   Phone: "416-555-0156", Address: "17 Liberty St, Toronto, ON"),
    };

    foreach (var (email, first, last, company, phone, address) in clientSeeds)
    {
        if (await userManager.FindByEmailAsync(email) is not null) continue;

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FirstName = first,
            LastName = last,
            CompanyName = company,
            PhoneNumber = phone,
            Address = address,
            IsActive = true
        };
        var result = await userManager.CreateAsync(user, "Client@2024");
        if (result.Succeeded)
            await userManager.AddToRoleAsync(user, "Customer");
    }

    if (!db.Orders.Any())
    {
        var prods = db.Products.ToList();
        var clients = await userManager.GetUsersInRoleAsync("Customer");

        Product P(string name) => prods.First(p => p.Name.Contains(name));
        string C(string company) => clients.First(u => u.CompanyName == company).Id;
        static decimal Total(List<OrderItem> items) => items.Sum(i => i.Quantity * i.UnitPrice);
        List<OrderItem> Items(params (Product p, int qty)[] lines) =>
            lines.Select(l => new OrderItem { Product = l.p, Quantity = l.qty, UnitPrice = l.p.Price }).ToList();

        // Monthly delivery runs
        var d1 = new Delivery { Date = new DateTime(2025, 11, 14), IsComplete = true,  Notes = "November run" };
        var d2 = new Delivery { Date = new DateTime(2025, 12, 12), IsComplete = true,  Notes = "December run" };
        var d3 = new Delivery { Date = new DateTime(2026,  1, 16), IsComplete = true,  Notes = "January run" };
        var d4 = new Delivery { Date = new DateTime(2026,  2, 13), IsComplete = true,  Notes = "February run" };
        var d5 = new Delivery { Date = new DateTime(2026,  3, 14), IsComplete = true,  Notes = "March run" };
        var d6 = new Delivery { Date = new DateTime(2026,  4, 11), IsComplete = true,  Notes = "April run" };
        var d7 = new Delivery { Date = new DateTime(2026,  5,  2), IsComplete = false, Notes = "May first run" };
        var d8 = new Delivery { Date = new DateTime(2026,  5,  9), IsComplete = false, Notes = "May second run" };
        db.Deliveries.AddRange(d1, d2, d3, d4, d5, d6, d7, d8);
        await db.SaveChangesAsync();

        var orders = new List<Order>();

        // ── November 2025 ─────────────────────────────────────────────────────
        var i1 = Items((P("Potatoes"), 20), (P("Roma"), 15), (P("Ground Beef"), 10), (P("Cola"), 4));
        orders.Add(new Order { CustomerId = C("Harbour Bistro"), Status = OrderStatus.ReadyForDelivery, CreatedAt = new DateTime(2025, 11,  3), RequestedDeliveryDate = d1.Date, Delivery = d1, TotalAmount = Total(i1), OrderItems = i1 });

        var i2 = Items((P("Potatoes"), 30), (P("Large Eggs"), 20), (P("Orange"), 8));
        orders.Add(new Order { CustomerId = C("Oakwood Cafe"),      Status = OrderStatus.ReadyForDelivery, CreatedAt = new DateTime(2025, 11,  4), RequestedDeliveryDate = d1.Date, Delivery = d1, TotalAmount = Total(i2), OrderItems = i2 });

        var i3 = Items((P("Roma"), 12), (P("Butter"), 10), (P("All-Purpose"), 8), (P("Takeout"), 6));
        orders.Add(new Order { CustomerId = C("Lakeview Grill"),     Status = OrderStatus.ReadyForDelivery, CreatedAt = new DateTime(2025, 11,  5), RequestedDeliveryDate = d1.Date, Delivery = d1, TotalAmount = Total(i3), OrderItems = i3 });

        var i4 = Items((P("Large Eggs"), 15), (P("Chicken"), 10), (P("Cola"), 6), (P("Nitrile"), 5));
        orders.Add(new Order { CustomerId = C("Birchwood Hotel"),     Status = OrderStatus.ReadyForDelivery, CreatedAt = new DateTime(2025, 11,  6), RequestedDeliveryDate = d1.Date, Delivery = d1, TotalAmount = Total(i4), OrderItems = i4 });

        // ── December 2025 ─────────────────────────────────────────────────────
        var i5 = Items((P("Potatoes"), 25), (P("Roma"), 20), (P("Ground Beef"), 12), (P("Canola"), 5));
        orders.Add(new Order { CustomerId = C("Harbour Bistro"), Status = OrderStatus.ReadyForDelivery, CreatedAt = new DateTime(2025, 12,  1), RequestedDeliveryDate = d2.Date, Delivery = d2, TotalAmount = Total(i5), OrderItems = i5, Notes = "Extra stock for the holiday menu." });

        var i6 = Items((P("Potatoes"), 35), (P("Large Eggs"), 25), (P("Orange"), 10), (P("Cola"), 3));
        orders.Add(new Order { CustomerId = C("Oakwood Cafe"),      Status = OrderStatus.ReadyForDelivery, CreatedAt = new DateTime(2025, 12,  2), RequestedDeliveryDate = d2.Date, Delivery = d2, TotalAmount = Total(i6), OrderItems = i6 });

        var i7 = Items((P("Butter"), 15), (P("All-Purpose"), 10), (P("Takeout"), 8));
        orders.Add(new Order { CustomerId = C("Lakeview Grill"),     Status = OrderStatus.ReadyForDelivery, CreatedAt = new DateTime(2025, 12,  3), RequestedDeliveryDate = d2.Date, Delivery = d2, TotalAmount = Total(i7), OrderItems = i7 });

        var i8 = Items((P("Roma"), 20), (P("Chicken"), 12), (P("Ground Beef"), 8), (P("Nitrile"), 6));
        orders.Add(new Order { CustomerId = C("Copper Kettle Catering"),         Status = OrderStatus.ReadyForDelivery, CreatedAt = new DateTime(2025, 12,  4), RequestedDeliveryDate = d2.Date, Delivery = d2, TotalAmount = Total(i8), OrderItems = i8 });

        var i9 = Items((P("Potatoes"), 10), (P("Roma"), 8), (P("Ground Beef"), 5));
        orders.Add(new Order { CustomerId = C("Sunrise Bakery"),    Status = OrderStatus.ReadyForDelivery, CreatedAt = new DateTime(2025, 12,  5), RequestedDeliveryDate = d2.Date, Delivery = d2, TotalAmount = Total(i9), OrderItems = i9 });

        // ── January 2026 ──────────────────────────────────────────────────────
        var i10 = Items((P("Potatoes"), 22), (P("Roma"), 18), (P("Large Eggs"), 10), (P("Ground Beef"), 12));
        orders.Add(new Order { CustomerId = C("Harbour Bistro"), Status = OrderStatus.ReadyForDelivery, CreatedAt = new DateTime(2026,  1,  6), RequestedDeliveryDate = d3.Date, Delivery = d3, TotalAmount = Total(i10), OrderItems = i10 });

        var i11 = Items((P("Potatoes"), 28), (P("Butter"), 15), (P("Orange"), 12), (P("Cola"), 4));
        orders.Add(new Order { CustomerId = C("Oakwood Cafe"),      Status = OrderStatus.ReadyForDelivery, CreatedAt = new DateTime(2026,  1,  7), RequestedDeliveryDate = d3.Date, Delivery = d3, TotalAmount = Total(i11), OrderItems = i11 });

        var i12 = Items((P("Large Eggs"), 20), (P("Chicken"), 15), (P("Cola"), 8), (P("Takeout"), 5));
        orders.Add(new Order { CustomerId = C("Birchwood Hotel"),     Status = OrderStatus.ReadyForDelivery, CreatedAt = new DateTime(2026,  1,  8), RequestedDeliveryDate = d3.Date, Delivery = d3, TotalAmount = Total(i12), OrderItems = i12 });

        var i13 = Items((P("Roma"), 15), (P("Potatoes"), 12), (P("Canola"), 6), (P("Nitrile"), 8));
        orders.Add(new Order { CustomerId = C("Copper Kettle Catering"),         Status = OrderStatus.ReadyForDelivery, CreatedAt = new DateTime(2026,  1,  9), RequestedDeliveryDate = d3.Date, Delivery = d3, TotalAmount = Total(i13), OrderItems = i13 });

        var i14 = Items((P("Potatoes"), 8), (P("Large Eggs"), 6), (P("Takeout"), 4), (P("Nitrile"), 5));
        orders.Add(new Order { CustomerId = C("Greenfield Deli"),       Status = OrderStatus.ReadyForDelivery, CreatedAt = new DateTime(2026,  1, 10), RequestedDeliveryDate = d3.Date, Delivery = d3, TotalAmount = Total(i14), OrderItems = i14 });

        // Rejected — delivery window missed
        var i15 = Items((P("Roma"), 10));
        orders.Add(new Order { CustomerId = C("Sunrise Bakery"),    Status = OrderStatus.Rejected,         CreatedAt = new DateTime(2026,  1, 11), TotalAmount = Total(i15), OrderItems = i15, Notes = "Delivery window missed — order voided." });

        // ── February 2026 ─────────────────────────────────────────────────────
        var i16 = Items((P("Potatoes"), 30), (P("Roma"), 20), (P("Ground Beef"), 15), (P("Canola"), 8));
        orders.Add(new Order { CustomerId = C("Harbour Bistro"), Status = OrderStatus.ReadyForDelivery, CreatedAt = new DateTime(2026,  2,  3), RequestedDeliveryDate = d4.Date, Delivery = d4, TotalAmount = Total(i16), OrderItems = i16 });

        var i17 = Items((P("Potatoes"), 32), (P("Large Eggs"), 28), (P("Butter"), 12), (P("Orange"), 10));
        orders.Add(new Order { CustomerId = C("Oakwood Cafe"),      Status = OrderStatus.ReadyForDelivery, CreatedAt = new DateTime(2026,  2,  4), RequestedDeliveryDate = d4.Date, Delivery = d4, TotalAmount = Total(i17), OrderItems = i17 });

        var i18 = Items((P("Butter"), 18), (P("All-Purpose"), 12), (P("Takeout"), 10), (P("Cola"), 3));
        orders.Add(new Order { CustomerId = C("Lakeview Grill"),     Status = OrderStatus.ReadyForDelivery, CreatedAt = new DateTime(2026,  2,  5), RequestedDeliveryDate = d4.Date, Delivery = d4, TotalAmount = Total(i18), OrderItems = i18 });

        var i19 = Items((P("Large Eggs"), 25), (P("Chicken"), 12), (P("Cola"), 10), (P("Takeout"), 6));
        orders.Add(new Order { CustomerId = C("Birchwood Hotel"),     Status = OrderStatus.ReadyForDelivery, CreatedAt = new DateTime(2026,  2,  6), RequestedDeliveryDate = d4.Date, Delivery = d4, TotalAmount = Total(i19), OrderItems = i19 });

        var i20 = Items((P("Potatoes"), 12), (P("Roma"), 10), (P("Ground Beef"), 6));
        orders.Add(new Order { CustomerId = C("Sunrise Bakery"),    Status = OrderStatus.ReadyForDelivery, CreatedAt = new DateTime(2026,  2,  7), RequestedDeliveryDate = d4.Date, Delivery = d4, TotalAmount = Total(i20), OrderItems = i20 });

        var i21 = Items((P("Roma"), 18), (P("Potatoes"), 15), (P("Ground Beef"), 8), (P("Nitrile"), 6));
        orders.Add(new Order { CustomerId = C("Copper Kettle Catering"),         Status = OrderStatus.ReadyForDelivery, CreatedAt = new DateTime(2026,  2,  8), RequestedDeliveryDate = d4.Date, Delivery = d4, TotalAmount = Total(i21), OrderItems = i21 });

        // ── March 2026 ────────────────────────────────────────────────────────
        var i22 = Items((P("Potatoes"), 25), (P("Roma"), 20), (P("Large Eggs"), 15), (P("Ground Beef"), 12), (P("Cola"), 5));
        orders.Add(new Order { CustomerId = C("Harbour Bistro"), Status = OrderStatus.ReadyForDelivery, CreatedAt = new DateTime(2026,  3,  3), RequestedDeliveryDate = d5.Date, Delivery = d5, TotalAmount = Total(i22), OrderItems = i22 });

        var i23 = Items((P("Potatoes"), 35), (P("Butter"), 20), (P("Orange"), 12), (P("Canola"), 8));
        orders.Add(new Order { CustomerId = C("Oakwood Cafe"),      Status = OrderStatus.ReadyForDelivery, CreatedAt = new DateTime(2026,  3,  4), RequestedDeliveryDate = d5.Date, Delivery = d5, TotalAmount = Total(i23), OrderItems = i23 });

        var i24 = Items((P("Roma"), 15), (P("Butter"), 12), (P("All-Purpose"), 10), (P("Takeout"), 8));
        orders.Add(new Order { CustomerId = C("Lakeview Grill"),     Status = OrderStatus.ReadyForDelivery, CreatedAt = new DateTime(2026,  3,  5), RequestedDeliveryDate = d5.Date, Delivery = d5, TotalAmount = Total(i24), OrderItems = i24 });

        var i25 = Items((P("Potatoes"), 10), (P("Large Eggs"), 8), (P("Cola"), 3), (P("Nitrile"), 4));
        orders.Add(new Order { CustomerId = C("Greenfield Deli"),       Status = OrderStatus.ReadyForDelivery, CreatedAt = new DateTime(2026,  3,  6), RequestedDeliveryDate = d5.Date, Delivery = d5, TotalAmount = Total(i25), OrderItems = i25 });

        var i26 = Items((P("Potatoes"), 15), (P("Roma"), 12), (P("Orange"), 8));
        orders.Add(new Order { CustomerId = C("Riverside Pub"),    Status = OrderStatus.ReadyForDelivery, CreatedAt = new DateTime(2026,  3,  7), RequestedDeliveryDate = d5.Date, Delivery = d5, TotalAmount = Total(i26), OrderItems = i26, Notes = "New customer — first order." });

        var i27 = Items((P("Chicken"), 18), (P("Cola"), 10), (P("Takeout"), 8), (P("Nitrile"), 6));
        orders.Add(new Order { CustomerId = C("Birchwood Hotel"),     Status = OrderStatus.ReadyForDelivery, CreatedAt = new DateTime(2026,  3,  8), RequestedDeliveryDate = d5.Date, Delivery = d5, TotalAmount = Total(i27), OrderItems = i27 });

        // Rejected — customer cancelled
        var i28 = Items((P("Roma"), 15), (P("Ground Beef"), 5));
        orders.Add(new Order { CustomerId = C("Copper Kettle Catering"),         Status = OrderStatus.Rejected,         CreatedAt = new DateTime(2026,  3,  9), TotalAmount = Total(i28), OrderItems = i28, Notes = "Order cancelled by customer." });

        // ── April 2026 — ReadyForDelivery (d6, completed Apr 11) ─────────────
        var i29 = Items((P("Potatoes"), 28), (P("Roma"), 22), (P("Ground Beef"), 14), (P("Canola"), 6), (P("Cola"), 4));
        orders.Add(new Order { CustomerId = C("Harbour Bistro"), Status = OrderStatus.ReadyForDelivery, CreatedAt = new DateTime(2026,  4,  2), RequestedDeliveryDate = d6.Date, Delivery = d6, TotalAmount = Total(i29), OrderItems = i29 });

        var i30 = Items((P("Potatoes"), 30), (P("Large Eggs"), 25), (P("Butter"), 10), (P("Orange"), 10));
        orders.Add(new Order { CustomerId = C("Oakwood Cafe"),      Status = OrderStatus.ReadyForDelivery, CreatedAt = new DateTime(2026,  4,  4), RequestedDeliveryDate = d6.Date, Delivery = d6, TotalAmount = Total(i30), OrderItems = i30 });

        var i31 = Items((P("Butter"), 14), (P("All-Purpose"), 8), (P("Takeout"), 6));
        orders.Add(new Order { CustomerId = C("Lakeview Grill"),     Status = OrderStatus.ReadyForDelivery, CreatedAt = new DateTime(2026,  4,  7), RequestedDeliveryDate = d6.Date, Delivery = d6, TotalAmount = Total(i31), OrderItems = i31 });

        // ── April 2026 — Accepted (d7, May 2) ────────────────────────────────
        var i32 = Items((P("Potatoes"), 25), (P("Roma"), 20), (P("Orange"), 8));
        orders.Add(new Order { CustomerId = C("Oakwood Cafe"),      Status = OrderStatus.Accepted,         CreatedAt = new DateTime(2026,  4, 15), RequestedDeliveryDate = d7.Date, Delivery = d7, TotalAmount = Total(i32), OrderItems = i32 });

        var i33 = Items((P("Large Eggs"), 22), (P("Chicken"), 14), (P("Cola"), 8));
        orders.Add(new Order { CustomerId = C("Birchwood Hotel"),     Status = OrderStatus.Accepted,         CreatedAt = new DateTime(2026,  4, 16), RequestedDeliveryDate = d7.Date, Delivery = d7, TotalAmount = Total(i33), OrderItems = i33 });

        var i34 = Items((P("Roma"), 18), (P("Potatoes"), 15), (P("Ground Beef"), 6), (P("Nitrile"), 5));
        orders.Add(new Order { CustomerId = C("Copper Kettle Catering"),         Status = OrderStatus.Accepted,         CreatedAt = new DateTime(2026,  4, 18), RequestedDeliveryDate = d7.Date, Delivery = d7, TotalAmount = Total(i34), OrderItems = i34 });

        var i35 = Items((P("Potatoes"), 18), (P("Roma"), 15), (P("Orange"), 10));
        orders.Add(new Order { CustomerId = C("Riverside Pub"),    Status = OrderStatus.Accepted,         CreatedAt = new DateTime(2026,  4, 20), RequestedDeliveryDate = d7.Date, Delivery = d7, TotalAmount = Total(i35), OrderItems = i35 });

        // ── April 2026 — Pending (d8, May 9) ─────────────────────────────────
        var i36 = Items((P("Potatoes"), 30), (P("Roma"), 25), (P("Ground Beef"), 15), (P("Cola"), 6));
        orders.Add(new Order { CustomerId = C("Harbour Bistro"), Status = OrderStatus.Pending,          CreatedAt = new DateTime(2026,  4, 24), RequestedDeliveryDate = d8.Date, Delivery = d8, TotalAmount = Total(i36), OrderItems = i36 });

        var i37 = Items((P("Roma"), 12), (P("Butter"), 10), (P("Ground Beef"), 6));
        orders.Add(new Order { CustomerId = C("Sunrise Bakery"),    Status = OrderStatus.Pending,          CreatedAt = new DateTime(2026,  4, 25), RequestedDeliveryDate = d8.Date, Delivery = d8, TotalAmount = Total(i37), OrderItems = i37 });

        var i38 = Items((P("Potatoes"), 8), (P("Large Eggs"), 6), (P("Takeout"), 4));
        orders.Add(new Order { CustomerId = C("Greenfield Deli"),       Status = OrderStatus.Pending,          CreatedAt = new DateTime(2026,  4, 28), RequestedDeliveryDate = d8.Date, Delivery = d8, TotalAmount = Total(i38), OrderItems = i38 });

        db.Orders.AddRange(orders);
        await db.SaveChangesAsync();
    }

    if (!db.SupplyItems.Any())
    {
        db.SupplyItems.AddRange(
            new SupplyItem { Name = "Pallet Wrap (18in roll)",         QuantityNeeded = 24, Unit = "roll", Category = "Warehouse" },
            new SupplyItem { Name = "Corrugated Boxes (bundle/25)",    QuantityNeeded = 30, Unit = "bundle", Category = "Warehouse" },
            new SupplyItem { Name = "Packing Tape (case/36)",          QuantityNeeded = 10, Unit = "case", Category = "Warehouse" },
            new SupplyItem { Name = "Shipping Labels (roll/500)",      QuantityNeeded = 12, Unit = "roll", Category = "Warehouse" },
            new SupplyItem { Name = "Bin Labels (roll/1000)",          QuantityNeeded =  6, Unit = "roll", Category = "Warehouse" },
            new SupplyItem { Name = "Gel Ice Packs (case/48)",         QuantityNeeded = 20, Unit = "case", Category = "Cold Chain" },
            new SupplyItem { Name = "Insulated Pallet Covers",         QuantityNeeded =  8, Unit = "each", Category = "Cold Chain" },
            new SupplyItem { Name = "Probe Thermometers",              QuantityNeeded =  6, Unit = "each", Category = "Cold Chain" },
            new SupplyItem { Name = "Cooler Blankets",                 QuantityNeeded = 10, Unit = "each", Category = "Cold Chain" },
            new SupplyItem { Name = "Load Straps (10-pack)",           QuantityNeeded =  6, Unit = "pack", Category = "Fleet" },
            new SupplyItem { Name = "Hand Truck Tires",                QuantityNeeded =  4, Unit = "each", Category = "Fleet" },
            new SupplyItem { Name = "Windshield Washer Fluid (4L)",    QuantityNeeded = 12, Unit = "jug", Category = "Fleet" },
            new SupplyItem { Name = "Nitrile Gloves (box/100)",        QuantityNeeded = 15, Unit = "box", Category = "Sanitation" },
            new SupplyItem { Name = "Food-Safe Sanitizer (4L)",        QuantityNeeded =  8, Unit = "jug", Category = "Sanitation" },
            new SupplyItem { Name = "Dish Soap (4L jug)",              QuantityNeeded = 10, Unit = "jug", Category = "Sanitation" },
            new SupplyItem { Name = "Paper Towels (case/12)",          QuantityNeeded =  6, Unit = "case", Category = "Sanitation" },
            new SupplyItem { Name = "Thermal Printer Rolls (case/24)", QuantityNeeded =  8, Unit = "case", Category = "Office" },
            new SupplyItem { Name = "Invoice Paper (box/2500)",        QuantityNeeded =  5, Unit = "box", Category = "Office" },
            new SupplyItem { Name = "Printer Toner",                   QuantityNeeded =  4, Unit = "each", Category = "Office" }
        );
        await db.SaveChangesAsync();
    }
}

static async Task SeedDemoAccountsAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

    var accounts = new[]
    {
        (Email: "owner@rwms.com",   Password: "Owner@2024",   First: "Demo",   Last: "Owner",   Role: "Owner"),
        (Email: "manager@rwms.com", Password: "Manager@2024", First: "Demo",   Last: "Manager", Role: "Manager"),
    };

    foreach (var (email, password, first, last, role) in accounts)
    {
        if (await userManager.FindByEmailAsync(email) is not null) continue;

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FirstName = first,
            LastName = last,
            IsActive = true
        };

        var result = await userManager.CreateAsync(user, password);
        if (result.Succeeded)
            await userManager.AddToRoleAsync(user, role);
    }
}

static async Task SeedRolesAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();

    var roles = new[]
    {
        new ApplicationRole { Name = "Owner", Description = "Restaurant owner — full access including financial reports" },
        new ApplicationRole { Name = "Manager", Description = "Restaurant manager — products, orders, and supply management" },
        new ApplicationRole { Name = "Customer", Description = "Wholesale customer — portal access to browse products and place orders" },
    };

    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role.Name!))
            await roleManager.CreateAsync(role);
    }
}
