using chatgot.Models;
using Newtonsoft.Json;

namespace chatgot.Units
{
    public static class HttpUnit
    {
        public static async Task<string> GetAuthorization(this HttpContext httpContext, string key = "Authorization")
        {
            if (!httpContext.Request.Headers.TryGetValue("Authorization", out var authorization))
            {
                httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
                await httpContext.Response.WriteAsync("Authorization is empty");
                return "";
            }
            return authorization;
        }

        public static async Task<ConversationDto> GetBody(this HttpContext httpContext)
        {
            httpContext.Request.EnableBuffering();
            var bodyStr = await new StreamReader(httpContext.Request.Body).ReadToEndAsync();
            httpContext.Request.Body.Position = 0;
            var body = JsonConvert.DeserializeObject<ConversationDto>(bodyStr) ?? throw new Exception("请传入数据");
            return body;
        }

        public static bool Verify(this HttpContext httpContext)
        {
            if (httpContext.Request.Method != "POST")
            {
                httpContext.Response.StatusCode = StatusCodes.Status405MethodNotAllowed;
                return false;
            }
            return true;
        }

    }
}
