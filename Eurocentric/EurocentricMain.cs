using System.Text;

public static class EurocentricMain
{
    public static void Main(String[] args)
    {
        Misc.generateMod("Eurocentric", generateModEurocentric);
    }

    public class FlavorizationRule
    {
        public string key;
        public string tier;
        public string type;
        public string gender;
    }

    public static void generateModEurocentric(FileResolver fileResolver, string outputModFolder)
    {
        string titlesFileRelativePath = "localization/english/culture/culture_titles_l_english.yml";
        LocalisationFileData titlesFileData = LocalisationParser.parse(fileResolver.readFileText(titlesFileRelativePath));
        if (titlesFileData.topLevelKey != "l_english")
            throw new Exception("Unexpected titles file format!");

        Dictionary<string, FlavorizationRule> flavorizationRules = new Dictionary<string, FlavorizationRule>();

        const string flavorizationRulePrefix = "EC_";

        foreach (string relativePath in fileResolver.getFilesInFolder("common/flavorization"))
        {
            if (!relativePath.EndsWith(".txt"))
                continue;

            string fileData = fileResolver.readFileText(relativePath);
            CkObject data = Parser.parse(fileData);

            foreach (CkKeyValuePair item in data.valuesList)
            {
                if (flavorizationRules.ContainsKey(item.key))
                    throw new Exception("Duplicate key: " + item.key);

                string type = item.valueObject.findFirstWithName("type").valueString;

                if (type != "character" && type != "title")
                    continue;

                CkKeyValuePair specialItem = item.valueObject.findFirstWithName("special");
                if (specialItem != null && specialItem.valueString != "holder")
                    continue;

                CkKeyValuePair tierItem = item.valueObject.findFirstWithName("tier");
                if (tierItem == null)
                {
                    Console.WriteLine("Skipping " + item.key + " as it has no tier");
                    continue;
                }

                CkKeyValuePair governmentsItem = item.valueObject.findFirstWithName("governments");
                if (governmentsItem != null)
                {
                    bool isMercenary = false;
                    foreach (CkKeyValuePair governmentItem in governmentsItem.valueObject.valuesList)
                    {
                        if (governmentItem.valueString == "mercenary_government")
                        {
                            isMercenary = true;
                            break;
                        }
                    }

                    if (isMercenary)
                    {
                        Console.WriteLine("Skipping " + item.key + " as it is mercenary");
                        continue;
                    }
                }

                if (titlesFileData.get(item.key) == null)
                {
                    Console.WriteLine("Skipping " + item.key + " as it has no localisation");
                    continue;
                }

                item.key = flavorizationRulePrefix + item.key;

                string gender = item.valueObject.findFirstWithName("gender")?.valueString;

                string tier = tierItem.valueString;

                flavorizationRules.Add(item.key, new FlavorizationRule()
                {
                    key = item.key,
                    tier = tier,
                    type = type,
                    gender = gender,
                });
            }

            string serialised = data.serialise();
            string outputPath = outputModFolder + "/" + relativePath;
            Directory.CreateDirectory(Directory.GetParent(outputPath).FullName);
            File.WriteAllText(outputPath, serialised, Encoding.UTF8);
        }

        Dictionary<string, string> placeToPerson = new Dictionary<string, string>()
        {
            { "barony", "baron" },
            { "county", "count" },
            { "duchy", "duke" },
            { "kingdom", "king" },
            { "empire", "emperor" },
        };

        Dictionary<string, string> baseValues = new Dictionary<string, string>()
        {
            { "baron", titlesFileData.get("baron").value },
            { "baron_female", titlesFileData.get("baron_female").value },
            { "count", titlesFileData.get("count").value },
            { "count_female", titlesFileData.get("count_female").value },
            { "duke", titlesFileData.get("duke").value },
            { "duke_female", titlesFileData.get("duke_female").value },
            { "king", titlesFileData.get("king").value },
            { "king_female", titlesFileData.get("king_female").value },
            { "emperor", titlesFileData.get("emperor").value },
            { "emperor_female", titlesFileData.get("emperor_female").value },

            { "barony", titlesFileData.get("barony_feudal").value },
            { "county", titlesFileData.get("county_feudal").value },
            { "duchy", titlesFileData.get("duchy_feudal").value },
            { "kingdom", titlesFileData.get("kingdom_feudal").value },
            { "empire", titlesFileData.get("empire_feudal").value },
        };

        foreach (FlavorizationRule rule in flavorizationRules.Values)
        {
            LocalisationEntry originalEntry = titlesFileData.get(rule.key.Substring(flavorizationRulePrefix.Length));
            if (originalEntry == null)
            {
                Console.WriteLine("No entry for " + rule.key);
                continue;
            }

            string baseKey;
            if (rule.type == "character")
            {
                baseKey = placeToPerson[rule.tier];
                if (rule.gender == "female")
                    baseKey += "_female";
            }
            else if (rule.type == "title")
            {
                baseKey = rule.tier;
            }
            else
            {
                throw new Exception("Unknown rule type: " + rule.type);
            }

            string baseValue = baseValues[baseKey];

            string newValue;
            if (originalEntry.value != baseValue)
                newValue = baseValue + " (" + originalEntry.value + ")";
            else
                newValue = originalEntry.value;

            LocalisationEntry newEntry = new LocalisationEntry()
            {
                key = rule.key,
                value = newValue,
            };

            titlesFileData.entries.Add(newEntry);
        }

        string titlesSerialised = LocalisationSerialiser.serialise(titlesFileData);
        string titlesOutputPath = outputModFolder + "/" + titlesFileRelativePath;
        Directory.CreateDirectory(Directory.GetParent(titlesOutputPath).FullName);
        File.WriteAllText(titlesOutputPath, titlesSerialised, Encoding.UTF8);
    }
}