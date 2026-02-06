using E_Commerce.Dtos.CartDto;
using E_Commerce.Dtos.OrderDto;
using E_Commerce.Dtos.UserDto;
using E_Commerce.Entities;
using E_Commerce.UnitOfWork;
using Microsoft.AspNetCore.Http.HttpResults;

namespace E_Commerce.Services.CartService
{
    public class CartServices : ICartService
    {
        private readonly IUnitOfWork work;
        
        public CartServices(IUnitOfWork work)
        {
            this.work = work;
        }
        public async Task AddToCart(int UserId, CartItemDto? item)
        {
            try
            {
                if (item == null)
                    return;

                var Cart = await work.Carts.GetByUserIdAsync(UserId);
                if (Cart == null)
                {
                    Cart = new Cart()
                    {
                        UserId = UserId,
                        Items = new List<CartItem>()
                    };
                    await work.Carts.AddAsync(Cart);
                }

                ProductVariant variant = null;
                if (item.ProductVariantId.HasValue && item.ProductVariantId.Value > 0)
                {
                    variant = await work.ProductVariants.GetByIdAsync(item.ProductVariantId.Value);
                }

                // إذا لم يوجد variantId أو لم يوجد variant، ابحث عن أول variant للمنتج
                if (variant == null && item.ProductId.HasValue)
                {
                    var allVariants = await work.ProductVariants.GetAllAsync();
                    variant = allVariants.FirstOrDefault(v => v.ProductId == item.ProductId.Value);
                }

                if (variant == null)
                {
                    throw new InvalidOperationException("No product variant found for this product. Please contact support or add a variant.");
                }

                var exsitingitem = Cart.Items.FirstOrDefault(c => c.ProductVariantId == variant.Id);

                if (exsitingitem != null)
                {
                    exsitingitem.Quantity += item.Quantity;
                }
                else
                {
                    var Cart_item = new CartItem
                    {
                        CartId = Cart.Id,
                        ProductVariantId = variant.Id,
                        ProductName = item.ProductName,
                        UnitPrice = item.UnitPrice,
                        Quantity = item.Quantity,
                    };
                    Cart.Items.Add(Cart_item);
                }

                await work.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Exception in AddToCart: " + ex.ToString());
                throw;
            }
        }

        public async Task RemoveFromCart(int UserId, CartItemDto item)
        {

            if (item == null)
                return;

            var Cart = await work.Carts.GetByUserIdAsync(UserId);
            if (Cart == null)
            {
                return;
            }


            var exsitingitem = Cart.Items.FirstOrDefault(c => c.ProductVariantId == item.ProductVariantId);

            if (exsitingitem != null)
            {
                if (exsitingitem.Quantity > item.Quantity)
                    exsitingitem.Quantity -= item.Quantity;
                else
                    Cart.Items.Remove(exsitingitem);
            }
            else
            {
                return;
            }

            await work.SaveChangesAsync();
        }
        public async Task<CartDto> GetUserCart (int UserId)
        {
            var Cart = await work.Carts.GetByUserIdAsync(UserId);

            if (Cart == null || Cart.Items == null || !Cart.Items.Any())
            {
                return new CartDto { Items = new List<CartItemDto>(), TotalPrice = 0m };
            }
            else
            {
                var CartDtoList = new List<CartItemDto>();
                decimal totalPrice = 0;
                foreach (var item in Cart.Items)
                {
                    var cdt = new CartItemDto()
                    {
                        ProductVariantId = item.ProductVariantId,
                        Quantity = item.Quantity,
                        ProductName = item.ProductName,
                        UnitPrice = item.UnitPrice,
                    };
                    totalPrice += item.UnitPrice*item.Quantity;
                    CartDtoList.Add(cdt);
                }

                return new CartDto()
                {
                    Items=CartDtoList,
                    TotalPrice=totalPrice,
                    TotalQuantity=CartDtoList.Sum(x=>x.Quantity)
                };
            }
        }

