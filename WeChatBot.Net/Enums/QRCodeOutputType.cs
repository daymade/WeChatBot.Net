using System;

namespace WeChatBot.Net.Enums
{
    /// <summary>
    /// Choose output to console or show as png
    /// </summary>
    [Flags]
    public enum QRCodeOutputType
    {
        /// <summary>
        ///     show as png outside
        /// </summary>
        PNG = 1 << 0,

        /// <summary>
        ///     show via tty, only effective in terminal
        /// </summary>
        TTY = 1 << 1,

        /// <summary>
        ///     both output the qrcode to terminal and show as png outside
        /// </summary>
        Both = PNG | TTY
    }
}