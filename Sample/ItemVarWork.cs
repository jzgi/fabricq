using System;
using System.Threading.Tasks;
using Greatbone;
using static Greatbone.Modal;
using static Samp.User;

namespace Samp
{
    public abstract class ItemVarWork : Work
    {
        const int PICAGE = 60 * 60;

        protected ItemVarWork(WorkConfig cfg) : base(cfg)
        {
        }

        public void icon(WebContext wc)
        {
            string orgid = wc[typeof(IOrgVar)];
            string name = wc[this];
            using (var dc = NewDbContext())
            {
                if (dc.Query1("SELECT icon FROM items WHERE orgid = @1 AND name = @2", p => p.Set(orgid).Set(name)))
                {
                    dc.Let(out byte[] bytes);
                    if (bytes == null) wc.Give(204); // no content 
                    else wc.Give(200, new StaticContent(bytes), true, PICAGE);
                }
                else wc.Give(404, @public: true, maxage: PICAGE); // not found
            }
        }

        public void img(WebContext wc, int ordinal)
        {
            string orgid = wc[-1];
            string name = wc[this];
            using (var dc = NewDbContext())
            {
                if (dc.Query1("SELECT img" + ordinal + " FROM items WHERE orgid = @1 AND name = @2", p => p.Set(orgid).Set(name)))
                {
                    dc.Let(out byte[] bytes);
                    if (bytes == null) wc.Give(204); // no content 
                    else wc.Give(200, new StaticContent(bytes), true, PICAGE);
                }
                else wc.Give(404, @public: true, maxage: PICAGE); // not found
            }
        }
    }

    public class SampItemVarWork : ItemVarWork
    {
        public SampItemVarWork(WorkConfig cfg) : base(cfg)
        {
        }

        [UserAccess]
        [Ui("购买"), Tool(AOpen, size: 1), Item('A')]
        public async Task buy(WebContext wc)
        {
            User prin = (User) wc.Principal;
            string orgid = wc[-1];
            string itemname = wc[this];
            var org = Obtain<Map<string, Org>>()[orgid];
            var item = Obtain<Map<(string, string), Item>>()[(orgid, itemname)];
            short num;
            if (wc.GET)
            {
                int posid = wc.Query[nameof(posid)];
                wc.GivePane(200, h =>
                {
                    using (var dc = NewDbContext())
                    {
                        h.FORM_();
                        if (posid > 0)
                        {
                            h.HIDDEN(nameof(posid), posid);
                        }
                        if (dc.Scalar("SELECT 1 FROM orders WHERE status = 0 AND custwx = @1 AND orgid = @2", p => p.Set(prin.wx).Set(orgid)) == null) // to create new
                        {
                            // show addr inputs for order creation
                            h.FIELDSET_("创建订单，填写收货地址");
                            h.TEXT(nameof(Order.custaddr), prin.addr, "地　址", max: 20, required: true);
                            h.LI_().LABEL("姓　名").TEXT(nameof(Order.custname), prin.name, max: 4, min: 2, required: true).LABEL("电　话").TEL(nameof(Order.custtel), prin.tel, pattern: "[0-9]+", max: 11, min: 11, required: true)._LI();
                            h._FIELDSET();
                        }
                        // quantity
                        h.FIELDSET_("加入货品");
                        h.LI_("货　品").PIC("icon", w: 0x16).SP().T(item.name)._LI();
                        h.LI_("数　量").NUMBER(nameof(num), item.min, min: item.min, max: item.stock, step: item.step).T(item.unit)._LI();
                        h._FIELDSET();

                        h.BOTTOMBAR_().BUTTON("确定")._BOTTOMBAR();

                        h._FORM();
                    }
                });
            }
            else // POST
            {
                using (var dc = NewDbContext())
                {
                    // determine whether add to existing order or create new
                    if (dc.Query1("SELECT * FROM orders WHERE status = 0 AND custwx = @1 AND orgid = @2", p => p.Set(prin.wx).Set(orgid)))
                    {
                        var o = dc.ToObject<Order>();
                        (await wc.ReadAsync<Form>()).Let(out num);
                        o.AddItem(itemname, item.unit, item.price, item.comp, num);
                        dc.Execute("UPDATE orders SET rev = rev + 1, items = @1, total = @2, net = @3 WHERE id = @4", p => p.Set(o.items).Set(o.total).Set(o.points).Set(o.id));
                    }
                    else // create a new order
                    {
                        const byte proj = 0xff ^ Order.KEY ^ Order.LATER;
                        var f = await wc.ReadAsync<Form>();
                        string oprid = f[nameof(oprid)];
                        var o = new Order
                        {
                            orgid = orgid,
                            orgname = org.name,
                            custid = prin.id,
                            custname = prin.name,
                            custwx = prin.wx,
                            created = DateTime.Now
                        };
                        o.Read(f, proj);
                        num = f[nameof(num)];
                        o.AddItem(itemname, item.unit, item.price, item.comp, num);
                        dc.Sql("INSERT INTO orders ")._(o, proj)._VALUES_(o, proj);
                        dc.Execute(p => o.Write(p, proj));
                    }
                    wc.GivePane(200, m =>
                    {
                        m.MSG_(true, "成功加入购物车", "商品已经成功加入购物车");
                        m.BOTTOMBAR_().A_GOTO("去付款", "cart", href: "/my//ord/")._BOTTOMBAR();
                    });
                }
            }
        }
    }

