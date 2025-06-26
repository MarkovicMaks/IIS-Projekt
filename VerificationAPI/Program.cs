using Microsoft.OpenApi.Models;
using VerificationAPI.Services;
using VerificationAPI.XmlModels;

namespace VerificationAPI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            var xsdPath = Path.Combine(builder.Environment.ContentRootPath, "Schemas", "MetadataXSD.xsd");
            builder.Services.AddSingleton(new XmlValidationService(xsdPath));
            builder.Services.AddSingleton<List<VideoMetadata>>();
            builder.Services.AddSingleton<List<string>>();

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

            builder.Services.AddControllers(opt =>
                            {
                                opt.InputFormatters.Insert(0, new RawXmlInputFormatter());
                            })
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
