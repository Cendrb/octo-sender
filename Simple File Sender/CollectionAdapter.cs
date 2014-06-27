using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Threading;

namespace Simple_File_Sender
{
    public class CollectionAdapter<T>
    {
        List<T> first;
        Dispatcher dispatcher;

        public CollectionAdapter(List<T> first, Dispatcher dispatcher)
        {
            this.first = first;
            this.dispatcher = dispatcher;
        }

        public void Add(T item)
        {
            dispatcher.BeginInvoke(new Action<T>((x) => first.Add(x)), item);
        }

        public void Remove(T item)
        {
            dispatcher.BeginInvoke(new Action<T>((x) => first.Remove(x)), item);
        }
    }
}
