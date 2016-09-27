using WeChatBot.Net.Enums;

namespace WeChatBot.Net
{
    public class Settings
    {
        /// <summary>
        /// show debug messages
        /// </summary>
        public bool Debug { get; set; }

        /// <summary>
        /// Choose output to console or show as png
        /// </summary>
        public QRCodeOutputType QRCodeOutputType { get; set; }
    }
}