namespace WeChatBot.Net
{
    internal class Message
    {
        public int msg_type_id { get; set; }
        public int msg_id { get; set; }
        public MessageContent content { get; set; }
        public string to_user_id { get; set; }
        public User user { get; set; }

        public class User
        {
            public string id { get; set; }
            public string name { get; set; }
        }
    }


    
}