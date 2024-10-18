using System;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Net;
using System.IO;
using System.Windows;

namespace Syousetsu
{
    public class Controls
    {
        public int ID { get; set; }
        public Label Label { get; set; }
        public ProgressBar ProgressBar { get; set; }
        public TextBlock TextBlock { get; set; }
        public Separator Separator { get; set; }
    }

    public class Constants
    {
        public class Chapter
        {
            public string title;
            public string number;
            public int index;
            public Chapter(string title, string number, int index)
            {
                this.title = title;
                this.number = number;
                this.index = index;
            }
        }

        public enum FileType { Text, HTML };
        public enum SiteType { Syousetsu, Kakuyomu };
        CookieContainer _cookies = new CookieContainer();
        string _title = String.Empty;
        string _start = String.Empty;
        string _end = String.Empty;
        FileType _fileType;
        string _path = String.Empty;
        string _link = String.Empty;
        string _seriesCode = String.Empty;
        string _fileNameFormat = String.Empty;
        List<Chapter> _chapters = new List<Chapter>();
        const string userAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/129.0.0.0 Safari/537.36";
        List<string> _userAgentList = new List<string>();
        int _lastDownloaded = 0;

        private static int _net_timeout = 5000;
        private static int _net_retry_count = 5;

        // Constructor
        public Constants(string link, string exedir, string dldir)
        {
            _link = link;
            _path = dldir;

            // load user agents from file
            if (!string.IsNullOrEmpty(exedir))
            {
                try
                {
                    string input = File.ReadAllText(exedir + "\\useragent.ini");
                    StringReader reader = new StringReader(input);
                    string line = null;
                    do
                    {
                        line = reader.ReadLine();
                        if (line != null) // do something with the line
                        {
                            _userAgentList.Add(line);
                        }
                    } while (line != null);
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        public static SiteType Site(string link)
        {
            if (link.Contains("syosetu.com"))
                return SiteType.Syousetsu;
            else
                return SiteType.Kakuyomu;
        }

        public SiteType Site() => Site(_link);

        public void AddChapter(string title, string number)
        {
            _chapters.Add(new Chapter(title, number, _chapters.Count));
        }

        public Chapter GetChapterByIndex(int index)
        {
            return _chapters[index];
        }

        public Chapter GetChapterByNumber(string number)
        {
            foreach (Chapter chapter in _chapters)
            {
                if (chapter.number.Equals(number, StringComparison.Ordinal))
                    return chapter;
            }
            return null;
        }

        public CookieContainer SyousetsuCookie
        {
            get
            {
                if (Site() == SiteType.Syousetsu)
                {
                    Cookie c = new Cookie
                    {
                        Domain = ".syosetu.com",
                        Value = "yes",
                        Name = "over18"
                    };

                    _cookies.Add(c);
                }
                return _cookies;
            }
        }

        public string SeriesTitle
        {
            get { return _title; }
            set { _title = value; }
        }

        public string Start
        {
            get { return _start; }
            set { _start = value; }
        }

        public string End
        {
            get { return _end; }
            set { _end = value; }
        }


        public FileType CurrentFileType
        {
            get { return _fileType; }
            set { _fileType = value; }
        }

        public string Path
        {
            get { return _path; }
        }

        public string Link
        {
            get { return _link; }
            set { _link = value; }
        }

        public string SeriesCode
        {
            get { return _seriesCode; }
            set { _seriesCode = value; }
        }

        public string FilenameFormat
        {
            get { return _fileNameFormat; }
            set { _fileNameFormat = value; }
        }

        public int TotalChapters
        {
            get { return _chapters.Count - 1; }
        }

        public int LastDownloaded
        {
            get { return _lastDownloaded; }
            set { _lastDownloaded = value; }
        }

        //public List<Chapter> Chapters
        //{
        //    get { return _chapters; }
        //    //set { _chapters = value; }
        //}

        public string UserAgent
        {
            get
            {
                if (_userAgentList.Count == 0)
                    return userAgent;
                else
                {
                    Random rnd = new Random();
                    return _userAgentList[rnd.Next(0, _userAgentList.Count)];
                }
            }
        }

        public static int NetTimeout { get { return _net_timeout; } set { _net_timeout = value; } }
        public static int NetRetryCount { get { return _net_retry_count; } set { _net_retry_count = value; } }
    }
}
