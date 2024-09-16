using chatgot.Models;
using chatgot.Models.MerlinModels;
using chatgot.Models.MonicaModels;
using chatgot.Models.NotdiamondModels;
using chatgot.Units;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;

namespace chatgot.SseServices
{
    public class NotdiamondService : CommonService
    {
        IConfiguration configuration;
        readonly string url;
        public NotdiamondService(IConfiguration configuration, string url)
        {
            this.configuration = configuration;
            this.url = url;
        }

        public List<NotdiamondRequest> MapperBody(CompletionsDto body)
        {
            string model;
            if (body.model.Contains("gpt"))
            {
                model = "openai";
            }
            else if (body.model.Contains(""))
            {
                model = "anthropic";
            }
            else
            {
                model = "google";
            }
            return new List<NotdiamondRequest>
            {
                new  NotdiamondRequest{
                     messages = body.messages,
                     provider = new Povider{
                         model = body.model,
                         provider = model
                     }
                },
            };
        }

        public override async Task SendAsync(HttpContext context, HttpClient httpClient)
        {
            var body = await context.GetBody();
            var mapper = MapperBody(body);
            string res = JsonConvert.SerializeObject(mapper);
            using var response = await SendRequest(mapper, httpClient, context, url);
            var comp = InitCompletionResponse(body.model);
            if (body.stream)
            {
                await this.SendStream<NotdiamondResponse>(response, context, async (data) =>
                {
                    if (data != null)
                    {
                        if (!string.IsNullOrEmpty(data.curr))
                        {
                            comp.choices![0].delta!.content = data.curr;
                        }
                        else if (data.diff != null)
                        {
                            comp.choices![0].delta!.content = data.diff[^1];
                        }
                        await this.FlushAsync(context, comp);
                    }
                });
            }
            else
            {
                await SendJson<List<NotdiamondResponse>>(response, context, body.model, (data) =>
                {
                    comp.choices![0].delta!.content = string.Join("", data.Select(s =>
                    {
                        if (!string.IsNullOrEmpty(s.curr))
                            return s.curr;
                        else if (s.diff != null)
                            return s.diff[^1];

                        return "";
                    }));
                    return comp;
                });
            }
        }


        public override async Task SendStream<T>(HttpResponseMessage response, HttpContext context, Action<T> fun)
        {
            SetResponseHeader(context);
            using var stream = await response.Content.ReadAsStreamAsync();
            using var reader = new StreamReader(stream);
            while (!reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync();
                if (!string.IsNullOrWhiteSpace(line))
                {
                    fun(DeserializeObject<T>(line));
                }
            }
        }

        public override T DeserializeObject<T>(string data)
        {
            if (!data.Contains("diff") && (!data.Contains("curr") || data.Contains("output")))
            {
                return default;
            }
            string patternDto = @"^.*?:";
            string jsonData = Regex.Replace(data, patternDto, "", RegexOptions.IgnoreCase);
            var res = JsonConvert.DeserializeObject<T>(jsonData);
            return res;
        }


        public override async Task<T> MapperJsonToObj<T>(string model, HttpResponseMessage response)
        {

            try
            {
                var responseStr = await response.Content.ReadAsStringAsync();
                string pattern = @"\{[^{}]+\}";
                MatchCollection matches = Regex.Matches(responseStr, pattern);
                var filterData = matches.Select(s => s.Value);
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

        public override async Task SetRequestHeader(HttpRequestMessage request, HttpContext context)
        {
            var nextAction = this.configuration.GetSection("Notdiamond:next-action").Value ?? throw new Exception("next-action is null");
            request.Headers.Add("next-action", nextAction);
            var resToken = await context.GetAuthorization();
            if (string.IsNullOrWhiteSpace(resToken)) throw new Exception("cookie is null");
            request.Headers.Add("cookie", ReplaceTimestamp(resToken));
            request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("text/plain; charset=UTF-8");
        }

        public string ReplaceTimestamp(string input)
        {
            // 获取当前时间戳
            long currentTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            // 替换找到的时间戳
            string result = Regex.Replace(input, @"%5B(\d+)%2C", match =>
            {
                return $"%5B{currentTimestamp + 6 * 60 * 1000}%2C";
            });

            result = Regex.Replace(result, @"%2C(\d+)%5D", match =>
            {
                return $"%2C{currentTimestamp}%5D";
            });

            return result;
        }
    }
}
