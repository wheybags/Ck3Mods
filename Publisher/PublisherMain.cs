using System.Diagnostics;
using System.IO.Compression;
using System.Reflection;

public static class PublisherMain
{
    public static void Main(string[] args)
    {
        string outFolder = getRootDir() + "/Releases";
        try { Directory.Delete(outFolder, true); } catch { }
        Directory.CreateDirectory(outFolder);

        buildRelease("NoMore", "win", outFolder + "/win");
        buildRelease("NoMore", "osx", outFolder + "/osx");
        buildRelease("Eurocentric", "win", outFolder + "/win");
        buildRelease("Eurocentric", "osx", outFolder + "/osx");
    }

    private static void buildRelease(string projectName, string os, string outputFolder)
    {
        ProcessStartInfo startInfo = new ProcessStartInfo()
        {
            FileName = "dotnet",
            Arguments = "publish " + projectName + "/" + projectName + ".csproj" +
                        " --os " + os +
                        " --arch x64" +
                        " --self-contained true" +
                        " /p:PublishSingleFile=true" +
                        " /p:PublishTrimmed=true" +
                        " /p:IncludeAllContentForSelfExtract=true" +
                        " /p:DebugType=None" +
                        " /p:DebugSymbols=false" +
                        " --configuration Release" +
                        " --output \"" + outputFolder + "\"",
            WorkingDirectory = getRootDir(),
        };
        Process process = Process.Start(startInfo);
        process.WaitForExit();

        if (process.ExitCode != 0)
            throw new Exception("build failed");

        if (os == "osx")
        {
            string outputZipPath = outputFolder + "/" + projectName + "_osx.zip";

            using (FileStream zipFile = new FileStream(outputZipPath, FileMode.Create))
            using (ZipArchive archive = new ZipArchive(zipFile, ZipArchiveMode.Create))
            {
                archive.CreateEntryFromFile(outputFolder + "/" + projectName, projectName);
            }

            // sets unix executable flag in the zip file
            Process p = Process.Start(Path.Join(getRootDir(), "zip_exec", "zip_exec.exe"), "\"" + outputZipPath + "\" " + projectName);
            p.WaitForExit();
            if (p.ExitCode != 0)
                throw new Exception();

            File.Delete(outputFolder + "/" + projectName);
        }
    }

    static string getRootDir()
    {
        string root = Assembly.GetEntryAssembly()!.Location;
        while (!File.Exists(root + "/Ck3Mods.sln"))
            root = Directory.GetParent(root).FullName;
        return root;
    }
}