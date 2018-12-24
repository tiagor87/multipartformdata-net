using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using MultiPartFormDataNet.Core.Tests.FakeApi;

namespace MultiPartFormDataNet.Core.Tests.Helpers
{
    public class IntegrationTestsWebAppFactory : WebApplicationFactory<FakeStartup>
    {
        protected override IWebHostBuilder CreateWebHostBuilder()
        {
            return WebHost.CreateDefaultBuilder()
                .UseStartup<FakeStartup>();
        }
    }
}