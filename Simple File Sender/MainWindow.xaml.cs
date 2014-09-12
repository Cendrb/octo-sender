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
using System.IO;

namespace Simple_File_Sender
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Sender sender;
        Receiver receiver;
        // Loads contacts
        DataLoader dataLoader;
        List<int> usedPorts = new List<int>();
        NotifyIcon trayIcon;
        System.Windows.Forms.ContextMenu trayMenu;

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
            try
            {
                trayIcon.Icon = new Icon("Resources\\icon.ico");
            }
            catch(Exception e)
            {
                Console.WriteLine(e.Message);
            }
            trayIcon.MouseClick += trayIcon_MouseClick;
            trayIcon.MouseMove += trayIcon_MouseMove;
            trayIcon.BalloonTipClicked += trayIcon_BalloonTipClicked;


            // Static properties
            StaticPenises.Initialize();

            // Loading saved contacts
            dataLoader = new DataLoader();
            if (!dataLoader.ReadData())
            {
                dataLoader.SaveData();
            }

            // Loading settings
            if (Properties.Settings.Default.FirstRun)
            {
                // First run setup
                Properties.Settings.Default.Reset();

                // TODO Add welcome window

                // Basic info
                BasicInfoDialog dialog = new BasicInfoDialog();
                dialog.ShowDialog();
                if (!dialog.Success)
                {
                    // Shutdown application if window was closed
                    System.Windows.Application.Current.Shutdown();
                    Thread.Sleep(50);
                }
                else
                {
                    // Save settings
                    Properties.Settings.Default.UserName = dialog.UsernameText.Text;
                    Properties.Settings.Default.DefaultSavePath = dialog.DefaultSavePath;
                    Properties.Settings.Default.UseDefaultSavePath = Directory.Exists(dialog.DefaultSavePath);
                    Properties.Settings.Default.FirstRun = false;
                    Properties.Settings.Default.Save();
                }
            }

            // Initialize sender
            sender = new Sender(usedPorts, sendingQueue.Items, Properties.Settings.Default.UserName);
            sender.FileSent += sender_FileSent;

            // Default path or ask everytime
            Func<string, string> choosePath;
            if (Properties.Settings.Default.UseDefaultSavePath)
                choosePath = (x) => Properties.Settings.Default.DefaultSavePath;
            else
                choosePath = choosePathMethod;

            // Initialize receiver
            receiver = new Receiver(usedPorts, receivingQueue.Items, contacts.Items, choosePath, Properties.Settings.Default.UserName);
            receiver.FileReceived += receiver_FileReceived;
            receiver.BannedIPs = Properties.Settings.Default.BannedIPs;
            receiver.Start(); // Starts listeners

            refresh(); // Adds contacts and tasks
        }

        private void AddContactFromPair(NameIPPair c, bool saved)
        {
            Contact contact = new Contact(c);
            contact.Delete += contact_Delete;
            contact.Saved = saved;
            contact.Ban += contact_Ban;
            contact.Pardon += contact_Pardon;
            contact.PingAndView();
            contact.SendFile += sender.SendFile;
            contact.Banned = Properties.Settings.Default.BannedIPs.Contains(contact.IP.ToString());
            contacts.Items.Add(contact);
        }

        private void PingAllContacts()
        {
            foreach (Contact contact in contacts.Items)
            {
                contact.PingAndView();
            }
        }

        /// <summary>
        /// Clears and adds tasks and contacts
        /// </summary>
        private void refresh()
        {
            DisableRefreshButtons();

            contacts.Items.Clear();
            AddSavedContacts();
            AddOnlineContacts(EnableRefreshButtons);
            PingAllContacts();


        }

        private void EnableRefreshButtons()
        {
            Refresh.IsEnabled = true;
            RefreshButton.IsEnabled = true;
            Refresh.Content = "Refresh";
            RefreshButton.Header = "Refresh";
        }

        private void DisableRefreshButtons()
        {
            Refresh.IsEnabled = false;
            RefreshButton.IsEnabled = false;
            Refresh.Content = "Refreshing...";
            RefreshButton.Header = "Refreshing...";
        }


        /// <summary>
        /// Adds contacts actually loaded in dataloader
        /// </summary>
        private void AddSavedContacts()
        {
            foreach (NameIPPair pair in dataLoader.Contacts)
                AddContactFromPair(pair, true);
        }


        /// <summary>
        /// Adds all available clients in local network to contacts
        /// </summary>
        /// <param name="callback">Action called after adding (could be null)</param>
        private async void AddOnlineContacts(Action callback)
        {
            IPAddress[] IPs = Dns.GetHostAddresses(Dns.GetHostName());

            Pinger pinger = new Pinger();
            List<NameIPPair> pingerContacts = await pinger.GetOnlineContacts(1000);
            foreach (NameIPPair c in pingerContacts)
            {
                if (!Properties.Settings.Default.ShowLocalClientInContacts && IPs.Contains(c.IP))
                    Console.WriteLine("Skipping local contact: " + c.ToString());
                else if (!Properties.Settings.Default.ShowBannedContactsInContacts && Properties.Settings.Default.BannedIPs.Contains(c.IP.ToString()))
                    Console.WriteLine("Skipping banned contact: " + c.ToString());
                else
                    AddContactFromPair(c, false);
            }
            if (callback != null)
                callback();
        }

        #region Event Handlers

        #region Buttons

        /// <summary>
        /// Adds contact to contacts list and saves it via dataloader
        /// </summary>
        /// <param name="penis"></param>
        /// <param name="e"></param>
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

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            refresh();
        }

        #endregion

        #region Menu
        private void NewContactButton_Click(object sender, RoutedEventArgs e)
        {
            AddContact_Click(sender, e);
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            Exit();
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            refresh();
        }

        private void BasicDataButton_Click(object sender, RoutedEventArgs e)
        {
            BasicInfoDialog dialog = new BasicInfoDialog();

            dialog.UsernameText.Text = Properties.Settings.Default.UserName;
            dialog.AskCheckbox.IsChecked = !Properties.Settings.Default.UseDefaultSavePath;
            if (Properties.Settings.Default.DefaultSavePath != String.Empty)
                dialog.DefaultSavePath = Properties.Settings.Default.DefaultSavePath;

            dialog.ShowDialog();
            if (dialog.Success)
            {
                Properties.Settings.Default.UserName = dialog.UsernameText.Text;
                Properties.Settings.Default.DefaultSavePath = dialog.DefaultSavePath;
                Properties.Settings.Default.UseDefaultSavePath = Directory.Exists(dialog.DefaultSavePath);
                Properties.Settings.Default.Save();

                receiver.Name = Properties.Settings.Default.UserName;

                Func<string, string> choosePath;

                if (Properties.Settings.Default.UseDefaultSavePath)
                    choosePath = (x) => Properties.Settings.Default.DefaultSavePath;
                else
                    choosePath = choosePathMethod;

                receiver.Path = choosePath;

                refresh();
            }
        }

        private void FactoryResetButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = System.Windows.MessageBox.Show("This will wipe all settings and banlist! (keeps your contacts)\nDo you really want to continue?", "Factory reset", MessageBoxButton.YesNo, MessageBoxImage.Exclamation);
            if (result == MessageBoxResult.Yes)
            {
                Properties.Settings.Default.FirstRun = true;
                Properties.Settings.Default.Save();

                System.Windows.MessageBox.Show("Restart Octo Sender to perform wipe", "Restart required", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void ClearBanlistButton_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.BannedIPs.Clear();
            receiver.BannedIPs.Clear();
            Properties.Settings.Default.Save();
            refresh();
        }

        private void PreferencesButton_Click(object sender, RoutedEventArgs e)
        {
            Preferences prefs = new Preferences();
            prefs.ShowDialogLoadAndSave();
            refresh();
        }

        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void BackupButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void RestoreButton_Click(object sender, RoutedEventArgs e)
        {

        }
        #endregion

        private void contact_Pardon(Contact contact)
        {
            Properties.Settings.Default.BannedIPs.Remove(contact.IP.ToString());
            Properties.Settings.Default.Save();
        }

        private void contact_Ban(Contact contact)
        {
            Properties.Settings.Default.BannedIPs.Add(contact.IP.ToString());
            Properties.Settings.Default.Save();
            if (!Properties.Settings.Default.ShowBannedContactsInContacts)
                contacts.Items.Remove(contact);
        }

        private void trayIcon_BalloonTipClicked(object sender, EventArgs e)
        {
            Console.WriteLine(sender.ToString());
            Visibility = System.Windows.Visibility.Visible;
        }

        /// <summary>
        /// File received handler
        /// </summary>
        /// <param name="task">Received file task</param>
        private void receiver_FileReceived(ReceiverTask task)
        {
            trayIcon.ShowBalloonTip(3000, "Octo Sender", task.ReceivedFileName + " received from " + task.SenderName, ToolTipIcon.Info);
        }

        /// <summary>
        /// File sent handler
        /// </summary>
        /// <param name="task">Sent file task</param>
        private void sender_FileSent(SenderTask task)
        {
            // Show balloon tip
            trayIcon.ShowBalloonTip(3000, "Octo Sender", task.SourceFile.Name + " sent to " + task.TargetContact.ContactName, ToolTipIcon.Info);
        }

        private void trayIcon_MouseMove(object penis, System.Windows.Forms.MouseEventArgs e)
        {
            trayIcon.Text = String.Format("Octo Sender\nReceiver tasks: {0}\nSender tasks: {1}", receiver.RunningTasks, sender.RunningTasks);
        }

        private void contact_Delete(Contact obj)
        {
            dataLoader.Contacts.Remove(obj.Pair);
            dataLoader.SaveData();
            contacts.Items.Remove(obj);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            CloseDialogResult result = new CloseDialog().ShowCloseDialog();
            switch (result)
            {
                case CloseDialogResult.Minimize:
                    e.Cancel = true;
                    Visibility = System.Windows.Visibility.Collapsed;
                    break;
                case CloseDialogResult.Back:
                    e.Cancel = true;
                    break;
                case CloseDialogResult.Exit:
                    e.Cancel = true;
                    Exit();
                    break;
            }

        }

        private void OnExit(object sender, EventArgs e)
        {
            Exit();
        }

        /// <summary>
        /// Tell user about possible running tasks
        /// </summary>
        private void Exit()
        {
            if (receiver.RunningTasks > 0 || sender.RunningTasks > 0)
            {
                DialogResult result = System.Windows.Forms.MessageBox.Show(String.Format("Total of {0} tasks is in progress.\nReceiver tasks: {1}\nSender tasks: {2}\nClosing will abort all running tasks.\nDo you want to close Octo Sender?", receiver.RunningTasks + sender.RunningTasks, receiver.RunningTasks, sender.RunningTasks), "Tasks not completed", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation);
                if (result == System.Windows.Forms.DialogResult.Yes)
                    Shutdown();
            }
            else
            {
                Shutdown();
            }
        }
        /// <summary>
        /// Force shutdown
        /// </summary>
        private void Shutdown()
        {
            receiver.StopAllTasks();
            sender.StopAllTasks();
            receiver.Stop();
            Thread.Sleep(100);
            System.Windows.Application.Current.Shutdown();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            Exit();
        }

        private void trayIcon_MouseClick(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            Visibility = System.Windows.Visibility.Visible;
        }

        private string choosePathMethod(string message)
        {
            System.Windows.Forms.FolderBrowserDialog folderDialog = new System.Windows.Forms.FolderBrowserDialog();
            folderDialog.Description = message;
            folderDialog.ShowDialog();
            if (Directory.Exists(folderDialog.SelectedPath))
                return folderDialog.SelectedPath;
            else
                return String.Empty;
        }

        #endregion
    }
}
