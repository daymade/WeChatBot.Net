using WeChatBot.Net.Enums;

namespace WeChatBot.Net.Console
{
    class Program
    {
        static void Main(string[] args)
        {
            var client = new Client
                         {
                             Debug = true,
                             QRCodeOutputType = QRCodeOutputType.TTY
                         };
            client.Run().Wait();
        }
    }
}
