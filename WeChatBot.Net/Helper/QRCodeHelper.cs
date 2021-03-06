﻿using System.Diagnostics;
using System.Threading.Tasks;
using QRCoder;
using WeChatBot.Net.Enums;
using WeChatBot.Net.Util;

namespace WeChatBot.Net.Helper
{
    public class QRCodeHelper
    {
        private readonly FileManager _fileManager = new FileManager();
        private readonly ConsoleWriter _consoleWriter = new ConsoleWriter();
        private readonly Logger _logger = new Logger();

        public async Task ShowQRCode(string uuid, QRCodeOutputType qrCodeOutputType)
        {
            var loginQRCode = GenerateLoginQRCode(uuid);

            await ShowQRCode(loginQRCode, qrCodeOutputType);

        }

        /// <summary>
        ///     Display Qrcode for user interaction
        /// </summary>
        /// <param name="qrCodeData">QrCodeData is Create by QRCodeGenerator.CreateQrCode</param>
        /// <param name="qrCodeOutputType">Choose output to console or show as png</param>
        /// <returns></returns>
        protected async Task ShowQRCode(QRCodeData qrCodeData, QRCodeOutputType qrCodeOutputType)
        {
            if (qrCodeOutputType.HasFlag(QRCodeOutputType.TTY))
            {
                await _consoleWriter.WriteToConsoleAsync(qrCodeData.ModuleMatrix);
            }

            if (qrCodeOutputType.HasFlag(QRCodeOutputType.PNG))
            {
                var qrCodePath = _fileManager.GetTempFilePath("wxqr.png");
                var qrCodeImage = new QRCode(qrCodeData).GetGraphic(10);
                qrCodeImage.Save(qrCodePath);

                await Task.Run(() => Process.Start(qrCodePath));
            }
        }

        /// <summary>
        ///     Generate QRCodeData for output to console or show as png
        /// </summary>
        /// <param name="uuid">an unique token within current session, generated by server</param>
        /// <returns></returns>
        private QRCodeData GenerateLoginQRCode(string uuid)
        {
            string str = $@"https://login.weixin.qq.com/l/{uuid}";

            var qrGenerator = new QRCodeGenerator();
            var qrCodeData = qrGenerator.CreateQrCode(str, QRCodeGenerator.ECCLevel.Q);

            return qrCodeData;
        }
    }
}
