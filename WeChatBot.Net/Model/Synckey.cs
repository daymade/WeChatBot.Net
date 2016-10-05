namespace WeChatBot.Net.Model
{
    public class Synckey
    {
        public int Count { get; set; }
        public List[] List { get; set; }
    }

    public class List
    {
        public int Key { get; set; }
        public int Val { get; set; }
    }
}