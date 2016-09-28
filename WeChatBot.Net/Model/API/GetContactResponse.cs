using WeChatBot.Net.Model.API.Base;

namespace WeChatBot.Net.Model.API
{
    public class GetContactResponse
    {
        public BaseResponse BaseResponse { get; set; }
        public int MemberCount { get; set; }
        public Memberlist[] MemberList { get; set; }
        public int Seq { get; set; }
    }
}