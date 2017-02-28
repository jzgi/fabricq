﻿using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using static Greatbone.Core.EventQueue;
using System.IO;

namespace Greatbone.Core
{
    ///
    /// A client to RPC, service and event queue.
    ///
    public class Client : HttpClient, IRollable
    {
        const int
            INITIAL = -1,
            TIME_OUT = 60,
            TIME_OUT_2 = 120,
            NO_CONTENT = 12,
            NOT_IMPLEMENTED = 720;

        readonly Service service;

        readonly string @event;

        // subdomain name or a reference name
        readonly string name;

        //
        // event polling & processing
        //

        bool connect;

        // last status
        bool status;

        // tick count
        int lastConnect;

        internal long lastid;

        // event last id
        FileStream eventid;

        public Client(string raddr) : this(null, null, raddr) { }

        public Client(Service service, string name, string raddr)
        {
            this.service = service;
            this.name = name;

            if (service != null) // build lastevent poll condition
            {
                Roll<EventInfo> eis = service.Events;
                if (eis != null)
                {
                    StringBuilder sb = new StringBuilder();
                    for (int i = 0; i < eis.Count; i++)
                    {
                        if (i > 0) sb.Append(',');
                        sb.Append(eis[i].Name);
                    }
                    @event = sb.ToString();
                }

                eventid = new FileStream(service.Context.GetFilePath(name), FileMode.OpenOrCreate, FileAccess.ReadWrite);
            }

            string addr = raddr.StartsWith("http") ? raddr : "http://" + raddr;
            BaseAddress = new Uri(addr);

        }

        public string Name => name;

        long ReadEventId()
        {
            byte[] buf = new byte[21];
            eventid.Read(buf, 0, buf.Length);
            return 0;
        }

        void WriteEventId(long id)
        {

        }


        public void ToPoll(int ticks)
        {
            if (lastConnect < 100)
            {
                return;
            }

            PollAndDoAsync();
        }

        static readonly Uri PollUri = new Uri("*", UriKind.Relative);

        /// NOTE: We make it async void because the scheduler doesn't need to await this method
        internal async void PollAndDoAsync()
        {
            for (;;)
            {
                HttpRequestMessage req = new HttpRequestMessage(HttpMethod.Get, PollUri);
                HttpRequestHeaders headers = req.Headers;
                headers.TryAddWithoutValidation("From", service.Moniker);
                headers.TryAddWithoutValidation(X_EVENT, @event);
                headers.TryAddWithoutValidation(X_SHARD, service.Context.shard);

                HttpResponseMessage resp = await SendAsync(req);
                if (resp.StatusCode == HttpStatusCode.NoContent)
                {
                    break;
                }

                byte[] cont = await resp.Content.ReadAsByteArrayAsync();
                EventContext ec = new EventContext(this);

                // parse and process one by one
                long id = 0;
                ec.name = resp.Headers.GetValue(X_EVENT);
                string arg = resp.Headers.GetValue(X_ARG);
                DateTime time;
                EventInfo ei = null;
                if (service.Events.TryGet(name, out ei))
                {
                    if (ei.IsAsync)
                    {
                        await ei.DoAsync(ec, arg);
                    }
                    else
                    {
                        ei.Do(ec, arg);
                    }
                }

                // database last id
                WriteEventId(id);
            }
        }

        internal void SetCancel()
        {
            status = false;
        }

        //
        // RPC
        //

        public async Task<byte[]> GetAsync(ActionContext ctx, string uri)
        {
            HttpRequestMessage req = new HttpRequestMessage(HttpMethod.Get, uri);
            req.Headers.Add("Authorization", "Bearer " + ctx.TokenText);
            HttpResponseMessage resp = await SendAsync(req, HttpCompletionOption.ResponseContentRead);
            return await resp.Content.ReadAsByteArrayAsync();
        }

