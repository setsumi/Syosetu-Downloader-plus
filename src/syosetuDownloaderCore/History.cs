using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Microsoft.VisualBasic.FileIO;

namespace Syousetsu
{
    public class History
    {
        public class Item : INotifyPropertyChanged
        {
            string _link = "";
            bool _favorite = false;
            string _title = "";
            int _downloaded = 0;
            int _total = 0;
            DateTime _date = new DateTime();
            Constants.SiteType _site;
            string _code = "";
            bool _finished = false;

            public string Link { get => _link; set => _link = value; }
            public bool Favorite
            {
                get => _favorite;
                set { _favorite = value; OnPropertyChanged(nameof(Favorite)); }
            }
            public string Title { get => _title; set => _title = value; }
            public int Downloaded { get => _downloaded; set => _downloaded = value; }
            public int Total
            {
                get => _total;
                set { _total = value; OnPropertyChanged(nameof(Total)); }
            }
            public DateTime Date { get => _date; set => _date = value; }
            public Constants.SiteType Site { get => _site; set => _site = value; }
            public string Code { get => _code; set => _code = value; }
            public string New
            {
                get
                {
                    if (Downloaded < Total) return "+";
                    else if (Downloaded > Total) return "-";
                    else return "";
                }
            }
            public bool Finished
            {
                get => _finished;
                set { _finished = value; OnPropertyChanged(nameof(Finished)); }
            }

            public event PropertyChangedEventHandler PropertyChanged;

            public void OnPropertyChanged(string propName)
            {
                if (this.PropertyChanged != null)
                    this.PropertyChanged(this, new PropertyChangedEventArgs(propName));
            }
        }

        private static readonly string _folder = System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) +
            System.IO.Path.DirectorySeparatorChar + "_HISTORY_" + System.IO.Path.DirectorySeparatorChar;
        private ObservableCollection<Item> _items = new ObservableCollection<Item>();

        public History()
        {
            // check directory
            if (!System.IO.Directory.Exists(_folder))
            {
                System.IO.Directory.CreateDirectory(_folder);
            }
        }

        public static void SaveItem(Item item)
        {
            SaveFile(item, _folder + item.Code + ".xml");
        }

        public static void DeleteItemFile(Item item)
        {
            string file = _folder + item.Code + ".xml";
            if (System.IO.File.Exists(file))
            {
                FileSystem.DeleteFile(file, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
            }
        }

        public static void SaveNovel(Constants details)
        {
            string file = _folder + details.SeriesCode + ".xml";
            Item item = new Item();
            LoadFile(item, file); // load last data
            item.Link = details.Link; // update with new data
            item.Title = details.SeriesTitle;
            item.Downloaded = details.LastDownloaded;
            item.Total = details.TotalChapters;
            item.Date = DateTime.Now;
            item.Site = details.Site();
            item.Code = details.SeriesCode;
            SaveFile(item, file);
        }

        public static void LoadNovel(Item item, Constants details)
        {
            string file = _folder + details.SeriesCode + ".xml";
            LoadFile(item, file); // load last data
        }

        public void LoadAll()
        {
            _items.Clear();
            string[] fileEntries = System.IO.Directory.GetFiles(_folder);
            foreach (string file in fileEntries)
            {
                if (System.IO.Path.GetExtension(file).Equals(".xml", StringComparison.OrdinalIgnoreCase))
                {
                    Item it = new Item();
                    LoadFile(it, file);
                    _items.Add(it);
                }
            }
        }

        public static void LoadFile(Item item, string file)
        {
            if (!System.IO.File.Exists(file)) return;

            XElement elem = XElement.Load(file).Element("item");
            item.Link = elem.Attribute("link").Value;
            item.Favorite = bool.Parse(elem.Attribute("favorite").Value);
            item.Title = elem.Attribute("title").Value;
            item.Downloaded = int.Parse(elem.Attribute("downloaded").Value);
            item.Total = int.Parse(elem.Attribute("total").Value);
            item.Date = DateTime.Parse(elem.Attribute("date").Value);
            Enum.TryParse(elem.Attribute("site").Value, out Constants.SiteType site);
            item.Site = site;
            item.Code = elem.Attribute("code").Value;
            try // newly added stuff
            {
                item.Finished = bool.Parse(elem.Attribute("finished").Value);
            }
            catch { }
        }

        public static void SaveFile(Item item, string file)
        {
            XmlDocument doc = new XmlDocument();
            XmlNode docNode = doc.CreateXmlDeclaration("1.0", "UTF-8", null);
            doc.AppendChild(docNode);

            XmlNode rootNode = doc.CreateElement("root");
            doc.AppendChild(rootNode);

            XmlNode node = doc.CreateElement("item");

            XmlAttribute attr = doc.CreateAttribute("link");
            attr.Value = item.Link;
            node.Attributes.Append(attr);

            attr = doc.CreateAttribute("favorite");
            attr.Value = item.Favorite.ToString();
            node.Attributes.Append(attr);

            attr = doc.CreateAttribute("title");
            attr.Value = item.Title;
            node.Attributes.Append(attr);

            attr = doc.CreateAttribute("downloaded");
            attr.Value = item.Downloaded.ToString();
            node.Attributes.Append(attr);

            attr = doc.CreateAttribute("total");
            attr.Value = item.Total.ToString();
            node.Attributes.Append(attr);

            attr = doc.CreateAttribute("date");
            attr.Value = item.Date.ToString();
            node.Attributes.Append(attr);

            attr = doc.CreateAttribute("site");
            attr.Value = item.Site.ToString();
            node.Attributes.Append(attr);

            attr = doc.CreateAttribute("code");
            attr.Value = item.Code;
            node.Attributes.Append(attr);

            attr = doc.CreateAttribute("finished");
            attr.Value = item.Finished.ToString();
            node.Attributes.Append(attr);

            rootNode.AppendChild(node);
            doc.Save(file);
        }

        public ObservableCollection<Item> Items
        {
            get { return _items; }
        }
    }
}
