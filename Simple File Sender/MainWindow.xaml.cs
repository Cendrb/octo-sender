using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;
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
using System.Windows.Forms;
using System.Drawing;

namespace Simple_File_Sender
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Sender sender;
        Receiver receiver;
        DataLoader dataLoader;
        List<int> usedPorts = new List<int>();

        private NotifyIcon trayIcon;
        private System.Windows.Forms.ContextMenu trayMenu;

        public MainWindow()
        {
            InitializeComponent();

            // Tray icon
            trayMenu = new System.Windows.Forms.ContextMenu();
            trayMenu.MenuItems.Add("Exit", OnExit);

            trayIcon = new NotifyIcon();
            trayIcon.Text = "Octo Sender";
            trayIcon.Visible = true;
            trayIcon.ContextMenu = trayMenu;
            trayIcon.Icon = new Icon("icon.ico");
            trayIcon.MouseClick += trayIcon_MouseClick;

            StaticPenises.Initialize();

            dataLoader = new DataLoader();

            if (!dataLoader.ReadData())
            {
                BasicInfoDialog dialog = new BasicInfoDialog();
                while (!dialog.Success)
                {
                    dialog.ShowDialog();
                }
                dataLoader.Name = dialog.UsernameText.Text;
                System.Windows.Forms.FolderBrowserDialog penis = new System.Windows.Forms.FolderBrowserDialog();
                while (penis.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                {

                }
                dataLoader.Path = penis.SelectedPath;
                dataLoader.SaveData();
            }

            usedPorts.Add(6969); // Main comm port
            usedPorts.Add(6967); // Name ping port (Pinger class)
            usedPorts.Add(6968); // Ping port (Receiver class)

            sender = new Sender(usedPorts, sendingQueue.Items, dataLoader.Name);

            receiver = new Receiver(usedPorts, receivingQueue.Items, dataLoader.Path, dataLoader.Name);
            receiver.Start();

            refresh();
        }

        private void trayIcon_MouseClick(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            Visibility = System.Windows.Visibility.Visible;
        }

        private void OnExit(object sender, EventArgs e)
        {
            System.Windows.Application.Current.Shutdown();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            receiver.Stop();
            System.Windows.Application.Current.Shutdown();
        }

        private void AddContact_Click(object penis, RoutedEventArgs e)
        {
            ContactInput input = new ContactInput();
            input.ShowDialog();
            if (input.Success)
            {
                dataLoader.Contacts.Add(new NameIPPair(input.NameText.Text, input.Address));
                dataLoader.SaveData();
                AddContactFromPair(new NameIPPair(input.NameText.Text, input.Address), true);
            }
        }

        private void AddContactFromPair(NameIPPair c, bool saved)
        {
            Contact contact = new Contact(c);
            contact.Delete += contact_Delete;
            contact.Saved = saved;
            contact.PingAndView();
            contact.SendFile += sender.SendFile;
            contacts.Items.Add(contact);
        }

        private void contact_Delete(Contact obj)
        {
            dataLoader.Contacts.Remove(obj.Pair);
            dataLoader.SaveData();
            contacts.Items.Remove(obj);
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
            refresh();
        }

        private void refresh()
        {
            contacts.Items.Clear();
            addOnlineContacts();
            addSavedContacts();
            PingAllContacts();
        }

        private void addSavedContacts()
        {
            foreach (NameIPPair pair in dataLoader.Contacts)
                AddContactFromPair(pair, true);
        }

        private async void addOnlineContacts()
        {
            Pinger pinger = new Pinger();
            List<NameIPPair> pingerContacts = await pinger.GetOnlineContacts();
            foreach (NameIPPair c in pingerContacts)
            {
                AddContactFromPair(c, false);
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            CloseDialogResult result = new CloseDialog().ShowCloseDialog();
            switch(result)
            {
                case CloseDialogResult.Minimize:
                    e.Cancel = true;
                    Visibility = System.Windows.Visibility.Collapsed;
                    break;
                case CloseDialogResult.Back:
                    e.Cancel = true;
                    break;
            }
            
        }

    }
}
