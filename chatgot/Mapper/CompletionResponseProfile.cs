using AutoMapper;
using chatgot.Models;

namespace chatgot.Mapper
{
    public class CompletionResponseProfile : Profile
    {
        public CompletionResponseProfile()
        {
            this.CreateMap<GotCompletionResponse, CompletionResponseDto>();
            this.CreateMap<ConversationDto, GotConversationDto>()
                .ForMember(c => c.model, b => b.Ignore());
        }
    }
}
