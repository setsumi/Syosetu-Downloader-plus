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
using System.Diagnostics;

namespace Syousetsu
{
    public class Methods
    {
        public static CancellationTokenSource AddDownloadJob(Syousetsu.Constants details, ProgressBar pb, Label lb)
        {
            var taskbar = Microsoft.WindowsAPICodePack.Taskbar.TaskbarManager.Instance;
            taskbar.SetProgressState(Microsoft.WindowsAPICodePack.Taskbar.TaskbarProgressBarState.Normal);

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
                    string subLink;
                    if (details.Site() == Constants.SiteType.Syousetsu) // syousetsu
                        subLink = details.Link + ctr;
                    else // kakuyomu
                        subLink = details.Link + "/episodes/" + details.GetChapterByIndex(ctr).number;

                    string[] chapter = Create.GenerateContents(details, GetPage(subLink, details), ctr);
                    Create.SaveFile(details, chapter, ctr);
                    // update downloaded history
                    details.LastDownloaded = ctr;
                    History.SaveNovel(details);

                    pb.Dispatcher.Invoke((Action)(() => { pb.Value = ctr; taskbar.SetProgressValue(ctr, max); }));
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
                    taskbar.SetProgressState(Microsoft.WindowsAPICodePack.Taskbar.TaskbarProgressBarState.NoProgress);
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

        public static void AddDownloadJob(Constants details)
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
                string[] chapter = Create.GenerateContents(details, GetPage(subLink, details), ctr);
                Create.SaveFile(details, chapter, ctr);

                if (upTo != -1 && ctr > upTo)//stop loop if the specifed range is reached
                {
                    break;
                }
            }
        }

        public static HtmlDocument GetTableOfContents(string link, Constants details)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(link);
            request.Method = "GET";
            request.CookieContainer = details.SyousetsuCookie;
            request.UserAgent = details.UserAgent;

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

        public static string GetTitle(HtmlDocument doc, Constants details)
        {
            if (details.Site() == Constants.SiteType.Syousetsu) // syosetu
            {
                HtmlNode titleNode = doc.DocumentNode.SelectSingleNode("//p[@class='novel_title']");
                return (titleNode == null) ? "title" : titleNode.InnerText.TrimStart().TrimEnd();
            }
            else // kakuyomu
            {
                HtmlNode titleNode = doc.DocumentNode.SelectSingleNode("//h1[@id='workTitle']/a");
                return (titleNode == null) ? "title" : titleNode.InnerText.TrimStart().TrimEnd();
            }
        }

        //public static string GetChapterTitle(HtmlDocument doc, Constants details)
        //{
        //    HtmlNode titleNode;
        //    if(details.Site() == Constants.SiteType.Syousetsu) // syousetsu
        //        titleNode = doc.DocumentNode.SelectSingleNode("//p[@class='novel_subtitle']");
        //    else // kakuyomu
        //        titleNode = doc.DocumentNode.SelectSingleNode("//p[@class='widget-episodeTitle']"); // FAILED !!!
        //    return (titleNode == null) ? "title" : titleNode.InnerText.TrimStart().TrimEnd();
        //}

