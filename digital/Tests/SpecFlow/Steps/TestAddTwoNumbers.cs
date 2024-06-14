using Digital.DigitalUtils.Jobs;
using OMREngineTest.Tests.Smoketests.Specflow.Step_Definitions;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using TechTalk.SpecFlow;
using Xunit;
using Xunit.Abstractions;

namespace Digital.Tests.SpecFlow.Steps
{
    [Binding]
    public class Class0614 
    {
         [When(@"I add two Numbers")]
        public void AddTwoNumbers()
        {
            int a = 10;
            int b = 20;
            int c = a + b;
            Assert.True(c == 40);
        }

    }
}
