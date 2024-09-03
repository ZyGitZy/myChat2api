using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using chatgot.Models;
using chatgot.SseServices;
using chatgot.Units;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace SseServices.MonicaService
{
    public class MonicaService : CommonService
    {
        private IConfiguration configuration;
        readonly string url;
        public MonicaService(IConfiguration configuration, string url)
        {
            this.configuration = configuration;
            this.url = url;
        }
        public override async Task SendAsync(HttpContext context, HttpClient httpClient)
        {
            var body = await context.GetBody();
            var mapperData = MapperBody(body);
            var response = await this.SendRequest(mapperData, httpClient, context, url, body.stream);
            var comp = InitCompletionResponse(body.model);
            if (body.stream)
            {
                await this.SendStream<Monica>(response, context, async (data) =>
                {
                    if (data != null)
                    {
                        comp.choices![0].delta!.content = data.text;
                        await this.FlushAsync(context, comp);
                    }
                });
            }
            else
            {
                await SendJson<List<Monica>>(response, context, body.model, (data) =>
                {
                    comp.choices![0].delta!.content = string.Join("", data.Select(s => s.text));
                    return comp;
                });
            }
        }

        public override async Task SetRequestHeader(HttpRequestMessage request, HttpContext context)
        {
            request.Content!.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            var resToken = await context.GetAuthorization();
            resToken = resToken.Replace("Bearer", "").Trim();
            request.Headers.Add("cookie", resToken);
        }

        public override TaskData MapperBody(ConversationDto body)
        {
            body.model ??= "gpt-4";
            var config = this.configuration.GetSection($"Monica:{body.model}");
            var task = new TaskData
            {
                task_uid = "task:a2774e95-c39c-4c0a-8d27-448dc3bddee1",
                bot_uid = config.GetSection("bot").Value ?? "gpt_4_o_mini_chat",
                data = new Data
                {
                    conversation_id = "conv:710c4143-44a1-40e2-9a5e-ae6c9f12eb27",
                    Items = new List<Item>
                {
                    new()
                    {
                        item_Id = "msg:6f80cf10-c742-4a2d-925c-d9ba1bb9f0c1",
                        conversation_id = "conv:710c4143-44a1-40e2-9a5e-ae6c9f12eb27",
                        item_type = "reply",
                        summary = "__RENDER_BOT_WELCOME_MSG__",
                        data = new ItemData
                        {
                            type = "text",
                            content = "__RENDER_BOT_WELCOME_MSG__",
                            render_in_streaming = false
                        }
                    },
                },
                    pre_generated_reply_Id = "msg:3fbff7c5-0949-45b0-ad94-1427d88aac3f",
                    pre_parent_item_Id = "msg:db70e0f8-d127-4925-8c32-afc22de6b5e7",
                    origin = "chrome-extension://ofpnmcalabcbjgholdjcjblkibolbppb/chatTab.html?tab=chat&botName=GPT-4&botUid=gpt_4_chat",
                    origin_page_title = "GPT-4 - Monica 智能体",
                    trigger_by = "auto",
                    usemodel = config.GetSection("model").Value ?? "",
                },
                language = "auto",
                locale = "zh_CN",
                task_type = "chat",
                tool_data = new ToolData
                {
                    sys_skill_list = new List<SysSkill>
                {
                    new()
                    {
                        uid = "artifacts",
                        enable = true
                    }
                }
                },
                ai_resp_language = "Chinese (Simplified)"
            };

            string perGuid = task.data.Items[0].item_Id;
            foreach (var item in body.messages)
            {
                var itemId = $"msg:{Guid.NewGuid()}";
                var newItem = new Item
                {
                    conversation_id = "conv:eed171ca-698e-4ceb-99f6-ba9971dfe872",
                    item_Id = itemId,
                    item_type = "reply",
                    summary = item.content,
                    parent_item_id = perGuid,
                    data = new ItemData
                    {
                        type = "text",
                        content = item.content,
                        quote_content = ""
                    }
                };

                perGuid = itemId;

                task.data.Items.Add(newItem);
            }

            task.data.Items[^1].item_type = "question";

            task.data.pre_parent_item_Id = perGuid;

            return task;

        }
    }
}
