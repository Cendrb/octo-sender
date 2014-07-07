using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Simple_File_Sender
{
    public class StaticPenises
    {
        public static readonly int PingPort = 6968;
        public static readonly int NamePingPort = 6967;
        public static readonly int MainPort = 6969;

        public static NumberFormatInfo Format = new NumberFormatInfo();

        public static void Initialize()
        {
            Format.NumberGroupSeparator = " ";
            Format.NumberDecimalDigits = 0;
        }
    }
}
