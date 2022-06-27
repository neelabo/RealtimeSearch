using System;
using System.Collections.Generic;
using System.Reflection;
using Xunit;
using Xunit.Sdk;
using NeeLaboratory.RealtimeSearch;

namespace RealtimeSratchUnitTest
{
    public class RealtimeSearchMain
    {
        [Theory]
        [InlineData("TestData/UserSetting.v3.xml")]
        public void Test1(string input)
        {
#pragma warning disable CS0612 // Œ^‚Ü‚½‚Íƒƒ“ƒo[‚ª‹ŒŒ^®‚Å‚·
            var legacy = SettingLegacy.Load(input);
#pragma warning restore CS0612 // Œ^‚Ü‚½‚Íƒƒ“ƒo[‚ª‹ŒŒ^®‚Å‚·
            var setting = legacy.ConvertToSetting();
        }
    }
}
