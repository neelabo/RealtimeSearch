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
#pragma warning disable CS0612 // 型またはメンバーが旧型式です
            var legacy = SettingLegacy.Load(input);
#pragma warning restore CS0612 // 型またはメンバーが旧型式です
            var setting = legacy.ConvertToAppConfig();
        }
    }
}
