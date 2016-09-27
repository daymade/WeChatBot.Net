using WeChatBot.Net.Model.API.Base;

namespace WeChatBot.Net.Model.API
{
    public class InitResponse
    {
        public BaseResponse BaseResponse { get; set; }
        public int Count { get; set; }
        public object[] ContactList { get; set; }
        public Synckey SyncKey { get; set; }
        public User User { get; set; }
        public string ChatSet { get; set; }
        public string SKey { get; set; }
        public int ClientVersion { get; set; }
        public int SystemTime { get; set; }
        public int GrayScale { get; set; }
        public int InviteStartCount { get; set; }
        public int MPSubscribeMsgCount { get; set; }
        public object[] MPSubscribeMsgList { get; set; }
        public int ClickReportInterval { get; set; }
    }
}