        public static string GetNovelBody(HtmlDocument doc, Constants details)
        {
            HtmlNode novelNode;
            HtmlNode footerNode;
            if (details.Site() == Constants.SiteType.Syousetsu) // syousetsu
            {
                novelNode = doc.DocumentNode.SelectSingleNode("//div[@id='novel_honbun']");
                footerNode = doc.DocumentNode.SelectSingleNode("//div[@id='novel_a']");
            }
            else // kakuyomu
            {
                novelNode = doc.DocumentNode.SelectSingleNode("//div[@id='contentMain-inner']");
                footerNode = null;
            }

            if (details.CurrentFileType == Constants.FileType.Text)
            {
                string s = novelNode.InnerText;
                if (footerNode != null)
                {
                    s += Environment.NewLine + "=====" + Environment.NewLine;
                    s += footerNode.InnerText;
                }

                return s;
            }
            else if (details.CurrentFileType == Constants.FileType.HTML)
            {
                StringBuilder sb = new StringBuilder();

                foreach (HtmlNode img in novelNode.Descendants("img"))
                {
                    string src = img.GetAttributeValue("src", null);
                    if (src != null)
                    {
                        if (!src.StartsWith("http"))
                        {
                            src = "https:" + src;
                            img.SetAttributeValue("src", src);
                        }

                        // try to download and embed image
                        try
                        {
                            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(src);
                            request.Method = "GET";
                            request.CookieContainer = details.SyousetsuCookie;
                            request.UserAgent = details.UserAgent;

                            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                            using (Stream stream = response.GetResponseStream())
                            using (MemoryStream ms = new MemoryStream())
                            {
                                stream.CopyTo(ms, (int)response.ContentLength);
                                string base64String = Convert.ToBase64String(ms.ToArray());
                                string imageSrc = string.Format("data:image/png;base64,{0}", base64String);
                                img.SetAttributeValue("src", imageSrc);
                            }
                        }
                        catch (Exception)
                        {
                            byte[] fileBytes = File.ReadAllBytes(Path.Combine(Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName), "image-error.png"));
                            string base64String = Convert.ToBase64String(fileBytes);
                            string imageSrc = string.Format("data:image/png;base64,{0}", base64String);
                            img.SetAttributeValue("src", imageSrc);
                        }
                    }
                }
                foreach (HtmlNode img in novelNode.Descendants("a"))
                {
                    string src = img.GetAttributeValue("href", null);
                    if (src != null)
                    {
                        if (!src.StartsWith("http"))
                        {
                            img.SetAttributeValue("href", "https:" + src);
                        }
                    }
                }

                foreach (HtmlNode childNode in novelNode.ChildNodes)
                {
                    sb.AppendLine(childNode.OuterHtml);
                }

                if (footerNode != null)
                {
                    sb.AppendLine("<br><br><hr><br>");
                    foreach (HtmlNode childNode in footerNode.ChildNodes)
                    {
                        sb.AppendLine(childNode.OuterHtml);
                    }
                }

                /*
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
                */

                return sb.ToString();
            }
            return String.Empty;
        }

        public static int GetTotalChapters(HtmlDocument doc, Constants details)
        {
            if (details.Site() == Constants.SiteType.Syousetsu) // syosetu
            {
                string pattern = "(href=\"/)(?<series>.+)/(?<num>.+)/\">(?<title>.+)(?=</a>)";
                Regex r = new Regex(pattern);

                HtmlNodeCollection chapterNode = doc.DocumentNode.SelectNodes("//div[@class='index_box']/dl/dd[@class='subtitle']");
                Match m = r.Match(chapterNode.Last().OuterHtml);

                return Convert.ToInt32(m.Groups["num"].Value);
            }
            else // kakuyomu
            {
                HtmlNodeCollection chapterNode = doc.DocumentNode.SelectNodes("//div[@class='widget-toc-main']/ol/li[@class='widget-toc-episode']");
                return chapterNode.Count;
            }
        }

        private static HtmlDocument GetPage(string link, Constants details)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(link);
            request.Method = "GET";
            request.CookieContainer = details.SyousetsuCookie;
            request.UserAgent = details.UserAgent;

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

        public static string GetNovelHeader(HtmlDocument doc, Constants details)
        {
            HtmlNode titleNode;
            HtmlNode headerNode;

            if (details.Site() == Constants.SiteType.Syousetsu) // syousetsu
            {
                titleNode = doc.DocumentNode.SelectSingleNode("//p[@class='chapter_title']");
                headerNode = doc.DocumentNode.SelectSingleNode("//div[@id='novel_p']");
            }
            else // kakuyomu
            {
                titleNode = doc.DocumentNode.SelectSingleNode("//p[@class='chapterTitle']/span");
                headerNode = null;
            }

            // construct text
            if (details.CurrentFileType == Constants.FileType.Text)
            {
                string s = "";
                if (titleNode != null)
                {
                    s = titleNode.InnerText.Trim() + Environment.NewLine + Environment.NewLine;
                }

                if (headerNode != null)
                {
                    s += headerNode.InnerText.Trim(Environment.NewLine.ToCharArray());
                    s += Environment.NewLine + Environment.NewLine + "=====" + Environment.NewLine + Environment.NewLine;
                }
                return s;
            }
            else if (details.CurrentFileType == Constants.FileType.HTML)
            {
                string ss = "";
                if (titleNode != null)
                {
                    ss = (titleNode.InnerText.Trim() != "") ? ("<p>" + titleNode.InnerText.Trim() + "<br/></p>") : ("<p><br/><br/></p>");
                }

                if (headerNode != null)
                {
                    string[] s = headerNode.InnerText.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None);

                    StringBuilder sb = new StringBuilder();
                    foreach (String str in s)
                    {
                        string temp = (str != "") ? ("<p>" + str + "</p>") : ("<p><br/></p>");
                        sb.AppendLine(temp);
                    }
                    sb.AppendLine("<hr/>");

                    ss += sb.ToString();
                }
                return ss;
            }
            else
            {
                return String.Empty;
            }
        }

        public static bool IsValid(HtmlDocument doc, Constants details)
        {
            HtmlNode titleNode = doc.DocumentNode.SelectSingleNode("//title");
            if (titleNode.InnerText.Contains("エラー") || // syousetsu
                titleNode.InnerText.Contains("お探しのページは")) // kakuyomu
                return false;
            else
                return true;
        }

        public static bool IsValidLink(string link)
        {
            string pattern = @"[^.]+[\.|]*(syosetu.com|kakuyomu.jp)/[\s\S]";
            Regex r = new Regex(pattern);
            Match m = r.Match(link);

            return m.Success;
        }

        public static string GetSeriesCode(string link)
        {
            string pattern;
            if (Constants.Site(link) == Constants.SiteType.Syousetsu) // syousetsu
                pattern = @"[^.]+[\.|]*syosetu.com/(?<seriesCode>.+)(?=/)";
            else // kakuyomu
                pattern = @"[^.]+[\.|]*kakuyomu.jp/works/(?<seriesCode>.+)";
            Regex r = new Regex(pattern);
            Match m = r.Match(link);

            return m.Groups["seriesCode"].Value;
        }

        public static void GetAllChapterTitles(Constants details, HtmlDocument doc)
        {
            if (details.Site() == Constants.SiteType.Syousetsu) // syousetsu
            {
                HtmlNodeCollection chapterNode = doc.DocumentNode.SelectNodes("//div[@class='index_box']/dl[@class='novel_sublist2']");
                foreach (HtmlNode node in chapterNode)
                {
                    string pattern = "(href=\"/)(?<series>.+)/(?<num>.+)/\">(?<title>.+)(?=</a>)";
                    Regex r = new Regex(pattern);
                    Match m = r.Match(node.ChildNodes["dd"].OuterHtml);
                    details.AddChapter(m.Groups["title"].Value.TrimStart().TrimEnd(),
                        m.Groups["num"].Value.TrimStart().TrimEnd());
                }
            }
            else // kakuyomu
            {
                HtmlNodeCollection chapterNode = doc.DocumentNode.SelectNodes("//div[@class='widget-toc-main']/ol/li[@class='widget-toc-episode']");
                foreach (HtmlNode node in chapterNode)
                {
                    string pattern = "(href=\"/works/)(?<series>.+)/episodes/(?<num>.+)\" class";
                    Regex r = new Regex(pattern);
                    Match m = r.Match(node.ChildNodes["a"].OuterHtml);
                    details.AddChapter(node.ChildNodes["a"].ChildNodes["span"].InnerText.TrimStart().TrimEnd(),
                        m.Groups["num"].Value.TrimStart().TrimEnd());
                }
            }
        }

        public static string FormatValidFileName(string filename)
        {
            return filename.Replace("\\", "＼").Replace("/", "／").Replace(":", "：").Replace("*", "＊").
                Replace("?", "？").Replace("\"", "“").Replace("<", "＜").Replace(">", "＞").Replace("|", "｜");
        }
    }

    public class Create
    {
        public static string[] GenerateContents(Constants details, HtmlDocument doc, int current)
        {
            string[] chapter = new string[2];
            if (details.CurrentFileType == Constants.FileType.Text)
            {
                //chapter[0] = Methods.GetChapterTitle(doc, details);
                chapter[0] = details.GetChapterByIndex(current).title;
                chapter[1] = Methods.GetNovelHeader(doc, details);
                chapter[1] += chapter[0];
                chapter[1] += Methods.GetNovelBody(doc, details);

                if (details.Site() == Constants.SiteType.Syousetsu) // syousetsu
                {
                    if (doc.DocumentNode.SelectSingleNode("//div[@id='novel_honbun']").InnerHtml.Contains("<img"))
                    {
                        string subLink = String.Format("{0}{1}", details.Link, current);
                        chapter[1] += String.Format("\n\n===\n\nContains image(s): {0}\n\n===", subLink);
                    }
                }
            }
            else if (details.CurrentFileType == Constants.FileType.HTML)
            {
                //chapter[0] = Methods.GetChapterTitle(doc, details);
                chapter[0] = details.GetChapterByIndex(current).title;
                chapter[1] = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n";
                chapter[1] += "<!DOCTYPE html PUBLIC \"-//W3C//DTD XHTML 1.1//EN\"\n";
                chapter[1] += "\"http://www.w3.org/TR/xhtml11/DTD/xhtml11.dtd\">\n\n";
                chapter[1] += "<html xmlns=\"http://www.w3.org/1999/xhtml\">\n";
                chapter[1] += "<head>\n";
                //chapter[1] += $"\t<title>{Methods.GetChapterTitle(doc, details)}" +
                //    $" ({current}) {details.SeriesTitle}</title>\n";
                chapter[1] += $"\t<title>{details.GetChapterByIndex(current).title}" +
                    $" ({current}) {details.SeriesTitle}</title>\n";
                chapter[1] += "\t<link href=\"ChapterStyle.css\" rel=\"stylesheet\" type=\"text/css\" />\n";
                chapter[1] += "</head>\n";
                chapter[1] += "<body>\n ";
                chapter[1] += Methods.GetNovelHeader(doc, details);
                chapter[1] += "\n<h2>" + chapter[0] + "</h2>\n\n";
                chapter[1] += Methods.GetNovelBody(doc, details);

                if (details.Site() == Constants.SiteType.Syousetsu) // syousetsu
                {
                    if (doc.DocumentNode.SelectSingleNode("//div[@id='novel_honbun']").InnerHtml.Contains("<img"))
                    {
                        string subLink = String.Format("{0}{1}", details.Link, current);
                        chapter[1] += String.Format("\n\n===\n\n<a href=\"{0}\">Contains image(s)</a>\n\n===", subLink);
                    }
                }
            }
            return chapter;
        }

        public static void SaveFile(Constants details, string[] chapter, int current)
        {
            string path = CheckDirectory(details, current);

            //replace illegal character(s)
            //string regexSearch = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());
            //Regex r = new Regex(string.Format("[{0}]", System.Text.RegularExpressions.Regex.Escape(regexSearch)));
            //chapter[0] = r.Replace(chapter[0], "□");
            chapter[0] = Methods.FormatValidFileName(chapter[0]);

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

                //File.WriteAllText(Path.Combine(path, "ChapterStyle.css"), "@charset \"UTF-8\";\n/*chapter css here*/\nrt{font-size:.8em;}");
                File.Copy(Path.Combine(Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName), "ChapterStyle.css"),
                    Path.Combine(path, "ChapterStyle.css"), true);
            }

            chapter[0] = String.Empty;
            fileName = Path.Combine(path, fileName);
            File.WriteAllLines(fileName, chapter, Encoding.Unicode);
        }

        public static void GenerateTableOfContents(Syousetsu.Constants details, HtmlDocument doc)
        {
            //create novel folder if it doesn't exist
            CheckDirectory(details);

            HtmlNode ptitleNode; // page title
            HtmlNode stitleNode; // series title
            HtmlNode titleNode;  // novel title
            HtmlNode writerNode; // author
            HtmlNode tocNode;    // table of contents

            if (details.Site() == Constants.SiteType.Syousetsu)
            {
                ptitleNode = doc.DocumentNode.SelectSingleNode("//title");
                stitleNode = doc.DocumentNode.SelectSingleNode("//p[@class='series_title']");
                titleNode = doc.DocumentNode.SelectSingleNode("//p[@class='novel_title']");
                writerNode = doc.DocumentNode.SelectSingleNode("//div[@class='novel_writername']");
                tocNode = doc.DocumentNode.SelectSingleNode("//div[@class='index_box']");
            }
            else // kakuyomu
            {
                ptitleNode = doc.DocumentNode.SelectSingleNode("//title");
                stitleNode = null;
                titleNode = doc.DocumentNode.SelectSingleNode("//h1[@id='workTitle']");
                writerNode = doc.DocumentNode.SelectSingleNode("//h2[@id='workAuthor']");
                tocNode = doc.DocumentNode.SelectSingleNode("//section[@class='widget-toc']");

                // remove left header
                tocNode.ChildNodes["header"].Remove();
            }

            HtmlNodeCollection cssNodeList = doc.DocumentNode.SelectNodes("//link[@rel='stylesheet']");

            var cssNode = (from n in cssNodeList
                           where n.Attributes["href"].Value.Contains("ncout.css") ||
                           n.Attributes["href"].Value.Contains("ncout2.css") ||
                           n.Attributes["href"].Value.Contains("kotei.css") || // ...
                           n.Attributes["href"].Value.Contains("reset.css") || // syousetsu
                           n.Attributes["href"].Value.Contains("kakuyomu.css") // kakuyomu
                           select n).ToList();

            //get css link and download
            List<string> cssink = new List<string>();
            string[] patterns = { "(href=\")(?<link>.+)(?=\" media)", "(href=\")(?<link>.+)(?=\">)" };
            string pattern;
            Regex r;
            Match m = Match.Empty;
            foreach (HtmlNode node in cssNode)
            {
                foreach (string p in patterns)
                {
                    r = new Regex(p);
                    m = r.Match(node.OuterHtml);
                    if (m.Groups["link"].Value.Length > 0) break;
                }

                //if (details.Site() == Constants.SiteType.Syousetsu) // syousetsu
                //    pattern = "(href=\")(?<link>.+)(?=\" media)";
                //else // kakuyomu
                //    pattern = "(href=\")(?<link>.+)(?=\">)";

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

            if (details.Site() == Constants.SiteType.Syousetsu) // syousetsu
                sb.AppendLine("<body>");
            else // kakuyomu
            {
                // copy <body> as is for it's id
                HtmlNode body = doc.DocumentNode.SelectSingleNode("//body");
                sb.AppendLine(body.OuterHtml.Substring(0, body.OuterHtml.IndexOf('>') + 1));
            }

            // restore header links
            if (stitleNode != null) // syousetsu
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
            else // kakuyomu
            {
                HtmlNode title_node = doc.DocumentNode.SelectSingleNode("//h1[@id='workTitle']/a");
                HtmlNode author_node = doc.DocumentNode.SelectSingleNode("//h2[@id='workAuthor']/span[@id='workAuthor-activityName']/a");
                if (title_node != null)
                {
                    string s = null; s = title_node.Attributes["href"]?.Value;
                    if (!string.IsNullOrEmpty(s))
                        title_node.Attributes["href"].Value = "https://kakuyomu.jp" + s;
                }
                if (author_node != null)
                {
                    string s = null; s = author_node.Attributes["href"]?.Value;
                    if (!string.IsNullOrEmpty(s))
                        author_node.Attributes["href"].Value = "https://kakuyomu.jp" + s;
                }
            }

            if (titleNode != null) sb.AppendLine(titleNode.OuterHtml);
            if (writerNode != null) sb.AppendLine(writerNode.OuterHtml);

            //edit all href
            int i = 1;
            HtmlNodeCollection chapterNode;
            if (details.Site() == Constants.SiteType.Syousetsu) // syousetsu
                chapterNode = doc.DocumentNode.SelectNodes("//div[@class='index_box']/dl[@class='novel_sublist2']");
            else // kakuyomu
                chapterNode = doc.DocumentNode.SelectNodes("//section[@class='widget-toc']/div[@class='widget-toc-main']/ol/li[@class='widget-toc-episode']");

            foreach (HtmlNode node in chapterNode)
            {
                //get current chapter number
                if (details.Site() == Constants.SiteType.Syousetsu) // syousetsu
                {
                    pattern = "(href=\"/)(?<series>.+)/(?<num>.+)/\">(?<title>.+)(?=</a>)";
                    r = new Regex(pattern);
                    m = r.Match(node.ChildNodes["dd"].OuterHtml);
                    //int current = Convert.ToInt32(m.Groups["num"].Value);

                    //edit href
                    string fileName = details.FilenameFormat;
                    fileName = String.Format(fileName + ".htm", i, Methods.FormatValidFileName(details.GetChapterByIndex(i).title), details.SeriesCode);
                    node.ChildNodes["dd"].ChildNodes["a"].Attributes["href"].Value = "./" + fileName;
                    node.ChildNodes["dd"].ChildNodes["a"].InnerHtml = "(" + i + ") " +
                        node.ChildNodes["dd"].ChildNodes["a"].InnerHtml;
                }
                else // kakuyomu
                {
                    pattern = "(href=\"/works/)(?<series>.+)/episodes/(?<num>.+)\" class.+item\">(?<title>.+)(?=</span>)";
                    r = new Regex(pattern);
                    m = r.Match(node.OuterHtml);

                    //edit href
                    string fileName = details.FilenameFormat;
                    fileName = String.Format(fileName + ".htm", i, Methods.FormatValidFileName(details.GetChapterByIndex(i).title), details.SeriesCode);
                    node.ChildNodes["a"].Attributes["href"].Value = "./" + fileName;
                    node.ChildNodes["a"].ChildNodes["span"].InnerHtml = "(" + i + ") " +
                        node.ChildNodes["a"].ChildNodes["span"].InnerHtml;
                }

                if (i <= Convert.ToInt32(details.End))
                {
                    CheckDirectory(details, i);
                }
                i++;
            }
            sb.AppendLine(tocNode.OuterHtml);

            sb.AppendLine("</body>");
            sb.AppendLine("</html>");

            File.WriteAllText(Path.Combine(details.Path, details.SeriesTitle, details.SeriesCode + ".htm"), sb.ToString());
        }

        private static void DownloadCss(Constants details, List<string> link)
        {
            string cssfile = Path.Combine(details.Path, details.SeriesTitle, details.SeriesCode + ".css");
            File.WriteAllText(cssfile, "");

            foreach (string f in link)
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(f);
                request.Method = "GET";
                request.UserAgent = details.UserAgent;

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

            // css fix
            using (StreamWriter writer = File.AppendText(cssfile))
            {
                if (details.Site() == Constants.SiteType.Syousetsu) // syousetsu
                    writer.Write("\n.index_box{width:100%!important;}");
                else // kakuyomu
                    writer.Write("\n.widget-toc-main{width:100%!important;float:none!important;}");
                writer.Close();
            }
        }

        private static string CheckDirectory(Constants details)
        {
            string path;
            if (!details.FilenameFormat.Contains('/') && !details.FilenameFormat.Contains('\\'))
            {
                path = Path.Combine(details.Path, details.SeriesTitle);
            }
            else
            {
                string[] temp = details.FilenameFormat
                    .Replace("/", "\\")
                    .Split('\\');
                temp = temp.Take(temp.Length - 1).ToArray();
                string tempFormat = (temp.Length > 1) ? String.Join("\\", temp) : temp[0];

                path = Path.Combine(new string[] {
                    details.Path,
                    details.SeriesTitle,
                    String.Format(tempFormat, new object[]{ 0, details.GetChapterByIndex(0).title, details.SeriesCode})
                });
            }
            if (!Directory.Exists(path)) { Directory.CreateDirectory(path); }
            return path;
        }

        private static string CheckDirectory(Constants details, int current)
        {
            string path;
            if (!details.FilenameFormat.Contains('/') && !details.FilenameFormat.Contains('\\'))
            {
                path = Path.Combine(details.Path, details.SeriesTitle);
            }
            else
            {
                string[] temp = details.FilenameFormat
                    .Replace("/", "\\")
                    .Split('\\');
                temp = temp.Take(temp.Length - 1).ToArray();
                string tempFormat = (temp.Length > 1) ? String.Join("\\", temp) : temp[0];

                path = Path.Combine(new string[] {
                    details.Path,
                    details.SeriesTitle,
                    String.Format(tempFormat, new object[]{ current, details.GetChapterByIndex(current).title,
                        details.SeriesCode})
                });
            }
            if (!Directory.Exists(path)) { Directory.CreateDirectory(path); }
            return path;
        }
    }
}
