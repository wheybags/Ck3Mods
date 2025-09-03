using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

public class Program
{
    public static void Main(String[] args)
    {
        Tests.run();

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

    private static void generateMod()
    {
        string gameInstallPath = Misc.getGameInstallFolder();
        if (gameInstallPath == null)
            throw new Exception("Couldn't find CK3 install path!");

        Console.WriteLine("Found CK3 install at: " + gameInstallPath);
        Console.WriteLine("");

        Playset selectedPlayset = Misc.selectPlaysetInteractive(gameInstallPath);


        string modName = "Eurocentric - " + selectedPlayset.name;
        string modFolderName = "eurocentric_" + selectedPlayset.name;
        string outputModFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\Paradox Interactive\\Crusader Kings III\\mod\\" + modFolderName;

        string titlesFileRelativePath = "localization\\english\\culture\\culture_titles_l_english.yml";

        List<Mod> baseMods = selectedPlayset.mods.Where(mod => mod.path != outputModFolder).ToList();
        FileResolver fileResolver = new FileResolver(gameInstallPath, baseMods);

        LocalisationFileData titlesFileData = LocalisationParser.parse(fileResolver.readFileText(titlesFileRelativePath));

        if (titlesFileData.sections.Count != 1 || titlesFileData.sections[0].key != "l_english")
            throw new Exception("Unexpected titles file format!");

        Dictionary<string, string> baseValues = new Dictionary<string, string>()
        {
            {"baron", titlesFileData.sections[0].get("baron").value},
            {"baron_female",titlesFileData.sections[0].get("baron_female").value},
            {"count", titlesFileData.sections[0].get("count").value},
            {"count_female", titlesFileData.sections[0].get("count_female").value},
            {"duke", titlesFileData.sections[0].get("duke").value},
            {"duke_female", titlesFileData.sections[0].get("duke_female").value},
            {"king", titlesFileData.sections[0].get("king").value},
            {"king_female", titlesFileData.sections[0].get("king_female").value},
            {"emperor",titlesFileData.sections[0].get("emperor").value},
            {"emperor_female", titlesFileData.sections[0].get("emperor_female").value},

            {"barony", titlesFileData.sections[0].get("barony_feudal").value},
            {"county", titlesFileData.sections[0].get("county_feudal").value},
            {"duchy", titlesFileData.sections[0].get("duchy_feudal").value},
            {"kingdom", titlesFileData.sections[0].get("kingdom_feudal").value},
            {"empire", titlesFileData.sections[0].get("empire_feudal").value},

            {"caliph", titlesFileData.sections[0].get("emperor").value},

            {"baron_theocracy_male", titlesFileData.sections[0].get("baron_theocracy_male_christianity_religion").value},
            {"baron_female_theocracy", titlesFileData.sections[0].get("baron_theocracy_female_christianity_religion").value},
            {"barony_theocracy", titlesFileData.sections[0].get("barony_theocracy_christianity_religion").value},
            {"count_theocracy_male", titlesFileData.sections[0].get("count_theocracy_male_christianity_religion").value},
            {"count_female_theocracy", titlesFileData.sections[0].get("count_theocracy_female_christianity_religion").value},
            {"county_theocracy", titlesFileData.sections[0].get("county_theocracy_christianity_religion").value},
            {"duke_theocracy_male", titlesFileData.sections[0].get("duke_theocracy_male_christianity_religion").value},
            {"duke_female_theocracy", titlesFileData.sections[0].get("duke_theocracy_female_christianity_religion").value},
            {"duchy_theocracy", titlesFileData.sections[0].get("duchy_theocracy_christianity_religion").value},
            {"king_theocracy_male", titlesFileData.sections[0].get("king_theocracy_male_christianity_religion").value},
            {"king_female_theocracy", titlesFileData.sections[0].get("king_theocracy_female_christianity_religion").value},
            {"kingdom_theocracy", titlesFileData.sections[0].get("kingdom_theocracy_christianity_religion").value},
            {"emperor_theocracy_male", titlesFileData.sections[0].get("emperor_theocracy_male_christianity_religion").value},
            {"emperor_female_theocracy", titlesFileData.sections[0].get("emperor_theocracy_female_christianity_religion").value},
            {"empire_theocracy", titlesFileData.sections[0].get("empire_theocracy_christianity_religion").value},

            {"baron_republic_male", titlesFileData.sections[0].get("baron_republic_male").value},
            {"baron_female_republic", titlesFileData.sections[0].get("baron_republic_female").value},
            {"barony_republic", titlesFileData.sections[0].get("barony_republic").value},
            {"count_republic_male", titlesFileData.sections[0].get("count_republic_male").value},
            {"count_female_republic", titlesFileData.sections[0].get("count_republic_female").value},
            {"county_republic", titlesFileData.sections[0].get("county_republic").value},
            {"duke_republic_male", titlesFileData.sections[0].get("duke_republic_male").value},
            {"duke_female_republic", titlesFileData.sections[0].get("duke_republic_female").value},
            {"duchy_republic", titlesFileData.sections[0].get("duchy_republic").value},
            {"king_republic_male", titlesFileData.sections[0].get("king_republic_male").value},
            {"king_female_republic", titlesFileData.sections[0].get("king_republic_female").value},
            {"kingdom_republic", titlesFileData.sections[0].get("kingdom_republic").value},
            {"emperor_republic_male", titlesFileData.sections[0].get("emperor_republic_male").value},
            {"emperor_female_republic", titlesFileData.sections[0].get("emperor_republic_female").value},
            {"empire_republic", titlesFileData.sections[0].get("empire_republic").value},
        };

        // First resolve any $lookups$ (eg $duke_feudal_male_arabic_group$ -> Emir) before adding bracketed titles,
        // so we don't end up with double applications like "Duke (Duke (Emir))"
        Regex lookupRegex = new Regex(@"\$(.*?)\$");
        foreach (LocalisationEntry entry in titlesFileData.sections[0].entries)
        {
            while (true)
            {
                Match match = lookupRegex.Match(entry.value);
                if (!match.Success)
                    break;

                string lookupKey = match.Groups[1].Value;
                string value = titlesFileData.sections[0].get(lookupKey)?.value;
                if (value == null)
                    break;
                entry.value = entry.value.Replace("$" + lookupKey + "$", value);
            }
        }

        // and then transform titles by prepending the base title, eg: Emir -> Duke (Emir)
        Regex femaleTitleRegex = new Regex(@"(count|baron|duke|king|emperor)_(.*)_female");
        foreach (LocalisationEntry entry in titlesFileData.sections[0].entries)
        {
            if (entry.key.Contains("plural") || entry.key.Contains("landless"))
                continue;

            string keyToMatch = entry.key;

            // rearrange female titles to something we can match easily
            // eg: duke_feudal_female -> duke_female_feudal
            // after transformation we can just match on starts with "duke_female"
            MatchCollection matches = femaleTitleRegex.Matches(keyToMatch);
            if (matches.Count == 1)
                keyToMatch = matches[0].Groups[1].Value + "_female_" + matches[0].Groups[2].Value;

            string bestMatch = null;
            foreach (string baseKey in baseValues.Keys)
            {
                if ((bestMatch == null || baseKey.Length > bestMatch.Length) && keyToMatch.StartsWith(baseKey))
                {
                    bestMatch = baseKey;
                }
            }

            if (bestMatch != null)
            {
                string baseValue = baseValues[bestMatch];
                if (baseValue == null)
                    baseValue = titlesFileData.sections[0].get(bestMatch).value;

                if (entry.value != baseValue)
                    entry.value = baseValue + " (" + entry.value + ")";

                // Console.WriteLine(entry.key + ": " + entry.value);
            }
        }

        string serialised = LocalisationSerialiser.serialise(titlesFileData);
        string outputPath = outputModFolder + "\\" + titlesFileRelativePath;
        Directory.CreateDirectory(Directory.GetParent(outputPath).FullName);
        File.WriteAllText(outputPath, serialised, Encoding.UTF8);

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