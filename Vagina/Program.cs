using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vagina
{
    class Program
    {
        static void Main(string[] args)
        {
            Yvonne y = new Yvonne();
            
        }
    }

    interface IFuckable
    {
        void GetFucked();
    }

    class Yvonne : IFuckable
    {

        public void GetFucked()
        {
            throw new NotImplementedException();
        }
        void GetRaped()
        {
            throw new NotImplementedException();
        }
    }
}
