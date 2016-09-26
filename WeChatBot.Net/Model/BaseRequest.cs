using System;
using System.Collections.Generic;

namespace WeChatBot.Net.Model
{
    public class BaseRequest
    {
        public int Uin { get; set; }
        public string Sid { get; set; }
        public string Skey { get; set; }
        public string DeviceID { get; set; }
    }
}