    public class OprItemVarWork : ItemVarWork
    {
        public OprItemVarWork(WorkConfig cfg) : base(cfg)
        {
        }

        [Ui("修改"), Tool(ButtonShow, size: 2), UserAccess(OPRMEM)]
        public async Task upd(WebContext wc)
        {
            string orgid = wc[-2];
            string name = wc[this];
            if (wc.GET)
            {
                using (var dc = NewDbContext())
                {
                    var o = dc.Query1<Item>("SELECT * FROM items WHERE orgid = @1 AND name = @2", p => p.Set(orgid).Set(name));
                    wc.GivePane(200, h =>
                    {
                        h.FORM_();
                        h.FIELDSET_("填写货品信息");
                        h.STATIC(o.name, "名称");
                        h.TEXTAREA(nameof(o.descr), o.descr, "描述", min: 20, max: 50, required: true);
                        h.TEXT(nameof(o.unit), o.unit, "单位", required: true);
                        h.NUMBER(nameof(o.price), o.price, "单价", required: true);
                        h.NUMBER(nameof(o.comp), o.comp, "佣金", min: (decimal) 0.00, step: (decimal) 0.01);
                        h.NUMBER(nameof(o.min), o.min, "起订", min: (short) 1);
                        h.NUMBER(nameof(o.step), o.step, "增减", min: (short) 1);
                        h.SELECT(nameof(o.status), o.status, Item.Statuses, "状态");
                        h.NUMBER(nameof(o.stock), o.stock, "可供");
                        h._FIELDSET();
                        h._FORM();
                    });
                }
            }
            else // POST
            {
                const byte proj = 0xff ^ Item.PK;
                var o = await wc.ReadObjectAsync<Item>(proj);
                using (var dc = NewDbContext())
                {
                    dc.Sql("UPDATE items")._SET_(Item.Empty, proj).T(" WHERE orgid = @1 AND name = @2");
                    dc.Execute(p =>
                    {
                        o.Write(p, proj);
                        p.Set(orgid).Set(name);
                    });
                }
                wc.GivePane(200); // close
            }
        }

        [Ui("照片"), Tool(ButtonCrop, size: 2), UserAccess(OPRMEM)]
        public new async Task icon(WebContext wc)
        {
            string orgid = wc[-2];
            string name = wc[this];
            if (wc.GET)
            {
                using (var dc = NewDbContext())
                {
                    if (dc.Query1("SELECT icon FROM items WHERE orgid = @1 AND name = @2", p => p.Set(orgid).Set(name)))
                    {
                        dc.Let(out ArraySegment<byte> byteas);
                        if (byteas.Count == 0) wc.Give(204); // no content 
                        else wc.Give(200, new StaticContent(byteas));
                    }
                    else wc.Give(404); // not found           
                }
            }
            else // POST
            {
                var f = await wc.ReadAsync<Form>();
                ArraySegment<byte> img = f[nameof(img)];
                using (var dc = NewDbContext())
                {
                    if (dc.Execute("UPDATE items SET icon = @1 WHERE orgid = @2 AND name = @3", p => p.Set(img).Set(orgid).Set(name)) > 0)
                    {
                        wc.Give(200); // ok
                    }
                    else wc.Give(500); // internal server error
                }
            }
        }
    }
}