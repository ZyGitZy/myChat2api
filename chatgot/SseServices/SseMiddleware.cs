using chatgot.Models;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
using System.Text;
using AutoMapper;
using chatgot.Units;
using SseServices;
using System.Net.Http.Headers;
using SseServices.MonicaService;

namespace chatgot.SseServices
{
    public class SseMiddleware
    {
        private readonly RequestDelegate _next;
        private HttpClient _httpClient;
        private readonly IMapper mapper;
        IConfiguration configuration;
        public SseMiddleware(RequestDelegate next, IMapper mapper,
            IConfiguration configuration
            )
        {
            this.configuration = configuration;
            this._httpClient = new HttpClient();
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
                await new MonicaService(this.configuration, "https://monica.im/api/custom_bot/chat").SendAsync(context, this._httpClient);
                return;
            }
            else if (context.Request.Path.Value.EndsWith("/merlin/v1/chat/completions"))
            {
                await new MerlinService(this.configuration, "https://uam.getmerlin.in/thread/unified?customJWT=true&version=1.1").SendAsync(context, this._httpClient);
                return;
            }
            else if (context.Request.Path.Value.EndsWith("/v1/chat/completions"))
            {
                await new GotService("https://api.chatgot.io/api/chat/conver").SendAsync(context, this._httpClient);
                return;
            }

            await _next(context);
        }
    }

}
