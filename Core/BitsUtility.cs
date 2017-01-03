﻿namespace Greatbone.Core
{
    ///
    /// The bits-flags that filter what to load or dump in persistance operations.
    ///
    public static class BitsUtility
    {
        public const byte

            // auto generated or with default
            AUTO = 0x80,

            // binary
            BINARY = 0x40,

            // late-handled
            LATER = 0x40,

            // many
            SUB = 0x18,

            // hidden or reserved
            KEPT = 0x04;


        public static bool Has(this byte bits, byte v)
        {
            return (bits & v) == v;
        }

        public static bool No(this byte bits, byte v)
        {
            return (bits & v) != v;
        }
    }
}