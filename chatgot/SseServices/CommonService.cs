using chatgot.Models;
using chatgot.Units;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace chatgot.SseServices
{
    public abstract class CommonService
    {
        public abstract Task SendAsync(HttpContext context, HttpClient httpClient);

        public abstract TargetDto MapperBody(ConversationDto body);

        public virtual async Task<HttpResponseMessage> SendRequest(object body, HttpClient httpClient, HttpContext context, string url, bool isStream = true)
        {
            HttpRequestMessage requset = await SetHttpRequestMessage(body, url, context);

            var response = await httpClient.SendAsync(requset, isStream ? HttpCompletionOption.ResponseHeadersRead : HttpCompletionOption.ResponseContentRead);

            return response;
        }

        public virtual async Task<HttpRequestMessage> SetHttpRequestMessage<T>(T body, string url, HttpContext context)
        {
            HttpRequestMessage message = new(HttpMethod.Post, url)
            {
                Content = new StringContent(JsonConvert.SerializeObject(body))
            };

            await SetRequestHeader(message, context);

            return message;
        }

        public async Task FlushAsync(HttpContext context, CompletionResponseDto data)
        {
            await context.Response.WriteAsync("data:" + JsonConvert.SerializeObject(data) + "\n\n");
            await context.Response.Body.FlushAsync();
        }

        public virtual T DeserializeObject<T>(string data)
        {
            string patternDto = @"^\s*data:\s*";
            string jsonData = Regex.Replace(data, patternDto, "", RegexOptions.IgnoreCase);
            var res = JsonConvert.DeserializeObject<T>(jsonData);
            return res;
        }

        public virtual async Task SetRequestHeader(HttpRequestMessage request, HttpContext context)
        {
            if (request.Content != null)
            {
                request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            }
            var resToken = await context.GetAuthorization();
            request.Headers.Add("Authorization", resToken);
        }

        public virtual async Task SendStream<T>(HttpResponseMessage response, HttpContext context, Action<T> fun)
        {
            SetResponseHeader(context);
            using var stream = await response.Content.ReadAsStreamAsync();
            using var reader = new StreamReader(stream);
            while (!reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync();
                if (!string.IsNullOrWhiteSpace(line) && line.StartsWith("data"))
                {
                    fun(DeserializeObject<T>(line));
                }
            }
        }

        public virtual async Task SendJson<T>(HttpResponseMessage response, HttpContext context, string model, Func<T, CompletionResponseDto> fun)
        {
            context.Response.Headers.Add("Content-Type", "application/json");
            context.Response.StatusCode = StatusCodes.Status200OK;
            var reuObj = await MapperJsonToObj<T>(model, response);
            await context.Response.WriteAsync(JsonConvert.SerializeObject(fun(reuObj)));
        }

        public virtual async Task<T> MapperJsonToObj<T>(string model, HttpResponseMessage response)
        {
            try
            {
                var responseStr = await response.Content.ReadAsStringAsync();
                var regex = new Regex(@"^\s*data:\s*", RegexOptions.Compiled | RegexOptions.IgnoreCase);
                var jsonDataArray = responseStr.Split(new[] { "\n\n" }, StringSplitOptions.RemoveEmptyEntries)
                                              .Select(data => regex.Replace(data, string.Empty))
                                              .ToList();
                var filterData = jsonDataArray.Where(w => w.Trim().StartsWith("{") && w.Trim().EndsWith("}"));
                var jsonArray = "[" + string.Join(",", filterData) + "]";
                var result = JsonConvert.DeserializeObject<T>(jsonArray);
                return result;
            }
            catch (JsonException jsonEx)
            {
                throw new InvalidOperationException("JSON解析失败", jsonEx);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("数据处理失败", ex);
            }
        }


        public CompletionResponseDto InitCompletionResponse(string model)
        {
            return new()
            {
                id = Guid.NewGuid().ToString(),
                created = DateTimeOffset.Now.ToUnixTimeSeconds(),
                Object = "chat.completion.chunk",
                model = model,
                choices = new List<Choice> { new()
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
        public virtual void SetResponseHeader(HttpContext context)
        {
            context.Response.Headers.Add("Content-Type", "text/event-stream");
            context.Response.Headers.Add("Cache-Control", "no-cache");
            context.Response.Headers.Add("Connection", "keep-alive");
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
