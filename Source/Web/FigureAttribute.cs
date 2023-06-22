﻿using System;

namespace ChainFx.Web
{
    /// <summary>
    /// To markup a figure/iamge.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true)]
    public class FigureAttribute : HelpAttribute
    {
        readonly string[] texts;

        public FigureAttribute(params string[] texts)
        {
            this.texts = texts;
        }

        public override bool IsDetail => true;

        public override void Render(HtmlBuilder h)
        {
            h.P_();

            if (texts != null)
            {
                h.T("<pre>");
                foreach (var v in texts)
                {
                    h.TT(v);
                }
                h.T("</pre>");
            }
            h._P();
        }
    }
}