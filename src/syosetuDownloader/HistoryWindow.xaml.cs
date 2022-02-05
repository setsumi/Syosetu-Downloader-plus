﻿using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Xml;
using System.Xml.Linq;

namespace syosetuDownloader
{
    /// <summary>
    /// Interaction logic for HistoryWindow.xaml
    /// </summary>
    public partial class HistoryWindow : Window
    {
        MainWindow _parent;

        bool _updating = false;
        public bool Updating
        {
            get
            {
                return _updating;
            }
            set
            {
                if (_updating != value)
                {
                    _updating = value;
                    CommandManager.InvalidateRequerySuggested();

                    if (!_updating) // finished updating data now refresh listview
                    {
                        this.Title = "History";
                        ICollectionView view = CollectionViewSource.GetDefaultView(viewHistoryList.ItemsSource);
                        view.Refresh();
                        // restore focus
                        FocusListView();
                    }
                    else
                    {
                        this.Title = "History - Updating...";
                    }
                }
            }
        }

        public HistoryWindow()
        {
            InitializeComponent();

            // my init
            _parent = (MainWindow)Application.Current.MainWindow;
            Syousetsu.History history = new Syousetsu.History();
            history.LoadAll();
            viewHistoryList.ItemsSource = history.Items;
            // the rest in Window_Loaded()
        }

        private void FocusListView(int index = -1)
        {
            // focus list view
            Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(() =>
            {
                ListViewItem item = viewHistoryList.ItemContainerGenerator.ContainerFromIndex(index == -1 ? viewHistoryList.SelectedIndex : index) as ListViewItem;
                item?.Focus();
                //Keyboard.Focus(item);
            }));
        }

        private Syousetsu.History.Item GetCurrentItem()
        {
            return (Syousetsu.History.Item)viewHistoryList?.SelectedItem;
        }

