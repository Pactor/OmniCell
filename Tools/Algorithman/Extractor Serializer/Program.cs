#region License

// Copyright (c) 2005-2013, CellAO Team
// 
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without modification, are permitted provided that the following conditions are met:
// 
//     * Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.
//     * Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the documentation and/or other materials provided with the distribution.
//     * Neither the name of the CellAO Team nor the names of its contributors may be used to endorse or promote products derived from this software without specific prior written permission.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
// "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
// LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
// A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
// CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL,
// EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO,
// PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR
// PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF
// LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
// NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

#endregion

#region License

// Copyright (c) 2005-2012, CellAO Team
// All rights reserved.
// Redistribution and use in source and binary forms, with or without modification, are permitted provided that the following conditions are met:
//     * Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.
//     * Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the documentation and/or other materials provided with the distribution.
//     * Neither the name of the CellAO Team nor the names of its contributors may be used to endorse or promote products derived from this software without specific prior written permission.
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
// "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
// LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
// A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
// CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL,
// EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO,
// PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR
// PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF
// LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
// NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

#endregion

#region License

// Copyright (c) 2005-2012, CellAO Team
// All rights reserved.
// Redistribution and use in source and binary forms, with or without modification, are permitted provided that the following conditions are met:
//     * Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.
//     * Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the documentation and/or other materials provided with the distribution.
//     * Neither the name of the CellAO Team nor the names of its contributors may be used to endorse or promote products derived from this software without specific prior written permission.
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
// "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
// LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
// A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
// CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL,
// EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO,
// PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR
// PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF
// LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
// NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

#endregion

namespace Extractor_Serializer
{
    #region Usings ...

    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Drawing.Imaging;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Mime;
    using System.Text;
    using System.Text.RegularExpressions;

    using OmniCell.Core.Content;
    using OmniCell.Core.Functions;
    using OmniCell.Core.Items;
    using OmniCell.Core.Nanos;
    using OmniCell.Core.Playfields;

    using Utility;

    #endregion

    /// <summary>
    /// The program.
    /// </summary>
    internal class Program
    {
        #region Constants

        /// <summary>
        /// </summary>
        private const int CopyStreamBufferLength = 1 * 1024 * 1024; // 8 MB

        #endregion

        #region Static Fields

        /// <summary>
        /// </summary>
        public static List<List<int>> Relations = new List<List<int>>(100000);

        /// <summary>
        /// </summary>
        public static Regex reg = new Regex(@".*\/item\/([0-9]*)\/.*");

        /// <summary>
        /// </summary>
        public static WebClient webClient = new WebClient();

        /// <summary>
        /// The ext.
        /// </summary>
        private static Extractor extractor;

        /// <summary>
        /// </summary>
        private static Dictionary<int, ItemTemplate> rawItemDictionary = new Dictionary<int, ItemTemplate>(130000);

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// The copy stream.
        /// </summary>
        /// <param name="input">
        /// The input.
        /// </param>
        /// <param name="output">
        /// The output.
        /// </param>
        public static void CopyStream(Stream input, Stream output)
        {
            var buffer = new byte[CopyStreamBufferLength];
            int len;
            while ((len = input.Read(buffer, 0, CopyStreamBufferLength)) > 0)
            {
                output.Write(buffer, 0, len);
                Console.Write(
                    "\rCompressing " + Convert.ToInt32(Math.Floor((double)input.Position / input.Length * 100.0)) + "%");
            }

            output.Flush();
        }

