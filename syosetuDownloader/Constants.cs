﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Threading;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using System.Net;

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
        public enum FileType { Text, HTML };
        private CookieContainer _cookies = new CookieContainer();
        string _title = String.Empty;
        string _start = String.Empty;
        string _end = String.Empty;
        FileType _fileType;
        string _path = String.Empty;
        string _link = String.Empty;
        string _seriesCode = String.Empty;

        public CookieContainer SyousetsuCookie
        {
            get
            {
                Cookie c = new System.Net.Cookie();
                c.Domain = ".syosetu.com";
                c.Value = "yes";
                c.Name = "over18";

                _cookies.Add(c);
                return _cookies;
            }
        }

        public string Title
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
    }
}