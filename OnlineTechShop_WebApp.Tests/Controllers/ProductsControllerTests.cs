using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using OnlineTechShop_WebApp.Controllers;
using OnlineTechShop_WebApp.Models;
using OnlineTechShop_WebApp.Services;
using OnlineTechShop_WebApp.Tests.Infrastructure;

namespace OnlineTechShop_WebApp.Tests.Controllers
{
    public class ProductsControllerTests : TestBase
    {
        private readonly ProductService _service;
        private readonly ProductsController _controller;

        public ProductsControllerTests()
        {
            _service = new ProductService(Context);
            _controller = new ProductsController(_service);
        }

        private async Task<Product> AddProduct(string name, decimal price, bool active = true)
        {
            var product = new Product
            {
                Name = name,
                Price = price,
                StockQuantity = 5,
                IsActive = active
            };
            Context.Products.Add(product);
            await Context.SaveChangesAsync();
            return product;
        }

        //API має віддавати список лише активних товарів клієнтам
        [Fact]
        public async Task GetAll_Should_Return_Only_Active_Products()
        {
            await AddProduct("Active1", 10, true);
            await AddProduct("Inactive", 10, false);

            var result = await _controller.GetAll() as OkObjectResult;
            var products = result!.Value as List<Product>;

            products.Should().HaveCount(1);
            products!.First().Name.Should().Be("Active1");
        }

        //Ендпоінт має повертати товар за ID зі статусом 200 OK
        [Fact]
        public async Task GetById_Should_Return_Product()
        {
            var product = await AddProduct("Mouse", 50);
            var result = await _controller.GetById(product.Id) as OkObjectResult;

            result!.Value.Should().BeOfType<Product>();
            ((Product)result.Value).Name.Should().Be("Mouse");
        }

        //перевірка обробки ситуації, коли товар не знайдено через API
        [Fact]
        public async Task GetById_Should_Throw_When_NotFound()
        {
            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _service.GetByIdAsync(999));
        }

        //Комплексна перевірка: створення, оновлення та вимкнення товару через контролер
        [Fact]
        public async Task Create_Update_Deactivate_Should_Work()
        {
            var product = new Product { Name = "Laptop", Price = 1000, StockQuantity = 5 };

            // Create
            var created = await _controller.Create(product) as CreatedAtActionResult;
            created!.Value.Should().BeOfType<Product>();

            // Update
            var updated = new Product 
            { 
                Name = "NewLaptop", 
                Price = 1200, 
                StockQuantity = 10, 
                IsActive = true 
            };
            await _controller.Update(((Product)created.Value).Id, updated);

            var dbProduct = await Context.Products.FindAsync(((Product)created.Value).Id);
            dbProduct!.Name.Should().Be("NewLaptop");

            // Deactivate
            await _controller.Deactivate(dbProduct.Id);
            dbProduct.IsActive.Should().BeFalse();
        }
    }
}