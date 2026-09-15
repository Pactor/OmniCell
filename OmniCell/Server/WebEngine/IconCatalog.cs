namespace WebEngine
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Compression;
    using System.Text;
    using System.Text.Json;
    using System.Text.RegularExpressions;
    using System.Threading;

    using AssetDecoder;
    using AssetDecoder.Icons;
    using AssetDecoder.Rdb;

    /// <summary>
    /// Every item icon in the classic client, read once from its ResourceDatabase, with the items
    /// (from SqlTables\itemnames.sql) that use each one.
    /// </summary>
    /// <remarks>
    /// Loading takes a few seconds, so it runs on a background thread started with WebEngine, and
    /// <see cref="StatusJson"/> reports how far it has got so the page can show a progress bar.
    /// The client install comes from AO_CLIENT in paths.cfg; no MySQL or other engine is needed.
    /// </remarks>
    public static class IconCatalog
    {
        public const int IconRecordType = 1010008;

        private static readonly object Gate = new object();

        private static Thread loader;

        private static Dictionary<int, byte[]> images;

        private static volatile string stage = "Not started";

        private static volatile int done;

        private static volatile int total;

        private static volatile string error;

        private static volatile Catalog ready;

        public sealed class Catalog
        {
            public byte[] Json;

            public byte[] Gzip;
        }

        /// <summary>The finished catalog, or null while it is still loading.</summary>
        public static Catalog Ready
        {
            get { return ready; }
        }

        public static void StartLoading()
        {
            lock (Gate)
            {
                if (loader != null)
                {
                    return;
                }

                loader = new Thread(Load);
                loader.IsBackground = true;
                loader.Name = "Icon catalog";
                loader.Start();
            }
        }

        /// <summary>An icon ready for a browser (PNG with transparency; one icon is a JPEG), or null.</summary>
        public static byte[] Image(int iconId)
        {
            Dictionary<int, byte[]> loaded = images;
            byte[] image;
            return loaded != null && loaded.TryGetValue(iconId, out image) ? image : null;
        }

        public static byte[] StatusJson()
        {
            Catalog catalog = ready;
            using (MemoryStream buffer = new MemoryStream())
            using (Utf8JsonWriter json = new Utf8JsonWriter(buffer))
            {
                json.WriteStartObject();
                json.WriteString("stage", stage);
                json.WriteNumber("done", done);
                json.WriteNumber("total", total);
                json.WriteBoolean("ready", catalog != null);
                json.WriteNumber("catalogBytes", catalog != null ? catalog.Json.Length : 0);
                if (error != null)
                {
                    json.WriteString("error", error);
                }

                json.WriteEndObject();
                json.Flush();
                return buffer.ToArray();
            }
        }

        private static void Load()
        {
            try
            {
                stage = "Opening the client's resource database";
                string client = ClientPaths.AoClient();
                using (ResourceDatabase database = new ResourceDatabase(Path.Combine(client, "cd_image", "data", "db")))
                {
                    List<int[]> icons = ReadIcons(database);
                    List<ItemRow> items = ReadItemNames(database);

                    stage = "Building the catalog";
                    done = 0;
                    total = 0;
                    byte[] json = BuildJson(icons, items);
                    ready = new Catalog { Json = json, Gzip = Compress(json) };
                }

                stage = "Ready";
            }
            catch (Exception ex)
            {
                error = ex.Message;
                stage = "Failed";
            }
        }

        private static List<int[]> ReadIcons(ResourceDatabase database)
        {
            List<int> instances = new List<int>(database.Instances(IconRecordType));
            Dictionary<int, byte[]> decoded = new Dictionary<int, byte[]>(instances.Count);
            List<int[]> icons = new List<int[]>(instances.Count);

            stage = "Reading icons";
            done = 0;
            total = instances.Count;
            foreach (int id in instances)
            {
                byte[] record = database.Read(IconRecordType, id);
                int width, height;
                IconImage.TryGetSize(record, out width, out height);
                decoded[id] = IconImage.ToTransparentPng(record);
                icons.Add(new[] { id, width, height });
                done++;
            }

            images = decoded;
            return icons;
        }

        private struct ItemRow
        {
            public int Id;

            public string Name;

            public bool Nano;

            public int Icon;
        }

        private static List<ItemRow> ReadItemNames(ResourceDatabase database)
        {
            stage = "Reading item names";
            done = 0;
            total = 0;

            List<ItemRow> items = new List<ItemRow>();
            string path = Path.Combine(AppContext.BaseDirectory, "SqlTables", "itemnames.sql");
            if (!File.Exists(path))
            {
                // Icons still browse by ID without names; the page says names are missing.
                return items;
            }

            string text = ReadSqlText(path);
            total = text.Length;
            Regex row = new Regex(@"\(\s*(\d+)\s*,\s*'((?:[^'\\]|''|\\.)*)'\s*,\s*'([^']*)'\s*,\s*'(\d+)'\s*\)");
            for (Match match = row.Match(text); match.Success; match = match.NextMatch())
            {
                int icon = int.Parse(match.Groups[4].Value);
                if (icon != 0 && database.Contains(IconRecordType, icon))
                {
                    items.Add(
                        new ItemRow
                        {
                            Id = int.Parse(match.Groups[1].Value),
                            Name = match.Groups[2].Value.Replace("''", "'").Replace("\\'", "'"),
                            Nano = match.Groups[3].Value == "Nano",
                            Icon = icon,
                        });
                }

                done = match.Index;
            }

            done = total;
            return items;
        }

        // Compact arrays keep the catalog small: icons are [id, width, height] and
        // items are [id, name, icon, 1 for a nano else 0], ordered by item ID.
        private static byte[] BuildJson(List<int[]> icons, List<ItemRow> items)
        {
            items.Sort((a, b) => a.Id.CompareTo(b.Id));
            using (MemoryStream buffer = new MemoryStream())
            using (Utf8JsonWriter json = new Utf8JsonWriter(buffer))
            {
                json.WriteStartObject();
                json.WriteBoolean("hasNames", items.Count > 0);
                json.WriteStartArray("icons");
                foreach (int[] icon in icons)
                {
                    json.WriteStartArray();
                    json.WriteNumberValue(icon[0]);
                    json.WriteNumberValue(icon[1]);
                    json.WriteNumberValue(icon[2]);
                    json.WriteEndArray();
                }

                json.WriteEndArray();
                json.WriteStartArray("items");
                foreach (ItemRow item in items)
                {
                    json.WriteStartArray();
                    json.WriteNumberValue(item.Id);
                    json.WriteStringValue(item.Name);
                    json.WriteNumberValue(item.Icon);
                    json.WriteNumberValue(item.Nano ? 1 : 0);
                    json.WriteEndArray();
                }

                json.WriteEndArray();
                json.WriteEndObject();
                json.Flush();
                return buffer.ToArray();
            }
        }

        private static byte[] Compress(byte[] data)
        {
            using (MemoryStream buffer = new MemoryStream())
            {
                using (GZipStream gzip = new GZipStream(buffer, CompressionLevel.Optimal, true))
                {
                    gzip.Write(data, 0, data.Length);
                }

                return buffer.ToArray();
            }
        }

        // Same rule as OmniCell.Database: repository copies are UTF-8, a freshly extracted one is 1252.
        private static string ReadSqlText(string path)
        {
            byte[] bytes = File.ReadAllBytes(path);
            try
            {
                return new UTF8Encoding(false, true).GetString(bytes).TrimStart('﻿');
            }
            catch (DecoderFallbackException)
            {
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                return Encoding.GetEncoding(1252).GetString(bytes);
            }
        }
    }
}
