using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LocresLib.IO
{
    public static class BinaryWriterExtensions
    {
        internal static bool IsAsciiString(string value)
        {
            return value.All(ch => ch < 128);
        }

        internal static void WriteUnrealString(this BinaryWriter writer, string value, bool forceUnicode = false)
        {
            value += "\0";

            if (!forceUnicode && IsAsciiString(value)) // ASCII
            {
                var data = Encoding.ASCII.GetBytes(value);
                writer.Write(data.Length);
                writer.Write(data);
            }
            else // UTF-16-LE
            {
                var data = Encoding.Unicode.GetBytes(value);
                writer.Write(value.Length * -1);
                writer.Write(data);
            }
        }
    }
}
