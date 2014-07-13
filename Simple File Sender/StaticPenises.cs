using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Simple_File_Sender
{
    public enum FilesFromNonContacts { ask, reject, accept }
    public class StaticPenises
    {
        public static readonly int PingPort = 6968;
        public static readonly int NamePingPort = 6967;
        public static readonly int MainPort = 6969;

        public static readonly int PacketSize = 65536;//65536

        public static readonly string BannedRefuseName = "danjegaypornosexvieazdaram";

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

    public class SendingRefusedException : Exception
    {
        public SendingRefusedException(string message)
            : base(message)
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

        public IPBannedException(Contact c)
            : base(String.Format("{0} ({1}) has banned your IP", c.ContactName, c.IP))
        {

        }
    }

    public class UnaccesableRemoteClientException : Exception
    {
        public UnaccesableRemoteClientException(IPAddress a)
            : base(String.Format("Failed to connect to {0}", a.ToString()))
        {

        }
    }

    public class NotInContactsException :Exception
    {
        public NotInContactsException(IPAddress a)
            : base(String.Format("IP {0} is not in contacts - receiving aborted", a.ToString()))
        {

        }
    }
}
