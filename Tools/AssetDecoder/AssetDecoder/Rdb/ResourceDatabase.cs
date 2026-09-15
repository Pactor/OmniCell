namespace AssetDecoder.Rdb;

/// <summary>
/// Read-only access to the classic client's ResourceDatabase: the .idx index plus the
/// ResourceDatabase.dat, .dat.001, ... data files, which are one logical stream split into parts.
/// </summary>
public sealed class ResourceDatabase : IDisposable
{
    // Every record in the data files starts with this many header bytes.
    private const int RecordHeaderSize = 34;

    private readonly List<FileStream> dataFiles = new();
    private readonly Dictionary<int, Dictionary<int, long>> offsets = new();
    private readonly long partSize;
    private readonly long partHeaderSize;

    public ResourceDatabase(string dbDirectory)
    {
        string indexPath = Path.Combine(dbDirectory, "ResourceDatabase.idx");
        if (!File.Exists(indexPath))
        {
            throw new FileNotFoundException("No ResourceDatabase.idx in " + dbDirectory, indexPath);
        }

        // .dat, then .dat.001, .dat.002, ... in order.
        foreach (string path in Directory.GetFiles(dbDirectory, "ResourceDatabase.dat*")
                     .Where(p => p.EndsWith(".dat", StringComparison.OrdinalIgnoreCase) || char.IsDigit(p[^1]))
                     .OrderBy(p => p, StringComparer.OrdinalIgnoreCase))
        {
            this.dataFiles.Add(OpenShared(path));
        }

        byte[] index;
        using (FileStream indexFile = OpenShared(indexPath))
        {
            index = new byte[indexFile.Length];
            indexFile.ReadExactly(index);
        }
        this.partHeaderSize = BitConverter.ToUInt32(index, 12);
        this.partSize = BitConverter.ToUInt32(index, 184);

        // The index is a chain of blocks. Each block: next-block offset, 4 unknown bytes,
        // entry count (int16), 18 unknown bytes, then 16-byte entries of
        // (data offset high/low, little-endian) and (type, instance, big-endian).
        uint block = BitConverter.ToUInt32(index, 72);
        uint next = BitConverter.ToUInt32(index, (int)block);
        while (next > 0)
        {
            int count = BitConverter.ToInt16(index, (int)block + 8);
            int entry = (int)block + 28;
            for (int i = 0; i < count; i++, entry += 16)
            {
                long offset = ((long)BitConverter.ToUInt32(index, entry) << 32) | BitConverter.ToUInt32(index, entry + 4);
                int type = ReadInt32BigEndian(index, entry + 8);
                int instance = ReadInt32BigEndian(index, entry + 12);
                if (!this.offsets.TryGetValue(type, out Dictionary<int, long>? instances))
                {
                    instances = new Dictionary<int, long>();
                    this.offsets.Add(type, instances);
                }

                instances[instance] = offset;
            }

            block = next;
            next = BitConverter.ToUInt32(index, (int)block);
        }
    }

    public IReadOnlyCollection<int> Types => this.offsets.Keys;

    public int Count(int type) => this.offsets.TryGetValue(type, out var instances) ? instances.Count : 0;

    public IEnumerable<int> Instances(int type) =>
        this.offsets.TryGetValue(type, out var instances) ? instances.Keys.Order() : Enumerable.Empty<int>();

    public bool Contains(int type, int instance) =>
        this.offsets.TryGetValue(type, out var instances) && instances.ContainsKey(instance);

    /// <summary>A record's payload. Safe to call from several threads at once.</summary>
    public byte[] Read(int type, int instance)
    {
        lock (this.dataFiles)
        {
            return this.ReadLocked(type, instance);
        }
    }

    private byte[] ReadLocked(int type, int instance)
    {
        long offset = this.offsets[type][instance];
        byte[] header = this.ReadAt(offset, RecordHeaderSize);
        int headerType = BitConverter.ToInt32(header, 10);
        int headerInstance = BitConverter.ToInt32(header, 14);
        if (headerType != type || headerInstance != instance)
        {
            throw new InvalidDataException(
                $"Record {type}:{instance} points at a header for {headerType}:{headerInstance}");
        }

        // The size field counts 12 header bytes that precede the payload.
        int length = BitConverter.ToInt32(header, 18) - 12;
        return this.ReadAt(offset + RecordHeaderSize, length);
    }

    public void Dispose()
    {
        foreach (FileStream file in this.dataFiles)
        {
            file.Dispose();
        }
    }

    // Offsets are logical: every part after the first repeats a header of partHeaderSize bytes,
    // so part n holds logical bytes [n * (partSize - partHeaderSize), ...) starting at partHeaderSize.
    private byte[] ReadAt(long logicalOffset, int length)
    {
        byte[] buffer = new byte[length];
        int part = (int)(logicalOffset / this.partSize);
        long position = logicalOffset - part * (this.partSize - this.partHeaderSize);
        int filled = 0;
        while (filled < length)
        {
            FileStream file = this.dataFiles[part];
            file.Position = position;
            int read = file.Read(buffer, filled, length - filled);
            filled += read;
            if (filled < length)
            {
                if (++part >= this.dataFiles.Count)
                {
                    throw new EndOfStreamException("Record runs past the last data file");
                }

                position = this.partHeaderSize;
            }
        }

        return buffer;
    }

    // Read-only, and sharing read, write and delete so the game client can have the same files
    // open while it runs (and while it patches).
    private static FileStream OpenShared(string path) =>
        new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);

    private static int ReadInt32BigEndian(byte[] data, int at) =>
        (data[at] << 24) | (data[at + 1] << 16) | (data[at + 2] << 8) | data[at + 3];
}
