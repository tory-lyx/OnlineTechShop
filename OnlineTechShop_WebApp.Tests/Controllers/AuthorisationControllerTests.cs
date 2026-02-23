using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using OnlineTechShop_WebApp.Controllers;
using OnlineTechShop_WebApp.Models;
using OnlineTechShop_WebApp.Services;
using OnlineTechShop_WebApp.Tests.Infrastructure;

namespace OnlineTechShop_WebApp.Tests.Controllers
{
    public class AuthorisationControllerTests : TestBase
    {
        private readonly AuthorisationController _controller;
        private readonly AuthService _authService;

        public AuthorisationControllerTests()
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "Jwt:Key", "SuperSecretKey12345678901234567890" },
                    { "Jwt:Issuer", "test" },
                    { "Jwt:Audience", "test" },
                    { "Jwt:ExpiresMinutes", "60" }
                }).Build();

            _authService = new AuthService(Context, config);
            _controller = new AuthorisationController(_authService);
        }

        //контролер має повертати 200 OK та токен при успішному вході
        [Fact]
        public async Task Login_Should_Return_Ok_With_Token()
        {
            // Arrange (підготовка) - створюємо об'єкти, готуємо дані
            var model = new AuthorisationModel { Login = "testuser", Password = "Password123" };
            await _authService.RegisterAsync(model);

            // Act (дія) - виклик методу, що хочемо перевірити 
            var result = await _controller.Login(model) as OkObjectResult;

            // Assert (перевірка) - порівняння отриманого з очікуваним
            result.Should().NotBeNull();
            result!.Value.ToString().Should().NotBeEmpty();
        }

        //контролер має повертати успішний статус (200-299) при створенні нового акаунта
        [Fact]
        public async Task Register_Should_Return_Success()
        {
            // Arrange  (підготовка) - створюємо об'єкти, готуємо дані
            // створення моделі 
            var loginName = "newuser_unique";

            var model = new AuthorisationModel 
            { 
                Login = loginName, 
                Password = "Password123" 
            };

            // Act (дія) - виклик методу, що хочемо перевірити 
            var result = await _controller.Register(model);

            // Assert (перевірка) - порівняння отриманого з очікуваним
            //перевіряємо на успішність (статус 200-299)
            if (result is ObjectResult objectResult)
            {
                objectResult.StatusCode.Should().BeInRange(200, 299);
            }
            else if (result is StatusCodeResult statusCodeResult)
            {
                statusCodeResult.StatusCode.Should().BeInRange(200, 299);
            }
            else
            {
                result.Should().BeAssignableTo<ActionResult>();
            }
            
            Context.Users.Should().Contain(u => u.Login == loginName);
        }
    }
}