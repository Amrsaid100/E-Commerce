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
            
            
            var exsitingitem = Cart.Items.FirstOrDefault(c => c.ProductVariantId == item.ProductVariantId);

            if (exsitingitem != null)
            {
                exsitingitem.Quantity += item.Quantity;
            }
            else
            {
                
                var Cart_item = new CartItem
                {
                    CartId = Cart.Id,
                    ProductVariantId = item.ProductVariantId,
                    ProductName = item.ProductName,
                    UnitPrice = item.UnitPrice,
                    Quantity = item.Quantity,
                };
                Cart.Items.Add(Cart_item);
            }
            
            await work.SaveChangesAsync();    
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

            if (Cart == null)
            {

                return null;
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
            foreach(var item in Cart.Items)
            {
                //var orderItemDto1=new OrderItemDto()
                //{
                //    ProductVariantId = item.ProductVariantId,
                //    ProductName = item.ProductName,
                //    Quantity = item.Quantity,
                //    UnitPrice = item.UnitPrice
                //};

                var OrderItem1 = new OrderItem()
                {
                    ProductVariantId = item.ProductVariantId,
                    ProductName = item.ProductName,
                    Quantity = item.Quantity,
                    UnitePrice = item.UnitPrice
                };
                totalPrice += item.UnitPrice * item.Quantity;

                //OrderItemDtos.Add(orderItemDto1);
                OrderItems.Add(OrderItem1);
            }
            Order NewOrder = new Order()
            {
                UserId = UserId,
                Items = OrderItems,
                Email = CheckOut.Email,
                Street = CheckOut.Street,
                City = CheckOut.City,
                PhoneNumber = CheckOut.PhoneNumber,
                TotalAmount = totalPrice,
                Status = OrderStatus.PendingPayment,
                CreatedAt = DateTime.UtcNow
            };

            //UserOrderDto FinalOrder = new UserOrderDto()
            //{
            //    Items = OrderItemDtos,
            //    TotalPrice = totalPrice
            //};

            await work.Orders.AddAsync(NewOrder);
            //Cart.Items.Clear();
            await work.SaveChangesAsync();

            return NewOrder.OrderId;
        }

        public async Task FromGuestCartToUserCart(int UserId, CartDto GuestCart)
        {
            if(GuestCart == null || GuestCart.Items == null || !GuestCart.Items.Any()||UserId==0)
                return;
            var Cart = await work.Carts.GetByUserIdAsync(UserId);
            List<CartItem> CartItems = new List<CartItem>();
            foreach(var item in GuestCart.Items)
            {
                var cartItem = new CartItem()
                {
                    ProductVariantId = item.ProductVariantId,
                    ProductName = item.ProductName,
                    UnitPrice = item.UnitPrice,
                    Quantity = item.Quantity,
                };
                CartItems.Add(cartItem);
            }
            if (Cart == null)
            {
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
                return;
            }

        }

    }
}