        /// <summary>
        /// The GetData.
        /// </summary>
        /// <param name="path">
        /// The path.
        /// </param>
        /// <param name="recordtype">
        /// The recordtype.
        /// </param>
        public static void GetData(string path, Extractor.RecordType recordtype)
        {
            int[] items = extractor.GetRecordInstances(recordtype);
            int cou = 0;
            foreach (int item in items)
            {
                try
                {

                    using (
                        var fileStream = new FileStream(Path.Combine(
                            path, item.ToString(CultureInfo.InvariantCulture)),
                            FileMode.Create,
                            FileAccess.Write))
                    {
                        byte[] data = extractor.GetRecordData(recordtype, item);

                        fileStream.Write(data, 0, data.Length);
                    }
                    if (cou % 10 == 0)
                    {
                        Console.WriteLine(item);
                    }

                    cou++;
                }
                catch (Exception)
                {
                }
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="path">
        /// </param>
        /// <returns>
        /// </returns>
        public static string GetVersion(string path)
        {
            string aopath = path;
            try
            {
                while (!File.Exists(Path.Combine(aopath, "version.id")))
                {
                    aopath = Path.Combine(aopath, "..");
                }
            }
            catch (Exception)
            {
                Console.WriteLine("File 'version.id' not found.");
                Console.WriteLine("Plese press <Enter> to exit.");
                Console.ReadLine();
                return string.Empty;
            }

            TextReader tr = new StreamReader(Path.Combine(aopath, "version.id"));

            string line = tr.ReadToEnd().Trim().Trim('\r').Trim('\n');
            tr.Close();
            return line;
        }

        /// <summary>
        /// </summary>
        public static void ReadItemRelations()
        {
            TextReader tr = new StreamReader("itemrelations.txt");
            string line;
            string lastline = null;
            while ((line = tr.ReadLine()) != null)
            {
                if ((line != lastline) && (!string.IsNullOrEmpty(line)))
                {
                    string[] rels = line.Split(' ');
                    List<int> temp = new List<int>();
                    foreach (string r in rels)
                    {
                        temp.Add(int.Parse(r));
                    }

                    Relations.Add(temp);
                }

                lastline = line;
            }

            tr.Close();
        }

        #endregion

        #region Methods

        /// <summary>
        /// </summary>
        /// <param name="ItemNamesSql">
        /// </param>
        private static void CompactingItemNamesSql(List<string> ItemNamesSql)
        {
            Console.WriteLine();
            Console.WriteLine("Compacting itemnames.sql");
            TextWriter itnsql = new StreamWriter("itemnames.sql", true, Encoding.GetEncoding("windows-1252"));
            StringBuilder bb = new StringBuilder(51);
            bool hasData = false;
            while (ItemNamesSql.Count > 0)
            {
                int count = 0;
                while ((count < 100) && (ItemNamesSql.Count > 0))
                {
                    if (hasData)
                    {
                        bb.Append(", ");
                    }

                    hasData = true;
                    bb.Append(ItemNamesSql[ItemNamesSql.Count - 1]);
                    ItemNamesSql.RemoveAt(ItemNamesSql.Count - 1);
                    count++;
                }

                if (hasData)
                {
                    itnsql.WriteLine("INSERT INTO itemnames VALUES " + bb.ToString() + ";");
                    bb.Clear();
                    hasData = false;
                }
            }

            itnsql.Close();
        }

        /// <summary>
        /// </summary>
        /// <returns>
        /// </returns>
        private static bool CopyDatafiles()
        {
            bool result = true;

            // Presume we are in OmniCell/Built/Debug or Release
            string pathToDatafiles = Path.Combine("..", "..", "Datafiles");
            if (File.Exists(Path.Combine(pathToDatafiles, "items.ocp")))
            {
                File.Delete(Path.Combine(pathToDatafiles, "items.ocp"));
            }

            File.Copy("items.ocp", Path.Combine(pathToDatafiles, "items.ocp"));

            if (File.Exists(Path.Combine(pathToDatafiles, "nanos.ocp")))
            {
                File.Delete(Path.Combine(pathToDatafiles, "nanos.ocp"));
            }

            File.Copy("nanos.ocp", Path.Combine(pathToDatafiles, "nanos.ocp"));

            if (File.Exists(Path.Combine(pathToDatafiles, "playfields.ocp")))
            {
                File.Delete(Path.Combine(pathToDatafiles, "playfields.ocp"));
            }

            File.Copy("playfields.ocp", Path.Combine(pathToDatafiles, "playfields.ocp"));

            pathToDatafiles = Path.Combine("..", "..", "Libraries", "Source", "OmniCell.Database", "SqlTables");
            if (File.Exists(Path.Combine(pathToDatafiles, "itemnames.sql")))
            {
                File.Delete(Path.Combine(pathToDatafiles, "itemnames.sql"));
            }

            File.Copy("itemnames.sql", Path.Combine(pathToDatafiles, "itemnames.sql"));

            return result;
        }

        /// <summary>
        /// </summary>
        /// <param name="itemNamesSqls">
        /// </param>
        /// <returns>
        /// </returns>
        private static List<ItemTemplate> ExtractItemTemplates(List<string> itemNamesSqls)
        {
            var np = new NewParser();
            int[] instances = extractor.GetRecordInstances(Extractor.RecordType.Item);
            List<ItemTemplate> rawItemList = new List<ItemTemplate>(instances.Length);
            rawItemDictionary = new Dictionary<int, ItemTemplate>(instances.Length);
            int counter = 0;
            foreach (int recnum in instances)
            {
                byte[] data = extractor.GetRecordData(Extractor.RecordType.Item, recnum);
                ItemTemplate xt = np.ParseItem(Extractor.RecordType.Item, recnum, data, itemNamesSqls);

                rawItemList.Add(xt);
                rawItemDictionary.Add(recnum, xt);

                if ((counter % 7500) == 0)
                {
                    Console.Write("\rItem ID: " + recnum.ToString().PadLeft(9));
                }

                counter++;
            }

            Console.Write("\rItem ID: " + rawItemList[rawItemList.Count - 1].ID.ToString().PadLeft(9));

            Console.WriteLine();
            return rawItemList;
        }

        /// <summary>
        /// </summary>
        /// <returns>
        /// </returns>
        private static List<PlayfieldData> ExtractPlayfieldData()
        {
            List<PlayfieldData> playfields = new List<PlayfieldData>(700);
            int[] instances = extractor.GetRecordInstances(Extractor.RecordType.Playfield);
            foreach (int recnum in instances)
            {
                if (recnum < 120)
                {
                    continue;
                }

                PlayfieldData pf = new PlayfieldData();
                pf.PlayfieldId = recnum;


                int[] doors = extractor.GetRecordInstances(Extractor.RecordType.Door);
                if (doors.Contains(recnum))
                {
                    byte[] doorData = extractor.GetRecordData(Extractor.RecordType.Door, recnum);
                    pf.Doors1 = PlayfieldParser.ParseDoors(doorData);
                }

                pf.Name = PlayfieldParser.ParseName(extractor.GetRecordData(Extractor.RecordType.Playfield, recnum));
                pf.Destinations = PlayfieldParser.ParseDestinations(extractor.GetRecordData(Extractor.RecordType.Playfield, recnum)).Destinations;
                /*Console.WriteLine("Parsing PF " + recnum+" "+pf.Name);
                if (recnum == 500)
                {
                    Console.ReadLine();
                }*/
                if (extractor.GetRecordInstances(Extractor.RecordType.Wall).Contains(recnum))
                {
                    pf.Walls = PlayfieldParser.ParseWalls(extractor.GetRecordData(Extractor.RecordType.Wall, recnum));
                }

                playfields.Add(pf);
            }

            return playfields;
        }

        /// <summary>
        /// </summary>
        /// <param name="playfields">
        /// </param>
        private static void ExtractPlayfieldStatels(List<PlayfieldData> playfields)
        {
            foreach (int recnum in extractor.GetRecordInstances(Extractor.RecordType.Statel)) // statels
            {
                Console.Write("Parsing Statels for playfield " + recnum + "\r");

                if (playfields.Any(x => x.PlayfieldId == recnum))
                {
                    playfields.First(x => x.PlayfieldId == recnum)
                        .Statels.AddRange(
                            PlayfieldParser.ParseStatels(
                                extractor.GetRecordData(Extractor.RecordType.Statel, recnum)
                                )

                        /* .Where(x => x.Events.Count > 0)*/

                        );
                }
            }
        }

        /// <summary>
        /// </summary>
        /// <returns>
        /// </returns>
        /// <summary>
        /// A data folder setting from paths.cfg in the repository root (see
        /// SETUP.md). An environment variable of the same name takes precedence.
        /// paths.cfg is found by walking up from this program's own folder, so
        /// it works wherever the repository was cloned.
        /// </summary>
        /// <returns>The value, or an empty string when it is not set.</returns>
        private static string ReadPathsSetting(string name)
        {
            string fromEnvironment = Environment.GetEnvironmentVariable(name);
            if (!string.IsNullOrWhiteSpace(fromEnvironment))
            {
                return fromEnvironment.Trim();
            }

            DirectoryInfo directory = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            while (directory != null)
            {
                string candidate = Path.Combine(directory.FullName, "paths.cfg");
                if (File.Exists(candidate))
                {
                    foreach (string line in File.ReadAllLines(candidate))
                    {
                        string trimmed = line.Trim();
                        if (trimmed.StartsWith('#') || !trimmed.Contains('='))
                        {
                            continue;
                        }

                        int equals = trimmed.IndexOf('=');
                        if (string.Equals(trimmed.Substring(0, equals).Trim(), name, StringComparison.OrdinalIgnoreCase))
                        {
                            return trimmed.Substring(equals + 1).Trim();
                        }
                    }

                    return string.Empty;
                }

                directory = directory.Parent;
            }

            return string.Empty;
        }

        private static string GetAOPath()
        {
            // AO_CLIENT from paths.cfg at the repository root (or the environment
            // variable of the same name) - the one place data folders are set.
            // See SETUP.md. Only when it is missing or wrong is the path asked for.
            string configured = ReadPathsSetting("AO_CLIENT");
            if (!string.IsNullOrEmpty(configured))
            {
                foreach (string candidate in new[] { configured, Path.Combine(configured, "cd_image", "data", "db") })
                {
                    try
                    {
                        extractor = new Extractor(candidate);
                        Console.WriteLine("Found AO Database on " + candidate + " (AO_CLIENT)");
                        return candidate;
                    }
                    catch (Exception)
                    {
                    }
                }

                Console.WriteLine("AO_CLIENT does not point at an Anarchy Online client install: " + configured);
            }

            string AOPath = string.Empty;
            bool foundAO = false;
            Console.WriteLine("Enter exit to close program");
            while (!foundAO)
            {
                foundAO = false;
                Console.Write("Please enter your AO Install Path [" + AOPath + "]:");
                string temp = Console.ReadLine();
                if (temp != string.Empty)
                {
                    AOPath = temp;
                }

                if (temp.ToLower() == "exit")
                {
                    return string.Empty;
                }

                if (!Directory.Exists(AOPath))
                {
                    continue;
                }

                try
                {
                    extractor = new Extractor(AOPath);
                    TextWriter tw2 = new StreamWriter("config.txt", false, Encoding.GetEncoding("windows-1252"));
                    tw2.WriteLine(AOPath);
                    tw2.Close();
                    foundAO = true;
                    Console.WriteLine("Found AO Database on " + AOPath);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    foundAO = false;
                }

                // Try to add cd_image\data\db
                if (!foundAO)
                {
                    try
                    {
                        AOPath = Path.Combine(AOPath, "cd_image", "data", "db");
                        extractor = new Extractor(AOPath);
                        TextWriter tw2 = new StreamWriter("config.txt", false, Encoding.GetEncoding("windows-1252"));
                        tw2.WriteLine(AOPath);
                        tw2.Close();
                        foundAO = true;
                        Console.WriteLine("Found AO Database on " + AOPath);
                    }
                    catch (Exception)
                    {
                        foundAO = false;
                    }
                }
            }

            return AOPath;
        }

        /// <summary>
        /// </summary>
        /// <param name="template">
        /// </param>
        private static void GetItemRelations(ItemTemplate template)
        {
            try
            {
                string html = webClient.DownloadString("http://www.aoitems.com/item/" + template.ID + "/");
                int pos;
                if ((pos = html.IndexOf("<select class=\"TemplateSelector\">", StringComparison.Ordinal)) != -1)
                {
                    // found template selector
                    // now narrow down to the links
                    html = html.Substring(pos + 33);
                    html = html.Substring(0, html.IndexOf("</select", StringComparison.Ordinal));
                    foreach (Match r in reg.Matches(html))
                    {
                        int id = int.Parse(r.Groups[1].Value);
                        template.Relations.Add(id);
                    }
                }
                else
                {
                    template.Relations.Add(template.ID);
                }
            }
            catch (Exception)
            {
                template.Relations.Add(template.ID);
            }
        }

        /// <summary>
        /// The main.
        /// </summary>
        /// <param name="args">
        /// The args.
        /// </param>
        private static void Main(string[] args)
        {
            // FunctionSets.cfg, itemnames.sql and config.txt are windows-1252. .NET Framework has every
            // Windows code page built in; .NET has them only once this provider is registered, and
            // Encoding.GetEncoding("windows-1252") throws until then.
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            if ((args.Length > 0) && string.Equals(args[0], "--verify-parser", StringComparison.OrdinalIgnoreCase))
            {
                VerifyParser(args);
                return;
            }

            if ((args.Length > 0) && string.Equals(args[0], "--convert-caches", StringComparison.OrdinalIgnoreCase))
            {
                ConvertLegacyCaches(args);
                return;
            }

            OnScreenBanner.PrintBanner(ConsoleColor.White);

            Console.WriteLine();

            string AOPath = GetAOPath();
            if (AOPath == string.Empty)
            {
                // Exit
                return;
            }

            Console.WriteLine();
            Console.WriteLine("Loading item relations...");
            ReadItemRelations();

            PrepareItemNamesSQL();

            Console.WriteLine("Number of Items to extract: " + extractor.GetRecordInstances(Extractor.RecordType.Item).Length);

            // ITEM RECORD TYPE
            Console.WriteLine("Number of Nanos to extract: " + extractor.GetRecordInstances(Extractor.RecordType.Nano).Length);
            Console.WriteLine();

            // NANO RECORD TYPE

            // Console.WriteLine(extractor.GetRecordInstances(0xF4241).Length); // Playfields
            // Console.WriteLine(extractor.GetRecordInstances(0xF4266).Length); // Nano Strains
            // Console.WriteLine(extractor.GetRecordInstances(0xF4264).Length); // Perks

            // GetData(@"D:\c#\extractor serializer\data\items\",0xf4254);
            // GetData(@"D:\c#\extractor serializer\data\nanos\",0xfde85);
            // GetData(@"D:\c#\extractor serializer\data\playfields\",0xf4241);
            // GetData(@"D:\c#\extractor serializer\data\nanostrains\",0xf4266);
            // GetData(@"D:\c#\extractor serializer\data\perks\",0xf4264);

            Console.WriteLine();
            // Shared by nanos and items so both end up in itemnames.sql.
            List<string> ItemNamesSql = new List<string>(extractor.GetRecordInstanceCount(0xF4254));

            List<NanoFormula> rawNanoList = ReadNanoFormulas(ItemNamesSql);
            Console.WriteLine();
            Console.WriteLine("Nanos extracted: " + rawNanoList.Count);
            Console.WriteLine();

            List<ItemTemplate> rawItemList = ExtractItemTemplates(ItemNamesSql);

            Console.WriteLine("Items extracted: " + rawItemList.Count);

            SetItemRelations(rawItemList);

            CompactingItemNamesSql(ItemNamesSql);

            // SerializationContext.Default.Serializers.Register(new AOFunctionArgumentsSerializer());
            Console.WriteLine();
            Console.WriteLine("Items extracted: " + rawItemList.Count);

            Console.WriteLine();
            Console.WriteLine("Creating serialized nano data file - please wait");

            string version = GetVersion(AOPath);
            OmniCellContentPack.WriteNanos("nanos.ocp", rawNanoList);

            Console.WriteLine();
            Console.WriteLine("Checking Nanos...");
            Console.WriteLine();
            NanoLoader.CacheAllNanos("nanos.ocp");
            Console.WriteLine();
            Console.WriteLine("Nanos: " + NanoLoader.NanoList.Count + " successfully converted");

            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine("Creating serialized item data file - please wait");

            OmniCellContentPack.WriteItems("items.ocp", rawItemList);

            Console.WriteLine();
            Console.WriteLine("Checking Items...");
            Console.WriteLine();

            ItemLoader.CacheAllItems("items.ocp");

            Console.WriteLine();
            Console.WriteLine("Items: " + ItemLoader.ItemList.Count + " successfully converted");

            Console.WriteLine("Extracting playfield walls/destinations/statels");
            List<PlayfieldData> playfields = ExtractPlayfieldData();
            ExtractPlayfieldStatels(playfields);
            Console.WriteLine();
            Console.WriteLine("Compressing playfield data...");
            OmniCellContentPack.WritePlayfields("playfields.ocp", playfields);
            Console.WriteLine();

            Console.WriteLine();
            Console.WriteLine("Further Instructions:");
            Console.WriteLine(
                "- Copy items.ocp, nanos.ocp and playfields.ocp into your OmniCell/Datafiles folder and overwrite.");
            Console.WriteLine("- Apply itemnames.sql to your database");
            Console.WriteLine();
            Console.WriteLine("   OR   ");
            Console.WriteLine();
            Console.WriteLine("Let me copy it over to the Source Tree");
            Console.WriteLine();
            while (true)
            {
                Console.WriteLine("Please choose:");
                Console.WriteLine("1: Copy the files to OmniCell/Datafiles and OmniCell/.../OmniCell.Database/SqlTables.");
                Console.WriteLine("2: Exit and copy yourself");
                Console.WriteLine("[1,2]:");
                string line = Console.ReadLine();
                if (line.Trim() == "1")
                {
                    if (CopyDatafiles())
                    {
                        break;
                    }
                }

                if (line.Trim() == "2")
                {
                    break;
                }
            }

            while (true)
            {
                Console.WriteLine("Do you want to extract the icons for WebCore? [Y/N]");
                string line = Console.ReadLine();
                if (line.Trim().ToLower() == "y")
                {
                    ExtractIcons();
                    break;
                }
                if (line.Trim().ToLower() == "n")
                {
                    break;
                }
            }

            Console.WriteLine("Press a key to exit.");
            Console.ReadLine();
        }

        /// <summary>
        /// Parses every item and nano without writing files or entering the
        /// interactive extractor workflow. This is safe for automated parser
        /// validation against a specific client database.
        /// </summary>
        private static void VerifyParser(string[] args)
        {
            if (args.Length != 2)
            {
                Console.Error.WriteLine("Usage: Extractor Serializer.exe --verify-parser <rdb-directory>");
                Environment.ExitCode = 2;
                return;
            }

            try
            {
                string rdbDirectory = Path.GetFullPath(args[1]);
                int itemCount = 0;
                int nanoCount = 0;
                int bareFunctionCount = 0;
                var itemSamples = new List<ItemTemplate>();
                var nanoSamples = new List<NanoFormula>();
                using (var source = new Extractor(rdbDirectory))
                {
                    var parser = new NewParser();
                    foreach (int recordId in source.GetRecordInstances(Extractor.RecordType.Item))
                    {
                        ItemTemplate item = parser.ParseItem(
                            Extractor.RecordType.Item,
                            recordId,
                            source.GetRecordData(Extractor.RecordType.Item, recordId),
                            null);
                        bareFunctionCount += item.Record.BareFunctions.Count;
                        if (itemSamples.Count == 0 ||
                            (item.Record.BareFunctions.Count > 0 && !itemSamples.Any(x => x.Record.BareFunctions.Count > 0)))
                        {
                            itemSamples.Add(item);
                        }
                        itemCount++;
                    }

                    foreach (int recordId in source.GetRecordInstances(Extractor.RecordType.Nano))
                    {
                        NanoFormula nano = parser.ParseNano(recordId, source.GetRecordData(Extractor.RecordType.Nano, recordId), null);
                        bareFunctionCount += nano.Record.BareFunctions.Count;
                        if (nanoSamples.Count == 0 ||
                            (nano.Record.BareFunctions.Count > 0 && !nanoSamples.Any(x => x.Record.BareFunctions.Count > 0)))
                        {
                            nanoSamples.Add(nano);
                        }
                        nanoCount++;
                    }
                }

                VerifyParserPackRoundTrip(itemSamples, nanoSamples);
                Console.WriteLine(
                    "Parser verified {0} items and {1} nanos ({2} bare functions); v3 content-pack round trip passed.",
                    itemCount,
                    nanoCount,
                    bareFunctionCount);
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine("Parser verification failed: " + exception.Message);
                Environment.ExitCode = 1;
            }
        }

        private static void VerifyParserPackRoundTrip(List<ItemTemplate> items, List<NanoFormula> nanos)
        {
            // The current 18.8.62 client has no bare body functions, so include
            // one synthetic value to exercise the version 3-only pack field.
            if (items.Count > 0 && !items.Any(x => x.Record.BareFunctions.Count > 0))
            {
                items[0].Record.BareFunctions.Add(new Function
                {
                    FunctionType = 53000,
                    Target = 2,
                    TickCount = 1,
                    TickInterval = 0,
                    Record = new FunctionRecordData
                    {
                        LeadingZeroWords = 1,
                        Header1 = 0,
                        Header2 = 4,
                        Header3 = 0,
                        Arguments = new byte[] { 1, 2, 3, 4 }
                    }
                });
            }

            string stem = Path.Combine(Path.GetTempPath(), "omnicell-parser-" + Guid.NewGuid().ToString("N"));
            string itemFile = stem + "-items.ocp";
            string nanoFile = stem + "-nanos.ocp";
            try
            {
                OmniCellContentPack.WriteItems(itemFile, items);
                OmniCellContentPack.WriteNanos(nanoFile, nanos);
                VerifyCanonicalRoundTrip(itemFile, path => OmniCellContentPack.WriteItems(path, OmniCellContentPack.ReadItems(itemFile)));
                VerifyCanonicalRoundTrip(nanoFile, path => OmniCellContentPack.WriteNanos(path, OmniCellContentPack.ReadNanos(nanoFile)));
            }
            finally
            {
                if (File.Exists(itemFile)) File.Delete(itemFile);
                if (File.Exists(nanoFile)) File.Delete(nanoFile);
            }
        }

        /// <summary>
        /// One-time migration from the extractor's intermediate object caches to
        /// OmniCell's canonical content packs. The source directory must be kept
        /// outside the repository after a successful conversion.
        /// </summary>
        private static void ConvertLegacyCaches(string[] args)
        {
            if (args.Length != 3)
            {
                Console.Error.WriteLine("Usage: Extractor Serializer.exe --convert-caches <legacy-directory> <content-directory>");
                Environment.ExitCode = 2;
                return;
            }

            string sourceDirectory = Path.GetFullPath(args[1]);
            string outputDirectory = Path.GetFullPath(args[2]);
            string itemsSource = Path.Combine(sourceDirectory, "items.dat");
            string nanosSource = Path.Combine(sourceDirectory, "nanos.dat");
            string playfieldsSource = Path.Combine(sourceDirectory, "playfields.dat");

            foreach (string source in new[] { itemsSource, nanosSource, playfieldsSource })
            {
                if (!File.Exists(source)) throw new FileNotFoundException("Required legacy cache was not found", source);
            }

            Directory.CreateDirectory(outputDirectory);
            string itemsOutput = Path.Combine(outputDirectory, "items.ocp");
            string nanosOutput = Path.Combine(outputDirectory, "nanos.ocp");
            string playfieldsOutput = Path.Combine(outputDirectory, "playfields.ocp");

            List<ItemTemplate> items = MessagePackZip.UncompressData<ItemTemplate>(itemsSource);
            List<NanoFormula> nanos = MessagePackZip.UncompressData<NanoFormula>(nanosSource);
            List<PlayfieldData> playfields = MessagePackZip.UncompressData<PlayfieldData>(playfieldsSource);

            OmniCellContentPack.WriteItems(itemsOutput, items);
            OmniCellContentPack.WriteNanos(nanosOutput, nanos);
            OmniCellContentPack.WritePlayfields(playfieldsOutput, playfields);

            int itemCount = OmniCellContentPack.ReadItems(itemsOutput).Count;
            int nanoCount = OmniCellContentPack.ReadNanos(nanosOutput).Count;
            int playfieldCount = OmniCellContentPack.ReadPlayfields(playfieldsOutput).Count;
            if ((itemCount != items.Count) || (nanoCount != nanos.Count) || (playfieldCount != playfields.Count))
            {
                throw new InvalidDataException("Content-pack round-trip record counts do not match the source caches.");
            }

            VerifyCanonicalRoundTrip(itemsOutput, path => OmniCellContentPack.WriteItems(path, OmniCellContentPack.ReadItems(itemsOutput)));
            VerifyCanonicalRoundTrip(nanosOutput, path => OmniCellContentPack.WriteNanos(path, OmniCellContentPack.ReadNanos(nanosOutput)));
            VerifyCanonicalRoundTrip(playfieldsOutput, path => OmniCellContentPack.WritePlayfields(path, OmniCellContentPack.ReadPlayfields(playfieldsOutput)));

            Console.WriteLine("Converted and verified {0} items, {1} nanos, and {2} playfields.", itemCount, nanoCount, playfieldCount);
        }

        private static void VerifyCanonicalRoundTrip(string canonicalFile, Action<string> rewrite)
        {
            string verificationFile = canonicalFile + ".verify";
            try
            {
                rewrite(verificationFile);
                if (!File.ReadAllBytes(canonicalFile).SequenceEqual(File.ReadAllBytes(verificationFile)))
                {
                    throw new InvalidDataException("Canonical round-trip changed content: " + canonicalFile);
                }
            }
            finally
            {
                if (File.Exists(verificationFile)) File.Delete(verificationFile);
            }
        }

        private static void ExtractIcons()
        {
            if (!Directory.Exists("icons"))
            {
                Directory.CreateDirectory("icons");
            }

            // Delete all pngs in that folder (makes conversion much faster)
            string[] filesToDelete = Directory.GetFiles("icons", "*.png", SearchOption.TopDirectoryOnly);
            foreach (string file in filesToDelete)
            {
                File.Delete(file);
            }

            int GCcount = 0;
            foreach (int inst in extractor.GetRecordInstances(Extractor.RecordType.Icon))
            // foreach (ItemTemplate template in ItemLoader.ItemList.Values)
            {
                string pngName = Path.Combine("icons", inst + ".png");
                if (!File.Exists(pngName))
                {
                    byte[] icon;
                    try
                    {
                        icon = extractor.GetRecordData(Extractor.RecordType.Icon, inst);
                    }
                    catch (Exception)
                    {
                        continue;
                    }

                    FileStream fs = new FileStream(
                        pngName,
                        FileMode.Create,
                        FileAccess.ReadWrite);
                    fs.Write(icon, 0, icon.Length);
                    fs.Close();

                    MakeTransparent(pngName);
                    GCcount++;
                    if (GCcount % 100 == 0)
                    {
                        GC.Collect();
                    }
                }
            }
        }

        private static void MakeTransparent(string p)
        {
            FileStream fs = new FileStream(p, FileMode.Open, FileAccess.Read);
            MemoryStream ms = new MemoryStream();
            fs.CopyTo(ms);
            ms.Position = 0;
            fs.Close();
            using (Image original = new Bitmap(ms))
            {
                using (Bitmap image = new Bitmap(original.Width, original.Height, PixelFormat.Format32bppArgb))
                {
                    Graphics g = Graphics.FromImage(image);
                    ImageAttributes ia = new ImageAttributes();
                    ia.SetColorKey(Color.FromArgb(0, 0xde, 0), Color.FromArgb(33, 255, 5));
                    g.DrawImage(
                        original,
                        new Rectangle(0, 0, original.Width, original.Height),
                        0,
                        0,
                        original.Width,
                        original.Height,
                        GraphicsUnit.Pixel,
                        ia);
                    image.Save(p, ImageFormat.Png);
                    g.Dispose();
                    ia.Dispose();
                }
            }
        }

        /// <summary>
        /// </summary>
        private static void PrepareItemNamesSQL()
        {
            TextWriter tw = new StreamWriter("itemnames.sql", false, Encoding.GetEncoding("windows-1252"));
            tw.WriteLine("DROP TABLE IF EXISTS `itemnames`;");
            tw.WriteLine("CREATE TABLE `itemnames` (");
            tw.WriteLine("  `Id` int(10) NOT NULL,");
            tw.WriteLine("  `Name` varchar(250) NOT NULL,");
            tw.WriteLine("  `ItemType` varchar(50) NOT NULL,");
            tw.WriteLine("  `Icon` varchar(20) NOT NULL,");
            tw.WriteLine("  PRIMARY KEY (`Id`)");
            tw.WriteLine(") ENGINE=MyIsam DEFAULT CHARSET=latin1;");
            tw.WriteLine();
            tw.Close();
        }

        /// <summary>
        /// </summary>
        /// <returns>
        /// </returns>
        private static List<NanoFormula> ReadNanoFormulas(List<string> itemNamesSqls)
        {
            var np = new NewParser();
            List<NanoFormula> rawNanoList = new List<NanoFormula>();
            int counter = 0;
            foreach (int recnum in extractor.GetRecordInstances(Extractor.RecordType.Nano))
            {
                if (counter == 0)
                {
                    counter = recnum;
                }

                byte[] data = extractor.GetRecordData(Extractor.RecordType.Nano, recnum);
                NanoFormula nano = np.ParseNano(recnum, data, itemNamesSqls);
                rawNanoList.Add(nano);
                if ((counter % 2000) == 0)
                {
                    Console.Write("\rNano ID: " + recnum.ToString().PadLeft(9));
                }

                counter++;
            }

            Console.Write("\rNano ID: " + rawNanoList[rawNanoList.Count - 1].ID.ToString().PadLeft(9));

            return rawNanoList;
        }

        /// <summary>
        /// </summary>
        /// <param name="rawItemList">
        /// </param>
        private static void SetItemRelations(List<ItemTemplate> rawItemList)
        {
            Dictionary<int, ItemTemplate> tp = new Dictionary<int, ItemTemplate>(150000);

            HashSet<ItemTemplate> hsitp = new HashSet<ItemTemplate>(rawItemList);

            Console.WriteLine("Setting item relations");

            foreach (ItemTemplate tep in rawItemList)
            {
                tp.Add(tep.ID, tep);
            }

            int perc = Relations.Count / 100;
            int counter = 0;
            int counter2 = 0;
            for (int pos = Relations.Count - 1; pos >= 0; pos--)
            {
                List<int> rels = Relations[pos];
                foreach (int id in rels)
                {
                    try
                    {
                        ItemTemplate temp = tp[id];

                        if (temp != null)
                        {
                            temp.Relations = rels;
                            hsitp.Remove(temp);
                        }
                    }
                    catch (Exception)
                    {
                        // throw;
                    }
                }
                if (perc > 0)
                {
                    if (counter % perc == 0)
                    {
                        Console.Write("\r" + counter2 + "% done");
                        counter2++;
                    }
                }

                counter++;
            }

            Console.WriteLine("\r100% done");
            if (hsitp.Count != 0)
            {
                foreach (ItemTemplate template in hsitp)
                {
                    GetItemRelations(template);
                    Console.Write("\rFound missing item relations for " + template.ID);
                }

                Console.WriteLine();
                Console.Write("Saving new itemrelations...");
                List<string> newItemrelations = new List<string>();
                foreach (ItemTemplate it in hsitp)
                {
                    string ir = string.Empty;
                    foreach (int i in it.Relations)
                    {
                        ir += ir == string.Empty ? i.ToString() : " " + i;
                    }

                    if (!newItemrelations.Contains(ir))
                    {
                        newItemrelations.Add(ir);
                    }
                }

                newItemrelations.Sort();

                TextWriter tw = new StreamWriter("itemrelations.txt", true);
                foreach (string s in newItemrelations)
                {
                    tw.WriteLine(s);
                }

                tw.Close();
                Console.WriteLine(" done");
            }

            Console.WriteLine();
        }

        #endregion
    }
}
