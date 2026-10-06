# Product art

One image per product, referenced by `CatalogProductViewModel.ImageSlug`.

Name each file after the product, lowercase and hyphenated — for example:

    russet-potatoes.png
    chicken-breast.png
    takeout-containers.png

Spec — the catalogue grid relies on these being consistent:

- **Transparent PNG**, 1200 x 900 (4:3), matching `.portal-product-art`
- Same camera angle, same light direction and same scale across the whole set,
  otherwise the grid reads as a collage
- Subject centred, roughly 85% of the frame wide

To add art for a product, drop the file here and add a case to `ImageSlug` in
`Models/ViewModels/Portal/CatalogViewModel.cs`. Unmapped products fall back to
the card's typographic panel, so a missing file is never a broken image.
