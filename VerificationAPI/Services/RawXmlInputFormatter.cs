namespace VerificationAPI.Services
{
    using Microsoft.AspNetCore.Mvc.Formatters;
    using System.Text;

    public sealed class RawXmlInputFormatter : TextInputFormatter
    {
        public RawXmlInputFormatter()
        {
            SupportedMediaTypes.Add("application/xml");
            SupportedMediaTypes.Add("text/xml");

            SupportedEncodings.Add(Encoding.UTF8);
            SupportedEncodings.Add(Encoding.Unicode);
        }

        protected override bool CanReadType(Type type) => type == typeof(string);

        public override async Task<InputFormatterResult> ReadRequestBodyAsync(
            InputFormatterContext ctx, Encoding enc)
        {
            using var reader = new StreamReader(ctx.HttpContext.Request.Body, enc);
            var xml = await reader.ReadToEndAsync();
            return await InputFormatterResult.SuccessAsync(xml);
        }
    }

}
