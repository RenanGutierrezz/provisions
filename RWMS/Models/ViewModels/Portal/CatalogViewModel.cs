namespace RWMS.Models.ViewModels.Portal;

public class CatalogViewModel
{
    public List<CatalogProductViewModel> Products { get; set; } = [];
    public List<string> Categories { get; set; } = [];
    public string? Search { get; set; }
    public string? SelectedCategory { get; set; }
}

public class CatalogProductViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public string Unit { get; set; } = string.Empty;
    public string? Category { get; set; }
    public int? ParLevel { get; set; }
    public DateTime? LastOrderedAt { get; set; }

    // Product art lives in wwwroot/img/products — see the README there for the
    // expected filename and image spec. Add a case per product once its file is
    // in place; unmapped products use the card's typographic panel instead.
    public string? ImageSlug => Name switch
    {
        _ => null
    };
}
