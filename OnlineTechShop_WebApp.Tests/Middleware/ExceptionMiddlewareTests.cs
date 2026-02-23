using FluentAssertions;
using Microsoft.AspNetCore.Http;
using OnlineTechShop_WebApp.Middleware;

namespace OnlineTechShop_WebApp.Tests.Middleware
{
    public class ExceptionMiddlewareTests
    {
        //якщо в коді станеться будь-яка непередбачувана помилка, middleware
        //має перехопити її та повернути клієнту статус 500 (Internal Server Error)
        [Fact]
        public async Task InvokeAsync_Should_Handle_Exception_And_Return_500()
        {
            // Arrange (підготовка) - створюємо об'єкти, готуємо дані
            var context = new DefaultHttpContext();

            // Імітація помилки (тобто делегат, який імітує помилку (будь-яку загальну помилку))
            RequestDelegate next = (innerContext) => throw new Exception("General Error");
            
            var middleware = new ExceptionMiddleware(next);

            // MemoryStream для зчитування відповіді (потрібен, щоб перехопити те, що Middleware запише у відповідь)
            context.Response.Body = new MemoryStream();

            // Act (дія) - виклик методу, що хочемо перевірити 
            await middleware.InvokeAsync(context);

            // Assert (перевірка) - порівняння отриманого з очікуваним
            //1. Перевіряємо статус (для загальної помилки Middleware поверне 500)
            context.Response.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);

            // 2. Перевіряємо заголовок (ContentType)
            context.Response.ContentType.Should().Be("application/json");

            // 3. (Опціонально) Перевіряємо вміст відповіді
            context.Response.Body.Seek(0, SeekOrigin.Begin);
            var reader = new StreamReader(context.Response.Body);
            var responseBody = await reader.ReadToEndAsync();

            responseBody.Should().Contain("Internal server error");
        }

        //Виклик (Invoke) має обробити помилку KeyNotFoundException та повернути статус 404
        //тобто при будь-якій помилці типу KeyNotFoundException автоматично конвертується у валідну HTTP-відповідь зі статусом 404
        [Fact]
        public async Task InvokeAsync_Should_Handle_KeyNotFound_And_Return_404()
        {
            // Arrange (підготовка) - створюємо об'єкти, готуємо дані
            var context = new DefaultHttpContext();
            RequestDelegate next = (innerContext) => throw new KeyNotFoundException("Product not found");
            var middleware = new ExceptionMiddleware(next);
            context.Response.Body = new MemoryStream();

            // Act (дія) - виклик методу, що хочемо перевірити 
            await middleware.InvokeAsync(context);

            // Assert (перевірка) - порівняння отриманого з очікуваним
            context.Response.StatusCode.Should().Be(404);
            var reader = new StreamReader(context.Response.Body);
            context.Response.Body.Seek(0, SeekOrigin.Begin);
            var responseBody = await reader.ReadToEndAsync();

            responseBody.Should().Contain("Product not found");
        }
    }
}