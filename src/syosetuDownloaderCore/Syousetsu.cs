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
using System.Windows.Media;

namespace Syousetsu
{
    public class Methods
    {
        public static CancellationTokenSource AddDownloadJob(Syousetsu.Constants details, ProgressBar pb, Label lb)
        {
            int max = Convert.ToInt32(pb.Maximum);

            int i = 0;
            int upTo = -1;
            if (details.Start != String.Empty && details.End == String.Empty)//determine if user don't want to start at chapter 1
            {
                i = Convert.ToInt32(details.Start);
            }
            else if (details.Start == String.Empty && details.End != String.Empty)//determine if user wants to end at a specific chapter
            {
                i = 1;
                upTo = max;
            }
            else if (details.Start != String.Empty && details.End != String.Empty)//determine if user only wants to download a specifc range
            {
                i = Convert.ToInt32(details.Start);//get start of the range
                upTo = max;//get the end of the range
            }
            else
            {
                i = 1;//if both textbox are blank assume user wants to start from the first chapter "http://*.syosetu.com/xxxxxxx/1" until the latest/last one "http://*.syosetu.com/xxxxxxx/*"
            }

            CancellationTokenSource ct = new CancellationTokenSource();
            Task.Factory.StartNew(() =>
            {
                bool cancelled = false;
                for (int ctr = i; ctr <= max; ctr++)
                {
                    string subLink = details.Link + ctr;
                    string[] chapter = Create.GenerateContents(details, GetPage(subLink, details.SyousetsuCookie), ctr);
                    Create.SaveFile(details, chapter, ctr);

                    pb.Dispatcher.Invoke((Action)(() => { pb.Value = ctr; }));
                    if (upTo != -1 && ctr > upTo)//stop loop if the specifed range is reached
                    {
                        break;
                    }

                    if (ct.IsCancellationRequested)
                    {
                        // another thread decided to cancel
                        cancelled = true;
                        break;
                    }
                }
                pb.Dispatcher.Invoke((Action)(() =>
                {
                    //pb.Value = max;
                    pb.ToolTip = null;
                    pb.Tag = 1;
                    if (cancelled)
                    {
                        lb.Content = "download aborted - " + lb.Content;
                        //lb.Background = Brushes.MistyRose;
                    }
                    else
                    {
                        pb.Value = max;
                        lb.Content = "finished - " + lb.Content;
                        //lb.Background = Brushes.Aquamarine;
                    }

                }));
            }, ct.Token);

            return ct;
        }

        public static void AddDownloadJob(Syousetsu.Constants details)
        {
            int max = Convert.ToInt32(details.End);

            int i = 0;
            int upTo = -1;
            if (details.Start != String.Empty && details.End == String.Empty)//determine if user don't want to start at chapter 1
            {
                i = Convert.ToInt32(details.Start);
            }
            else if (details.Start == String.Empty && details.End != String.Empty)//determine if user wants to end at a specific chapter
            {
                i = 1;
                upTo = max;
            }
            else if (details.Start != String.Empty && details.End != String.Empty)//determine if user only wants to download a specifc range
            {
                i = Convert.ToInt32(details.Start);//get start of the range
                upTo = max;//get the end of the range
            }
            else
            {
                i = 1;//if both textbox are blank assume user wants to start from the first chapter "http://*.syosetu.com/xxxxxxx/1" until the latest/last one "http://*.syosetu.com/xxxxxxx/*"
            }

            for (int ctr = i; ctr <= max; ctr++)
            {
                string subLink = details.Link + ctr;
                string[] chapter = Create.GenerateContents(details, GetPage(subLink, details.SyousetsuCookie), ctr);
                Create.SaveFile(details, chapter, ctr);

                if (upTo != -1 && ctr > upTo)//stop loop if the specifed range is reached
                {
                    break;
                }
            }
        }

        public static HtmlDocument GetTableOfContents(string link, CookieContainer cookies)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(link);
            request.Method = "GET";
            request.CookieContainer = cookies;
            request.UserAgent = Constants.UserAgent;

            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            var stream = response.GetResponseStream();

