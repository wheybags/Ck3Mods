using System.Diagnostics;
using Microsoft.Data.Sqlite;
using Microsoft.Win32;

public class Mod
{
    public string name;
    public string path;
}

public class Playset
{
    public string id;
    public string name;
    public List<Mod> mods = new List<Mod>();
}

public static class Misc
{
    public static string getGameInstallFolder()
    {
        string steamPath = Registry.GetValue("HKEY_CURRENT_USER\\SOFTWARE\\Valve\\Steam", "SteamPath", null) as string;
        string vdfPath = steamPath.Replace("/", "\\") + "\\steamapps\\libraryfolders.vdf";


        foreach (string line in File.ReadAllLines(vdfPath))
        {
            string trimmedLine = line.Trim();
            if (trimmedLine.StartsWith("\"path\""))
            {
                // line is something like:
                // "path"		"C:\\Program Files (x86)\\Steam"
                string libraryPath = trimmedLine.Substring("\"path\"".Length).Trim();
                libraryPath = libraryPath.Substring(1, libraryPath.Length - 2) + "\\steamapps\\common\\Crusader Kings III";

                if (Directory.Exists(libraryPath))
                    return libraryPath;
            }
        }

        return null;
    }

    public static List<Playset> fetchPlaysets(string gameInstallPath)
    {
        string sqlitePath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\Paradox Interactive\\Crusader Kings III\\launcher-v2.sqlite";
        using SqliteConnection connection = new SqliteConnection("Data Source=" + sqlitePath + ";Mode=ReadOnly");
        connection.Open();

        List<Playset> playsets = new List<Playset>();
        {
            using SqliteCommand selectPlaysetsCommand = connection.CreateCommand();
            selectPlaysetsCommand.CommandText = @"
        SELECT id, name
        FROM playsets
    ";

            using var reader = selectPlaysetsCommand.ExecuteReader();
            while (reader.Read())
            {
                Playset playset = new Playset();
                playset.id = reader.GetString(0);
                playset.name = reader.GetString(1);
                playsets.Add(playset);
            }
        }

        foreach (Playset playset in playsets)
        {
            using SqliteCommand selectPlaysetModsCommand = connection.CreateCommand();
            selectPlaysetModsCommand.CommandText = @"
            SELECT mods.steamId, mods.gameRegistryId, mods.displayName
            FROM playsets_mods
            INNER JOIN mods ON playsets_mods.modId=mods.id
            WHERE playsets_mods.playsetId = $id AND playsets_mods.enabled = 1
            ORDER BY playsets_mods.position
        ";

            selectPlaysetModsCommand.Parameters.AddWithValue("id", playset.id);

            using var reader = selectPlaysetModsCommand.ExecuteReader();
            while (reader.Read())
            {
                string steamId = null;
                if (!reader.IsDBNull(0))
                    steamId = reader.GetString(0);

                string gameRegistryId = reader.GetString(1);
                string displayName = reader.GetString(2);


                Mod mod = new Mod();
                if (steamId != null)
                {
                    string steamappsFolder = new DirectoryInfo(gameInstallPath).Parent.Parent.FullName;
                    mod.path = steamappsFolder + "\\workshop\\content\\1158310\\" + steamId;
                }
                else
                {
                    // gameRegistryId is something like "mod/ugc_2887120253.mod"
                    string relativePath = gameRegistryId.Replace("/", "\\").Substring(0, gameRegistryId.Length - 4);
                    mod.path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\Paradox Interactive\\Crusader Kings III\\" + relativePath;
                }

                mod.name = displayName;

                if (!Directory.Exists(mod.path))
                    throw new Exception("Mod path not found");

                playset.mods.Add(mod);
            }
        }

        return playsets;
    }


    public delegate void GenerateModDelegate(FileResolver fileResolver, string outputModFolder);

    public static void generateMod(string modBaseName, GenerateModDelegate generateModDelegate)
    {
        void doGeneration()
        {
            string gameInstallPath = getGameInstallFolder();
            if (gameInstallPath == null)
                throw new Exception("Couldn't find CK3 install path!");

            Console.WriteLine("Found CK3 install at: " + gameInstallPath);
            Console.WriteLine("");

            List<Playset> playsets = fetchPlaysets(gameInstallPath);

            Console.WriteLine("Playsets:");
            for (int i = 0; i < playsets.Count; i++)
            {
                Playset playset = playsets[i];
                Console.WriteLine("  " + (i + 1) + ": " + playset.name);
                foreach (Mod mod in playset.mods)
                    Console.WriteLine("    - " + mod.name);
            }
            Console.WriteLine("");

            Playset selectedPlayset = null;

            if (playsets.Count == 1)
            {
                selectedPlayset = playsets[0];
            }
            else
            {
                while (true)
                {
                    Console.Write("Choose a playset (1-" + playsets.Count + "): ");
                    string read = Console.ReadLine();

                    if (int.TryParse(read, out int selectedIndex))
                    {
                        if (selectedIndex >= 1 && selectedIndex <= playsets.Count)
                        {
                            selectedPlayset = playsets[selectedIndex - 1];
                            break;
                        }
                    }
                }
            }

            Console.WriteLine("Using playset " + selectedPlayset.name);

            string modName = modBaseName + " - " + selectedPlayset.name;
            string modFolderName = modBaseName + "_" + selectedPlayset.name;
            string outputModFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\Paradox Interactive\\Crusader Kings III\\mod\\" + modFolderName;

            try { Directory.Delete(outputModFolder, true);  } catch (Exception) {}
            Directory.CreateDirectory(outputModFolder);

            List<Mod> baseMods = selectedPlayset.mods.Where(mod => mod.path != outputModFolder).ToList();
            FileResolver fileResolver = new FileResolver(gameInstallPath, baseMods);

            generateModDelegate(fileResolver, outputModFolder);

            string dotModData = "version=\"1.0\"\n" +
                                "tags={\n" +
                                "    \"Fixes\"\n" +
                                "}\n" +
                                "name=\"" + modName + "\"";

            File.WriteAllText(outputModFolder + "\\descriptor.mod", dotModData);
            File.WriteAllText(Directory.GetParent(outputModFolder).FullName + "\\" + modFolderName + ".mod", dotModData + "\npath=\"" + outputModFolder.Replace("\\", "/") + "\"");

            Console.WriteLine("Generating mod done!");
            Console.WriteLine("");
            Console.WriteLine("**************");
            Console.WriteLine("* READ THIS! *");
            Console.WriteLine("**************");
            Console.WriteLine("");
            Console.WriteLine("In the game launcher, edit the \"" + selectedPlayset.name + "\" playset, and enable the mod \"" + modName + "\"");
            Console.WriteLine("Make sure it is always last in the load order!");
            Console.WriteLine("");
            Console.WriteLine("ALSO NOTE: You need to re-run this tool every time you change your game data!");
            Console.WriteLine("This means whenever you add / remove a mod, or update the base game or a mod");
            Console.WriteLine("");
        }

        Tests.run();

        if (!Debugger.IsAttached)
        {
            try
            {
                doGeneration();
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: " + e);
                Console.WriteLine("");
                Console.WriteLine("Mod generation failed!");
            }
        }
        else
        {
            doGeneration();
        }

        Console.Write("Press enter to close");
        Console.ReadLine();
    }
}