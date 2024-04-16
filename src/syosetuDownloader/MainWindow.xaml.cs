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
using System.Threading.Tasks;
using System.Threading;
using WindowsInput;

namespace syosetuDownloader
{
    public static class Tools
    {
        public static void ThreadMessageBoxError(string message)
        {
            Application.Current.Dispatcher.InvokeAsync(new Action(() =>
            {
                MessageBox.Show(message, "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
            }));
        }
    }

    public static class DlOptions
    {
        private static readonly object obj = new object();

        private static string _exe_dir = "";
        private static string _dl_dir = "";
        private static string _format = "";
        private static Syousetsu.Constants.FileType _file_type = Syousetsu.Constants.FileType.HTML;
        private static bool _generate_toc = true;

        private static string _link = "";
        private static string _start = "";
        private static string _end = "";

        public static string ExeDir { get { lock (obj) { return _exe_dir; } } set { lock (obj) { _exe_dir = value; } } }
        public static string DlDir { get { lock (obj) { return _dl_dir; } } set { lock (obj) { _dl_dir = value; } } }
        public static string Format { get { lock (obj) { return _format; } } set { lock (obj) { _format = value; } } }
        public static Syousetsu.Constants.FileType FileType { get { lock (obj) { return _file_type; } } set { lock (obj) { _file_type = value; } } }
        public static bool GenerateTOC { get { lock (obj) { return _generate_toc; } } set { lock (obj) { _generate_toc = value; } } }

        public static string Link { get { lock (obj) { return _link; } } set { lock (obj) { _link = value; } } }
        public static string Start { get { lock (obj) { return _start; } } set { lock (obj) { _start = value; } } }
        public static string End { get { lock (obj) { return _end; } } set { lock (obj) { _end = value; } } }
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        int _row = 0;
        List<Syousetsu.Controls> _controls = new List<Syousetsu.Controls>();
        static readonly Random _random = new Random((int)DateTime.Now.Ticks & 0x0000FFFF);
        static readonly InputSimulator _sim = new InputSimulator();

        readonly string _version = "2.4.0 plus 24";
        readonly Shell32.Shell _shell;

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

            _shell = new Shell32.Shell();
            DlOptions.ExeDir = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
            DlOptions.DlDir = DlOptions.ExeDir;
            DlOptions.Format = GetFilenameFormat();

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
            Shell32.Folder shortcut_folder = _shell.NameSpace(DlOptions.DlDir);

            string[] fileEntries = Directory.GetFiles(DlOptions.DlDir);
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
                string input = File.ReadAllText(Path.Combine(DlOptions.ExeDir, "website.txt"));
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

        public string GetFilenameFormat()
        {
            string format = "";
            using (System.IO.StreamReader sr = new System.IO.StreamReader("format.ini"))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    if (!line.StartsWith(";"))
                    {
                        format = line;
                        break;
                    }
                }
            }
            return format;
        }

        public class BatchBackgroundQueue
        {
            private Task previousTask = Task.FromResult(true);
            private object key = new object();

            private readonly MainWindow _window;
            private int _count = 0;
            public int Count
            {
                get { return _count; }
                set
                {
                    _count = value;
                    _window.Dispatcher.Invoke(() => _window.btnQueue.Content = $"Queue {Count}");
                }
            }

            public BatchBackgroundQueue(MainWindow window) { _window = window; }

            public Task QueueTask(Action action, CancellationToken ct)
            {
                lock (key)
                {
                    Count++;
                    previousTask = previousTask.ContinueWith(
                      t => { if (!ct.IsCancellationRequested) action(); Count--; },
                      ct,
                      TaskContinuationOptions.None,
                      TaskScheduler.Default);
                    return previousTask;
                }
            }

            public Task<T> QueueTask<T>(Func<T> work)
            {
                lock (key)
                {
                    var task = previousTask.ContinueWith(
                      t => work(),
                      CancellationToken.None,
                      TaskContinuationOptions.None,
                      TaskScheduler.Default);
                    previousTask = task;
                    return task;
                }
            }
        }

        private void btnQueue_Click(object sender, RoutedEventArgs e)
        {
            btnQueue.IsEnabled = false;
            Syousetsu.Methods._batchCancel.Cancel();
        }

