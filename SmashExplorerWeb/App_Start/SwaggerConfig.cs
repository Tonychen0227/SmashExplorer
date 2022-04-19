using System.Web.Http;
using WebActivatorEx;
using SmashExplorerWeb;
using Swashbuckle.Application;

[assembly: PreApplicationStartMethod(typeof(SwaggerConfig), "Register")]

namespace SmashExplorerWeb
{
    public class SwaggerConfig
    {
        public static void Register()
        {
            var thisAssembly = typeof(SwaggerConfig).Assembly;

            GlobalConfiguration.Configuration
                .EnableSwagger(c => c.SingleApiVersion("v1", "SmashExplorerWeb"))
                .EnableSwaggerUi();
        }
    }
}