            HtmlDocument doc = new HtmlDocument();
            using (StreamReader reader = new StreamReader(stream))
            {
                string html = reader.ReadToEnd();
                doc.LoadHtml(html);
            }

            return doc;
        }

        public static string GetTitle(HtmlDocument doc)
        {
            HtmlNode titleNode = doc.DocumentNode.SelectSingleNode("//p[@class='novel_title']");
            return (titleNode == null) ? "title" : titleNode.InnerText.TrimStart().TrimEnd();
        }

        public static string GetChapterTitle(HtmlDocument doc)
        {
            HtmlNode titleNode = doc.DocumentNode.SelectSingleNode("//p[@class='novel_subtitle']");
            return (titleNode == null) ? "title" : titleNode.InnerText.TrimStart().TrimEnd();
        }

        public static string GetNovelBody(HtmlDocument doc, Constants.FileType fileType)
        {
            HtmlNode novelNode = doc.DocumentNode.SelectSingleNode("//div[@id='novel_honbun']");
            HtmlNode footerNode = doc.DocumentNode.SelectSingleNode("//div[@id='novel_a']");
            if (fileType == Constants.FileType.Text)
            {
                string s = novelNode.InnerText;
                if (footerNode != null)
                {
                    s += Environment.NewLine + "=====" + Environment.NewLine;
                    s += footerNode.InnerText;
                }

                return s;
            }
            else if (fileType == Constants.FileType.HTML)
            {
                string[] s = novelNode.InnerText.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None);

                StringBuilder sb = new StringBuilder();
                foreach (String str in s)
                {
                    string temp = (str != "") ? ("<p>" + str + "</p>") : ("<p><br/></p>");
                    sb.AppendLine(temp);
                }

                if (footerNode != null)
                {
                    sb.AppendLine("<hr/>");

                    s = footerNode.InnerText.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None);
                    foreach (String str in s)
                    {
                        string temp = (str != "") ? ("<p>" + str + "</p>") : ("<p><br/></p>");
                        sb.AppendLine(temp);
                    }
                }