        public async Task BatchDownloadAsync(List<Syousetsu.History.Item> items)
        {
            btnQueue.IsEnabled = true;
            btnQueue.Content = "Queue";
            btnQueue.Visibility = Visibility.Visible;
            btnHistory.IsEnabled = false;

            Syousetsu.Methods._batchCancel = new CancellationTokenSource();
            var queue = new BatchBackgroundQueue(this);
            int count = 0;
            foreach (var item in items)
            {
                if (item.Finished || item.Downloaded >= item.Total) continue;
                count++;
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                queue.QueueTask(() =>
                {
                    Syousetsu.Methods._dlJobEvent.Reset();
                    DownloadBegin(item.Link, item.Downloaded.ToString(), "");
                    Syousetsu.Methods._dlJobEvent.WaitOne();

                    Thread.Sleep(_random.Next(100, 1001));
                    //System.Media.SystemSounds.Beep.Play();
                }, Syousetsu.Methods._batchCancel.Token);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            }
            _ = await queue.QueueTask(() => { return 0; });

            if (count == 0) MessageBox.Show("Nothing to download.\nCheck for updates first.", this.Title, MessageBoxButton.OK, MessageBoxImage.Information);
            btnQueue.Visibility = Visibility.Hidden;
            btnHistory.IsEnabled = true;
            FocusHistoryButton();
        }

