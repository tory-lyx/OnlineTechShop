using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using OnlineTechShop_WebApp.Controllers;
using OnlineTechShop_WebApp.Models;
using OnlineTechShop_WebApp.Services;
using OnlineTechShop_WebApp.Tests.Infrastructure;

namespace OnlineTechShop_WebApp.Tests.Controllers
{
    public class AdminControllerTests : TestBase
    {
        private readonly AdminController _controller;

        public AdminControllerTests()
        {
            _controller = new AdminController(Context);
        }

        private async Task<User> AddUser(int id, string login, UserRole role = UserRole.Customer)
        {
            var user = new User
            {
                Id = id,
                Login = login,
                PasswordHash = "hash",
                Role = role
            };
            Context.Users.Add(user);
            await Context.SaveChangesAsync();
            return user;
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

        private async Task<Order> AddOrder(int userId, Dictionary<int, int> products)
        {
            var orderService = new OrderService(Context);
            return await orderService.CreateOrderAsync(userId, products);
        }

        //адмін має отримувати повний список користувачів системи
        [Fact]
        public async Task GetUsers_Should_Return_All_Users()
        {
            await AddUser(1, "user1");
            await AddUser(2, "user2");
            await AddUser(3, "admin", UserRole.Admin);

            var result = await _controller.GetUsers() as OkObjectResult;

            result.Should().NotBeNull();
            var users = result!.Value as IEnumerable<object>;
            users!.Count().Should().Be(3);
        }

        //коректна обробка порожнього списку користувачів
        [Fact]
        public async Task GetUsers_Should_Return_Empty_When_No_Users()
        {
            var result = await _controller.GetUsers() as OkObjectResult;

            result.Should().NotBeNull();
            var users = result!.Value as IEnumerable<object>;
            users!.Should().BeEmpty();
        }

        //перевірка отримання всіх замовлень з деталями (товарами) для адмін-панелі
        [Fact]
        public async Task GetOrders_Should_Return_All_Orders()
        {
            var user1 = await AddUser(1, "user1");
            var user2 = await AddUser(2, "user2");

            var product1 = await AddProduct("Laptop", 1000, 5);
            var product2 = await AddProduct("Mouse", 50, 10);

            await AddOrder(user1.Id, new Dictionary<int, int> { { product1.Id, 1 } });
            await AddOrder(user2.Id, new Dictionary<int, int> { { product2.Id, 2 } });

            var result = await _controller.GetOrders() as OkObjectResult;

            result.Should().NotBeNull();
            var orders = result!.Value as IEnumerable<Order>;
            orders!.Count().Should().Be(2);

            orders.First().Items.Should().NotBeEmpty();
        }

        //коректна обробка порожнього списку замовлень
        [Fact]
        public async Task GetOrders_Should_Return_Empty_When_No_Orders()
        {
            var result = await _controller.GetOrders() as OkObjectResult;

            result.Should().NotBeNull();
            var orders = result!.Value as IEnumerable<Order>;
            orders!.Should().BeEmpty();
        }
    }
}