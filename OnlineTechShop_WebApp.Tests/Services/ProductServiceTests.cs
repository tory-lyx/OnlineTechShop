using FluentAssertions;
using OnlineTechShop_WebApp.Models;
using OnlineTechShop_WebApp.Services;
using OnlineTechShop_WebApp.Tests.Infrastructure;

namespace OnlineTechShop_WebApp.Tests.Services
{
    public class ProductServiceTests : TestBase
    {
        private readonly ProductService _service;

        public ProductServiceTests()
        {
            _service = new ProductService(Context);
        }

        //створення товару має успішно додавати його в БД
        [Fact]
        public async Task CreateAsync_Should_Create_Product()
        {
            var product = new Product
            {
                Name = "Laptop",
                Price = 1000,
                StockQuantity = 5
            };

            var result = await _service.CreateAsync(product);

            Context.Products.Should().ContainSingle();
            result.Id.Should().BeGreaterThan(0);
        }

        //помилка, якщо ціна товару дорівнює 0 або менше
        [Fact]
        public async Task CreateAsync_Should_Throw_When_Price_Invalid()
        {
            var product = new Product
            {
                Name = "Laptop",
                Price = 0,
                StockQuantity = 5
            };

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.CreateAsync(product));
        }

        //помилка, якщо кількість на складі вказана як від'ємне число
        [Fact]
        public async Task CreateAsync_Should_Throw_When_Stock_Negative()
        {
            var product = new Product
            {
                Name = "Laptop",
                Price = 100,
                StockQuantity = -1
            };

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.CreateAsync(product));
        }

        //пошук має повертати лише ті товари, які не були деактивовані
        [Fact]
        public async Task GetActiveProductsAsync_Should_Return_Only_Active()
        {
            Context.Products.AddRange(
                new Product { Name = "Active1", Price = 10, StockQuantity = 1, IsActive = true },
                new Product { Name = "Inactive", Price = 10, StockQuantity = 1, IsActive = false }
            );
            await Context.SaveChangesAsync();

            var result = await _service.GetActiveProductsAsync();

            result.Should().HaveCount(1);
            result.First().Name.Should().Be("Active1");
        }

        //отримання товару за його ID має працювати коректно
        [Fact]
        public async Task GetByIdAsync_Should_Return_Product()
        {
            var product = new Product { Name = "Mouse", Price = 50, StockQuantity = 10 };
            Context.Products.Add(product);
            await Context.SaveChangesAsync();

            var result = await _service.GetByIdAsync(product.Id);

            result.Name.Should().Be("Mouse");
        }

        //помилка "не знайдено", якщо товару з таким ID немає
        [Fact]
        public async Task GetByIdAsync_Should_Throw_When_Not_Found()
        {
            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _service.GetByIdAsync(999));
        }

        //оновлення даних товару (назва, ціна) має успішно зберігатися
        [Fact]
        public async Task UpdateAsync_Should_Update_Product()
        {
            var product = new Product { 
                Name = "Old", 
                Price = 10, 
                StockQuantity = 1 
            };
            Context.Products.Add(product);
            await Context.SaveChangesAsync();

            var updated = new Product
            {
                Name = "New",
                Price = 20,
                StockQuantity = 5,
                IsActive = false
            };

            await _service.UpdateAsync(product.Id, updated);

            var dbProduct = await Context.Products.FindAsync(product.Id);

            dbProduct!.Name.Should().Be("New");
            dbProduct.Price.Should().Be(20);
            dbProduct.StockQuantity.Should().Be(5);
            dbProduct.IsActive.Should().BeFalse();
        }

        //помилка при спробі оновити неіснуючий товар
        [Fact]
        public async Task UpdateAsync_Should_Throw_When_Not_Found()
        {
            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _service.UpdateAsync(999, new Product()));
        }

        //"м'яке видалення": товар має залишатися в базі, але ставати неактивним
        [Fact]
        public async Task DeactivateAsync_Should_Set_IsActive_False()
        {
            var product = new Product { 
                Name = "Keyboard", 
                Price = 30, 
                StockQuantity = 5 
            };
            Context.Products.Add(product);
            await Context.SaveChangesAsync();

            await _service.DeactivateAsync(product.Id);

            var dbProduct = await Context.Products.FindAsync(product.Id);
            dbProduct!.IsActive.Should().BeFalse();
        }

        //помилка при спробі деактивувати неіснуючий товар
        [Fact]
        public async Task DeactivateAsync_Should_Throw_When_Not_Found()
        {
            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _service.DeactivateAsync(999));
        }
    }
}