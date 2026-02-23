using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OnlineTechShop_WebApp.Controllers;
using OnlineTechShop_WebApp.Models;
using OnlineTechShop_WebApp.Services;
using OnlineTechShop_WebApp.Tests.Infrastructure;
using System.Security.Claims;

namespace OnlineTechShop_WebApp.Tests.Controllers
{
    public class OrdersControllerTests : TestBase
    {
        private readonly OrderService _service;
        private readonly OrdersController _controller;

        public OrdersControllerTests()
        {
            _service = new OrderService(Context);
            _controller = new OrdersController(_service);
        }

        private async Task<Product> AddProduct(string name, decimal price, int stock = 5)
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

        private async Task<User> AddUser(int id = 1, string role = "Customer")
        {
            var user = new User
            {
                Id = id,
                Login = $"user{id}",
                PasswordHash = "hash",
                Role = role == "Admin" ? UserRole.Admin : UserRole.Customer
            };
            Context.Users.Add(user);
            await Context.SaveChangesAsync();
            return user;
        }

        private void SetUserClaims(int userId, string role)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Role, role)
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };
        }

        //перевірка створення замовлення через HTTP-запит
        [Fact]
        public async Task Create_Should_Create_Order_And_Decrease_Stock()
        {
            var product = await AddProduct("Laptop", 1000, 5);
            var user = await AddUser(1);

            SetUserClaims(user.Id, "Customer");

            var products = new Dictionary<int, int> { { product.Id, 2 } };

            var result = await _controller.Create(products) as OkObjectResult;
            var order = result!.Value as Order;

            order!.TotalPrice.Should().Be(2000);
            order.Items.Should().HaveCount(1);

            var dbProduct = await Context.Products.FindAsync(product.Id);
            dbProduct!.StockQuantity.Should().Be(3);
        }

        //перевірка, що API повертає замовлення саме того користувача, який авторизований
        [Fact]
        public async Task MyOrders_Should_Return_Only_User_Orders()
        {
            //arrange (підготовка) - створюємо об'єкти, готуємо дані
            var product = await AddProduct("PC", 1000, 10);
            var user1 = await AddUser(1);
            var user2 = await AddUser(2);

            // Створюємо замовлення для двох користувачів
            await _service.CreateOrderAsync(user1.Id, new Dictionary<int, int> { { product.Id, 1 } });
            await _service.CreateOrderAsync(user2.Id, new Dictionary<int, int> { { product.Id, 1 } });

            SetUserClaims(user1.Id, "Customer");

            //act (дія) - виклик методу, що хочемо перевірити 
            var result = await _controller.MyOrders() as OkObjectResult;

            //assert (перевірка) - порівняння отриманого з очікуваним
            var orders = result!.Value as List<Order>;

            orders.Should().HaveCount(1);
        }

        //перевірка доступу адміна до всіх замовлень через API
        [Fact]
        public async Task AllOrders_Should_Return_All_For_Admin()
        {
            var product = await AddProduct("Monitor", 200, 10);
            var user1 = await AddUser(1);
            var user2 = await AddUser(2);

            await _service.CreateOrderAsync(user1.Id, new Dictionary<int, int> { { product.Id, 1 } });
            await _service.CreateOrderAsync(user2.Id, new Dictionary<int, int> { { product.Id, 1 } });

            SetUserClaims(99, "Admin"); 

            var result = await _controller.AllOrders() as OkObjectResult;
            var orders = result!.Value as List<Order>;

            orders.Should().HaveCount(2);
        }

        //перевірка, що адмін може успішно міняти статус через контролер
        [Fact]
        public async Task ChangeStatus_Should_Update_Status_For_Admin()
        {
            var product = await AddProduct("Mouse", 50, 10);
            var user = await AddUser(1);

            var order = await _service.CreateOrderAsync(user.Id, new Dictionary<int, int> { { product.Id, 1 } });

            SetUserClaims(99, "Admin");

            await _controller.ChangeStatus(order.Id, OrderStatus.Paid);

            var dbOrder = await Context.Orders.FindAsync(order.Id);
            dbOrder!.Status.Should().Be(OrderStatus.Paid);
        }

        //обробка помилки відсутності замовлення в API
        [Fact]
        public async Task ChangeStatus_Should_Throw_When_Order_Not_Found()
        {
            SetUserClaims(99, "Admin");

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _service.ChangeStatusAsync(999, OrderStatus.Paid));
        }

        //обробка помилки при спробі змінити вже фінальний статус через API
        [Fact]
        public async Task ChangeStatus_Should_Throw_When_Final_Status()
        {
            var product = await AddProduct("TV", 800, 5);
            var user = await AddUser(1);

            var order = await _service.CreateOrderAsync(user.Id, new Dictionary<int, int> { { product.Id, 1 } });

            SetUserClaims(99, "Admin");

            await _service.ChangeStatusAsync(order.Id, OrderStatus.Completed);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.ChangeStatusAsync(order.Id, OrderStatus.Paid));
        }
    }
}