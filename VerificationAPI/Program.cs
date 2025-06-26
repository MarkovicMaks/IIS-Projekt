using Microsoft.OpenApi.Models;
using VerificationAPI.Services;

namespace VerificationAPI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            var xsdPath = Path.Combine(builder.Environment.ContentRootPath, "Schemas", "Video.xsd");
            builder.Services.AddSingleton(new XmlValidationService(xsdPath));
            builder.Services.AddSingleton<XmlConverter>();

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "Verification API",
                    Version = "v1",
                    Description = "Import TikTok JSON → XML → XSD validate"
                });
            });

            builder.Services
                .AddControllers()
                .AddXmlSerializerFormatters();

            var app = builder.Build();

            // Configure the HTTP request pipeline.

            //app.UseAuthorization();
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Verification API v1");
            });

            app.MapControllers();

            app.Run();
        }
    }
}
