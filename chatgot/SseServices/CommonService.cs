using chatgot.Models;
using chatgot.Units;
using Newtonsoft.Json;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace chatgot.SseServices
{
    public abstract class CommonService
    {
        public abstract Task CommonMapper(HttpContext context, HttpClient httpClient);

        public virtual async Task<HttpResponseMessage> SendRequest<T>(T body, HttpClient httpClient, HttpContext context, string url)
        {
            HttpRequestMessage requset = new(HttpMethod.Post, url)
            {
                Content = new StringContent(JsonConvert.SerializeObject(body))
            };

            await SetHeaderAuthorization(requset, context);

            var response = await httpClient.SendAsync(requset, HttpCompletionOption.ResponseHeadersRead);

            return response;
        }

        public virtual async Task SetHeaderAuthorization(HttpRequestMessage requset, HttpContext context)
        {
            requset.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            var authorization = await context.GetAuthorization();
            requset.Headers.Add("authorization", authorization);
        }

        public async Task SendJson(HttpResponseMessage response, HttpContext context, Func<string, CompletionResponseDto> send)
        {
            context.Response.Headers.Add("Content-Type", "application/json");
            context.Response.StatusCode = StatusCodes.Status200OK;
            CompletionResponseDto comp = new();
            await ReaderStream(response, async (response) =>
            {
                comp = send(response);
                return await Task.FromResult(false);
            });

            await context.Response.WriteAsync(JsonConvert.SerializeObject(comp));
        }

        public CompletionResponseDto InitCompletionResponse(string model)
        {
            return new()
            {
                id = Guid.NewGuid().ToString(),
                created = DateTimeOffset.Now.ToUnixTimeSeconds(),
                Object = "chat.completion.chunk",
                model = model,
                choices = new List<Choice> { new Choice()
                {
                     index = 0,
                     finish_reason = null,
                     delta = new Delta{
                     content = ""
                     }
                   }
                }
            };
        }

        public async Task SendStream(HttpResponseMessage response, HttpContext context, Action<string> fun)
        {
            context.Response.Headers.Add("Content-Type", "text/event-stream");
            context.Response.Headers.Add("Cache-Control", "no-cache");
            context.Response.Headers.Add("Connection", "keep-alive");

            await ReaderStream(response, async (response) =>
            {
                if (!context.Response.HttpContext.RequestAborted.IsCancellationRequested)
                {
                    fun(response);
                    return await Task.FromResult(false);
                }
                return await Task.FromResult(true);
            });

        }

        public async Task ReaderStream(HttpResponseMessage response, Func<string, Task<bool>> fun)
        {
            using var stream = await response.Content.ReadAsStreamAsync();
            using var reader = new StreamReader(stream);

            while (!reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync();

                if (!string.IsNullOrWhiteSpace(line) && line.StartsWith("data"))
                {
                    if (await fun(line))
                    {
                        return;
                    }
                }
            }

            response.Dispose();
        }

        public string GetUserAgent()
        {
            if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
            {
                return "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0.0.0 Safari/537.36";
            }
            else if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Linux))
            {
                return "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0.0.0 Safari/537.36";
            }
            else if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.OSX))
            {
                return "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0.0.0 Safari/537.36";
            }
            else
            {
                return "Mozilla/5.0 (Unknown OS) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0.0.0 Safari/537.36";
            }
        }


        public string ToCurl(HttpRequestMessage request)
        {
            var sb = new StringBuilder();

            sb.Append($"curl -X {request.Method.Method}");

            foreach (var header in request.Headers)
            {
                foreach (var value in header.Value)
                {
                    sb.Append($" -H \"{header.Key}: {value}\"");
                }
            }

            if (request.Content != null)
            {
                if (request.Content is StringContent stringContent)
                {
                    var contentString = stringContent.ReadAsStringAsync().Result;
                    sb.Append($" -d '{contentString}'");
                }
                else
                {
                    var contentType = request.Content.Headers.ContentType?.MediaType;
                    if (contentType != null)
                    {
                        sb.Append($" -H \"Content-Type: {contentType}\"");
                    }
                }
            }

            sb.Append($" {request.RequestUri.AbsoluteUri}");

            return sb.ToString();
        }


    }
}
