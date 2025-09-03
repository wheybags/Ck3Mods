using System.Text;

public class FileResolver
{
    public readonly List<string> sourcePaths = new List<string>();

    public FileResolver(string gameInstallPath, List<Mod> mods)
    {
        sourcePaths.Add(gameInstallPath + "\\game");
        foreach (Mod mod in mods)
            sourcePaths.Add(mod.path);
    }

    public HashSet<string> getFilesInFolder(string relativeFolderPath)
    {
        HashSet<string> files = new HashSet<string>();
        foreach (string root in sourcePaths)
        {
            string fullPath = root + "\\" + relativeFolderPath;
            if (Directory.Exists(fullPath))
            {
                foreach (string file in Directory.GetFiles(fullPath))
                {
                    string relativePath = file.Substring(root.Length + 1);
                    files.Add(relativePath);
                }
            }
        }

        return files;
    }

    public string resolve(string relativePath)
    {
        string lastPath = null;
        foreach (string root in sourcePaths)
        {
            string fullPath = root + "\\" + relativePath;
            if (File.Exists(fullPath))
                lastPath = root;
        }

        return lastPath;
    }

    public string readFileText(string relativePath)
    {
        string root = resolve(relativePath);
        if (root == null)
            throw new Exception("Couldn't find file " + relativePath + " in any source path!");

        return File.ReadAllText(root + "\\" + relativePath, Encoding.UTF8);
    }
}