namespace Compiler;

public interface IFileSystem
{
    string ResolveToFullPath(string anyPath);
    Stream OpenRead(string fullPath);
    string ReadAll(string fullPath);
}