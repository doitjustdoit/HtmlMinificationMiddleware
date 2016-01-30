namespace DotnetThoughts.AspNet
{
    using Microsoft.AspNet.Http;
    using System.IO;
    using System.Threading.Tasks;
    using System.Text;
    using System.Text.RegularExpressions;
    using Microsoft.AspNet.Builder;

    public class HtmlMinificationMiddleware
    {
        private RequestDelegate _next;
        private HtmlMinificationOptions _minificationOptions;
        public HtmlMinificationMiddleware(RequestDelegate next)
            : this(next, null)
        {
        }
        public HtmlMinificationMiddleware(RequestDelegate next, HtmlMinificationOptions minificationOptions)
        {
            _next = next;
            _minificationOptions = minificationOptions;
        }
        public async Task Invoke(HttpContext context)
        {
            var stream = context.Response.Body;
            if(_minificationOptions != null)
            {
                var filter = _minificationOptions.ExcludeFilter;
                if(Regex.IsMatch(context.Request.Path, filter))
                {
                    await _next(context);
                    return;
                }
            }
            
            using (var buffer = new MemoryStream())
            {
                context.Response.Body = buffer;
                await _next(context);
                var isHtml = context.Response.ContentType?.ToLower().Contains("text/html");

                buffer.Seek(0, SeekOrigin.Begin);
                using (var reader = new StreamReader(buffer))
                {
                    string responseBody = await reader.ReadToEndAsync();
                    if (context.Response.StatusCode == 200 && isHtml.GetValueOrDefault())
                    {
                        System.Console.WriteLine("Minification started");
                        responseBody = Regex.Replace(responseBody, @">\s+<", "><", RegexOptions.Compiled);
                        responseBody = Regex.Replace(responseBody, @"<!--(?!\s*(?:\[if [^\]]+]|<!|>))(?:(?!-->)(.|\n))*-->", "", RegexOptions.Compiled);
                        System.Console.WriteLine("Minification end");
                    }
                    var bytes = Encoding.UTF8.GetBytes(responseBody);
                    using (var memoryStream = new MemoryStream(bytes))
                    {
                        memoryStream.Seek(0, SeekOrigin.Begin);
                        await memoryStream.CopyToAsync(stream);
                    }
                }

            }
        }
    }
}
