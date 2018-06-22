using System;
using System.Threading.Tasks;
using Greatbone;
using static Greatbone.Modal;

namespace Samp
{
    public abstract class InfVarWork : Work
    {
        protected InfVarWork(WorkConfig cfg) : base(cfg)
        {
        }

        public void icon(WebContext wc)
        {
            short infid = wc[this];
            using (var dc = NewDbContext())
            {
                if (dc.Query1("SELECT icon FROM infs WHERE id = @1", p => p.Set(infid)))
                {
                    dc.Let(out byte[] bytes);
                    if (bytes == null) wc.Give(204); // no content 
                    else wc.Give(200, new StaticContent(bytes));
                }
                else wc.Give(404); // not found           
            }
        }
    }

    public class PubInfVarWork : InfVarWork
    {
        public PubInfVarWork(WorkConfig cfg) : base(cfg)
        {
        }

        public void @default(WebContext ac)
        {
        }
    }

    public class AdmInfVarWork : InfVarWork
    {
        public AdmInfVarWork(WorkConfig cfg) : base(cfg)
        {
        }

        [Ui("修改"), Tool(ButtonShow)]
        public async Task edit(WebContext ac)
        {
            short infid = ac[this];
            if (ac.GET)
            {
                using (var dc = NewDbContext())
                {
                    dc.Sql("SELECT ").collst(Inf.Empty).T(" FROM infs WHERE id = @1");
                    var o = dc.Query1<Inf>(p => p.Set(infid));
                    ac.GivePane(200, h =>
                    {
                        h.FORM_().FIELDSET_();
                        h.DATE(nameof(o.created), o.created, "日　期");
                        h.TEXT(nameof(o.subject), o.subject, "标　题");
                        h.TEXTAREA(nameof(o.text), o.text, "正　文");
                        h._FIELDSET()._FORM();
                    });
                }
            }
            else // post
            {
                var o = await ac.ReadObjectAsync<Inf>();
                using (var dc = NewDbContext())
                {
                    dc.Sql("UPDATE ")._SET_(o)._VALUES_(o).T(" WHERE id = @1");
                    dc.Execute(p => p.Set(infid));
                }
                ac.GivePane(200);
            }
        }

        [Ui("插图"), Tool(ButtonCrop, size: 2)]
        public new async Task icon(WebContext wc)
        {
            short infid = wc[this];
            if (wc.GET)
            {
                base.icon(wc);
            }
            else // POST
            {
                ArraySegment<byte> img = (await wc.ReadAsync<Form>())[nameof(img)];
                using (var dc = NewDbContext())
                {
                    dc.Execute("UPDATE infs SET icon = @1 WHERE id = @2", p => p.Set(img).Set(infid));
                }
                wc.Give(200);
            }
        }
    }
}