using System;

namespace WeChatBot.Net.Model
{
    public class Uuid
    {
        public bool HasValue => !string.IsNullOrWhiteSpace(UUID);
        public string UUID { get; set; }

        public Uuid(string uuid = null)
        {
            UUID = uuid;
        }

        public static Uuid Null = new Uuid();
    }
}
