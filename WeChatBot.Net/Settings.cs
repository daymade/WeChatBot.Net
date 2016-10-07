using WeChatBot.Net.Enums;

namespace WeChatBot.Net
{
    public class Settings
    {
        /// <summary>
        /// Choose output to console or show as png
        /// </summary>
        public QRCodeOutputType QRCodeOutputType { get; set; }

        /// <summary>
        /// show debug messages
        /// </summary>
        public bool Debug { get; set; }

        /// <summary>
        /// use proxy of fiddler inspector 
        /// </summary>
        public bool UseFiddlerProxy { get; set; }
    }
}