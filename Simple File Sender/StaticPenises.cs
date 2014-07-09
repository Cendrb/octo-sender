using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Simple_File_Sender
{
    public class StaticPenises
    {
        public static readonly int PingPort = 6968;
        public static readonly int NamePingPort = 6967;
        public static readonly int MainPort = 6969;

        public static readonly string RefuseName = "PENISadisPopopenis";

        public static NumberFormatInfo Format = new NumberFormatInfo();

        public static void Initialize()
        {
            Format.NumberGroupSeparator = " ";
            Format.NumberDecimalDigits = 0;
        }
    }

    public class InterruptedByUserException : Exception
    {
        public InterruptedByUserException()
            : base("Operation was interrupted by user")
        {

        }
    }

    public class RefusedByOppositeSideException : Exception
    {
        public RefusedByOppositeSideException()
            : base("Operation was refused by opposite side")
        {

        }
    }
    public class IPBannedException : Exception
    {
        public IPBannedException(IPAddress a)
            : base(String.Format("IP {0} is banned", a.ToString()))
        {

        }
    }
}