        public async Task ClearCart (int UserId)
        {
            var Cart = await work.Carts.GetByUserIdAsync(UserId);

            if (Cart == null)
            {

                return;
            }
            Cart.Items.Clear();
            Cart.TotalPrice = 0;

            await work.SaveChangesAsync();
        }


        public async Task<int> CheckOutAsync(int UserId,CheckOutDto CheckOut)
        {
            var Cart = await work.Carts.GetByUserIdAsync(UserId);

            if (Cart == null || !Cart.Items.Any())
            {
                return 0;
            }
            //List<OrderItemDto> OrderItemDtos = new List<OrderItemDto>();
            List<OrderItem> OrderItems = new List<OrderItem>();
            decimal totalPrice = 0;
            
            // IMPORTANT: Validate stock BEFORE creating order items
            foreach(var item in Cart.Items)
            {
                var variant = await work.ProductVariants.GetByIdAsync(item.ProductVariantId);
                if (variant == null)
                {
                    throw new InvalidOperationException($"Product variant not found");
                }
                
                // Check if enough stock available
                if (variant.Quantity < item.Quantity)
                {
                    throw new InvalidOperationException($"Insufficient stock for '{item.ProductName}'. Only {variant.Quantity} available, but you requested {item.Quantity}.");
                }
            }
            
            // IMPORTANT: Decrease stock for each ordered item
            foreach(var item in Cart.Items)
            {
                // Get the product variant and decrease stock
                var variant = await work.ProductVariants.GetByIdAsync(item.ProductVariantId);
                
                // Decrease stock
                variant.Quantity -= item.Quantity;
                await work.ProductVariants.UpdatdeAsync(variant);
                Console.WriteLine($"📦 Stock decreased for {item.ProductName}: {variant.Quantity + item.Quantity} → {variant.Quantity}");

                var OrderItem1 = new OrderItem()
                {
                    ProductVariantId = item.ProductVariantId,
                    ProductName = item.ProductName,
                    Quantity = item.Quantity,
                    UnitePrice = item.UnitPrice
                };
                totalPrice += item.UnitPrice * item.Quantity;

                OrderItems.Add(OrderItem1);
            }
            // Calculate shipping cost based on governorate
            decimal shippingCost = 0;
            if (CheckOut.GovernorateId.HasValue)
            {
                var governorate = await work.Governorates.GetGovernorateByIdAsync(CheckOut.GovernorateId.Value);
                if (governorate != null)
                {
                    shippingCost = governorate.ShippingCost;
                }
            }

            Order NewOrder = new Order()
            {
                UserId = UserId,
                Items = OrderItems,
                Email = CheckOut.Email,
                Street = CheckOut.Street,
                PhoneNumber = CheckOut.PhoneNumber,
                Neighborhood = CheckOut.Neighborhood,
                GovernorateId = CheckOut.GovernorateId,
                ShippingCost = shippingCost,
                TotalAmount = totalPrice + shippingCost,
                Status = OrderStatus.PendingPayment,
                CreatedAt = DateTime.UtcNow
            };

            //UserOrderDto FinalOrder = new UserOrderDto()
            //{
            //    Items = OrderItemDtos,
            //    TotalPrice = totalPrice
            //};

            await work.Orders.AddAsync(NewOrder);
            
            // Clear cart after successful order
            Cart.Items.Clear();
            await work.SaveChangesAsync();

            return NewOrder.Id;
        }

