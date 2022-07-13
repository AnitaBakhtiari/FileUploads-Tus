namespace tusdotnet.Stores;

internal sealed class InternalFileRep
{
    private InternalFileRep(string fileId, string path)
    {
        FileId = fileId;
        Path = path;
    }

    public string Path { get; }

    public string FileId { get; set; }

    public void Delete()
    {
        File.Delete(Path);
    }

    public bool Exist()
    {
        return File.Exists(Path);
    }

    public void Write(string text)
    {
        File.WriteAllText(Path, text);
    }

    public long ReadFirstLineAsLong(bool fileIsOptional = false, long defaultValue = -1)
    {
        var content = ReadFirstLine(fileIsOptional);
        if (long.TryParse(content, out var value)) return value;

        return defaultValue;
    }

    public string ReadFirstLine(bool fileIsOptional = false)
    {
        if (fileIsOptional && !File.Exists(Path)) return null;

        using var stream = GetStream(FileMode.Open, FileAccess.Read, FileShare.Read);
        using var sr = new StreamReader(stream);
        return sr.ReadLine();
    }

    public FileStream GetStream(FileMode mode, FileAccess access, FileShare share)
    {
        return new FileStream(Path, mode, access, share, 4096, true);
    }

    public long GetLength()
    {
        return new FileInfo(Path).Length;
    }

    internal sealed class FileRepFactory
    {
        private readonly string _directoryPath;

        public FileRepFactory(string directoryPath)
        {
            _directoryPath = directoryPath;
        }

        public InternalFileRep Data(InternalFileId fileId)
        {
            return Create(fileId, "");
        }

        public InternalFileRep UploadLength(InternalFileId fileId)
        {
            return Create(fileId, "uploadlength");
        }

        public InternalFileRep UploadConcat(InternalFileId fileId)
        {
            return Create(fileId, "uploadconcat");
        }

        public InternalFileRep Metadata(InternalFileId fileId)
        {
            return Create(fileId, "metadata");
        }

        public InternalFileRep Expiration(InternalFileId fileId)
        {
            return Create(fileId, "expiration");
        }

        public InternalFileRep ChunkStartPosition(InternalFileId fileId)
        {
            return Create(fileId, "chunkstart");
        }

        public InternalFileRep ChunkComplete(InternalFileId fileId)
        {
            return Create(fileId, "chunkcomplete");
        }

        private InternalFileRep Create(InternalFileId fileId, string extension)
        {
            string fileName = fileId;
            if (!string.IsNullOrEmpty(extension)) fileName += "." + extension;

            return new InternalFileRep(fileId, System.IO.Path.Combine(_directoryPath, fileName));
        }
    }
}