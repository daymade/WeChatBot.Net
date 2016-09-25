using System;
using System.Diagnostics;

namespace WeChatBot.Net
{
    public class Logger
    {
        public Logger()
        {
        }

        public void Info(string message)
        {
            Console.WriteLine($@"[{CurrentMethodName().ToUpper()}] {message}");
        }

        public void Error(string message)
        {
            Console.WriteLine($@"[{CurrentMethodName().ToUpper()}] {message}");
        }

        /// <summary>
        ///     Function to display parent function
        /// </summary>
        /// <returns></returns>
        private static string CurrentMethodName()
        {
            var stackTrace = new StackTrace();
            var stackFrame = stackTrace.GetFrame(1);
            var methodBase = stackFrame.GetMethod();

            return methodBase.Name;
        }
    }
}