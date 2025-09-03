public class LocalisationFileData
{
    public List<LocalisationSection> sections = new List<LocalisationSection>();

    public string whitespaceAfterLastEntry = null;
}

public class LocalisationSection
{
    public string key;
    public List<LocalisationEntry> entries = new List<LocalisationEntry>();

    public string whitespaceBeforeKey = null;

    public LocalisationEntry get(string key)
    {
        foreach (LocalisationEntry entry in entries)
        {
            if (entry.key == key)
                return entry;
        }

        return null;
    }

    public void set(string key, string value, int? number = null)
    {
        LocalisationEntry entry = get(key);
        if (entry == null)
        {
            entry = new LocalisationEntry();
            entry.key = key;
            entries.Add(entry);
        }

        entry.value = value;
        entry.number = number;
    }
}


public class LocalisationEntry
{
    public string key;
    public int? number;
    public string value;

    public string whitespaceBeforeKey = null;
}

// This one is an ad-hoc parser that doesn't use a proper grammar. Hopefully should be good enough
public static class LocalisationParser
{
    public static LocalisationFileData parse(string input)
    {
        LocalisationFileData output = new LocalisationFileData();

        LocalisationSection currentSection = null;

        int index = 0;

        while (index < input.Length)
        {
            KeyParseResult keyParse = parseKey(input, ref index);

            if (keyParse.key == null)
            {
                if (index != input.Length)
                    throw new Exception("unexpected missing key");
                output.whitespaceAfterLastEntry = keyParse.junkBeforeKey;
            }
            else if (keyParse.indentLevel == 0)
            {
                if (keyParse.number != null)
                    throw new Exception("number not allowed on top-level key");

                currentSection = new LocalisationSection();
                currentSection.key = keyParse.key;
                currentSection.whitespaceBeforeKey = keyParse.junkBeforeKey;
                output.sections.Add(currentSection);
            }
            else
            {
                if (currentSection == null)
                    throw new Exception("entry before section start");

                if (keyParse.indentLevel != 1)
                    throw new Exception("only one level of indentation supported");

                LocalisationEntry entry = new LocalisationEntry();
                entry.key = keyParse.key;
                entry.number = keyParse.number;
                entry.whitespaceBeforeKey = keyParse.junkBeforeKey;
                entry.value = parseValue(input, ref index);
                currentSection.entries.Add(entry);
            }
        }


        return output;
    }

    private class KeyParseResult
    {
        public string junkBeforeKey = null;

        public int indentLevel = 0;
        public string key = null;
        public int? number = null;
    }

    enum KeyParseState
    {
        BeforeKey,
        InCommentBeforeKey,
        InKey,
        AfterKey,
        InNumber,
        Done,
    }

    private static KeyParseResult parseKey(string input, ref int index)
    {
        KeyParseResult result = new KeyParseResult();

        KeyParseState state = KeyParseState.BeforeKey;
        string accumulator = "";

        string breakAccumulator()
        {
            string ret = accumulator;
            accumulator = "";
            return ret.Length > 0 ? ret : null;
        }

        for (; index < input.Length; index++)
        {
            char c = input[index];

            switch (state)
            {
                case KeyParseState.BeforeKey:
                {
                    if (c == '#')
                    {
                        state = KeyParseState.InCommentBeforeKey;
                        accumulator += c;
                    }
                    else if (isWhitespace(c))
                    {
                        accumulator += c;
                    }
                    else
                    {
                        state = KeyParseState.InKey;
                        result.junkBeforeKey = breakAccumulator();
                        accumulator += c;
                    }
                    break;
                }

                case KeyParseState.InCommentBeforeKey:
                {
                    accumulator += c;
                    if (c == '\n')
                        state = KeyParseState.BeforeKey;

                    break;
                }

                case KeyParseState.InKey:
                {
                    if (c == ':')
                    {
                        result.key = breakAccumulator();

                        if (result.junkBeforeKey != null)
                        {
                            result.indentLevel = 0;
                            for (int i = result.junkBeforeKey.Length - 1; i >= 0; i--)
                            {
                                if (result.junkBeforeKey[i] == ' ')
                                    result.indentLevel += 1;
                                else if (result.junkBeforeKey[i] == '\n')
                                    break;
                                else
                                    throw new Exception("unexpected junk before key");
                            }
                        }

                        state = KeyParseState.AfterKey;
                    }
                    else if (isWhitespace(c))
                    {
                        throw new Exception("unexpected whitespace in key");
                    }
                    else
                    {
                        accumulator += c;
                    }

                    break;
                }

                case KeyParseState.AfterKey:
                {
                    if (isNumeric(c))
                    {
                        state = KeyParseState.InNumber;
                        accumulator += c;
                    }
                    else if (isWhitespace(c))
                    {
                        state = KeyParseState.Done;
                    }

                    break;
                }

                case KeyParseState.InNumber:
                {
                    if (isNumeric(c))
                    {
                        accumulator += c;
                    }
                    else if (isWhitespace(c))
                    {
                        result.number = int.Parse(breakAccumulator());
                        state = KeyParseState.Done;
                    }

                    break;
                }
            }

            if (state == KeyParseState.Done)
                break;
        }

        switch (state)
        {
            case KeyParseState.BeforeKey:
            {
                if (accumulator.Length > 0)
                    result.junkBeforeKey = breakAccumulator();
                break;
            }
            case KeyParseState.Done:
            {
                break;
            }
            default:
            {
                throw new Exception("unexpected end of input while parsing key");
            }
        }

        return result;
    }

    private static string parseValue(string input, ref int index)
    {
        if (index >= input.Length)
            throw new Exception("expected value");

        if (input[index] != ' ')
            throw new Exception("expected space before value");
        index++;

        if (input[index] != '"')
            throw new Exception("expected opening quote for value");
        index++;

        string accumulator = "";
        while (true)
        {
            if (index >= input.Length)
            {
                if (accumulator.Length > 0 && accumulator[accumulator.Length - 1] == '"')
                {
                    accumulator = accumulator.Substring(0, accumulator.Length - 1);
                    return accumulator;
                }
                else
                {
                    throw new Exception("unterminated value");
                }
            }

            if (input[index] == '"' &&
                index + 1 < input.Length &&
                (input[index + 1] == '\r' || input[index + 1] == '\n'))
            {
                index++;
                return accumulator;
            }

            accumulator += input[index];
            index++;
        }
    }

    private static bool isWhitespace(char c)
    {
        return c == '\t' || c == '\r' || c == '\n' || c == ' ';
    }

    private static bool isNumeric(char c)
    {
        return c >= '0' && c <= '9';
    }
}