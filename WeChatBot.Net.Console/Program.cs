using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Nito.AsyncEx;
using WeChatBot.Net.Enums;

namespace WeChatBot.Net.Console
{
    class Program
    {
        static int Main(string[] args)
        {
            var origWidth = System.Console.WindowWidth;
            var origHeight = System.Console.WindowHeight;

            System.Console.SetWindowSize(origWidth, origHeight * 2);
            try
            {
                return AsyncContext.Run(() => MainAsync(args));
            }
            catch (Exception ex)
            {
                System.Console.Error.WriteLine(ex);
                return -1;
            }
            finally
            {
                System.Console.ReadLine();
            }
        }

        [DebuggerStepThrough]
        static async Task<int> MainAsync(string[] args)
        {
            try
            {
                var client = new Client(new Settings() {
                    Debug = true,
                    QRCodeOutputType = QRCodeOutputType.TTY
                });
                await client.Run();
                return 0;
            }
            catch (Exception ex)
            {
                System.Console.Error.WriteLine(ex);
                if (Debugger.IsAttached)
                {
                    //the debugger no longer breaks here
                    throw;
                }
            }
            return -1;
        }
    }
}