                return sb.ToString();
            }
            return String.Empty;
        }

        public static int GetTotalChapters(HtmlDocument doc)
        {
            string pattern = "(href=\"/)(?<series>.+)/(?<num>.+)/\">(?<title>.+)(?=</a>)";
            Regex r = new Regex(pattern);

            HtmlNodeCollection chapterNode = doc.DocumentNode.SelectNodes("//div[@class='index_box']/dl/dd[@class='subtitle']");
            Match m = r.Match(chapterNode.Last().OuterHtml);

            return Convert.ToInt32(m.Groups["num"].Value);
        }

        private static HtmlDocument GetPage(string link, CookieContainer cookies)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(link);
            request.Method = "GET";
            request.CookieContainer = cookies;
            request.UserAgent = Constants.UserAgent;


            try
            {
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                var stream = response.GetResponseStream();

                //When you get the response from the website, the cookies will be stored
                //automatically in "_cookies".

                using (StreamReader reader = new StreamReader(stream))
                {
                    string html = reader.ReadToEnd();
                    HtmlDocument doc = new HtmlDocument();
                    doc.LoadHtml(html);
                    return doc;
                }
            }
            catch (WebException)
            {
                StringBuilder html = new StringBuilder();
                html.Append("<html>");
                html.Append("<head>");
                html.Append("<title>エラー</title>");
                html.Append("</head>");
                html.Append("<body>");
                html.Append("</body>");
                html.Append("</html>");

                var doc = new HtmlAgilityPack.HtmlDocument();
                doc.LoadHtml(html.ToString());
                return doc;
            }
        }

        public static string GetNovelHeader(HtmlDocument doc, Constants.FileType fileType)
        {
            HtmlNode headerNode = doc.DocumentNode.SelectSingleNode("//div[@id='novel_p']");
            if (headerNode == null) { return String.Empty; }

            if (fileType == Constants.FileType.Text)
            {
                string s = headerNode.InnerText.Trim(Environment.NewLine.ToCharArray());
                s += Environment.NewLine + Environment.NewLine + "=====" + Environment.NewLine + Environment.NewLine;
                return s;
            }
            else if (fileType == Constants.FileType.HTML)
            {
                string[] s = headerNode.InnerText.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None);

                StringBuilder sb = new StringBuilder();
                foreach (String str in s)
                {
                    string temp = (str != "") ? ("<p>" + str + "</p>") : ("<p><br/></p>");
                    sb.AppendLine(temp);
                }

                sb.AppendLine("<hr/>");

                return sb.ToString();
            }
            else
            {
                return String.Empty;
            }
        }

        public static bool IsValid(HtmlDocument doc)
        {
            return (GetTitle(doc) != "エラー") ? true : false;
        }

        public static bool IsValidLink(string link)
        {
            string pattern = @"[^.]+[\.|]*syosetu.com/[\s\S]";
            Regex r = new Regex(pattern);
            Match m = r.Match(link);

            return m.Success;
        }

        public static string GetSeriesCode(string link)
        {
            string pattern = @"[^.]+[\.|]*syosetu.com/(?<seriesCode>.+)(?=/)";
            Regex r = new Regex(pattern);
            Match m = r.Match(link);

            return m.Groups["seriesCode"].Value;
        }

        public static void GetAllChapterTitles(Syousetsu.Constants details, HtmlDocument doc)
        {
            //edit all href
            HtmlNodeCollection chapterNode = doc.DocumentNode.SelectNodes("//div[@class='index_box']/dl[@class='novel_sublist2']");
            foreach (HtmlNode node in chapterNode)
            {
                //get current chapter number
                string pattern = "(href=\"/)(?<series>.+)/(?<num>.+)/\">(?<title>.+)(?=</a>)";
                Regex r = new Regex(pattern);
                Match m = r.Match(node.ChildNodes["dd"].OuterHtml);
                int current = Convert.ToInt32(m.Groups["num"].Value);
                details.ChapterTitle.Add(m.Groups["title"].Value.TrimStart().TrimEnd());

            }
        }
    }

    public class Create
    {
        public static string[] GenerateContents(Syousetsu.Constants details, HtmlDocument doc, int current)
        {
            string[] chapter = new string[2];
            if (details.CurrentFileType == Constants.FileType.Text)
            {
                chapter[0] = Methods.GetChapterTitle(doc).TrimStart().TrimEnd();
                chapter[1] = Methods.GetNovelHeader(doc, details.CurrentFileType);
                chapter[1] += chapter[0];
                chapter[1] += Methods.GetNovelBody(doc, details.CurrentFileType);

                if (doc.DocumentNode.SelectSingleNode("//div[@id='novel_honbun']").InnerHtml.Contains("<img"))
                {
                    string subLink = String.Format("{0}{1}", details.Link, current);
                    chapter[1] += String.Format("\n\n===\n\nContains image(s): {0}\n\n===", subLink);
                }
            }
            else if (details.CurrentFileType == Constants.FileType.HTML)
            {
                chapter[0] = Methods.GetChapterTitle(doc).TrimStart().TrimEnd();
                chapter[1] = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n";
                chapter[1] += "<!DOCTYPE html PUBLIC \"-//W3C//DTD XHTML 1.1//EN\"\n";
                chapter[1] += "\"http://www.w3.org/TR/xhtml11/DTD/xhtml11.dtd\">\n\n";
                chapter[1] += "<html xmlns=\"http://www.w3.org/1999/xhtml\">\n";
                chapter[1] += "<head>\n";
                chapter[1] += $"\t<title>{Methods.GetChapterTitle(doc).TrimStart().TrimEnd()}" +
                    $" ({current}) {details.SeriesTitle}</title>\n";
                chapter[1] += "\t<link href=\"ChapterStyle.css\" rel=\"stylesheet\" type=\"text/css\" />\n";
                chapter[1] += "</head>\n";
                chapter[1] += "<body>\n ";
                chapter[1] += Methods.GetNovelHeader(doc, details.CurrentFileType);
                chapter[1] += "\n<h2>" + chapter[0] + "</h2>\n\n";
                chapter[1] += Methods.GetNovelBody(doc, details.CurrentFileType);

                if (doc.DocumentNode.SelectSingleNode("//div[@id='novel_honbun']").InnerHtml.Contains("<img"))
                {
                    string subLink = String.Format("{0}{1}", details.Link, current);
                    chapter[1] += String.Format("\n\n===\n\n<a href=\"{0}\">Contains image(s)</a>\n\n===", subLink);
                }
            }
            return chapter;
        }

        public static void SaveFile(Syousetsu.Constants details, string[] chapter, int current)
        {
            string path = CheckDirectory(details, current);

            //replace illigal character(s) with "□"
            string regexSearch = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());
            Regex r = new Regex(string.Format("[{0}]", System.Text.RegularExpressions.Regex.Escape(regexSearch)));
            chapter[0] = r.Replace(chapter[0], "□");

            //save the chapter
            string fileName = details.FilenameFormat.Replace("/", "\\")
                    .Split('\\').Last();
            if (details.CurrentFileType == Constants.FileType.Text)
            {
                fileName = String.Format(fileName + ".txt",
                    new object[] { current, chapter[0], details.SeriesCode });
            }
            else if (details.CurrentFileType == Constants.FileType.HTML)
            {
                fileName = String.Format(fileName + ".htm",
                    new object[] { current, chapter[0], details.SeriesCode });

                File.WriteAllText(Path.Combine(path, "ChapterStyle.css"), "@charset \"UTF - 8\";\n/*chapter css here*/");
            }

            chapter[0] = String.Empty;
            fileName = Path.Combine(path, fileName);
            File.WriteAllLines(fileName, chapter, Encoding.Unicode);
        }

        public static void GenerateTableOfContents(Syousetsu.Constants details, HtmlDocument doc)
        {
            //create novel folder if it doesn't exist
            CheckDirectory(details);

            HtmlNode ptitleNode = doc.DocumentNode.SelectSingleNode("//title");
            HtmlNode stitleNode = doc.DocumentNode.SelectSingleNode("//p[@class='series_title']");
            HtmlNode titleNode = doc.DocumentNode.SelectSingleNode("//p[@class='novel_title']");
            HtmlNode writerNode = doc.DocumentNode.SelectSingleNode("//div[@class='novel_writername']");

            HtmlNode tocNode = doc.DocumentNode.SelectSingleNode("//div[@class='index_box']");
            HtmlNodeCollection cssNodeList = doc.DocumentNode.SelectNodes("//link[@rel='stylesheet']");

            var cssNode = (from n in cssNodeList
                           where n.Attributes["href"].Value.Contains("ncout.css") ||
                           n.Attributes["href"].Value.Contains("ncout2.css") ||
                           n.Attributes["href"].Value.Contains("kotei.css") ||
                           n.Attributes["href"].Value.Contains("reset.css")
                           select n).ToList();

            //get css link and download
            List<string> cssink = new List<string>();
            string pattern;
            Regex r;
            Match m;
            foreach (HtmlNode node in cssNode)
            {
                pattern = "(href=\")(?<link>.+)(?=\" media)";
                r = new Regex(pattern);
                m = r.Match(node.OuterHtml);
                cssink.Add(m.Groups["link"].Value);
            }
            DownloadCss(details, cssink);

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("<html>");
            sb.AppendLine("<head>");
            sb.AppendLine("\t<meta charset=\"UTF-8\">");
            sb.AppendLine("\t<link rel=\"stylesheet\" type=\"text/css\" href=\"" + details.SeriesCode + ".css\" media=\"screen,print\" />");
            if (ptitleNode != null) sb.AppendLine(ptitleNode.OuterHtml);
            sb.AppendLine("</head>");

            sb.AppendLine("<body>");

            if (stitleNode != null)
            {
                if (null != stitleNode.ChildNodes["a"] &&
                    null != stitleNode.ChildNodes["a"].Attributes["href"])
                {
                    var href = stitleNode.ChildNodes["a"].Attributes["href"].Value;
                    if (!string.IsNullOrEmpty(href))
                    {
                        stitleNode.ChildNodes["a"].Attributes["href"].Value = "https://ncode.syosetu.com" + href;
                    }
                }
                sb.AppendLine(stitleNode.OuterHtml);
            }
            if (titleNode != null) sb.AppendLine(titleNode.OuterHtml);
            if (writerNode != null) sb.AppendLine(writerNode.OuterHtml);

            //edit all href
            int i = 1;
            HtmlNodeCollection chapterNode = doc.DocumentNode.SelectNodes("//div[@class='index_box']/dl[@class='novel_sublist2']");
            foreach (HtmlNode node in chapterNode)
            {
                //get current chapter number
                pattern = "(href=\"/)(?<series>.+)/(?<num>.+)/\">(?<title>.+)(?=</a>)";
                r = new Regex(pattern);
                m = r.Match(node.ChildNodes["dd"].OuterHtml);
                int current = Convert.ToInt32(m.Groups["num"].Value);
                //already filled
                //details.ChapterTitle.Add(m.Groups["title"].Value);

                //edit href
                string fileName = details.FilenameFormat;
                fileName = String.Format(fileName + ".htm", current, details.ChapterTitle[current], details.SeriesCode);
                node.ChildNodes["dd"].ChildNodes["a"].Attributes["href"].Value = "./" + fileName;
                node.ChildNodes["dd"].ChildNodes["a"].InnerHtml = "(" + i + ") " + node.ChildNodes["dd"].ChildNodes["a"].InnerHtml;

                if (i <= Convert.ToInt32(details.End))
                {
                    CheckDirectory(details, current);
                }
                i++;
            }
            sb.AppendLine(tocNode.OuterHtml);

            sb.AppendLine("</body>");
            sb.AppendLine("</html>");

            File.WriteAllText(Path.Combine(details.Path, details.SeriesTitle, details.SeriesCode + ".htm"), sb.ToString());
        }

        private static void DownloadCss(Syousetsu.Constants details, List<string> link)
        {
            string cssfile = Path.Combine(details.Path, details.SeriesTitle, details.SeriesCode + ".css");
            File.WriteAllText(cssfile, "");

            foreach (string f in link)
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(f);
                request.Method = "GET";
                request.UserAgent = Constants.UserAgent;

                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                var stream = response.GetResponseStream();

                using (StreamReader reader = new StreamReader(stream))
                {
                    string css = reader.ReadToEnd();
                    using (StreamWriter writer = File.AppendText(cssfile))
                    {
                        writer.Write(css);
                        writer.Close();
                    }
                }
            }
        }

        private static string CheckDirectory(Syousetsu.Constants details)
        {
            string path;
            if (!details.FilenameFormat.Contains('/') && !details.FilenameFormat.Contains('\\'))
            {
                path = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), details.SeriesTitle);
            }
            else
            {
                string[] temp = details.FilenameFormat
                    .Replace("/", "\\")
                    .Split('\\');
                temp = temp.Take(temp.Length - 1).ToArray();
                string tempFormat = (temp.Length > 1) ? String.Join("\\", temp) : temp[0];

                path = Path.Combine(new string[] {
                    Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location),
                    details.SeriesTitle,
                    String.Format(tempFormat, new object[]{ 0, details.ChapterTitle[0], details.SeriesCode})
                });
            }
            if (!Directory.Exists(path)) { Directory.CreateDirectory(path); }
            return path;
        }

        private static string CheckDirectory(Syousetsu.Constants details, int current)
        {
            string path;
            if (!details.FilenameFormat.Contains('/') && !details.FilenameFormat.Contains('\\'))
            {
                path = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), details.SeriesTitle);
            }
            else
            {
                string[] temp = details.FilenameFormat
                    .Replace("/", "\\")
                    .Split('\\');
                temp = temp.Take(temp.Length - 1).ToArray();
                string tempFormat = (temp.Length > 1) ? String.Join("\\", temp) : temp[0];

                path = Path.Combine(new string[] {
                    Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location),
                    details.SeriesTitle,
                    String.Format(tempFormat, new object[]{ current, details.ChapterTitle[current], details.SeriesCode})
                });
            }
            if (!Directory.Exists(path)) { Directory.CreateDirectory(path); }
            return path;
        }
    }
}
