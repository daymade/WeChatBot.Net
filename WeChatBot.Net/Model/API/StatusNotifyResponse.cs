using WeChatBot.Net.Model.API.Base;

namespace WeChatBot.Net.Model.API
{
    public class StatusNotifyResponse
    {
        public BaseResponse BaseResponse { get; set; }
        public string MsgID { get; set; }
    }
}