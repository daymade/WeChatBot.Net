namespace WeChatBot.Net.Model.API.Base
{
    public class BaseRequest
    {
        public int Uin { get; set; }
        public string Sid { get; set; }
        public string Skey { get; set; }
        public string DeviceID { get; set; }
    }
}