﻿using System.Threading.Tasks;
using Greatbone;
using static Greatbone.Modal;
using static Samp.Order;
using static Samp.User;
using static Greatbone.Style;

namespace Samp
{
    public abstract class OrderWork<V> : Work where V : OrderVarWork
    {
        protected OrderWork(WorkConfig cfg) : base(cfg)
        {
            CreateVar<V, int>((obj) => ((Order) obj).id);
        }

        // for customer side viewing
        protected void GiveBoardPage(WebContext wc, Order[] arr, bool tooling = true)
        {
            wc.GivePage(200, h =>
            {
                if (tooling)
                {
                    h.TOOLBAR(title: "购物车");
                }
                h.BOARD(arr, o =>
                    {
                        h.T("<header class=\"uk-card-header uk-flex uk-flex-middle\">");
                        h.T("<h5>").T(o.orgname).T("</h5>").BADGE(Statuses[o.status], o.status == 0 ? Warning : o.status == 1 ? Success : None);
                        h.T("</header>");

                        h.UL_("uk-list uk-list-divider uk-card-body");

                        h.LI("收　货", o.custaddr, o.custname, o.custtel);
                        h.LI_();
                        h.UL_("uk-list uk-list-divider uk-width-1-1");
                        for (int i = 0; i < o.items.Length; i++)
                        {
                            var oi = o.items[i];
                            h.LI_();
                            h.ICO_(w: 0x16).T('/').T(o.orgid).T('/').T(oi.name).T("/icon")._ICO();
                            if (o.status == CREATED)
                            {
                                h.P(oi.name, w: 0x13).P_(w: 0x13).CUR(oi.price).T("／").T(oi.unit)._P();
                                h.T("<p class=\"uk-width-1-6 \">").TOOL(nameof(MyOrderVarWork.Upd), i, oi.qty.ToString())._P();
                            }
                            else
                            {
                                h.P(oi.name, w: 0x12).P_(w: 0x16).CUR(oi.price)._P().P_(w: 0x16).T(oi.qty).T(oi.unit)._P();
                            }
                            h._LI();
                        }
                        h._UL();
                        h._LI();
                        h.LI_("总　额").CUR(o.total)._LI();
                        if (o.posid > 0)
                        {
                            h.P_("净额", w: 0x12).CUR(o.points)._P();
                        }
                        h._UL(); // uk-card-body

                        if (tooling) h.VARTOOLS(css: "uk-card-footer");
                    }
                );
            }, false, 2);
        }

        // for org side viewing
        protected void GiveAccordionPage(WebContext wc, Order[] arr, bool tools = true)
        {
            wc.GivePage(200, h =>
            {
                if (tools)
                {
                    h.TOOLBAR();
                }
                h.ACCORDION(arr,
                    o =>
                    {
                        h.T("<section class=\"uk-accordion-title\">");
                        h.T("<h4 class=\"uk-width-expand\">").T(o.custname).T("</h4>").BADGE(Statuses[o.status], o.status == 0 ? Warning : o.status == 1 ? Success : None);
                        h.T("</section>");

                        h.T("<section class=\"uk-accordion-content uk-grid\">");
                        h.P_("收　货").SP().T(o.custname).SP().T(o.custaddr).SP().T(o.custtel)._P();
                        h.UL_("uk-grid");
                        for (int i = 0; i < o.items.Length; i++)
                        {
                            var oi = o.items[i];
                            h.LI_();
                            h.SPAN_(0x11).T(oi.name)._SPAN().SPAN_(w: 0x23).T('￥').T(oi.price).T("／").T(oi.unit)._SPAN().SPAN(oi.qty, w: 0x13);
                            h._LI();
                        }
                        h._UL();
                        h.P_("总　额", w: 0x12).CUR(o.total)._P();

                        h.VARTOOLS();

                        h.T("</section>");
                    }, null);
            }, false, 2);
        }
    }

    public class MyOrderWork : OrderWork<MyOrderVarWork>
    {
        public MyOrderWork(WorkConfig cfg) : base(cfg)
        {
        }

