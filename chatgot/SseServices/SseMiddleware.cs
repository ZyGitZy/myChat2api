using chatgot.Models;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
using System.Text;
using AutoMapper;
using chatgot.Units;
using SseServices;
using System.Net.Http.Headers;

namespace chatgot.SseServices
{
    public class SseMiddleware
    {
        private readonly RequestDelegate _next;
        private HttpClient _httpClient;
        private HttpClient _monicaHttpClient;
        private readonly IMapper mapper;
        private readonly int delay;
        IConfiguration configuration;
        readonly JsonSerializerSettings settings = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };
        public SseMiddleware(RequestDelegate next, IMapper mapper,
            IConfiguration configuration
            )
        {
            this.configuration = configuration;
            this._httpClient = new HttpClient();
            this._monicaHttpClient = new HttpClient();
            _next = next;
            this.mapper = mapper;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (string.IsNullOrWhiteSpace(context.Request.Path.Value))
            {
                await _next(context);
                return;
            }

            if (context.Request.Path.Value.EndsWith("/v1/chat/completions") && !context.Verify()) return;

            if (context.Request.Path.Value.EndsWith("/monica/v1/chat/completions"))
            {
                // 同一个httpclinet不知道会不会有问题
                await MonicaMapper(context, this._httpClient);
                return;
            }
            else if (context.Request.Path.Value.EndsWith("/v1/chat/completions"))
            {
                await MapperGot(context, this._httpClient);
                return;
            }

            await _next(context);
        }

        private async Task MonicaMapper(HttpContext context, HttpClient httpClient)
        {
            var body = await HttpUnit.GetBody(context);
            var task = MapperBody(body);

            HttpRequestMessage requset = new(HttpMethod.Post, "https://monica.im/api/custom_bot/chat")
            {
                Content = new StringContent(JsonConvert.SerializeObject(task))
            };
            requset.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            requset.Headers.Add("cookie", (await context.GetAuthorization()).Replace("Bearer","").Trim());
            var response = await httpClient.SendAsync(requset, HttpCompletionOption.ResponseHeadersRead);
            var comp = InitCompletionResponse(body.model);
            if (body.stream)
            {
                await SendStream(response, context, async (e) =>
                {
                    if (!string.IsNullOrWhiteSpace(e))
                    {
                        string patternDto = @"^\s*data:\s*";
                        string jsonData = Regex.Replace(e, patternDto, "", RegexOptions.IgnoreCase);
                        var res = JsonConvert.DeserializeObject<Monica>(jsonData);
                        comp.choices[0].delta.content = res.text;

                        if (res.finished.HasValue)
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
                       string patternDto = @"^\s*data:\s*";
                       string jsonData = Regex.Replace(e, patternDto, "", RegexOptions.IgnoreCase);
                       var res = JsonConvert.DeserializeObject<Monica>(jsonData);
                       if (res != null)
                       {
                           comp.choices[0].delta.content += res.text;
                       }
                       return comp;
                   });
        }


        private CompletionResponseDto InitCompletionResponse(string model)
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

        private TaskData MapperBody(ConversationDto body)
        {
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
                    new Item
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
                    new SysSkill
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
                    summary = item.Content,
                    parent_item_id = perGuid,
                    data = new ItemData
                    {
                        type = "text",
                        content = item.Content,
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

        private async Task MapperGot(HttpContext context, HttpClient httpClient)
        {
            var body = await HttpUnit.GetBody(context);
            body.model ??= "gpt-4";
            var gotDto = new GotConversationDto
            {
                Type = body.model,
                Timezone = "Etc/GMT-8",
                Messages = body.messages,
            };
            var idName = "openai";
            if (body.model.Contains("claude"))
            {
                idName = "anthropic";
            }
            gotDto.Model = new Model
            {
                Id = $"{idName}/{body.model}",
                Owner = $"{(body.model.Contains("claude") ? "Anthropic" : "OpenAI")}",
                Name = $"{idName}/{body.model}",
                Order = 1,
                Placeholder = "",
                Type = "Queries",
                PicConv = "self-sufficient",
                DefaultRec = true,
                Level = "advanced",
                ContentLength = "200k",
                Title = body.model,
            };

            var request = await FillRequsetHeader(context, gotDto);
            //var toCurl = ToCurl(request);

            var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            if (body.stream)
            {
                await SendStream(response, context, async (line) =>
                {
                    if (!string.IsNullOrWhiteSpace(line) && line.StartsWith("data"))
                    {
                        await context.Response.WriteAsync(line + "\n\n");
                        await context.Response.Body.FlushAsync();
                    }
                });
                return;
            }
            CompletionResponseDto? comp = null;
            await SendJson(response, context, (responseStr) =>
               {
                   if (comp == null)
                   {
                       string patternDto = @"^\s*data:\s*";
                       string jsonData = Regex.Replace(responseStr, patternDto, "", RegexOptions.IgnoreCase);
                       var gotResult = JsonConvert.DeserializeObject<GotCompletionResponse>(jsonData);
                       var mapperResult = this.mapper.Map<GotCompletionResponse, CompletionResponseDto>(gotResult);
                       comp = mapperResult;
                   }
                   else
                   {
                       string pattern = $"\"content\":\"([^\"]*)\"";
                       Match match = Regex.Match(responseStr, pattern);
                       if (match.Success)
                       {
                           comp.choices![0].delta!.content += match.Groups[1].Value;
                       }
                   }

                   return comp;
               });
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

            await context.Response.WriteAsync(JsonConvert.SerializeObject(comp, settings));
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

        private async Task ReaderStream(HttpResponseMessage response, Func<string, Task<bool>> fun)
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

        private async Task<HttpRequestMessage> FillRequsetHeader(HttpContext context, GotConversationDto gotDto)
        {
            var jStr = JsonConvert.SerializeObject(gotDto, settings);
            var content = new StringContent(jStr, Encoding.UTF8, "application/json");

            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.chatgot.io/api/chat/conver")
            {
                Content = content
            };
            request.Headers.Add("User-Agent", GetUserAgent());
            var resToken = await context.GetAuthorization();
            request.Headers.Add("Authorization", resToken);

            return request;
        }

        private string GetUserAgent()
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

    }

}
