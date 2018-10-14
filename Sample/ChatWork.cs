using System;
using System.Threading.Tasks;
using Greatbone;
using static Greatbone.Modal;

namespace Samp
{
    public abstract class ChatWork<V> : Work where V : ChatVarWork
    {
        public const short PicWidth = 360, PicHeight = 270;

        protected ChatWork(WorkConfig cfg) : base(cfg)
        {
            MakeVar<V>();
        }
    }

    public class SampChatWork : ChatWork<SampChatVarWork>
    {
        public SampChatWork(WorkConfig cfg) : base(cfg)
        {
        }

        public void @default(WebContext wc, int page)
        {
            using (var dc = NewDbContext())
            {
                const byte proj = 0xff ^ Chat.DETAIL;
                dc.Sql("SELECT ").collst(Chat.Empty, proj).T(" FROM chats ORDER BY status, posted DESC OFFSET @1 LIMIT 20");
                var arr = dc.Query<Chat>(p => p.Set(page * 20), proj);
                wc.GivePage(200, h =>
                    {
                        h.TOPBAR(false);
                        h.LIST(arr, o =>
                        {
                            h.T("<a class=\"uk-col uk-link-heading uk-padding-small\" href=\"").T(o.id).T("/\" onclick=\"return dialog(this, 8, false, 2, '").T(o.subject).T("');\">");
                            h.DIV_("uk-row uk-flex-between").DIV_("uk-text-lead").T(o.subject)._DIV();
                            h.T(o.fcount)._DIV();
                            h.DIV_("uk-row uk-flex-between uk-text-muted").SPAN_().T("<span uk-icon=\"user\"></span>&nbsp;").T(o.uname)._SPAN().SPAN_().T(o.fname).SP().T(o.posted)._DIV();
                            h._DIV();
                            h.T("</a>");
                        });
                        h.BOTTOM_().TOOL(nameof(@new))._BOTTOM();
                    }, true
                );
            }
        }

        [Ui("发新帖"), Tool(ButtonShow, css: "uk-button-primary", size: 2)]
        public async Task @new(WebContext wc)
        {
            string subject = null;
            string text = null;
            ArraySegment<byte> img;
            if (wc.GET)
            {
                wc.GivePane(200, h =>
                {
                    h.FORM_(mp: true);
                    h.FIELDUL_();
                    h.LI_().TEXT(null, nameof(subject), text, tip: "填写标题", max: 20, required: true)._LI();
                    h.LI_().TEXTAREA(null, nameof(text), text, tip: "填写文字内容", max: 500, required: true)._LI();
                    h._FIELDUL();
                    h.CROP(nameof(img), "附图片", PicWidth, PicHeight);
                    h._FORM();
                });
            }
            else // POST
            {
                var now = DateTime.Now;
                var prin = (User) wc.Principal;
                var f = await wc.ReadAsync<Form>();
                subject = f[nameof(subject)];
                text = f[nameof(text)];
                img = f[nameof(img)];
                var chat = new Chat
                {
                    subject = subject,
                    uname = prin.name,
                    posts = new[]
                    {
                        new Post
                        {
                            uid = prin.id, uname = prin.name, teamat = prin.teamid, text = text,
                            img = (short) (img.Count == 0 ? 0 : 1),
                            time = now
                        }
                    },
                    posted = now
                };
                using (var dc = NewDbContext())
                {
                    const byte proj = 0xff ^ Chat.ID;
                    if (img.Count == 0)
                    {
                        dc.Sql("INSERT INTO chats")._(chat, proj)._VALUES_(chat, proj);
                        dc.Execute(p => { chat.Write(p, proj); });
                    }
                    else
                    {
                        dc.Sql("INSERT INTO chats")._(chat, proj, "img1")._VALUES_(chat, proj, "@1");
                        dc.Execute(p =>
                        {
                            chat.Write(p, proj);
                            p.Set(img);
                        });
                    }
                }
                wc.GivePane(200);
            }
        }
    }
}