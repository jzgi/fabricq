﻿using System;
using System.Collections.Generic;

namespace Greatbone.Core
{
    ///
    /// Represents a provider of data entry or entries.
    ///
    public interface IDataInput
    {
        bool Get(string name, ref bool v);

        bool Get(string name, ref short v);

        bool Get(string name, ref int v);

        bool Get(string name, ref long v);

        bool Get(string name, ref double v);

        bool Get(string name, ref decimal v);

        bool Get(string name, ref DateTime v);

        bool Get(string name, ref string v);

        bool Get(string name, ref ArraySegment<byte> v);

        bool Get(string name, ref short[] v);

        bool Get(string name, ref int[] v);

        bool Get(string name, ref long[] v);

        bool Get(string name, ref string[] v);

        bool Get(string name, ref Dictionary<string, string> v);

        bool Get<D>(string name, ref D v, ushort proj = 0) where D : IData, new();

        bool Get<D>(string name, ref D[] v, ushort proj = 0) where D : IData, new();


        IDataInput Let(out bool v);

        IDataInput Let(out short v);

        IDataInput Let(out int v);

        IDataInput Let(out long v);

        IDataInput Let(out double v);

        IDataInput Let(out decimal v);

        IDataInput Let(out DateTime v);

        IDataInput Let(out string v);

        IDataInput Let(out ArraySegment<byte> v);

        IDataInput Let(out short[] v);

        IDataInput Let(out int[] v);

        IDataInput Let(out long[] v);

        IDataInput Let(out string[] v);

        IDataInput Let(out Dictionary<string, string> v);

        IDataInput Let<D>(out D v, ushort proj = 0) where D : IData, new();

        IDataInput Let<D>(out D[] v, ushort proj = 0) where D : IData, new();


        D ToData<D>(ushort proj = 0) where D : IData, new();

        D[] ToDatas<D>(ushort proj = 0) where D : IData, new();

        ///
        /// Write a single (or current) data entry into the given output object.
        ///
        void WriteData<R>(IDataOutput<R> o) where R : IDataOutput<R>;

        ///
        /// Dump as sendable content.
        ///
        DynamicContent Dump();

        ///
        /// If this includes multiple data entries.
        ///
        bool DataSet { get; }

        ///
        /// Move to next data entry.
        ///
        bool Next();
    }
}