        public async Task FromGuestCartToUserCart(int UserId, CartDto GuestCart)
        {
            if(GuestCart == null || GuestCart.Items == null || !GuestCart.Items.Any()||UserId==0)
                return;
            
            Console.WriteLine($"🔄 FromGuestCartToUserCart: UserId={UserId}, Items={GuestCart.Items.Count}");
            
            var Cart = await work.Carts.GetByUserIdAsync(UserId);
            if (Cart == null)
            {
                // If user has no cart, create one and try to resolve variant ids for guest items
                List<CartItem> CartItems = new List<CartItem>();
                // Fetch all variants once to allow fallback from ProductId -> VariantId
                var allVariants = await work.ProductVariants.GetAllAsync();
                Console.WriteLine($"📦 Total variants in database: {allVariants.Count()}");
                
                foreach (var item in GuestCart.Items)
                {
                    Console.WriteLine($"🔍 Processing guest item: ProductId={item.ProductId}, ProductVariantId={item.ProductVariantId}, Name={item.ProductName}");
                    
                    int? resolvedVariantId = item.ProductVariantId ?? item.ProductId;
                    if (!resolvedVariantId.HasValue)
                    {
                        // Try to resolve by product id using available variants
                        var found = allVariants.FirstOrDefault(v => v.ProductId == item.ProductId);
                        if (found != null)
                        {
                            resolvedVariantId = found.Id;
                            Console.WriteLine($"✅ Resolved variant ID from ProductId: {resolvedVariantId}");
                        }
                    }

                    if (!resolvedVariantId.HasValue)
                    {
                        // Skip items we cannot resolve to a variant
                        Console.Error.WriteLine($"⚠️ Warning: skipping guest cart item '{item?.ProductName}' - no variant or product mapping available.");
                        continue;
                    }
                    
                    // Verify the variant actually exists
                    var variant = allVariants.FirstOrDefault(v => v.Id == resolvedVariantId.Value);
                    if (variant == null)
                    {
                        Console.Error.WriteLine($"⚠️ Warning: Variant ID {resolvedVariantId} not found in database for item '{item?.ProductName}'");
                        // Try to find any variant for this product
                        var anyVariant = allVariants.FirstOrDefault(v => v.ProductId == item.ProductId);
                        if (anyVariant != null)
                        {
                            resolvedVariantId = anyVariant.Id;
                            Console.WriteLine($"✅ Found alternative variant ID: {resolvedVariantId}");
                        }
                        else
                        {
                            Console.Error.WriteLine($"❌ Skipping item - no variants found for ProductId {item.ProductId}");
                            continue;
                        }
                    }

                    var cartItem = new CartItem()
                    {
                        ProductVariantId = resolvedVariantId.Value,
                        ProductName = item.ProductName,
                        UnitPrice = item.UnitPrice,
                        Quantity = item.Quantity,
                    };
                    CartItems.Add(cartItem);
                    Console.WriteLine($"✅ Added item to cart: {item.ProductName}");
                }
                
                Console.WriteLine($"💾 Creating new cart with {CartItems.Count} items");
                Cart = new Cart()
                {
                    UserId = UserId,
                    Items = CartItems
                };
                await work.Carts.AddAsync(Cart);
                await work.SaveChangesAsync();
                Console.WriteLine($"✅ Cart saved successfully");
            }
            else
            {
                // Fetch variants once for fallback resolution
                var allVariants = await work.ProductVariants.GetAllAsync();
                Console.WriteLine($"📦 Merging into existing cart, Total variants: {allVariants.Count()}");

                foreach (var item in GuestCart.Items)
                {
                    Console.WriteLine($"🔍 Processing guest item: ProductId={item.ProductId}, ProductVariantId={item.ProductVariantId}");
                    
                    int? resolvedVariantId = item.ProductVariantId ?? item.ProductId;
                    if (!resolvedVariantId.HasValue)
                    {
                        var found = allVariants.FirstOrDefault(v => v.ProductId == item.ProductId);
                        if (found != null)
                        {
                            resolvedVariantId = found.Id;
                            Console.WriteLine($"✅ Resolved variant ID: {resolvedVariantId}");
                        }
                    }

                    if (!resolvedVariantId.HasValue)
                    {
                        Console.Error.WriteLine($"⚠️ Warning: skipping guest cart item '{item?.ProductName}' - no variant or product mapping available.");
                        continue;
                    }
                    
                    // Verify the variant exists
                    var variant = allVariants.FirstOrDefault(v => v.Id == resolvedVariantId.Value);
                    if (variant == null)
                    {
                        Console.Error.WriteLine($"⚠️ Variant ID {resolvedVariantId} not found, looking for alternative");
                        var anyVariant = allVariants.FirstOrDefault(v => v.ProductId == item.ProductId);
                        if (anyVariant != null)
                        {
                            resolvedVariantId = anyVariant.Id;
                            Console.WriteLine($"✅ Found alternative variant: {resolvedVariantId}");
                        }
                        else
                        {
                            Console.Error.WriteLine($"❌ Skipping item - no variants for ProductId {item.ProductId}");
                            continue;
                        }
                    }

                    var existing = Cart.Items.FirstOrDefault(c => c.ProductVariantId == resolvedVariantId.Value);
                    if (existing != null)
                    {
                        existing.Quantity += item.Quantity;
                        Console.WriteLine($"✅ Updated existing item quantity: {existing.ProductName}");
                    }
                    else
                    {
                        var cartItem = new CartItem()
                        {
                            CartId = Cart.Id,
                            ProductVariantId = resolvedVariantId.Value,
                            ProductName = item.ProductName,
                            UnitPrice = item.UnitPrice,
                            Quantity = item.Quantity,
                        };
                        Cart.Items.Add(cartItem);
                        Console.WriteLine($"✅ Added new item: {item.ProductName}");
                    }
                }
                await work.SaveChangesAsync();
                Console.WriteLine($"✅ Cart merge completed successfully");
            }
        }

