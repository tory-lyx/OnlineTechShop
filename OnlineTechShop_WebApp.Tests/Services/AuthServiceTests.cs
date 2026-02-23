using FluentAssertions;
using Microsoft.Extensions.Configuration;
using OnlineTechShop_WebApp.Models;
using OnlineTechShop_WebApp.Services;
using OnlineTechShop_WebApp.Tests.Infrastructure;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace OnlineTechShop_WebApp.Tests.Services
{
    public class AuthServiceTests : TestBase
    {
        private readonly AuthService _service;
        public AuthServiceTests()
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "Jwt:Key", "ThisIsAReallyLongSuperSecretKey123456!" },
                    { "Jwt:Issuer", "testIssuer" },
                    { "Jwt:Audience", "testAudience" },
                    { "Jwt:ExpiresMinutes", "60" }
                })
                .Build();
            
            _service = new AuthService(Context, config);
        }

        //реєстрація має успішно створювати нового користувача в базі
        [Fact]
        public async Task Register_Should_Create_User()
        {
            var model = new AuthorisationModel
            {
                Login = "user1",
                Password = "Password123"
            };

            await _service.RegisterAsync(model);

            Context.Users.Should().ContainSingle();
            Context.Users.First().Login.Should().Be("user1");
            Context.Users.First().Role.Should().Be(UserRole.Customer);
        }

        //має виникати помилка, якщо користувач з таким логіном уже існує
        [Fact]
        public async Task Register_Should_Throw_When_User_Exists()
        {
            Context.Users.Add(new User 
            { 
                Login = "user1", 
                PasswordHash = "hash" 
            });
            await Context.SaveChangesAsync();

            var model = new AuthorisationModel
            {
                Login = "user1",
                Password = "Password123"
            };

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.RegisterAsync(model));
        }

        //вхід має повертати JWT-токен, якщо логін і пароль правильні
        [Fact]
        public async Task Login_Should_Return_Token_When_Credentials_Valid()
        {
            await _service.RegisterAsync(new AuthorisationModel
            {
                Login = "user2",
                Password = "Password123"
            });

            var token = await _service.LoginAsync(new AuthorisationModel
            {
                Login = "user2",
                Password = "Password123"
            });

            token.Should().NotBeNullOrWhiteSpace();
        }

        //помилка доступу, якщо користувач з таким ім'ям не існує
        [Fact]
        public async Task Login_Should_Throw_When_User_Not_Found()
        {
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _service.LoginAsync(new AuthorisationModel
                {
                    Login = "ghost",
                    Password = "Password123"
                }));
        }

        //помилка доступу, якщо пароль не збігається
        [Fact]
        public async Task Login_Should_Throw_When_Wrong_Password()
        {
            await _service.RegisterAsync(new AuthorisationModel
            {
                Login = "user3",
                Password = "Password123"
            });

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _service.LoginAsync(new AuthorisationModel
                {
                    Login = "user3",
                    Password = "WrongPassword"
                }));
        }

        //перевірка на те, що пароль не зберігається у відкритому вигляді (перевірка хешування)
        [Fact]
        public async Task Register_Should_Hash_Password()
        {
            var model = new AuthorisationModel
            {
                Login = "secureUser",
                Password = "Password123"
            };

            await _service.RegisterAsync(model);

            var user = Context.Users.First();

            user.PasswordHash.Should().NotBe("Password123");
            user.PasswordHash.Should().NotBeNullOrWhiteSpace();
        }

        //перевірка на те, що Jwt містить правильні Claims
        //тобто токен має містити правильні дані про роль користувача (наприклад, Customer)
        [Fact]
        public async Task Login_Should_Return_Token_With_Correct_Claims()
        {
            await _service.RegisterAsync(new AuthorisationModel
            {
                Login = "claimUser",
                Password = "Password123"
            });

            var tokenString = await _service.LoginAsync(new AuthorisationModel
            {
                Login = "claimUser",
                Password = "Password123"
            });

            var handler = new JwtSecurityTokenHandler();
            var token = handler.ReadJwtToken(tokenString);

            token.Claims.Should().Contain(c =>
                c.Type == ClaimTypes.Role && c.Value == UserRole.Customer.ToString());
        }
    }
}