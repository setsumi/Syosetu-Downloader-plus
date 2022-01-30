using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Threading;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
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
        const string userAgent = "Mozilla/5.0 (X11; Linux i586; rv:31.0) Gecko/20100101 Firefox/70.0";
        List<string> _userAgentList = new List<string>();

        // Constructor
        public Constants(string link, string exedir)
        {
            _link = link;

            // load user agents from file
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
            catch { };
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
            set { _path = value; }
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
    }
}