        public async Task<int> BuyNowAsync(int UserId, BuyNowDto buyNowDto)
        {
            if (buyNowDto == null || buyNowDto.Item == null || UserId == 0)
                return 0;

            var variantId = buyNowDto.Item.VariantId ?? buyNowDto.Item.ProductId;
            
            var variant = await work.ProductVariants.GetByIdAsync(variantId);
            if (variant == null)
            {
                // Try to find any variant for this product
                var allVariants = await work.ProductVariants.GetAllAsync();
                variant = allVariants.FirstOrDefault(v => v.ProductId == buyNowDto.Item.ProductId);
                
                if (variant == null)
                {
                    throw new InvalidOperationException($"Product variant not found for product ID {buyNowDto.Item.ProductId}.");
                }
                
                variantId = variant.Id;
            }
            
            if (variant.Quantity < buyNowDto.Item.Quantity)
            {
                throw new InvalidOperationException($"Insufficient stock for '{buyNowDto.Item.ProductName}'. Only {variant.Quantity} available, but you requested {buyNowDto.Item.Quantity}.");
            }

            // IMPORTANT: Decrease stock when buying now
            variant.Quantity -= buyNowDto.Item.Quantity;
            await work.ProductVariants.UpdatdeAsync(variant);
            Console.WriteLine($"📦 Stock decreased for {buyNowDto.Item.ProductName}: {variant.Quantity + buyNowDto.Item.Quantity} → {variant.Quantity}");

            var orderItem = new OrderItem()
            {
                ProductVariantId = variantId,
                ProductName = buyNowDto.Item.ProductName,
                Quantity = buyNowDto.Item.Quantity,
                UnitePrice = buyNowDto.Item.Price
            };

            // Calculate shipping cost based on governorate
            decimal shippingCost = 0;
            if (buyNowDto.GovernorateId.HasValue)
            {
                var governorate = await work.Governorates.GetGovernorateByIdAsync(buyNowDto.GovernorateId.Value);
                if (governorate != null)
                {
                    shippingCost = governorate.ShippingCost;
                }
            }

            var itemTotal = buyNowDto.Item.Price * buyNowDto.Item.Quantity;
            
            var order = new Order()
            {
                UserId = UserId,
                Email = buyNowDto.Email,
                PhoneNumber = buyNowDto.PhoneNumber,
                GovernorateId = buyNowDto.GovernorateId,
                Street = buyNowDto.Street,
                Neighborhood = buyNowDto.Neighborhood,
                Status = OrderStatus.PendingPayment,
                ShippingCost = shippingCost,
                TotalAmount = itemTotal + shippingCost,
                Items = new List<OrderItem> { orderItem },
                CreatedAt = DateTime.UtcNow
            };

            await work.Orders.AddAsync(order);
            await work.SaveChangesAsync();

            return order.Id;
        }
    }
}