        public async Task<M> GetAsync<M>(ActionContext ctx, string uri) where M : class, IDataInput
        {
            HttpRequestMessage req = new HttpRequestMessage(HttpMethod.Get, uri);
            if (ctx != null)
            {
                req.Headers.Add("Authorization", "Bearer " + ctx.TokenText);
            }
            HttpResponseMessage resp = await SendAsync(req, HttpCompletionOption.ResponseContentRead);
            byte[] bytea = await resp.Content.ReadAsByteArrayAsync();
            string ctyp = resp.Content.Headers.GetValue("Content-Type");
            return (M)WebUtility.ParseContent(ctyp, bytea, 0, bytea.Length);
        }

        public async Task<D> GetObjectAsync<D>(ActionContext ctx, string uri, int proj = 0) where D : IData, new()
        {
            HttpRequestMessage req = new HttpRequestMessage(HttpMethod.Get, uri);
            if (ctx != null)
            {
                req.Headers.Add("Authorization", "Bearer " + ctx.TokenText);
            }
            HttpResponseMessage resp = await SendAsync(req, HttpCompletionOption.ResponseContentRead);
            IDataInput src = null;
            return src.ToObject<D>(proj);
        }

        public async Task<D[]> GetArrayAsync<D>(ActionContext ctx, string uri, int proj = 0) where D : IData, new()
        {
            HttpRequestMessage req = new HttpRequestMessage(HttpMethod.Get, uri);
            if (ctx != null)
            {
                req.Headers.Add("Authorization", "Bearer " + ctx.TokenText);
            }
            HttpResponseMessage resp = await SendAsync(req, HttpCompletionOption.ResponseContentRead);

            IDataInput srcset = null;
            return srcset.ToArray<D>(proj);
        }

        public async Task<List<D>> GetListAsync<D>(ActionContext ctx, string uri, int proj = 0) where D : IData, new()
        {
            HttpRequestMessage req = new HttpRequestMessage(HttpMethod.Get, uri);
            if (ctx != null)
            {
                req.Headers.Add("Authorization", "Bearer " + ctx.TokenText);
            }
            HttpResponseMessage resp = await SendAsync(req, HttpCompletionOption.ResponseContentRead);

            IDataInput srcset = null;
            return srcset.ToList<D>(proj);
        }

        public Task<HttpResponseMessage> PostAsync<C>(ActionContext ctx, string uri, C content) where C : HttpContent, IContent
        {
            HttpRequestMessage req = new HttpRequestMessage(HttpMethod.Post, uri);
            if (ctx != null)
            {
                req.Headers.Add("Authorization", "Bearer " + ctx.TokenText);
            }
            req.Content = content;
            req.Headers.Add("Content-Type", content.Type);
            req.Headers.Add("Content-Length", content.Size.ToString());

            return SendAsync(req, HttpCompletionOption.ResponseContentRead);
        }

        public Task<HttpResponseMessage> PostAsync(ActionContext ctx, string uri, IDataInput inp)
        {
            HttpRequestMessage req = new HttpRequestMessage(HttpMethod.Post, uri);
            if (ctx != null)
            {
                req.Headers.Add("Authorization", "Bearer " + ctx.TokenText);
            }
            IContent cont = inp.Dump();
            req.Content = (HttpContent)cont;
            req.Content.Headers.ContentType.MediaType = cont.Type;
            req.Content.Headers.ContentLength = cont.Size;

            return SendAsync(req, HttpCompletionOption.ResponseContentRead);
        }


        public Task<HttpResponseMessage> PostJsonAsync(ActionContext ctx, string uri, object model)
        {
            HttpRequestMessage req = new HttpRequestMessage(HttpMethod.Post, uri);
            if (ctx != null)
            {
                req.Headers.Add("Authorization", "Bearer " + ctx.TokenText);
            }

            if (model is Form)
            {

            }
            else if (model is JObj)
            {
                JsonContent cont = new JsonContent(true, true);
                ((JObj)model).WriteData(cont);
                req.Content = cont;
            }
            else if (model is IData)
            {
                JsonContent cont = new JsonContent(true, true);
                ((JObj)model).WriteData(cont);
                req.Content = cont;
            }
            return SendAsync(req, HttpCompletionOption.ResponseContentRead);
        }
    }
}