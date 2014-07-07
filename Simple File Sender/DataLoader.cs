using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace Simple_File_Sender
{
    public class DataLoader
    {
        public string Name { get; set; }
        public List<NameIPPair> Contacts { get; private set; }
        public string Path { get; set; }
        string path;

        public DataLoader()
        {
            if (!Directory.Exists(System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Octo Sender")))
                Directory.CreateDirectory(System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Octo Sender"));

            path = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Octo Sender", "settings.dat");
            Contacts = new List<NameIPPair>();
        }

        public void SaveData()
        {
            XmlDocument document = new XmlDocument();

            XmlDeclaration deklarace = document.CreateXmlDeclaration("1.0", "utf-8", null);
            document.AppendChild(deklarace);

            XmlElement main = document.CreateElement("OctoSender");

            XmlElement settings = document.CreateElement("Settings");
            XmlElement contacts = document.CreateElement("Contacts");

            settings.SetAttribute("Name", Name);
            settings.SetAttribute("Path", Path);

            main.AppendChild(settings);

            foreach(NameIPPair contactPair in Contacts)
            {
                XmlElement contact = document.CreateElement("Contact");
                contact.SetAttribute("Name", contactPair.Name);
                contact.SetAttribute("IP", contactPair.IP.ToString());
                contacts.AppendChild(contact);
            }
            main.AppendChild(contacts);

            document.AppendChild(main);

            document.Save(path);
        }

        public bool ReadData()
        {
            try
            {
                XmlDocument document = new XmlDocument();
                document.Load(path);

                XmlElement main = (XmlElement)document.GetElementsByTagName("OctoSender")[0];

                XmlElement settings = (XmlElement)main.GetElementsByTagName("Settings")[0];
                XmlElement contacts = (XmlElement)main.GetElementsByTagName("Contacts")[0];

                Name = settings.GetAttribute("Name");
                Path = settings.GetAttribute("Path");

                foreach(XmlNode node in contacts.ChildNodes)
                {
                    XmlElement element = node as XmlElement;
                    Contacts.Add(new NameIPPair(element.GetAttribute("Name"), IPAddress.Parse(element.GetAttribute("IP"))));
                }

                return true;
            }
            catch(Exception e)
            {
                Console.WriteLine(e.Message);
                return false;
            }
        }
    }
}