        public void @default(WebContext wc)
        {
            int myid = wc[-1];
            using (var dc = NewDbContext())
            {
                var arr = dc.Query<Order>("SELECT * FROM orders WHERE status BETWEEN 0 AND 1 AND custid = @1 ORDER BY id DESC", p => p.Set(myid));
                GiveBoardPage(wc, arr);
            }
        }

        [Ui("历史订单"), Tool(AOpen, size: 2)]
        public void old(WebContext wc, int page)
        {
            int myid = wc[-1];
            using (var dc = NewDbContext())
            {
                var arr = dc.Query<Order>("SELECT * FROM orders WHERE status >= 2 AND custid = @1 ORDER BY id DESC", p => p.Set(myid));
                GiveBoardPage(wc, arr, false);
            }
        }
    }

    [Ui("新单")]
    public class OprNewoWork : OrderWork<OprNewoVarWork>
    {
        public OprNewoWork(WorkConfig cfg) : base(cfg)
        {
        }

        public void @default(WebContext wc)
        {
            string orgid = wc[-1];
            using (var dc = NewDbContext())
            {
                dc.Query("SELECT * FROM orders WHERE status BETWEEN 0 AND 1 AND orgid = @1 ORDER BY id DESC", p => p.Set(orgid));
                GiveAccordionPage(wc, dc.ToArray<Order>());
            }
        }

        static readonly Map<string, string> MSGS = new Map<string, string>
        {
            ["订单处理"] = "我们已经接到您的订单（金额{0}元）",
            ["派送通知"] = "销售人员正在派送您所购的商品",
            ["sdf"] = "",
        };

        [Ui("通知"), Tool(ButtonPickShow)]
        public void send(WebContext wc)
        {
            long[] key = wc.Query[nameof(key)];
            string msg = null;
            if (wc.GET)
            {
                wc.GivePane(200, m =>
                {
                    m.FORM_();
                    m.RADIOSET(nameof(msg), msg, MSGS, "消息通知买家", w: 0x4c);
                    m._FORM();
                });
            }
            else
            {
                using (var dc = NewDbContext())
                {
                    dc.Sql("SELECT wx FROM orders WHERE id")._IN_(key);
                    dc.Execute(prepare: false);
                }
                wc.GivePane(200);
            }
        }

        [Ui("清理", "清理三天以前未付款或者已撤销的订单"), Tool(ButtonConfirm)]
        public void clean(WebContext wc)
        {
            string orgid = wc[-1];
            using (var dc = NewDbContext())
            {
                dc.Execute("DELETE FROM orders WHERE (status = 0 OR status = -1) AND orgid = @1 AND (created + interval '3 day' < localtimestamp)", p => p.Set(orgid));
            }
            wc.GiveRedirect();
        }
    }

    [Ui("旧单"), UserAccess(OPR)]
    public class OprOldoWork : OrderWork<OprOldoVarWork>
    {
        public OprOldoWork(WorkConfig cfg) : base(cfg)
        {
        }

        public void @default(WebContext wc, int page)
        {
            string orgid = wc[-1];
            using (var dc = NewDbContext())
            {
                var arr = dc.Query<Order>("SELECT * FROM orders WHERE status >= 2 AND orgid = @1 ORDER BY id DESC LIMIT 20 OFFSET @2", p => p.Set(orgid).Set(page * 20));
                GiveAccordionPage(wc, arr);
            }
        }

        [Ui("查询"), Tool(AShow)]
        public void send(WebContext wc)
        {
            long[] key = wc.Query[nameof(key)];
            using (var dc = NewDbContext())
            {
                dc.Sql("UPDATE orders SET status = @1 WHERE id")._IN_(key);
                dc.Execute();
            }
            wc.GiveRedirect();
        }

        [Ui("回退", "【警告】把选中的订单回退成新单？"), Tool(ButtonPickConfirm)]
        public async Task back(WebContext wc)
        {
            string orgid = wc[-2];
            var f = await wc.ReadAsync<Form>();
            string[] key = f[nameof(key)];
            if (key != null)
            {
                using (var dc = NewDbContext())
                {
                    dc.Sql("UPDATE orders SET status = ").T(PAID).T(" WHERE status > ").T(PAID).T(" AND orgid = @1 AND id")._IN_(key);
                    dc.Execute(p => p.Set(orgid), prepare: false);
                }
            }
            wc.GiveRedirect();
        }
    }
}