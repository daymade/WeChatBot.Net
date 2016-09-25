using NUnit.Framework;
using WeChatBot.Net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WeChatBot.Net.Enums;

namespace WeChatBot.Net.Tests
{
    [TestFixture()]
    public class ClientTests
    {
        [Test()]
        public async Task GetUuidTest_Normal_ShouldReturnTrue()
        {
            var q = await new Client().GetUuid();
            Assert.IsTrue(q.Item1);
        }

        [Test()]
        [Ignore("manual run this for debug")]
        public async Task RunTest_Normal_ShouldOuputQRCode()
        {
            var client = new Client
            {
                Debug = true,
                QRCodeOutputType = QRCodeOutputType.TTY
            };
            await client.Run();

            Assert.Pass();
        }
    }
}