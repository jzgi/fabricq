using System.Threading.Tasks;
using Greatbone;
using static Greatbone.Modal;
using static Samp.User;

namespace Samp
{
    [UserAuthorize]
    public class MyVarWork : Work
    {
        public MyVarWork(WorkConfig cfg) : base(cfg)
        {
            Make<MyOrderWork>("order");

            Make<SampChatWork>("chat");
        }

        public void @default(WebContext wc)
        {
            var o = (User) wc.Principal;
            string hubid = wc[0];
            var orgs = Obtain<Map<short, Team>>();
            wc.GivePage(200, h =>
            {
                h.DIV_(css: "uk-card- uk-card-primary");
                h.UL_(css: "uk-card-body");
                h.LI_().FI("真实姓名", o.name)._LI();
                h.LI_().FI("手　　机", o.tel)._LI();
                h.LI_().FI("参　　团", orgs[o.teamid]?.name)._LI();
                h.LI_().FI("送货地址", o.addr)._LI();
                h._UL();
                h.TOOLS(css: "uk-card-footer uk-flex-center");
                h._DIV();

                h.DIV_(css: "uk-card- uk-card-primary uk-card-body");
                h.QRCODE("http://144000.tv/nc/catch-3?teamid=" + o.teamid);
                h.DIV_(css: "uk-card-footer uk-flex-center").T("团推荐码")._DIV();
                h._DIV();
            }, title: "我的设置");
        }

        const string PASSMASK = "t#0^0z4R4pX7";

        [Ui("修改设置", "修改用户设置"), Tool(ButtonOpen, css: "uk-button-primary")]
        public async Task upd(WebContext wc)
        {
            string hubid = wc[0];
            var prin = (User) wc.Principal;

            // if caused by scanning a referrer's code
            short refid = wc.Query[nameof(refid)];
            if (refid > 0)
            {
                prin.teamid = refid;
            }

            string password = null;
            if (wc.IsGet)
            {
                var orgs = Obtain<Map<short, Team>>();
                using (var dc = NewDbContext())
                {
                    var o = dc.Query1<User>("SELECT * FROM users WHERE id = @1", p => p.Set(prin.id));
                    if (o.credential != null) password = PASSMASK;
                    wc.GivePane(200, h =>
                    {
                        h.FORM_();
                        h.FIELDUL_("用户基本信息");
                        h.LI_().TEXT("真实姓名", nameof(o.name), o.name, max: 4, min: 2, required: true)._LI();
                        h.LI_().TEXT("手　　机", nameof(o.tel), prin.tel, pattern: "[0-9]+", max: 11, min: 11, required: true)._LI();
                        h.LI_().SELECT("参　　团", nameof(o.teamid), prin.teamid, orgs, filter: x => x.hubid == hubid)._LI();
                        h.LI_().TEXT("上门地址", nameof(o.addr), prin.addr, max: 20, min: 2, required: true)._LI();
                        h._FIELDUL();
                        h.FIELDUL_("用于从微信以外登录（可选）");
                        h.LI_().PASSWORD("外部密码", nameof(password), password, max: 12, min: 3)._LI();
                        h._FIELDUL();
                        h.BOTTOMBAR_().BUTTON(caption: "确定", css: "uk-button-primary")._BOTTOMBAR();
                        h._FORM();
                    });
                }
            }
            else
            {
                var f = await wc.ReadAsync<Form>();
                prin.Read(f);
                password = f[nameof(password)];
                using (var dc = NewDbContext())
                {
                    if (password != PASSMASK) // password being changed
                    {
                        prin.credential = TextUtility.MD5(prin.tel + ":" + password);
                        dc.Sql("INSERT INTO users ")._(prin, PRIVACY)._VALUES_(prin, PRIVACY).T(" ON CONFLICT (wx) DO UPDATE ")._SET_(prin, PRIVACY).T(" WHERE users.id = @1");
                        dc.Execute(p => prin.Write(p, PRIVACY | ID));
                    }
                    else // password no change
                    {
                        dc.Sql("INSERT INTO users ")._(prin, 0)._VALUES_(prin, 0).T(" ON CONFLICT (wx) DO UPDATE ")._SET_(prin, 0).T(" WHERE users.id = @id");
                        dc.Execute(p => prin.Write(p, ID));
                    }
                    wc.SetTokenCookie(prin, 0xff ^ PRIVACY);
                    if (refid > 0) // if was opened by referring qrcode
                    {
                        var hub = Obtain<Map<string, Hub>>()[hubid];
                        wc.GiveRedirect(hub.watchurl); // redirect to the weixin account watch page
                    }
                    else // was opened manually
                    {
                        wc.GivePane(200); // close dialog
                    }
                }
            }
        }
    }


    [UserAuthorize(shoply: 1)]
    [Ui("动态")]
    public class ShoplyVarWork : Work, IOrgVar
    {
        public ShoplyVarWork(WorkConfig cfg) : base(cfg)
        {
            Make<ShoplyOrderWork>("order");

            Make<ShoplyOprWork>("user");

            Make<OrgRepayWork>("repay");
        }

        public void @default(WebContext wc)
        {
            string hubid = wc[0];
            short id = wc[this];
            var shop = Obtain<Map<short, Team>>()[id];
            bool inner = wc.Query[nameof(inner)];
            if (!inner)
            {
                wc.GiveFrame(200, false, 900, title: shop?.name);
            }
            else
            {
                wc.GivePage(200, h =>
                {
                    h.TOOLBAR();

                    h.SECTION_("uk-card uk-card-default");
                    h.HEADER_("uk-card-header").H4("订单")._HEADER();
                    h.MAIN_("uk-card-body")._MAIN();
                    h._SECTION();

                    h.SECTION_("uk-card uk-card-default");
                    h.HEADER_("uk-card-header").H4("人员")._HEADER();
                    h.MAIN_("uk-card-body")._MAIN();
                    h._SECTION();
                });
            }
        }
    }

    [UserAuthorize(teamly: 1)]
    [Ui("动态")]
    public class TeamlyVarWork : Work, IOrgVar
    {
        public TeamlyVarWork(WorkConfig cfg) : base(cfg)
        {
            Make<TeamlyOrderWork>("order");

            Make<TeamlyUserWork>("user");

            Make<TeamlyOprWork>("opr");

            Make<OrgRepayWork>("repay");
        }

        public void @default(WebContext wc)
        {
            string hubid = wc[0];
            short id = wc[this];
            var org = Obtain<Map<short, Team>>()[id];
            bool inner = wc.Query[nameof(inner)];
            if (!inner)
            {
                wc.GiveFrame(200, false, 900, org?.name);
            }
            else
            {
                wc.GivePage(200, h =>
                {
                    h.TOOLBAR();

                    h.SECTION_("uk-card uk-card-default");
                    h.HEADER_("uk-card-header").H4("订单")._HEADER();
                    h.MAIN_("uk-card-body")._MAIN();
                    h._SECTION();

                    h.SECTION_("uk-card uk-card-default");
                    h.HEADER_("uk-card-header").H4("成员")._HEADER();
                    h.MAIN_("uk-card-body")._MAIN();
                    h._SECTION();
                });
            }
        }
    }
}