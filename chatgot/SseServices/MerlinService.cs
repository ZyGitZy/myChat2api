using chatgot.Models;
using chatgot.Models.MerlinModels;
using chatgot.Models.MonicaModels;
using chatgot.Units;
using Newtonsoft.Json;
using SseServices;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace chatgot.SseServices
{
    public class MerlinService : CommonService
    {
        IConfiguration configuration;
        readonly string url;
        public MerlinService(IConfiguration configuration, string url)
        {
            this.configuration = configuration;
            this.url = url;
        }

        public override async Task SendAsync(HttpContext context, HttpClient httpClient)
        {
            var body = await HttpUnit.GetBody(context);
            var merlin = MapperBody(body);

            using var response = await SendRequest(merlin, httpClient, context, url);

            var comp = InitCompletionResponse(body.model);
            if (body.stream)
            {
                await SendStream<MerlinResponse>(response, context, async (data) =>
                {
                    if (data != null)
                    {
                        comp.choices![0].delta!.content = data.data.content;
                        await this.FlushAsync(context, comp);
                    }
                });
            }
            else
            {
                await SendJson<List<MerlinResponse>>(response, context, body.model, (data) =>
                {
                    comp.choices![0].delta!.content = string.Join("", data.Select(s => s.data.content));
                    return comp;
                });
            }
        }


        public override async Task<T> MapperJsonToObj<T>(string model, HttpResponseMessage response)
        {
            try
            {
                var responseStr = await response.Content.ReadAsStringAsync();
                var regex = new Regex(@"^\s*data:\s*", RegexOptions.Compiled | RegexOptions.IgnoreCase);
                var jsonDataArray = responseStr.Split(new[] { "\n\n" }, StringSplitOptions.RemoveEmptyEntries)
                                              .Select(data => regex.Replace(data, string.Empty))
                                              .ToList();
                var filterData = jsonDataArray.Select(s => s.Replace("event: message\ndata:", string.Empty));
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

        public MerlinRequest MapperBody(CompletionsDto body)
        {
            body.model ??= "gpt-4";
            var mapeprModel = configuration.GetSection("Merlin").GetSection(body.model).Value ?? body.model;
            MerlinRequest merlin = new()
            {
                action = new Models.MerlinModels.Action
                {
                    message = new MerlinMessage
                    {
                        attachments = new List<string>(),
                        content = body.messages.LastOrDefault()?.content ?? "",
                        metadata = new MerlinMetadata
                        {
                            context = ""
                        },
                        parentId = "",
                        role = "user"
                    },
                    type = "NEW"
                },
                activeThreadSnippet = new List<ActiveThreadSnippet>(),
                chatId = Guid.NewGuid().ToString(),
                language = "CHINESE_SIMPLIFIED",
                metadata = null,
                mode = "VANILLA_CHAT",
                model = mapeprModel,
                personaConfig = new PersonaConfig { }
            };

            if (body.messages.Count > 1) body.messages.RemoveAt(body.messages.Count - 1);

            var paredtId = "root";
            foreach (var message in body.messages)
            {
                var active = new ActiveThreadSnippet
                {
                    attachments = new List<string>(),
                    content = message.content,
                    id = Guid.NewGuid().ToString(),
                    parentId = paredtId,
                    role = "user",
                    metadata = new MerlinMetadata { },
                    status = "SUCCESS",
                    activeChildIdx = 0,
                    totalChildren = 1,
                    idx = 0,
                    totSiblings = 1,
                };

                // 伪造个回答 其实应该没啥用
                var active2 = new ActiveThreadSnippet
                {
                    content = "",
                    id = Guid.NewGuid().ToString(),
                    parentId = active.id,
                    role = "assistant",
                    metadata = new MerlinMetadata { },
                    status = "SUCCESS",
                    activeChildIdx = 0,
                    totalChildren = 1,
                    idx = 0,
                    totSiblings = 1,
                };

                paredtId = active2.id.ToString();
                merlin.activeThreadSnippet.Add(active);
                merlin.activeThreadSnippet.Add(active2);
            }

            return merlin;
        }

    }
}
