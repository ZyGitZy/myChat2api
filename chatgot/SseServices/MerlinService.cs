using chatgot.Models;
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

            var response = await SendRequest(merlin, httpClient, context, url);

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
                await SendJson<MerlinResponse>(response, context, body.model, (e) =>
                {
                    comp.choices![0].delta!.content = e.data.content;
                    return comp;
                });
            }
        }

        public override Merlin MapperBody(ConversationDto body)
        {
            body.model ??= "gpt-4";
            var mapeprModel = configuration.GetSection("Merlin").GetSection(body.model).Value ?? body.model;
            Merlin merlin = new()
            {
                action = new Models.Action
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
