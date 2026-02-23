using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using OnlineTechShop_WebApp;

namespace OnlineTechShop_WebApp.Tests.Infrastructure
{
    //IDisposable дозволяє розробнику самостійно вказати момент звільнення ресурсів, щойно вони стають не потрібними
    public abstract class TestBase : IDisposable 
    {
        protected readonly AppDbContext Context;

        protected TestBase()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .ConfigureWarnings(w => 
                    w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            Context = new AppDbContext(options);
        }

        public void Dispose()
        {
            //логіка очищення ресурсів
            Context.Database.EnsureDeleted();
            Context.Dispose();
        }
    }
}