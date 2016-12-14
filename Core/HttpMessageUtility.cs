﻿using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Greatbone.Core
{
    ///
    /// A lot of convenient method for request/response operations.
    ///
    public static class HttpMessageUtility
    {

        public static void Set(this HttpRequestMessage msg, Obj obj)
        {
            JsonContent cont = new JsonContent(true, true);
            obj.Dump(cont);
            msg.Content = cont;
        }

        public static void Set(this HttpRequestMessage msg, Arr obj)
        {
            JsonContent cont = new JsonContent(true, true);
            obj.Dump(cont);
            msg.Content = cont;
        }

        public static void Set<D>(this HttpRequestMessage msg, D dat) where D : IDat
        {
            JsonContent cont = new JsonContent(true, true);
            cont.Put(null, dat);
            msg.Content = cont;
        }

        public static void Set<D>(this HttpRequestMessage msg, D[] dats) where D : IDat
        {
            JsonContent cont = new JsonContent(true, true);
            cont.Put(null, dats);
            msg.Content = cont;
        }

        //
        // response
        //

        public static int Status(this HttpResponseMessage msg) => (int)msg.StatusCode;


        public static async Task<Obj> ToObjAsync(this HttpResponseMessage msg)
        {
            byte[] bytes = await msg.Content.ReadAsByteArrayAsync();
            JsonParse p = new JsonParse(bytes, bytes.Length);
            return (Obj)p.Parse();
        }

        public static async Task<Arr> ToArrAsync(this HttpResponseMessage msg)
        {
            byte[] bytes = await msg.Content.ReadAsByteArrayAsync();
            JsonParse p = new JsonParse(bytes, bytes.Length);
            return (Arr)p.Parse();
        }

        public static async Task<Elem> ToElemAsync(this HttpResponseMessage msg)
        {
            byte[] bytes = await msg.Content.ReadAsByteArrayAsync();
            XmlParse p = new XmlParse(bytes, bytes.Length);
            return (Elem)p.Parse();
        }

        public static async Task<D> ToDatAsync<D>(this HttpResponseMessage msg, byte z = 0) where D : IDat, new()
        {
            byte[] bytes = await msg.Content.ReadAsByteArrayAsync();
            JsonParse p = new JsonParse(bytes, bytes.Length);
            Obj obj = (Obj)p.Parse();
            return obj.ToDat<D>(z);
        }

        public static async Task<D[]> ToDatsAsync<D>(this HttpResponseMessage msg, byte z = 0) where D : IDat, new()
        {
            byte[] bytes = await msg.Content.ReadAsByteArrayAsync();
            JsonParse p = new JsonParse(bytes, bytes.Length);
            Arr arr = (Arr)p.Parse();
            return arr.ToDats<D>(z);
        }

        public static async Task<byte[]> ToBytesAsync(this HttpResponseMessage msg)
        {
            return await msg.Content.ReadAsByteArrayAsync();
        }
    }
}