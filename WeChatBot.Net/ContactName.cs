using System;
using System.Linq;

namespace WeChatBot.Net
{
    public class ContactName
    {
        public bool HasValue => Value != null;
        public string Value => new[] { RemarkName, Nickname, DisplayName }.FirstOrDefault(x=> !String.IsNullOrEmpty(x));

        public string RemarkName { get; set; }
        public string Nickname { get; set; }
        public string DisplayName { get; set; }
    }
}