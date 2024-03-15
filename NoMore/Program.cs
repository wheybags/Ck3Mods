using System.Diagnostics;

public class Program
{
    public static void Main(String[] args)
    {
        if (!Debugger.IsAttached)
        {
            try
            {
                generateMod();
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
            generateMod();
        }

        Console.Write("Press enter to close");
        Console.ReadLine();
    }

    private static void generateMod()
    {
        string gameInstallPath = GamePaths.getGameInstallFolder();
        if (gameInstallPath == null)
            throw new Exception("Couldn't find CK3 install path!");

        Console.WriteLine("Found CK3 install at: " + gameInstallPath);
        Console.WriteLine("");

        List<Playset> playsets = GamePaths.fetchPlaysets(gameInstallPath);

        Console.WriteLine("Playsets:");
        for (int i = 0; i < playsets.Count; i++)
        {
            Playset playset = playsets[i];
            Console.WriteLine("  " +(i+1) + ": " + playset.name);
            foreach (Mod mod in playset.mods)
                Console.WriteLine("    - " + mod.name);
        }
        Console.WriteLine("");

        Playset selectedPlayset = null;

        while (true)
        {
            Console.Write("Choose a playset (1-" + (playsets.Count + 1) + "): ");
            string read = Console.ReadLine();

            if (int.TryParse(read, out int selectedIndex))
            {
                if (selectedIndex >= 1 && selectedIndex <= playsets.Count)
                {
                    selectedPlayset = playsets[selectedIndex-1];
                    break;
                }
            }
        }

        Console.WriteLine("Using playset " + selectedPlayset.name);

        List<string> sourcePaths = new List<string>()
        {
            gameInstallPath + "\\game",
        };

        string modName = "No More - " + selectedPlayset.name;
        string modFolderName = "nomore_" + selectedPlayset.name;

        string outputModFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\Paradox Interactive\\Crusader Kings III\\mod\\" + modFolderName;

        foreach (Mod mod in selectedPlayset.mods)
        {
            if (mod.path != outputModFolder)
                sourcePaths.Add(mod.path);
        }

        HashSet<string> dataFileRelativePaths = new HashSet<string>();
        foreach (string sourcePath in sourcePaths)
        {
            string characterInteractionsPath = Path.Join(sourcePath, "common", "character_interactions");

            if (!Directory.Exists(characterInteractionsPath))
                continue;

            foreach (string path in Directory.EnumerateFiles(characterInteractionsPath))
            {
                if (path.EndsWith(".txt"))
                    dataFileRelativePaths.Add(Path.GetRelativePath(sourcePath, path));
            }
        }

        if (Directory.Exists(outputModFolder))
            Directory.Delete(outputModFolder, true);

        foreach (string relativePath in dataFileRelativePaths)
        {
            Console.WriteLine("Generating " + relativePath);

            string sourcePath = null;
            for (int i = sourcePaths.Count - 1; i >= 0; i--)
            {
                string pathInSource = Path.Join(sourcePaths[i], relativePath);
                if (File.Exists(pathInSource))
                {
                    sourcePath = pathInSource;
                    break;
                }
            }

            string fileData = File.ReadAllText(sourcePath);
            // string fileData = File.ReadAllText("D:\\SteamLibrary\\steamapps\\common\\Crusader Kings III\\game\\common\\character_interactions\\00_debug_interactions.txt");


            // string fileData = File.ReadAllText("C:\\users\\wheybags\\desktop\\test.txt");

            // fileData = @"AND = { # Explicit AND to ensure no funny business";
            //        fileData = @"text = ""mystical_ancestors_disinherit""
            // 	}
            // } >= asd";


            //  fileData = @"scope:actor.dynasty = {
            // 	dynasty_prestige >= medium_dynasty_prestige_value
            // }";

            // fileData = "scope:target = { exists = var:relic_religion}\n";


            List<Token> tokens = Tokeniser.tokenise(fileData);

            Parser parser = new Parser()
            {
                tokens = tokens
            };

            CkObject data = parser.parseRoot();

            foreach (CkKeyValuePair interaction in data.valuesList)
            {
                if (interaction.valueIsString)
                    continue;

                CkKeyValuePair commonInteractionItem = interaction.valueObject.findFirstWithName("common_interaction");

                if (commonInteractionItem == null)
                {
                    commonInteractionItem = new CkKeyValuePair() { key = "common_interaction" };
                    interaction.valueObject.valuesList.Insert(0, commonInteractionItem);
                }

                commonInteractionItem.valueString = "yes";
            }


            string serialised = CkObject.serialise(data);
            string outputPath = outputModFolder + "\\" + relativePath;
            Directory.CreateDirectory(Directory.GetParent(outputPath).FullName);
            File.WriteAllText(outputPath, serialised);
        }

        string dotModData = "version=\"1.0\"\n" +
                            "tags={\n" +
                            "    \"Gui\"\n" +
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
}