        private void FavoriteCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = !_updating && (GetCurrentItem() != null);
        }

        private void FavoriteCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var item = GetCurrentItem();
            item.Favorite = !item.Favorite;
            Syousetsu.History.SaveItem(item);
        }

        private void UpdateCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Updating = true;

            var item = GetCurrentItem();
            Syousetsu.Constants sc = new Syousetsu.Constants(item.Link, null);
            HtmlDocument toc = Syousetsu.Methods.GetTableOfContents(item.Link, sc);
            if (!Syousetsu.Methods.IsValid(toc, sc))
            {
                MessageBox.Show("Link not valid!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                goto updateCommand_end;
            }
            item.Total = Syousetsu.Methods.GetTotalChapters(toc, sc);
            Syousetsu.History.SaveItem(item);

        updateCommand_end:
            Updating = false;
        }

        private void RemoveCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var item = GetCurrentItem();
            (viewHistoryList.ItemsSource as ObservableCollection<Syousetsu.History.Item>).Remove(item);
            Syousetsu.History.DeleteItemFile(item);
        }

        private void SelectCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var item = GetCurrentItem();
            //MainWindow parent = (MainWindow)Application.Current.MainWindow;
            _parent.txtLink.Text = item.Link;
            int from = item.Downloaded;
            _parent.txtFrom.Text = (from > item.Total ? item.Total : from).ToString();
            _parent.txtTo.Text = "";
            //Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(() => _parent.btnDownload.Focus()));
            this.Close();
        }

        private void WebCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            new Shell32.Shell().Open(GetCurrentItem().Link);
            //Util.GridViewSort.ApplySort(viewHistoryList.Items, "Date", viewHistoryList,
            //    Util.GridViewTool.GetHeaderColimn(viewHistoryList, "Last DL"));
            //Util.GridViewTool.SortInfo si = Util.GridViewTool.GetSort(viewHistoryList);
        }

        private void CloseCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            this.Close();
        }

        private void OnPreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Point point = Mouse.GetPosition(viewHistoryList);
            // click was on header, not on cell
            if (point.Y < 2 + Util.GridViewTool.GetHeaderHeight(viewHistoryList, 0)) return;

            int col = 0;
            double accumulatedWidth = 0.0;

            // calc col mouse was over
            foreach (var columnDefinition in viewHistoryGrid.Columns)
            {
                accumulatedWidth += columnDefinition.ActualWidth;
                if (accumulatedWidth >= point.X)
                    break;
                col++;
            }

            if (col == 0) // favorite
            {
                if (GetCurrentItem() != null)
                {
                    FavoriteCommand_Executed(null, null);
                }
            }
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            _parent.sortInfo = Util.GridViewTool.GetSort(viewHistoryList);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Util.GridViewTool.SetSort(viewHistoryList, _parent.sortInfo);
            FocusListView();
        }
    }
    // End of HistoryWindow ========================

    public static class CustomCommands
    {
        public static readonly RoutedUICommand Favorite = new RoutedUICommand
            (
                "Favorite",
                "Favorite",
                typeof(CustomCommands),
                new InputGestureCollection() { new KeyGesture(Key.F2, ModifierKeys.None) }
            );

        public static readonly RoutedUICommand Update = new RoutedUICommand
            (
                "Update",
                "Update",
                typeof(CustomCommands),
                new InputGestureCollection() { new KeyGesture(Key.F5, ModifierKeys.None) }
            );

        public static readonly RoutedUICommand Remove = new RoutedUICommand
            (
                "Remove",
                "Remove",
                typeof(CustomCommands),
                new InputGestureCollection() { new KeyGesture(Key.F8, ModifierKeys.None) }
            );
        public static readonly RoutedUICommand Select = new RoutedUICommand
            (
                "Select",
                "Select",
                typeof(CustomCommands),
                new InputGestureCollection() { new KeyGesture(Key.Enter, ModifierKeys.None) }
            );
        public static readonly RoutedUICommand Web = new RoutedUICommand
            (
                "Web",
                "Web",
                typeof(CustomCommands),
                new InputGestureCollection() { new KeyGesture(Key.Enter, ModifierKeys.Control) }
            );
        public static readonly RoutedUICommand Close = new RoutedUICommand
            (
                "Close",
                "Close",
                typeof(CustomCommands),
                new InputGestureCollection() { new KeyGesture(Key.Escape, ModifierKeys.None) }
            );
    }

    public class FavoriteBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter,
                System.Globalization.CultureInfo culture)
        {
            if (value is bool)
            {
                if ((bool)value == true)
                    return "🖤";
                else
                    return " ";
            }
            return " ";
        }
        // unused
        public object ConvertBack(object value, Type targetType, object parameter,
                System.Globalization.CultureInfo culture)
        {
            switch (value.ToString())
            {
                case "🖤":
                    return true;
                case " ":
                    return false;
            }
            return false;
        }
    }

    public class CategoryHighlightStyleSelector : StyleSelector
    {
        public Style DefaultChaptersClassStyle { get; set; }
        public Style NewChaptersClassStyle { get; set; }
        public Style InvalidChaptersClassStyle { get; set; }

        public override Style SelectStyle(object itm, DependencyObject container)
        {
            Syousetsu.History.Item item = (Syousetsu.History.Item)itm;
            if (item.Downloaded < item.Total)
                return NewChaptersClassStyle;
            else if (item.Downloaded > item.Total)
                return InvalidChaptersClassStyle;
            else
                return DefaultChaptersClassStyle;
        }

        //public override Style SelectStyle(object item, DependencyObject container)
        //{
        //    Style st = new Style();
        //    st.TargetType = typeof(ListViewItem);
        //    Setter foregroundSetter = new Setter();
        //    foregroundSetter.Property = ListViewItem.ForegroundProperty;
        //    ListView listView = ItemsControl.ItemsControlFromItemContainer(container) as ListView;
        //    //int index = listView.ItemContainerGenerator.IndexFromContainer(container);
        //    //if (index % 2 == 0)
        //    //{
        //    //    foregroundSetter.Value = Brushes.LightBlue;
        //    //}
        //    //else
        //    //{
        //    //    foregroundSetter.Value = Brushes.Beige;
        //    //}
        //    var i = listView.ItemContainerGenerator.ItemFromContainer(container) as Syousetsu.History.Item;
        //    //Syousetsu.History.Item i = (Syousetsu.History.Item)item;
        //    if (i.Downloaded < i.Total)
        //        foregroundSetter.Value = Brushes.Green;
        //    else if (i.Downloaded > i.Total)
        //        foregroundSetter.Value = Brushes.Red;
        //    else
        //        return st;
        //    st.Setters.Add(foregroundSetter);
        //    return st;
        //}
    }
}