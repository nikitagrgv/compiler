namespace Compiler;

public interface IFileSystem
{
    string ResolveToFullPath(string anyPath);
    string ReadAllText(string fullPath);
}