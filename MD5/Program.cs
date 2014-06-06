using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security;
using System.IO;

namespace MD5
{
    class Program
    {
        static void Main(string[] args)
        {
            FileStream file = new FileStream("penis", FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
            using(System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
            {
                byte[] penis = md5.ComputeHash(file);
                Console.ReadKey();
            }
        }
    }
}
