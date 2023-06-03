using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Net;
using HtmlAgilityPack;
using System.IO;
using System.Diagnostics;
using System.Windows.Threading;
using System.Xml.Linq;
using System.Xml;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace syosetuDownloader
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        int _row = 0;
        string _link = String.Empty;
        string _start = String.Empty;
        string _end = String.Empty;
        string _format = String.Empty;
        Syousetsu.Constants.FileType _fileType;
        List<Syousetsu.Controls> _controls = new List<Syousetsu.Controls>();

        Shell32.Shell _shell;
        string _exe_dir;
        string _dl_dir;
        readonly string _version = "2.4.0 plus 19";

        public Util.GridViewTool.SortInfo sortInfo = new Util.GridViewTool.SortInfo();

        public class NovelDrop
        {
            public string Novel { get; set; }
            public string Link { get; set; }
            public override string ToString() { return Link; }
        }
        public class SiteLink
        {
            public string Name { get; set; }
            public string Link { get; set; }
            public override string ToString() { return Name + "\t" + Link; }
        }

        public MainWindow()
        {
            InitializeComponent();

            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls
                | SecurityProtocolType.Ssl3
                | (SecurityProtocolType)768
                | (SecurityProtocolType)3072
                | (SecurityProtocolType)12288;

            _exe_dir = System.IO.Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
            _dl_dir = _exe_dir;
            _shell = new Shell32.Shell();

            this.Title += _version;
            txtLink.ToolTip = "(Alt+↓)  dropdown list, populated from (.url) web shortcuts"
                + Environment.NewLine + "(Ctrl+Enter)  open url in browser";
            cbSite.ToolTip = "(Alt+↓)  dropdown list, populated from website.txt"
                + Environment.NewLine + "(Ctrl+Enter)  open site in browser";
        }

        void PopulateNovelsURLs(ComboBox cb)
        {
            List<NovelDrop> items = new List<NovelDrop>();

            // Get the shortcut's folder.
            Shell32.Folder shortcut_folder = _shell.NameSpace(_dl_dir);

            string[] fileEntries = Directory.GetFiles(_dl_dir);
            foreach (string file in fileEntries)
            {
                if (Path.GetExtension(file).Equals(".url", StringComparison.OrdinalIgnoreCase))
                {
                    //Get the shortcut's file.
                    Shell32.FolderItem folder_item =
                        shortcut_folder.Items().Item(Path.GetFileName(file));
                    // Get shortcut's information.
                    Shell32.ShellLinkObject lnk = (Shell32.ShellLinkObject)folder_item.GetLink;
                    items.Add(new NovelDrop() { Novel = folder_item.Name, Link = lnk.Path });
                }
            }
            // Assign data to combobox
            cb.ItemsSource = items;
        }
        void PopulateSiteLinks(ComboBox cb)
        {
            List<SiteLink> items = new List<SiteLink>();
            try
            {
                int odd = 1;
                SiteLink link = null;
                string input = File.ReadAllText(_exe_dir + "\\website.txt");
                StringReader reader = new StringReader(input);
                string line = string.Empty;
                do
                {
                    line = reader.ReadLine();
                    if (line != null) // do something with the line
                    {
                        if (odd++ % 2 != 0)
                        {
                            link = new SiteLink { Name = line };
                        }
                        else
                        {
                            link.Link = line;
                            items.Add(link);
                        }
                    }
                } while (line != null);
            }
            catch { };
            cb.ItemsSource = items;
        }

        public void GetFilenameFormat()
        {
            using (System.IO.StreamReader sr = new System.IO.StreamReader("format.ini"))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    if (!line.StartsWith(";"))
                    {
                        _format = line;
                        break;
                    }
                }
            }
        }

        private class ProgressMultiValueConverter : System.Windows.Data.IMultiValueConverter
        {
            public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            {
                if ((double)values[0] == 0) values[0] = "";
                return $"{values[0]} {values[1]}";
            }

            public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }
        private static Action EmptyDelegate = delegate () { };
        private void btnDownload_Click(object sender, RoutedEventArgs e)
        {
            btnHistory.Focus();

            var taskbar = Microsoft.WindowsAPICodePack.Taskbar.TaskbarManager.Instance;
            taskbar.SetProgressState(Microsoft.WindowsAPICodePack.Taskbar.TaskbarProgressBarState.Indeterminate);

            Label lb = new Label();
            lb.Content = "Preparing...";
            lb.Background = Brushes.Gold;

            ProgressBar pb = new ProgressBar();
            pb.Height = 15;
            pb.Tag = 0;

            TextBlock tb = new TextBlock();
            var binding = new System.Windows.Data.MultiBinding();
            binding.Bindings.Add(new System.Windows.Data.Binding("Value") { Source = pb });
            binding.Bindings.Add(new System.Windows.Data.Binding("ToolTip") { Source = pb });
            binding.Converter = new ProgressMultiValueConverter();
            tb.SetBinding(TextBlock.TextProperty, binding);
            tb.HorizontalAlignment = HorizontalAlignment.Center;
            tb.VerticalAlignment = VerticalAlignment.Center;
            tb.Margin = new Thickness(0, -pb.Height, 0, 0);
            tb.IsHitTestVisible = false;

            Separator s = new Separator();
            s.Height = 5;

            _row += 1;
            _controls.Add(new Syousetsu.Controls { ID = _row, Label = lb, ProgressBar = pb, Separator = s });

            stackPanel1.Children.Add(lb);
            stackPanel1.Children.Add(pb);
            stackPanel1.Children.Add(tb);
            stackPanel1.Children.Add(s);
            scrollViewer1.ScrollToLeftEnd();
            scrollViewer1.ScrollToBottom();
            scrollViewer1.Dispatcher.Invoke(DispatcherPriority.Render, EmptyDelegate);

            Application.Current.Dispatcher.InvokeAsync(() =>
            {
                try { Download(); }
                catch (Exception ex) { Syousetsu.Methods.Error(ex); }
            }, DispatcherPriority.Background);
        }
        private void Download()
        {
            this._link = txtLink.Text;
            this._start = txtFrom.Text;
            this._end = txtTo.Text;

            bool fromToValid = (System.Text.RegularExpressions.Regex.IsMatch(_start, @"^\d+$") || _start.Equals(String.Empty)) &&
                (System.Text.RegularExpressions.Regex.IsMatch(_end, @"^\d+$") || _end.Equals(String.Empty));

            if (String.IsNullOrWhiteSpace(_link) && fromToValid)
            {
                MessageBox.Show("Error parsing link and/or chapter range.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (!_link.StartsWith("http")) { _link = @"http://" + _link; }

            if (Syousetsu.Constants.Site(_link) == Syousetsu.Constants.SiteType.Syousetsu) // syousetsu
                if (!_link.EndsWith("/")) { _link += "/"; }

            if (!Syousetsu.Methods.IsValidLink(_link))
            {
                MessageBox.Show("Link is not valid.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            Syousetsu.Constants sc = new Syousetsu.Constants(_link, _exe_dir, _dl_dir);
            sc.AddChapter("", ""); // start chapters from 1
            HtmlDocument toc = Syousetsu.Methods.GetTableOfContents(_link, sc);

            if (!Syousetsu.Methods.IsValid(toc, sc))
            {
                MessageBox.Show("Link is not valid.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }


            GetFilenameFormat();

            // set up download progress gui-controls
            Label lb = _controls.Last().Label;
            lb.Content = Syousetsu.Methods.FormatValidFileName(Syousetsu.Methods.GetTitle(toc, sc));
            lb.Background = Brushes.Transparent;
            lb.ToolTip = "Click to open folder";

            ProgressBar pb = _controls.Last().ProgressBar;
            pb.Maximum = (_end == String.Empty) ? Syousetsu.Methods.GetTotalChapters(toc, sc) : Convert.ToDouble(_end);
            pb.ToolTip = "Click to stop download";

            _start = (_start == String.Empty) ? "1" : _start;
            _end = pb.Maximum.ToString();

            sc.SeriesTitle = lb.Content.ToString();
            sc.Link = _link;
            sc.Start = _start;
            sc.End = _end;
            sc.CurrentFileType = _fileType;
            sc.SeriesCode = Syousetsu.Methods.GetSeriesCode(_link);
            sc.FilenameFormat = _format;
            Syousetsu.Methods.GetAllChapterTitles(sc, toc);

            if (chkList.IsChecked == true)
            {
                Syousetsu.Create.GenerateTableOfContents(sc, toc);
            }

            System.Threading.CancellationTokenSource ct = Syousetsu.Methods.AddDownloadJob(sc, pb, lb);
            pb.MouseDown += (snt, evt) =>
            {
                ct.Cancel();
                pb.ToolTip = null;
            };
            lb.MouseDown += (snt, evt) =>
            {
                _shell.Explore(Path.Combine(_dl_dir, sc.SeriesTitle));
            };

            scrollViewer1.ScrollToLeftEnd();
            scrollViewer1.ScrollToBottom();
            scrollViewer1.Dispatcher.Invoke(DispatcherPriority.Render, EmptyDelegate);
        }

        private void rbText_Checked(object sender, RoutedEventArgs e)
        {
            _fileType = Syousetsu.Constants.FileType.Text;
        }

        private void rbHtml_Checked(object sender, RoutedEventArgs e)
        {
            _fileType = Syousetsu.Constants.FileType.HTML;
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            _controls.Where((c) => (int)c.ProgressBar.Tag != 0).ToList().ForEach((c) =>
            {
                stackPanel1.Children.Remove(c.Label);
                stackPanel1.Children.Remove(c.ProgressBar);
                stackPanel1.Children.Remove(c.Separator);
            });

            _controls = (from c in _controls
                         where (int)c.ProgressBar.Tag == 0
                         select c).ToList();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Focus editable combobox
            //var textBox = (txtLink.Template.FindName("PART_EditableTextBox", txtLink) as TextBox);
            //if (textBox != null)
            //{
            //    textBox.Focus();
            //    textBox.SelectionStart = textBox.Text.Length;
            //}

            LoadConfig();
            PopulateNovelsURLs(txtLink);
            PopulateSiteLinks(cbSite);
            if (cbSite.Items.Count > 0) cbSite.SelectedIndex = 0;

            // focus history button
            btnHistory.Focus();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            SaveConfig();
        }

        private void btnExplore_Click(object sender, RoutedEventArgs e)
        {
            _shell.Explore(_dl_dir);
        }

        private void btnHistory_Click(object sender, RoutedEventArgs e)
        {
            HistoryWindow win = new HistoryWindow();
            win.ShowDialog();
        }

        private void txtLink_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            ComboBox combobox = sender as ComboBox;
            // Open novel link in browser
            if (e.Key == Key.Enter && e.KeyboardDevice.Modifiers == ModifierKeys.Control)
            {
                if (!String.IsNullOrEmpty(combobox.Text) && combobox.Text.Length > 8 &&
                    combobox.Text.Substring(0, 8).Equals("https://", StringComparison.InvariantCultureIgnoreCase)
                    )
                {
                    _shell.Open(combobox.Text);
                }
            }
        }

        private void cbSite_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            ComboBox combobox = sender as ComboBox;
            // Open site in browser
            if (e.Key == Key.Enter && e.KeyboardDevice.Modifiers == ModifierKeys.Control)
            {
                if (combobox.SelectedIndex != -1)
                {
                    _shell.Open(((SiteLink)combobox.SelectedItem).Link);
                }
            }
        }

        public void LoadConfig()
        {
            string file = _exe_dir + Path.DirectorySeparatorChar + "config.xml";
            if (!File.Exists(file)) return;

            XElement fileElem = XElement.Load(file);

            XElement elem = fileElem.Element("historySort");
            Enum.TryParse(elem.Attribute("direction").Value, out System.ComponentModel.ListSortDirection dir);
            sortInfo.Direction = dir;
            sortInfo.PropertyName = elem.Attribute("propertyName").Value;
            sortInfo.ColumnName = elem.Attribute("columnName").Value;
            try // newly added stuff
            {
                elem = fileElem.Element("config");
                _dl_dir = elem.Attribute("dlfolder").Value;
            }
            catch { }
        }

        public void SaveConfig()
        {
            string file = _exe_dir + Path.DirectorySeparatorChar + "config.xml";

            XmlDocument doc = new XmlDocument();
            XmlNode docNode = doc.CreateXmlDeclaration("1.0", "UTF-8", null);
            doc.AppendChild(docNode);

            XmlNode rootNode = doc.CreateElement("root");
            doc.AppendChild(rootNode);

            XmlNode node = doc.CreateElement("historySort");

            XmlAttribute attr = doc.CreateAttribute("direction");
            attr.Value = sortInfo.Direction.ToString();
            node.Attributes.Append(attr);

            attr = doc.CreateAttribute("propertyName");
            attr.Value = sortInfo.PropertyName;
            node.Attributes.Append(attr);

            attr = doc.CreateAttribute("columnName");
            attr.Value = sortInfo.ColumnName;
            node.Attributes.Append(attr);

            rootNode.AppendChild(node);

            node = doc.CreateElement("config");

            attr = doc.CreateAttribute("dlfolder");
            attr.Value = _dl_dir;
            node.Attributes.Append(attr);

            rootNode.AppendChild(node);

            doc.Save(file);
        }

        private void DownloadFolderDropdownButton_Click(object sender, RoutedEventArgs e)
        {
            var addButton = sender as FrameworkElement;
            if (addButton != null)
            {
                addButton.ContextMenu.IsOpen = true;
            }
        }
        private void DownloadFolderMenuItem_GotFocus(object sender, RoutedEventArgs e)
        {
            // focus history button
            btnHistory.Focus();

            //TraversalRequest request = new TraversalRequest(FocusNavigationDirection.Next);
            //MoveFocus(request);
        }

        private void DownloadFolderMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new CommonOpenFileDialog();
            dialog.IsFolderPicker = true;
            dialog.EnsurePathExists = true;
            dialog.Multiselect = false;
            dialog.DefaultDirectory = _dl_dir;
            dialog.InitialDirectory = _dl_dir;
            CommonFileDialogResult result = dialog.ShowDialog();
            if (result == CommonFileDialogResult.Ok)
            {
                _dl_dir = dialog.FileName;
                SaveConfig();
            }
        }

    }
}
