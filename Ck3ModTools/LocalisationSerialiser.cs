using System.Text;

public static class LocalisationSerialiser
{
    public static string serialise(LocalisationFileData localisationFileData)
    {
        StringBuilder stringBuilder = new StringBuilder();
        for (int sectionIndex = 0; sectionIndex < localisationFileData.sections.Count; sectionIndex++)
        {
            LocalisationSection section = localisationFileData.sections[sectionIndex];
            stringBuilder.Append(section.whitespaceBeforeKey ?? (sectionIndex == 0 ? "" : "\n"));
            stringBuilder.Append(section.key);
            stringBuilder.Append(':');

            for (int entryIndex = 0; entryIndex < section.entries.Count; entryIndex++)
            {
                LocalisationEntry entry = section.entries[entryIndex];
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
        }

        stringBuilder.Append(localisationFileData.whitespaceAfterLastEntry);

        return stringBuilder.ToString();
    }
}