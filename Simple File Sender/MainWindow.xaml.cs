using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace Simple_File_Sender
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Sender sender;
        Receiver receiver;
        List<int> usedPorts = new List<int>();

        public MainWindow()
        {
            InitializeComponent();

            usedPorts.Add(6969);
            usedPorts.Add(6968);

            //Task.Factory.StartNew(setupReceiverAndSender);

            sender = new Sender(usedPorts, sendingQueue.Items, "Superpennys139");

            receiver = new Receiver(usedPorts, receivingQueue.Items, @"C:\Users\cendr_000\Desktop");
            receiver.Start();


            contacts.Items.Add(new Contact("Ivona Součková", IPAddress.Parse("127.0.0.1")));
            contacts.Items.Add(new Contact("Mama", IPAddress.Parse("192.168.1.2")));
            contacts.Items.Add(new Contact("Whoa?", IPAddress.Parse("192.168.1.5")));

            foreach (Contact contact in contacts.Items)
            {
                contact.SendFile += sender.SendFile;
                contact.PingAndView();
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void AddContact_Click(object penis, RoutedEventArgs e)
        {
            ContactInput input = new ContactInput();
            input.ShowDialog();
            if (input.Success)
            {
                Contact contact = new Contact(input.NameText.Text, input.Address);
                contact.SendFile += sender.SendFile;
                contacts.Items.Add(contact);
                contact.PingAndView();
            }
        }

        private void PingAllContacts()
        {
            foreach (Contact contact in contacts.Items)
            {
                contact.PingAndView();
            }
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            PingAllContacts();
        }

    }
}
