using System.Text;

public static class LocalisationSerialiser
{
    public static string serialise(LocalisationFileData localisationFileData)
    {
        StringBuilder stringBuilder = new StringBuilder();
        stringBuilder.Append(localisationFileData.whitespaceBeforeTopLevelKey ?? "");
        stringBuilder.Append(localisationFileData.topLevelKey);
        stringBuilder.Append(':');

        for (int entryIndex = 0; entryIndex < localisationFileData.entries.Count; entryIndex++)
        {
            LocalisationEntry entry = localisationFileData.entries[entryIndex];
            stringBuilder.Append(entry.whitespaceBeforeKey ?? "\n ");
            stringBuilder.Append(entry.key);
            stringBuilder.Append(':');
            if (entry.number != null)
                stringBuilder.Append(entry.number.Value);
            stringBuilder.Append(' ');
            stringBuilder.Append('"');
            stringBuilder.Append(entry.value);
            stringBuilder.Append('"');
        }

        stringBuilder.Append(localisationFileData.whitespaceAfterLastEntry);

        return stringBuilder.ToString();
    }
}