using CommandLine;
using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace syosetuDownloaderCLI
{
    class Program
    {
        class Options
        {
            [Option('l', "link", HelpText = "URL to be processed")]
            public string Link { get; set; }

            [Option('s', "start", HelpText = "Stating chapter", DefaultValue = "1")]
            public string Start { get; set; }

            [Option('e', "end", HelpText = "Ending chapter")]
            public string End { get; set; }

            [Option('f', "format", HelpText = "File format (HTML | TXT)", DefaultValue = "TXT")]
            public string Format { get; set; }

            [Option('t', "toc", HelpText = "Generates Table of contents", DefaultValue = false)]
            public bool ToC { get; set; }

            [Option('h', "help")]
            public bool Help { get; set; }
        }
        static string GetFilenameFormat()
        {
            using (System.IO.StreamReader sr = new System.IO.StreamReader("format.ini"))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    if (!line.StartsWith(";"))
                    {
                        return line;
                    }
                }
            }

            return "";
        }

        static void Main(string[] args)
        {
            Options o = new Options();
            if (args.Any())
            {
                /* do stuff without a GUI */
                Parser.Default.ParseArguments(args, o);

                if (o.Help == true)
                {
                    Console.WriteLine(CommandLine.Text.HelpText.AutoBuild(o));
                    return;
                }

                o.Link = o.Link.Trim(' ', '\t', '\n', '\v', '\f', '\r', '"', '\'');

                Syousetsu.Constants sc = new Syousetsu.Constants();
                HtmlDocument toc = Syousetsu.Methods.GetTableOfContents(o.Link, sc.SyousetsuCookie);

                sc.ChapterTitle.Add("");
                sc.SeriesTitle = Syousetsu.Methods.GetTitle(toc);
                sc.Link = o.Link;
                sc.Start = o.Start;
                sc.End = String.IsNullOrWhiteSpace(o.End) ? Syousetsu.Methods.GetTotalChapters(toc).ToString() : o.End;
                sc.SeriesCode = Syousetsu.Methods.GetSeriesCode(o.Link);
                sc.FilenameFormat = GetFilenameFormat();
                Syousetsu.Methods.GetAllChapterTitles(sc, toc);
                Syousetsu.Constants.FileType fileType = Syousetsu.Constants.FileType.Text;
                switch (o.Format.ToUpper())
                {
                    case "HTML": { fileType = Syousetsu.Constants.FileType.HTML; break; }
                    case "TXT": { fileType = Syousetsu.Constants.FileType.Text; break; }
                    default: { fileType = Syousetsu.Constants.FileType.Text; break; }
                }
                sc.CurrentFileType = fileType;
                Syousetsu.Methods.GetAllChapterTitles(sc, toc);

                if (o.ToC == true)
                {
                    Syousetsu.Create.GenerateTableOfContents(sc, toc);
                }

                System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
                sw.Start();
                Syousetsu.Methods.AddDownloadJob(sc);
                sw.Stop();

                Console.WriteLine(sw.Elapsed);
            }
        }
    }
}
