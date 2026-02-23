using FluentAssertions;
using OnlineTechShop_WebApp.Models;
using OnlineTechShop_WebApp.Services;
using OnlineTechShop_WebApp.Tests.Infrastructure;

namespace OnlineTechShop_WebApp.Tests.Services
{
    public class OrderServiceTests : TestBase
    {
        private readonly OrderService _service;
        public OrderServiceTests()
        {
            _service = new OrderService(Context);
        }

        private async Task<Product> AddProduct(string name, decimal price, int stock)
        {
            var product = new Product
            {
                Name = name,
                Price = price,
                StockQuantity = stock,
                IsActive = true
            };

            Context.Products.Add(product);
            await Context.SaveChangesAsync();
            return product;
        }

        private async Task<User> AddUser(int id = 1)
        {
            var user = new User
            {
                Id = id,
                Login = $"user{id}",
                PasswordHash = "hash",
                Role = UserRole.Customer
            };

            Context.Users.Add(user);
            await Context.SaveChangesAsync();
            return user;
        }

        //при замовленні має створюватися запис, а к-ть товару на складі - зменшуватися
        [Fact]
        public async Task CreateOrder_Should_Create_Order_And_Decrease_Stock()
        {
            var product = await AddProduct("Laptop", 1000, 5);
            await AddUser(1);

            var products = new Dictionary<int, int> {{ product.Id, 2 }};

            var order = await _service.CreateOrderAsync(1, products);

            order.TotalPrice.Should().Be(2000);
            order.Items.Should().HaveCount(1);

            var dbProduct = await Context.Products.FindAsync(product.Id);
            dbProduct!.StockQuantity.Should().Be(3);
        }
        
        //не можна створити порожнє замовлення (без товарів)
        [Fact]
        public async Task CreateOrder_Should_Throw_When_Empty()
        {
            await AddUser(1);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.CreateOrderAsync(1, new Dictionary<int, int>()));
        }

        //помилка, якщо в кошику є товар, якого немає в базі
        [Fact]
        public async Task CreateOrder_Should_Throw_When_Product_Not_Found()
        {
            await AddUser(1);

            var products = new Dictionary<int, int> {{ 999, 1 }};

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.CreateOrderAsync(1, products));
        }

        //помилка, якщо к-ть товару в замовленні 0 або менше
        [Fact]
        public async Task CreateOrder_Should_Throw_When_Quantity_Invalid()
        {
            var product = await AddProduct("Phone", 500, 5);
            await AddUser(1);

            var products = new Dictionary<int, int> {{ product.Id, 0 }};

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.CreateOrderAsync(1, products));
        }

        //помилка, якщо клієнт хоче купити більше товарів, ніж є на складі
        [Fact]
        public async Task CreateOrder_Should_Throw_When_Insufficient_Stock()
        {
            var product = await AddProduct("Tablet", 300, 1);
            await AddUser(1);

            var products = new Dictionary<int, int> {{ product.Id, 5 }};

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.CreateOrderAsync(1, products));
        }

        //користувач має бачити лише свої замовлення, а не чужі
        [Fact]
        public async Task GetUserOrders_Should_Return_Only_User_Orders()
        {
            var product = await AddProduct("PC", 1000, 10);
            await AddUser(1);
            await AddUser(2);

            await _service.CreateOrderAsync(1, new Dictionary<int, int> { { product.Id, 1 } });
            await _service.CreateOrderAsync(2, new Dictionary<int, int> { { product.Id, 1 } });

            var result = await _service.GetUserOrdersAsync(1);

            result.Should().HaveCount(1);
            result.First().UserId.Should().Be(1);
        }

        //система має повертати повний список усіх замовлень (для адміна)
        [Fact]
        public async Task GetAllOrders_Should_Return_All()
        {
            var product = await AddProduct("Monitor", 200, 10);
            await AddUser(1);
            await AddUser(2);

            await _service.CreateOrderAsync(1, new Dictionary<int, int> { { product.Id, 1 } });
            await _service.CreateOrderAsync(2, new Dictionary<int, int> { { product.Id, 1 } });

            var result = await _service.GetAllOrdersAsync();

            result.Should().HaveCount(2);
        }

        //статус замовлення (наприклад, з "нове" на "оплачено") має змінюватися
        [Fact]
        public async Task ChangeStatus_Should_Update_Status()
        {
            var product = await AddProduct("Mouse", 50, 10);
            await AddUser(1);

            var order = await _service.CreateOrderAsync(1, new Dictionary<int, int>
            {
                { product.Id, 1 }
            });

            await _service.ChangeStatusAsync(order.Id, OrderStatus.Paid);

            var dbOrder = await Context.Orders.FindAsync(order.Id);
            dbOrder!.Status.Should().Be(OrderStatus.Paid);
        }

        //помилка при спробі змінити статус неіснуючого замовлення
        [Fact]
        public async Task ChangeStatus_Should_Throw_When_Order_Not_Found()
        {
            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _service.ChangeStatusAsync(999, OrderStatus.Paid));
        }

        //заборона змінювати статус замовлення, яке вже завершене (completed)
        [Fact]
        public async Task ChangeStatus_Should_Throw_When_Final_Status()
        {
            var product = await AddProduct("TV", 800, 5);
            await AddUser(1);

            var order = await _service.CreateOrderAsync(1, new Dictionary<int, int>
            {
                { product.Id, 1 }
            });

            await _service.ChangeStatusAsync(order.Id, OrderStatus.Completed);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.ChangeStatusAsync(order.Id, OrderStatus.Paid));
        }
    }
}