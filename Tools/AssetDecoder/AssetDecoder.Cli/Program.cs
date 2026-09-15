using AssetDecoder.Rdb;

namespace AssetDecoder;

public static class Program
{
    private const int IconType = 1010008;

    public static int Main(string[] args)
    {
        string client = ClientPaths.AoClient();
        string dbDirectory = Path.Combine(client, "cd_image", "data", "db");
        using var db = new ResourceDatabase(dbDirectory);

        string command = args.Length > 0 ? args[0] : "types";
        switch (command)
        {
            case "types":
                foreach (int type in db.Types.Order())
                {
                    Console.WriteLine($"{type,10}  0x{type:X6}  {db.Count(type),7}");
                }

                return 0;

            case "survey-icons":
                SurveyIcons(db);
                return 0;

            case "instances":
                // instances <type> [count]: the lowest instance numbers of a record type.
                Console.WriteLine(string.Join(' ', db.Instances(int.Parse(args[1])).Take(args.Length > 2 ? int.Parse(args[2]) : 20)));
                return 0;

            case "icon":
                // icon <instance> <file>: the icon as a PNG with real transparency.
                File.WriteAllBytes(args[2], Icons.IconImage.ToTransparentPng(db.Read(IconType, int.Parse(args[1]))));
                return 0;

            case "icon-coverage":
                // icon-coverage <itemnames.sql>: how the items' Icon column lines up with icon records.
                IconCoverage(db, args[1]);
                return 0;

            case "raw":
                // raw <type> <instance> <file>: the record's payload, byte for byte.
                File.WriteAllBytes(args[3], db.Read(int.Parse(args[1]), int.Parse(args[2])));
                return 0;

            default:
                Console.Error.WriteLine("Commands: types, survey-icons, raw <type> <instance> <file>");
                return 1;
        }
    }

    private static void IconCoverage(ResourceDatabase db, string itemNamesSql)
    {
        var icons = db.Instances(IconType).ToHashSet();
        var rows = System.Text.RegularExpressions.Regex.Matches(
            File.ReadAllText(itemNamesSql), @"\(\s*(\d+)\s*,\s*'(?:[^']|'')*'\s*,\s*'[^']*'\s*,\s*'(\d+)'\s*\)");
        int items = 0, noIcon = 0, missing = 0, decodeFailures = 0;
        var referenced = new HashSet<int>();
        foreach (System.Text.RegularExpressions.Match row in rows)
        {
            items++;
            int icon = int.Parse(row.Groups[2].Value);
            if (icon == 0)
            {
                noIcon++;
            }
            else if (!icons.Contains(icon))
            {
                missing++;
            }
            else
            {
                referenced.Add(icon);
            }
        }

        int keyed = 0;
        foreach (int icon in icons)
        {
            try
            {
                Icons.IconImage.ToTransparentPng(db.Read(IconType, icon), out int keyedPixels);
                keyed += keyedPixels > 0 ? 1 : 0;
            }
            catch (Exception)
            {
                decodeFailures++;
            }
        }

        Console.WriteLine($"Icons with colour-keyed (transparent) pixels: {keyed}");

        Console.WriteLine($"Items: {items}  with no icon (0): {noIcon}  pointing at a missing icon: {missing}");
        Console.WriteLine($"Icon records: {icons.Count}  used by at least one item: {referenced.Count}  decode failures: {decodeFailures}");
    }

    // How many icon records there are, and what their bytes start with.
    private static void SurveyIcons(ResourceDatabase db)
    {
        var signatures = new Dictionary<string, int>();
        int failed = 0;
        foreach (int instance in db.Instances(IconType))
        {
            try
            {
                byte[] data = db.Read(IconType, instance);
                string signature = Convert.ToHexString(data, 0, Math.Min(8, data.Length));
                if (signature == "89504E470D0A1A0A")
                {
                    // IHDR is always the first chunk: width, height (big-endian), bit depth, colour type.
                    int width = (data[16] << 24) | (data[17] << 16) | (data[18] << 8) | data[19];
                    int height = (data[20] << 24) | (data[21] << 16) | (data[22] << 8) | data[23];
                    bool transparencyChunk = data.AsSpan().IndexOf("tRNS"u8) >= 0;
                    signature = $"PNG {width}x{height} depth {data[24]} colour {data[25]}{(transparencyChunk ? " tRNS" : "")}";
                }
                else
                {
                    signature += $" (instance {instance})";
                }

                signatures[signature] = signatures.GetValueOrDefault(signature) + 1;
            }
            catch (Exception)
            {
                failed++;
            }
        }

        Console.WriteLine($"Icon records: {db.Count(IconType)}  unreadable: {failed}");
        foreach (var (signature, count) in signatures.OrderByDescending(s => s.Value))
        {
            Console.WriteLine($"  {signature}  {count}");
        }
    }
}
