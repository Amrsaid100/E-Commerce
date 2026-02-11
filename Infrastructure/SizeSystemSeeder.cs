using E_Commerce.DataContext;
using E_Commerce.Entities;
using Microsoft.EntityFrameworkCore;

namespace E_Commerce.Infrastructure
{
    public static class SizeSystemSeeder
    {
        public static async Task SeedSizeDataAsync(EcommerceDbContext context)
        {
            // Check if categories already exist
            var existingCategories = await context.Categories.ToListAsync();
            
            var categoriesToAdd = new List<Category>();

            // 1. بناطيل (Numeric: 30-40)
            if (!existingCategories.Any(c => c.Name == "بناطيل" || c.Name == "Pants"))
            {
                categoriesToAdd.Add(new Category
                {
                    Name = "بناطيل",
                    Description = "بناطيل جينز ورسمية بمقاسات 30-40",
                    SizeType = SizeType.Numeric,
                    MinSize = 30,
                    MaxSize = 40
                });
            }
            else
            {
                var pants = existingCategories.FirstOrDefault(c => c.Name == "بناطيل" || c.Name == "Pants");
                if (pants != null)
                {
                    pants.SizeType = SizeType.Numeric;
                    pants.MinSize = 30;
                    pants.MaxSize = 40;
                    pants.AvailableSizes = null;
                }
            }

            // 2. أحذية (Numeric: 40-45)
            if (!existingCategories.Any(c => c.Name == "أحذية" || c.Name == "Shoes"))
            {
                categoriesToAdd.Add(new Category
                {
                    Name = "أحذية",
                    Description = "أحذية رياضية وكاجوال بمقاسات 40-45",
                    SizeType = SizeType.Numeric,
                    MinSize = 40,
                    MaxSize = 45
                });
            }
            else
            {
                var shoes = existingCategories.FirstOrDefault(c => c.Name == "أحذية" || c.Name == "Shoes");
                if (shoes != null)
                {
                    shoes.SizeType = SizeType.Numeric;
                    shoes.MinSize = 40;
                    shoes.MaxSize = 45;
                    shoes.AvailableSizes = null;
                }
            }

            // 3. تيشيرتات (Clothing: S-XXXL)
            if (!existingCategories.Any(c => c.Name == "تيشيرتات" || c.Name == "T-Shirts"))
            {
                categoriesToAdd.Add(new Category
                {
                    Name = "تيشيرتات",
                    Description = "تيشيرتات قطن ورياضية بمقاسات S-XXXL",
                    SizeType = SizeType.Clothing,
                    AvailableSizes = "S,M,L,XL,XXL,XXXL"
                });
            }
            else
            {
                var tshirts = existingCategories.FirstOrDefault(c => c.Name == "تيشيرتات" || c.Name == "T-Shirts");
                if (tshirts != null)
                {
                    tshirts.SizeType = SizeType.Clothing;
                    tshirts.MinSize = null;
                    tshirts.MaxSize = null;
                    tshirts.AvailableSizes = "S,M,L,XL,XXL,XXXL";
                }
            }

            // 4. قمصان (Clothing: S-XXXL)
            if (!existingCategories.Any(c => c.Name == "قمصان" || c.Name == "Shirts"))
            {
                categoriesToAdd.Add(new Category
                {
                    Name = "قمصان",
                    Description = "قمصان رسمية وكاجوال بمقاسات S-XXXL",
                    SizeType = SizeType.Clothing,
                    AvailableSizes = "S,M,L,XL,XXL,XXXL"
                });
            }
            else
            {
                var shirts = existingCategories.FirstOrDefault(c => c.Name == "قمصان" || c.Name == "Shirts");
                if (shirts != null)
                {
                    shirts.SizeType = SizeType.Clothing;
                    shirts.MinSize = null;
                    shirts.MaxSize = null;
                    shirts.AvailableSizes = "S,M,L,XL,XXL,XXXL";
                }
            }

            // 5. ساعات (None)
            if (!existingCategories.Any(c => c.Name == "ساعات" || c.Name == "Watches"))
            {
                categoriesToAdd.Add(new Category
                {
                    Name = "ساعات",
                    Description = "ساعات يد رجالية ونسائية",
                    SizeType = SizeType.None
                });
            }
            else
            {
                var watches = existingCategories.FirstOrDefault(c => c.Name == "ساعات" || c.Name == "Watches");
                if (watches != null)
                {
                    watches.SizeType = SizeType.None;
                    watches.MinSize = null;
                    watches.MaxSize = null;
                    watches.AvailableSizes = null;
                }
            }

            // 6. حقائب (None)
            if (!existingCategories.Any(c => c.Name == "حقائب" || c.Name == "Bags"))
            {
                categoriesToAdd.Add(new Category
                {
                    Name = "حقائب",
                    Description = "حقائب يد وظهر",
                    SizeType = SizeType.None
                });
            }
            else
            {
                var bags = existingCategories.FirstOrDefault(c => c.Name == "حقائب" || c.Name == "Bags");
                if (bags != null)
                {
                    bags.SizeType = SizeType.None;
                    bags.MinSize = null;
                    bags.MaxSize = null;
                    bags.AvailableSizes = null;
                }
            }

            // Add new categories
            if (categoriesToAdd.Any())
            {
                await context.Categories.AddRangeAsync(categoriesToAdd);
            }

            await context.SaveChangesAsync();
            
            Console.WriteLine($"✅ Size system seeded: {categoriesToAdd.Count} new categories added, {existingCategories.Count} updated");
        }
    }
}
