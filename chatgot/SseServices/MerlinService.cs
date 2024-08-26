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
        public MerlinService(IConfiguration configuration)
        {
            this.configuration = configuration;
        }
        public override async Task CommonMapper(HttpContext context, HttpClient httpClient)
        {
            var body = await HttpUnit.GetBody(context);
            var task = MapperBody(body);

            var response = await SendRequest(task, httpClient, context, "https://uam.getmerlin.in/thread/unified?customJWT=true&version=1.1");

            var comp = InitCompletionResponse(body.model);
            if (body.stream)
            {
                await SendStream(response, context, async (e) =>
                {
                    if (!string.IsNullOrWhiteSpace(e) && e.Contains("content"))
                    {
                        string patternDto = @"^\s*data:\s*";
                        string jsonData = Regex.Replace(e, patternDto, "", RegexOptions.IgnoreCase);
                        var res = JsonConvert.DeserializeObject<MerlinResponse>(jsonData);
                        comp.choices[0].delta.content = res.data.content;

                        if (res.data.eventType == "DONE")
                        {
                            comp.choices[0].finish_reason = "stop";
                            var str = "data:" + JsonConvert.SerializeObject(comp) + "\n\n";
                            str += "data: [DONE]\n\n";
                            await context.Response.WriteAsync(str);
                            await context.Response.Body.FlushAsync();
                        }
                        else
                        {
                            await context.Response.WriteAsync("data:" + JsonConvert.SerializeObject(comp) + "\n\n");
                            await context.Response.Body.FlushAsync();
                        }

                    }
                });
                return;
            }

            await SendJson(response, context, (e) =>
            {
                if (!string.IsNullOrWhiteSpace(e) && e.Contains("content"))
                {
                    string patternDto = @"^\s*data:\s*";
                    string jsonData = Regex.Replace(e, patternDto, "", RegexOptions.IgnoreCase);
                    var res = JsonConvert.DeserializeObject<MerlinResponse>(jsonData);
                    if (res != null)
                    {
                        comp.choices[0].delta.content += res.data.content;
                    }
                }

                return comp;
            });
        }

        private Merlin MapperBody(ConversationDto body)
        {
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
                chatId =Guid.NewGuid().ToString(),
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
                    metadata = new Models.MerlinMetadata { },
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
                    metadata = new Models.MerlinMetadata { },
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
