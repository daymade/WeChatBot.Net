using System.Threading.Tasks;
using NUnit.Framework;
using QRCoder;
using WeChatBot.Net.Helper;

namespace WeChatBot.Net.Util.Tests
{
    [TestFixture()]
    public class ImageExtensionsTests
    {
        [Test()]
        public async Task WriteToConsoleTest()
        {
            var qrCodeData = new QRCodeGenerator().CreateQrCode("123",QRCodeGenerator.ECCLevel.Q);

            await new ConsoleWriter().WriteToConsoleAsync(qrCodeData.ModuleMatrix);
            Assert.Pass();
        }
    }
}