        private void btnDownload_Click(object sender, RoutedEventArgs e)
        {
            FocusHistoryButton();

            string link = txtLink.Text;
            string from = txtFrom.Text;
            string to = txtTo.Text;
            Task.Run(() => DownloadBegin(link, from, to));
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
        private void DownloadBegin(string link, string start, string end)
        {
            DlOptions.Link = link;
            DlOptions.Start = start;
            DlOptions.End = end;

            this.Dispatcher.Invoke(() =>
            {
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
                _controls.Add(new Syousetsu.Controls { ID = _row, Label = lb, ProgressBar = pb, TextBlock = tb, Separator = s });

                stackPanel1.Children.Add(lb);
                stackPanel1.Children.Add(pb);
                stackPanel1.Children.Add(tb);
                stackPanel1.Children.Add(s);
                scrollViewer1.ScrollToLeftEnd();
                scrollViewer1.ScrollToBottom();
                scrollViewer1.Dispatcher.Invoke(DispatcherPriority.Render, EmptyDelegate);
            });

            try { Download(); }
            catch (Exception ex) { Syousetsu.Methods.Error(ex); }
        }

        private void Download()
        {
            bool fromToValid = (System.Text.RegularExpressions.Regex.IsMatch(DlOptions.Start, @"^\d+$") || DlOptions.Start.Equals(String.Empty)) &&
                (System.Text.RegularExpressions.Regex.IsMatch(DlOptions.End, @"^\d+$") || DlOptions.End.Equals(String.Empty));

            if (String.IsNullOrWhiteSpace(DlOptions.Link) && fromToValid)
            {
                Tools.ThreadMessageBoxError("Error parsing link and/or chapter range.");
                return;
            }

            if (!DlOptions.Link.StartsWith("http")) { DlOptions.Link = @"http://" + DlOptions.Link; }

            if (Syousetsu.Constants.Site(DlOptions.Link) == Syousetsu.Constants.SiteType.Syousetsu) // syousetsu
                if (!DlOptions.Link.EndsWith("/")) { DlOptions.Link += "/"; }

            if (!Syousetsu.Methods.IsValidLink(DlOptions.Link))
            {
                Tools.ThreadMessageBoxError("Link is not valid.");
                return;
            }

            Syousetsu.Constants sc = new Syousetsu.Constants(DlOptions.Link, DlOptions.ExeDir, DlOptions.DlDir);
            sc.AddChapter("", ""); // start chapters from 1
            HtmlDocument toc = Syousetsu.Methods.GetFullTableOfContents(DlOptions.Link, sc);

            if (!Syousetsu.Methods.IsValid(toc, sc))
            {
                Tools.ThreadMessageBoxError("Link is not valid.");
                return;
            }

            this.Dispatcher.Invoke(() =>
            {
                // set up download progress gui-controls
                Label lb = _controls.Last().Label;
                lb.Background = Brushes.Transparent;
                lb.ToolTip = "Click to open folder";

                ProgressBar pb = _controls.Last().ProgressBar;
                pb.Maximum = string.IsNullOrWhiteSpace(DlOptions.End) ? Syousetsu.Methods.GetTotalChapters(toc, sc) : Convert.ToDouble(DlOptions.End);
                if (DlOptions.GenerateTOC) pb.ToolTip = "Getting Chapter List";

                DlOptions.Start = string.IsNullOrWhiteSpace(DlOptions.Start) ? "1" : DlOptions.Start;
                DlOptions.End = pb.Maximum.ToString();

                sc.Link = DlOptions.Link;
                sc.Start = DlOptions.Start;
                sc.End = DlOptions.End;
                sc.CurrentFileType = DlOptions.FileType;
                sc.SeriesCode = Syousetsu.Methods.GetSeriesCode(DlOptions.Link);
                sc.FilenameFormat = DlOptions.Format;

                // get novel title (also folder) from history
                string title;
                var item = new Syousetsu.History.Item();
                Syousetsu.History.LoadNovel(item, sc);
                if (!string.IsNullOrEmpty(item.Title))
                    title = item.Title;
                else
                    title = Syousetsu.Methods.FormatValidFileName(Syousetsu.Methods.GetTitle(toc, sc));
                // set title
                lb.Content = title;
                sc.SeriesTitle = title;

                Syousetsu.Methods.GetAllChapterTitles(sc, toc);
            });

            if (DlOptions.GenerateTOC)
            {
                Syousetsu.Create.GenerateTableOfContents(sc, toc);
            }

            this.Dispatcher.Invoke(() =>
            {
                Label lb = _controls.Last().Label;
                ProgressBar pb = _controls.Last().ProgressBar;
                pb.ToolTip = "Click to stop download";
                System.Threading.CancellationTokenSource ct = Syousetsu.Methods.AddDownloadJob(sc, pb, lb);
                pb.MouseDown += (snt, evt) =>
                {
                    ct.Cancel();
                    pb.ToolTip = null;
                };
                lb.MouseDown += (snt, evt) =>
                {
                    //_shell.Explore(Path.Combine(_dl_dir, sc.SeriesTitle));
                    var psi = new ProcessStartInfo
                    {
                        FileName = Path.Combine(DlOptions.DlDir, sc.SeriesTitle),
                        UseShellExecute = true
                    };
                    Process.Start(psi);
                };

                scrollViewer1.ScrollToLeftEnd();
                scrollViewer1.ScrollToBottom();
                scrollViewer1.Dispatcher.Invoke(DispatcherPriority.Render, EmptyDelegate);
            });
        }

        private void rbText_Checked(object sender, RoutedEventArgs e)
        {
            DlOptions.FileType = Syousetsu.Constants.FileType.Text;
        }

        private void rbHtml_Checked(object sender, RoutedEventArgs e)
        {
            DlOptions.FileType = Syousetsu.Constants.FileType.HTML;
        }

        private void chkList_Checked(object sender, RoutedEventArgs e)
        {
            DlOptions.GenerateTOC = (bool)chkList.IsChecked;
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            _controls.Where((c) => (int)c.ProgressBar.Tag != 0).ToList().ForEach((c) =>
            {
                stackPanel1.Children.Remove(c.Label);
                stackPanel1.Children.Remove(c.ProgressBar);
                stackPanel1.Children.Remove(c.TextBlock);
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

            btnQueue.Visibility = Visibility.Hidden;
            LoadConfig();
            PopulateNovelsURLs(txtLink);
            PopulateSiteLinks(cbSite);
            if (cbSite.Items.Count > 0) cbSite.SelectedIndex = 0;

            FocusHistoryButton();
        }

        private void FocusHistoryButton()
        {
            btnHistory.Focus();
            ShowCaret();
        }

        private void ShowCaret()
        {
            // force keyboard focus caret to appear
            _sim.Keyboard.KeyPress(WindowsInput.Native.VirtualKeyCode.MENU);
            _sim.Keyboard.KeyPress(WindowsInput.Native.VirtualKeyCode.MENU);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            SaveConfig();
        }

        private void btnExplore_Click(object sender, RoutedEventArgs e)
        {
            //_shell.Explore(_dl_dir);
            var psi = new ProcessStartInfo
            {
                FileName = DlOptions.DlDir,
                UseShellExecute = true
            };
            Process.Start(psi);
        }

        private void btnHistory_Click(object sender, RoutedEventArgs e)
        {
            HistoryWindow win = new HistoryWindow();
            win.DownloadFolder = DlOptions.DlDir;
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
            string file = Path.Combine(DlOptions.ExeDir, "config.xml");
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
                DlOptions.DlDir = elem.Attribute("dlfolder").Value;
            }
            catch { }
        }

        public void SaveConfig()
        {
            string file = Path.Combine(DlOptions.ExeDir, "config.xml");

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
            attr.Value = DlOptions.DlDir;
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
            dialog.DefaultDirectory = DlOptions.DlDir;
            dialog.InitialDirectory = DlOptions.DlDir;
            CommonFileDialogResult result = dialog.ShowDialog();
            if (result == CommonFileDialogResult.Ok)
            {
                DlOptions.DlDir = dialog.FileName;
                SaveConfig();
            }
        }

    }
}
