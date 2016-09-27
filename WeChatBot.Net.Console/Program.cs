using System;
using System.Threading.Tasks;
using Nito.AsyncEx;
using WeChatBot.Net.Enums;

namespace WeChatBot.Net.Console
{
    class Program
    {
        static int Main(string[] args)
        {
            try
            {
                return AsyncContext.Run(() => MainAsync(args));
            }
            catch (Exception ex)
            {
                System.Console.Error.WriteLine(ex);
                return -1;
            }
        }

        static async Task<int> MainAsync(string[] args)
        {
            var client = new Client
            {
                Debug = true,
                QRCodeOutputType = QRCodeOutputType.TTY
            };
            await client.Run();
            return 0;
        }
    }
}
