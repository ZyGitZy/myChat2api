using chatgot.Models;
using chatgot.Models.MonicaModels;
using chatgot.Models.YouModels;
using chatgot.Units;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace chatgot.SseService
{
    public class YouService : CommonService
    {
        readonly string url;
        public YouService(string url)
        {
            this.url = url;
        }
        public override async Task SendAsync(HttpContext context, HttpClient httpClient)
        {
            var body = await context.GetBody();
            var response = await SendRequest(MapperBody(body), httpClient, context, url, body.stream);
            var comp = InitCompletionResponse(body.model);
            if (body.stream)
            {
                await this.SendStream<YouResponse>(response, context, async (data) =>
                {
                    if (data != null)
                    {
                        comp.choices![0].delta!.content = data.youChatToken;
                        await this.FlushAsync(context, comp);
                    }
                }, "youChatToken");
            }
            else
            {
                await SendJson<List<YouResponse>>(response, context, body.model, (data) =>
                {
                    comp.choices![0].delta!.content = string.Join("", data.Select(s => s.youChatToken));
                    return comp;
                });
            }
        }



        public override Task<T> MapperJsonToObj<T>(string model, HttpResponseMessage response)
        {
            return base.MapperJsonToObj<T>(model, response);
        }

        public override async Task<HttpResponseMessage> SendRequest(object body, HttpClient httpClient, HttpContext context, string url, bool isStream = true)
        {
            var paramsStr = this.GetRequsetParams(body);
            url += $"?{paramsStr}";
            HttpRequestMessage message = new(HttpMethod.Get, url);
            var toket = await context.GetAuthorization();
            message.Headers.Add("Cookie", toket.Replace("Bearer", "").Trim());
            message.Headers.Add("Referer", "https://you.com/search?q=%E4%BD%A0%E7%9A%84%E6%A8%A1%E5%9E%8B&fromSearchBar=true&tbm=youchat&cid=c0_14068704-f230-405c-9f65-48972a008c47&chatMode=custom");

            return await httpClient.SendAsync(message, isStream ? HttpCompletionOption.ResponseHeadersRead : HttpCompletionOption.ResponseContentRead);
        }

        private YouRequest MapperBody(CompletionsDto body)
        {
            var result = new YouRequest();
            var lastData = body.messages.LastOrDefault();
            result.pastChatLength = lastData?.content.Length ?? 0;
            result.selectedAiModel = body.model;
            result.q = lastData?.content ?? "";
            result.queryTraceId = "f6f06528-0897-4057-bdf0-a1f5322c1d5d";
            result.chatId = "14068704-f230-405c-9f65-48972a008c47";
            result.conversationTurnId = Guid.NewGuid().ToString();
            DateTime utcNow = DateTime.UtcNow;
            string formattedTime = utcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
            result.traceId = $"{result.chatId}|{result.conversationTurnId}|{formattedTime}";
            body.messages.RemoveAt(body.messages.Count - 1);
            // 伪造回答其实可以不要
            List<Answer> chats = new();
            foreach (var message in body.messages)
            {
                chats.Add(new Answer
                {
                    question = message.content,
                    answer = message.content,
                });
            }
            result.chat = JsonConvert.SerializeObject(chats);

            return result;
        }
    }
}
