using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WeChatBot.Net.Util
{
    public class FileManager
    {
        public string GetTempFilePath(string fileName)
        {
            return Path.Combine(GetCacheDirectory(), fileName);
        }

        protected string GetCacheDirectory()
        {
            var cacheDirectory = Path.Combine(Directory.GetCurrentDirectory(), "temp");
            if (!Directory.Exists(cacheDirectory))
            {
                Directory.CreateDirectory(cacheDirectory);
            }
            return cacheDirectory;
        }
    }
}
