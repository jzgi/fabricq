﻿namespace Greatbone.Core
{
    public class WebTie : ITie
    {
        internal string key;

        public string Key => key;

        public bool IsVar { get; internal set; }

        public WebSub Parent { get; internal set; }

        public WebService Service { get; internal set; }
    }
}