using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace WeChatBot.Net.Helper
{
    public static class QRCodeHelper
    {
        private const string Black = "██";
        private const string White = "  ";

        /// <summary>
        /// 
        /// </summary>
        /// <param name="bitMatrix"></param>
        /// <param name="withBorder"></param>
        /// <returns></returns>
        public static async Task WriteToConsoleAsync(List<BitArray> bitMatrix, bool withBorder = true)
        {
            await Task.Run(() =>
                           {
                               foreach (var line in bitMatrix)
                               {
                                   foreach (bool set in line)
                                   {
                                       var block = withBorder ? set ? White : Black
                                                              : set ? Black : White;
                                       Console.Write(block);
                                   }
                                   Console.Write(Environment.NewLine);
                               }
                           });
        }
    }
}
