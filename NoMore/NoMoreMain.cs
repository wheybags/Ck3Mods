using System.Text;

public static class Program
{
    public static void Main(String[] args)
    {
        Misc.generateMod("NoMore", generateModNoMore);
    }

    private static void generateModNoMore(FileResolver fileResolver, string outputModFolder)
    {
        HashSet<string> dataFileRelativePaths = fileResolver.getFilesInFolder("common\\character_interactions");

        foreach (string relativePath in dataFileRelativePaths)
        {
            if (!relativePath.EndsWith(".txt"))
                continue;

            Console.WriteLine("Generating " + relativePath);

            string fileData = fileResolver.readFileText(relativePath);
            CkObject data = Parser.parse(fileData);

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


            string serialised = data.serialise();
            string outputPath = outputModFolder + "\\" + relativePath;
            Directory.CreateDirectory(Directory.GetParent(outputPath).FullName);
            File.WriteAllText(outputPath, serialised, Encoding.UTF8);
        }
    }
}