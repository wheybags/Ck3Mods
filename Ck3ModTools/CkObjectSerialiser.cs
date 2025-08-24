using System.Text;

public class CkObjectSerialiser
{
    public static string serialise(CkObject obj)
    {
        return new CkObjectSerialiser(obj).serialise();
    }

    private CkObject root;

    private CkObjectSerialiser(CkObject root)
    {
        this.root = root;
    }

    private string serialise()
    {
        StringBuilder stringBuilder = new StringBuilder();
        serialise(stringBuilder, root);
        return stringBuilder.ToString();
    }


    // private const int tabSize = 4;
    //
    // private void indent(StringBuilder stringBuilder, int indentCount)
    // {
    //     int tabCount = indentCount / tabSize;
    //     for (int i = 0; i < tabCount; i++)
    //         stringBuilder.Append('\t');
    // }


    private string calculateCommonPrefix(List<CkKeyValuePair> values)
    {
        List<string> indents = new List<string>();

        foreach (CkKeyValuePair pair in values)
        {
            string initialWhitespace = null;
            if (pair.key != null)
            {
                initialWhitespace = pair.whitespaceBeforeKeyName;
            }
            else
            {
                if (pair.typeTag != null)
                    initialWhitespace = pair.whitespaceBeforeTypeTag;
                else
                    initialWhitespace = pair.whitespaceBeforeValue;
            }

            if (initialWhitespace == null)
                continue;

            int start = 0;
            for (int i = initialWhitespace.Length - 1; i > 0; i--)
            {
                if (initialWhitespace[i] == '\n')
                {
                    start = i;
                    break;
                }
            }

            if (start > 0 && initialWhitespace[start-1] == '\r')
                start--;

            indents.Add(initialWhitespace.Substring(start));
        }

        string shortest = null;
        foreach (string indent in indents)
        {
            if (shortest == null || indent.Length < shortest.Length)
                shortest = indent;
        }

        if (shortest != null)
        {
            bool matchesAll = true;
            foreach (string indent in indents)
            {
                if (!indent.StartsWith(shortest))
                {
                    matchesAll = false;
                    break;
                }
            }

            if (matchesAll)
                return shortest;
        }

        return " ";
    }


    private void serialise(StringBuilder stringBuilder, CkObject obj)
    {
        string commonPrefix = null;
        string getCommonPrefix()
        {
            if (commonPrefix == null)
                commonPrefix = calculateCommonPrefix(obj.valuesList);
            return commonPrefix;
        }

        foreach (CkKeyValuePair pair in obj.valuesList)
        {
            if (pair.key != null)
            {
                if (pair.whitespaceBeforeKeyName == null)
                    stringBuilder.Append(getCommonPrefix());
                else
                    stringBuilder.Append(pair.whitespaceBeforeKeyName);

                stringBuilder.Append(pair.key);
                stringBuilder.Append(pair.whitespaceBeforeOperator ?? " ");
                stringBuilder.Append(pair.operatorString);
            }

            if (pair.valueIsString)
            {
                stringBuilder.Append(pair.whitespaceBeforeValue ?? " ");
                stringBuilder.Append(pair.valueString);
            }
            else
            {
                if (pair.typeTag != null)
                {
                    if (pair.key == null && pair.whitespaceBeforeTypeTag == null)
                        stringBuilder.Append(getCommonPrefix());
                    else
                        stringBuilder.Append(pair.whitespaceBeforeTypeTag ?? " ");

                    stringBuilder.Append(pair.typeTag);
                }

                if (pair.key == null && pair.typeTag == null && pair.whitespaceBeforeValue == null)
                    stringBuilder.Append(getCommonPrefix());
                else
                    stringBuilder.Append(pair.whitespaceBeforeValue ?? " ");

                stringBuilder.Append("{");
                serialise(stringBuilder, pair.valueObject);
                stringBuilder.Append("}");
            }
        }

        stringBuilder.Append(obj.whitespaceAfterLastValue);
    }
}