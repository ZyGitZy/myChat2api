using chatgot.Models;
using chatgot.Units;
using Newtonsoft.Json;

namespace chatgot.SseServices
{
    public class GotService : CommonService
    {
        readonly string url;
        public GotService(string url)
        {
            this.url = url;
        }
        public override GotConversationDto MapperBody(ConversationDto body)
        {
            body.model ??= "gpt-4";
            var gotDto = new GotConversationDto
            {
                type = body.model,
                timezone = "Etc/GMT-8",
                messages = body.messages,
            };
            var idName = "openai";
            if (body.model.Contains("claude"))
            {
                idName = "anthropic";
            }
            gotDto.model = new Model
            {
                id = $"{idName}/{body.model}",
                owner = $"{(body.model.Contains("claude") ? "Anthropic" : "OpenAI")}",
                name = $"{idName}/{body.model}",
                order = 1,
                placeholder = "",
                type = "Queries",
                picConv = "self-sufficient",
                defaultRec = true,
                level = "advanced",
                contentLength = "200k",
                title = body.model,
            };

            return gotDto;
        }

        public override async Task SendAsync(HttpContext context, HttpClient httpClient)
        {
            var body = await context.GetBody();

            var mapperData = MapperBody(body);
            var response = await this.SendRequest(mapperData, httpClient, context, url, body.stream);
            var comp = InitCompletionResponse(body.model);
            if (body.stream)
            {
                await this.SendStream<GotCompletionResponse>(response, context, async (data) =>
                {
                    if (data != null)
                    {
                        comp.choices![0].delta!.content = data.Choices[0].delta.content;
                        await context.Response.WriteAsync("data:" + JsonConvert.SerializeObject(comp) + "\n\n");
                        await context.Response.Body.FlushAsync();
                    }
                });
            }
            else
            {
                await SendJson<GotCompletionResponse>(response, context, body.model, (data) =>
                {
                    comp.choices![0].delta!.content = data.Choices[0].delta.content;
                    return comp;
                });
            }
        }

        public override Task SetRequestHeader(HttpRequestMessage request, HttpContext context)
        {
            request.Headers.Add("User-Agent", GetUserAgent());
            return base.SetRequestHeader(request, context);
        }
    }
}
