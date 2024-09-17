using chatgot.Models;
using chatgot.Models.ChatgotModels;
using chatgot.Units;
using Newtonsoft.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace chatgot.SseServices
{
    public class ChatgotService : CommonService
    {
        readonly string url;
        public ChatgotService(string url)
        {
            this.url = url;
        }
        public ChatgotRequest MapperBody(CompletionsDto body)
        {
            body.model ??= "gpt-4";
            var gotDto = new ChatgotRequest
            {
                clId = "66e2b296e451cb2ab96b4f67",
                model = body.model,
                prompt = body.messages.LastOrDefault()?.content ?? "",
                webAccess = "close",
                timezone = "Asia/Shanghai"
            };
            return gotDto;
        }

        public override async Task SendAsync(HttpContext context, HttpClient httpClient)
        {
            var body = await context.GetBody();

            var mapperData = MapperBody(body);
            using var response = await SendRequest(mapperData, httpClient, context, url, body.stream);
            var comp = InitCompletionResponse(body.model);
            if (body.stream)
            {
                await SendStream<ChatgotResponse>(response, context, async (data) =>
                {
                    if (data != null)
                    {
                        comp.choices![0].delta!.content = data.data.content;
                        await context.Response.WriteAsync("data:" + JsonConvert.SerializeObject(comp) + "\n\n");
                        await context.Response.Body.FlushAsync();
                    }
                }, "content");
            }
            else
            {
                await SendJson<List<ChatgotResponse>>(response, context, body.model, (data) =>
                {
                    comp.choices![0].delta!.content = string.Join(string.Empty, data.Select(s => s.data.content));
                    return comp;
                });
            }
        }

        public override async Task<T> MapperJsonToObj<T>(string model, HttpResponseMessage response)
        {
            var responseStr = await response.Content.ReadAsStringAsync();

            string[] lines = responseStr.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

            StringBuilder stringBuilder = new();
            stringBuilder.Append('[');
            foreach (var line in lines)
            {
                int dataIndex = line.IndexOf("data: ");
                if (dataIndex != -1)
                {
                    string jsonData = line.Substring(dataIndex + "data: ".Length);

                    stringBuilder.Append(jsonData + ",");
                }
            }

            var jsonArray = stringBuilder.ToString().TrimEnd(',') + "]";

            return JsonConvert.DeserializeObject<T>(jsonArray);
        }

        public override Task SetRequestHeader(HttpRequestMessage request, HttpContext context)
        {
            request.Headers.Add("User-Agent", GetUserAgent());
            return base.SetRequestHeader(request, context);
        }
    }
}
