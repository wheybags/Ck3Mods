public class Token
{
    public enum Type
    {
        String,
        GreaterOrEqual,
        LessOrEqual,
        Less,
        Greater,
        QuestionEqual,
        Equals,
        Assign,
        OpenBrace,
        CloseBrace,
        FileEnd,
    }

    public Type type;
    public string ignoredTextBeforeToken;

    public string stringValue;

    public int startRow;
    public int startColumn;
}

public class Tokeniser
{
    public static List<Token> tokenise(string input)
    {
        List<Token> tokens = new List<Token>();

        string accumulatedJunk = "";
        string accumulator = "";

        bool inComment = false;
        bool inBareString = false;
        bool inQuotedString = false;

        int startColumn = -1;
        int startRow = -1;

        List<Tuple<string, Token.Type>> keywordMapping = new List<Tuple<string, Token.Type>>()
        {
            new ("<=", Token.Type.LessOrEqual),
            new (">=", Token.Type.GreaterOrEqual),
            new ("?=", Token.Type.QuestionEqual),
            new ("==", Token.Type.Equals),
            new ("<", Token.Type.Less),
            new (">", Token.Type.Greater),
            new ("=", Token.Type.Assign),
            new ("{", Token.Type.OpenBrace),
            new ("}", Token.Type.CloseBrace),
        };

        void breakToken()
        {
            if (accumulator.Length == 0)
                return;

            Token newToken = new Token();
            newToken.ignoredTextBeforeToken = accumulatedJunk;
            newToken.startRow = startRow;
            newToken.startColumn = startColumn;

            if (inQuotedString || inBareString)
            {
                newToken.type = Token.Type.String;
                newToken.stringValue = accumulator;
            }
            else
            {
                bool found = false;
                foreach (var pair in keywordMapping)
                {
                    if (pair.Item1 == accumulator)
                    {
                        newToken.type = pair.Item2;
                        found = true;
                        break;
                    }
                }

                if (!found)
                    throw new Exception("invalid keyword");
            }

            tokens.Add(newToken);

            accumulatedJunk = "";
            accumulator = "";
        }

        int column = 1;
        int row = 1;

        int characterIndex = 0;

        bool inputStartsWith(string s)
        {
            if (characterIndex + s.Length > input.Length)
                return false;

            for (int i = 0; i < s.Length; i++)
            {
                if (s[i] != input[characterIndex + i])
                    return false;
            }

            return true;
        }

        for (; characterIndex < input.Length; characterIndex++)
        {
            char c = input[characterIndex];
            if (c == '\n')
            {
                row++;
                column = 0;
            }
            else
            {
                column++;
            }


            if (inComment)
            {
                accumulatedJunk += c;
                if (c == '\n')
                    inComment = false;

                continue;
            }

            if (inBareString)
            {
                if (isWhitespace(c) || c == '{' || c == '}' || inputStartsWith("<") || inputStartsWith(">") || inputStartsWith("=") || inputStartsWith("!=") || inputStartsWith("?="))
                {
                    breakToken();
                    inBareString = false;
                    // no continue here, so we will process the character as (potentially) the start of a new token
                }
                else
                {
                    accumulator += c;
                    continue;
                }
            }

            if (inQuotedString)
            {
                accumulator += c;

                if (c == '"')
                {
                    breakToken();
                    inQuotedString = false;
                }
                continue;
            }


            if (accumulator.Length == 0)
            {
                bool foundKeyword = false;
                foreach (var pair in keywordMapping)
                {
                    if (inputStartsWith(pair.Item1))
                    {
                        startColumn = column;
                        startRow = row;
                        accumulator += pair.Item1;
                        breakToken();
                        characterIndex += pair.Item1.Length - 1;
                        foundKeyword = true;
                        break;
                    }
                }

                if (foundKeyword)
                    continue;

                if (c == '"')
                {
                    startColumn = column;
                    startRow = row;
                    accumulator += c;
                    inQuotedString = true;
                }
                else if (c == '#')
                {
                    breakToken();
                    inComment = true;
                    accumulatedJunk += c;
                }
                else if (isWhitespace(c))
                {
                    accumulatedJunk += c;
                }
                else
                {
                    startColumn = column;
                    startRow = row;
                    inBareString = true;
                    accumulator += c;
                }
            }
            else
            {
                throw new Exception();
            }
        }

        breakToken();

        Token endToken = new Token();
        endToken.type = Token.Type.FileEnd;
        endToken.ignoredTextBeforeToken = accumulatedJunk;
        tokens.Add(endToken);

        return tokens;
    }

    private static bool isWhitespace(char c)
    {
        return c == '\t' || c == '\r' || c == '\n' || c == ' ';
    }
}