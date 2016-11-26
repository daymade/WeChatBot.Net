namespace WeChatBot.Net.Model
{
    public class SyncResult
    {
        public int RetCode { get; set; }
        public int Selector { get; set; }

        public SyncResult(int retcode, int selector)
        {
            RetCode = retcode;
            Selector = selector;
        }
    }
}
