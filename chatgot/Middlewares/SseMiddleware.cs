﻿using AutoMapper;
using chatgot.Units;
using SseServices.MonicaService;
using chatgot.SseServices;
using chatgot.SseService;

namespace chatgot.Middlewares
{
    public class SseMiddleware
    {
        private readonly RequestDelegate _next;
        private HttpClient _httpClient;
        HttpClientHandler handler;
        IConfiguration configuration;
        public SseMiddleware(RequestDelegate next,
            IConfiguration configuration
            )
        {
            this.configuration = configuration;
            handler = new HttpClientHandler();
            handler.MaxResponseHeadersLength = 8196;
            _httpClient = new HttpClient(handler);
            _next = next;
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
                await new MonicaService(configuration, "https://monica.im/api/custom_bot/chat").SendAsync(context, _httpClient);
                return;
            }
            else if (context.Request.Path.Value.EndsWith("/merlin/v1/chat/completions"))
            {
                await new MerlinService(configuration, "https://uam.getmerlin.in/thread/unified?customJWT=true&version=1.1").SendAsync(context, _httpClient);
                return;
            }
            else if (context.Request.Path.Value.EndsWith("/notdiamond/v1/chat/completions"))
            {
                await new NotdiamondService(configuration, "https://chat.notdiamond.ai/").SendAsync(context, _httpClient);
                return;
            }
            else if (context.Request.Path.Value.EndsWith("/you/v1/chat/completions"))
            {
                await new YouService("https://you.com/api/streamingSearch").SendAsync(context, _httpClient);
                return;
            }
            else if (context.Request.Path.Value.EndsWith("/v1/chat/completions"))
            {
                await new ChatgotService("https://api.chatgot.io/api/v2/chat/conversation").SendAsync(context, _httpClient);
                return;
            }

            await _next(context);
        }
    }

}
