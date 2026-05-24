namespace miniMOO.Data.FileSystem;

public sealed class FileWorldLoadException : Exception {
    public FileWorldLoadException(string message) : base(message) {
    }

    public FileWorldLoadException(string message, Exception innerException)
        : base(message, innerException) {
    }
}
