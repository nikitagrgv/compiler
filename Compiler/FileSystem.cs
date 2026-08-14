namespace Compiler;

public class FileSystem : IFileSystem
{
    private readonly string _currentDirectory;

    public FileSystem(string currentDirectory)
    {
        _currentDirectory = currentDirectory;
    }

    public string ResolveToFullPath(string anyPath)
    {
        if (Path.IsPathFullyQualified(anyPath))
        {
            return anyPath;
        }

        return Path.GetFullPath(anyPath, _currentDirectory);
    }

    public Stream OpenRead(string fullPath)
    {
        return File.OpenRead(fullPath);
    }

    public string ReadAll(string fullPath)
    {
        return File.ReadAllText(fullPath);
    }
}