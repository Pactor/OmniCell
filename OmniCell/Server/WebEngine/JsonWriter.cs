#region License

// Copyright (c) 2026, OmniCell contributors
//
// This file is part of OmniCell and is distributed under the terms the project
// is licensed under. See the repository root for details.

#endregion

namespace WebEngine
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Text;

    /// <summary>
    /// Writes the JSON the lookup endpoints answer with.
    /// </summary>
    /// <remarks>
    /// Replaces System.Web.Script.Serialization.JavaScriptSerializer, which is not
    /// part of .NET 10. For the values People builds - strings, numbers, null,
    /// string-keyed dictionaries, lists and arrays - it writes exactly what that
    /// serializer wrote, including its escaping of &lt; &gt; &amp; ' and the line
    /// separators U+0085, U+2028 and U+2029 as \uXXXX, so bots get the same bytes
    /// as before. Checked against JavaScriptSerializer for every UTF-16 code unit.
    /// One runtime difference remains: on .NET 10 a double such as an average level
    /// is written with the fewest digits that read back as the same value (0.3333333333333333
    /// rather than .NET Framework's 0.33333333333333331) - the same number to any JSON reader.
    /// </remarks>
    public static class JsonWriter
    {
        /// <summary>
        /// Writes a value as JSON.
        /// </summary>
        /// <exception cref="NotSupportedException">For a type the endpoints do not use.</exception>
        public static string Serialize(object value)
        {
            StringBuilder json = new StringBuilder();
            Write(json, value);
            return json.ToString();
        }

        private static void Write(StringBuilder json, object value)
        {
            if (value == null)
            {
                json.Append("null");
                return;
            }

            string text = value as string;
            if (text != null)
            {
                WriteString(json, text);
                return;
            }

            if (value is bool)
            {
                json.Append((bool)value ? "true" : "false");
                return;
            }

            if (value is double)
            {
                json.Append(((double)value).ToString("r", CultureInfo.InvariantCulture));
                return;
            }

            if (value is float)
            {
                json.Append(((float)value).ToString("r", CultureInfo.InvariantCulture));
                return;
            }

            if (value is int || value is long || value is short || value is sbyte || value is uint || value is ulong
                || value is ushort || value is byte || value is decimal)
            {
                json.Append(((IFormattable)value).ToString(null, CultureInfo.InvariantCulture));
                return;
            }

            IDictionary<string, object> dictionary = value as IDictionary<string, object>;
            if (dictionary != null)
            {
                json.Append('{');
                bool first = true;
                foreach (KeyValuePair<string, object> pair in dictionary)
                {
                    if (!first)
                    {
                        json.Append(',');
                    }

                    first = false;
                    WriteString(json, pair.Key);
                    json.Append(':');
                    Write(json, pair.Value);
                }

                json.Append('}');
                return;
            }

            IEnumerable items = value as IEnumerable;
            if (items != null)
            {
                json.Append('[');
                bool first = true;
                foreach (object item in items)
                {
                    if (!first)
                    {
                        json.Append(',');
                    }

                    first = false;
                    Write(json, item);
                }

                json.Append(']');
                return;
            }

            throw new NotSupportedException("JsonWriter does not write " + value.GetType().FullName);
        }

        private static void WriteString(StringBuilder json, string text)
        {
            json.Append('"');
            foreach (char c in text)
            {
                switch (c)
                {
                    case '"':
                        json.Append("\\\"");
                        break;
                    case '\\':
                        json.Append("\\\\");
                        break;
                    case '\b':
                        json.Append("\\b");
                        break;
                    case '\f':
                        json.Append("\\f");
                        break;
                    case '\n':
                        json.Append("\\n");
                        break;
                    case '\r':
                        json.Append("\\r");
                        break;
                    case '\t':
                        json.Append("\\t");
                        break;
                    case '\'':
                    case '<':
                    case '>':
                    case '&':
                    case (char)0x85:
                    case (char)0x2028:
                    case (char)0x2029:
                        AppendEscaped(json, c);
                        break;
                    default:
                        if (c < ' ')
                        {
                            AppendEscaped(json, c);
                        }
                        else
                        {
                            json.Append(c);
                        }

                        break;
                }
            }

            json.Append('"');
        }

        private static void AppendEscaped(StringBuilder json, char c)
        {
            json.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
        }
    }
}
