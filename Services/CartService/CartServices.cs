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


        public async Task<int> CheckOutAsync(int UserId, CheckOutDto CheckOut)
        {
            var Cart = await work.Carts.GetByUserIdAsync(UserId);

            if (Cart == null || !Cart.Items.Any())
            {
                return 0;
            }

            List<OrderItem> OrderItems = new List<OrderItem>();
            decimal totalPrice = 0;

            foreach (var item in Cart.Items)
            {
                // ✅ Stock Decrement Logic - أضف هذا
                var variant = await work.ProductVariants.GetByIdAsync(item.ProductVariantId);
                if (variant != null)
                {
                    if (variant.Quantity < item.Quantity)
                    {
                        throw new InvalidOperationException($"Insufficient stock for {item.ProductName}. Available: {variant.Quantity}, Requested: {item.Quantity}");
                    }
                    variant.Quantity -= item.Quantity;
                }

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

            // ✅ Shipping Cost Calculation - أضف هذا
            decimal shippingCost = 0;
            if (CheckOut.GovernorateId.HasValue && CheckOut.GovernorateId.Value > 0)
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
                City = CheckOut.City,
                PhoneNumber = CheckOut.PhoneNumber,
                Neighborhood = CheckOut.Neighborhood,
                GovernorateId = CheckOut.GovernorateId,  // جديد
                ShippingCost = shippingCost,  // جديد
                TotalAmount = totalPrice + shippingCost,  // عدل هنا
                Status = OrderStatus.PendingPayment,
                CreatedAt = DateTime.UtcNow
            };

            await work.Orders.AddAsync(NewOrder);
            await work.SaveChangesAsync();

            return NewOrder.Id;
        }


        public async Task FromGuestCartToUserCart(int UserId, CartDto GuestCart)
        {
            if(GuestCart == null || GuestCart.Items == null || !GuestCart.Items.Any()||UserId==0)
                return;
            var Cart = await work.Carts.GetByUserIdAsync(UserId);
            if (Cart == null)
            {
              
                List<CartItem> CartItems = new List<CartItem>();
                foreach(var item in GuestCart.Items)
                {
                    var cartItem = new CartItem()
                    {
                        ProductVariantId = (int)item.ProductVariantId,
                        ProductName = item.ProductName,
                        UnitPrice = item.UnitPrice,
                        Quantity = item.Quantity,
                    };
                    CartItems.Add(cartItem);
                }
                Cart = new Cart()
                {
                    UserId = UserId,
                    Items = CartItems
                };
                await work.Carts.AddAsync(Cart);
                await work.SaveChangesAsync();
            }
            else
            {
                // إذا لدى المستخدم سلة بالفعل، دمج العناصر
                foreach(var item in GuestCart.Items)
                {
                    var existing = Cart.Items.FirstOrDefault(c => c.ProductVariantId == (int)item.ProductVariantId);
                    if (existing != null)
                    {
                        existing.Quantity += item.Quantity;
                    }
                    else
                    {
                        var cartItem = new CartItem()
                        {
                            CartId = Cart.Id,
                            ProductVariantId = (int)item.ProductVariantId,
                            ProductName = item.ProductName,
                            UnitPrice = item.UnitPrice,
                            Quantity = item.Quantity,
                        };
                        Cart.Items.Add(cartItem);
                    }
                }
                await work.SaveChangesAsync();
            }

        }

    }
}
