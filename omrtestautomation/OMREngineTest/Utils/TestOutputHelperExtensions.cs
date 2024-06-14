using System;
using System.Collections.Generic;
using System.Text;
using Xunit.Abstractions;

namespace OMREngineTest.Utils
{
    public static class TestOutputHelperExtensions
    {
        public static void LogMessage(this ITestOutputHelper output, string message)
        {
            Console.WriteLine(message);
            output.WriteLine(message);
        }

        public static void FatalErrorHandler(this ITestOutputHelper output, string message)
        {
            LogMessage(output, "\n**************************************************  FATAL ERROR  *******************************************************");
            LogMessage(output, "\n\t" + message);
            LogMessage(output, "\n************************************************************************************************************************\n");
        }

        public static void WarningMessage(this ITestOutputHelper output, string message)
        {
            LogMessage(output, "*Warning* " + message);
        }
    }
}
