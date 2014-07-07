using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Simple_File_Sender
{
    public class NameIPPair
    {
        public IPAddress IP {get;set;}
        public string Name {get;set;}

        public NameIPPair(string name, IPAddress address)
        {
            Name = name;
            IP = address;
        }

        public static bool operator ==(NameIPPair pair1, NameIPPair pair2)
        {
            return pair1.Name == pair2.Name && pair1.IP == pair2.IP;
        }

        public static bool operator !=(NameIPPair pair1, NameIPPair pair2)
        {
            return !(pair1 == pair2);
        }

        public override bool Equals(object obj)
        {
            return this == obj as NameIPPair;
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode() + IP.GetHashCode();
        }
    }
}
