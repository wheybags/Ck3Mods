// Root                 = ObjectBody $End
// ObjectBody           = Item ObjectBody | Nil
// Item                 = $String ItemRest | "{" ObjectBody "}"
// ItemRest             = Operator AfterOperator | Nil
// Operator             = "=" | ">=" | "?=" | "<=" | "==" | "<" | ">"
// AfterOperator        = $String MaybeObject | "{" ObjectBody "}"
// MaybeObject          = "{" ObjectBody "}" | Nil


public class Parser
{
    public static CkObject parse(string inputString)
    {
        Parser parser = new Parser();
        parser.tokens = Tokeniser.tokenise(inputString);
        return parser.parseRoot();
    }

    public List<Token> tokens;
    private int tokenIndex = 0;

    private string tempBefore = "";

    Token peek()
    {
        return tokens[tokenIndex];
    }

    Token pop()
    {
        Token retval = tokens[tokenIndex];
        tokenIndex++;
        return retval;
    }

    public CkObject parseRoot()
    {
        CkObject root = new CkObject();
        parseObjectBody(root);
        if (peek().type != Token.Type.FileEnd)
            throw new Exception("expected file end");
        return root;
    }

    private void parseObjectBody(CkObject obj)
    {
        if (peek().type == Token.Type.String || peek().type == Token.Type.OpenBrace)
        {
            parseItem(obj);
            parseObjectBody(obj);
        }
        else if (peek().type == Token.Type.FileEnd || peek().type == Token.Type.CloseBrace)
        {
            obj.whitespaceAfterLastValue = peek().ignoredTextBeforeToken;
        }
        else
        {
            throw new Exception("unexpected");
        }
    }

    private void parseItem(CkObject obj)
    {
        if (peek().type == Token.Type.String)
        {
            Token keyToken = pop();
            parseItemRest(obj, keyToken);
        }
        else if (peek().type == Token.Type.OpenBrace)
        {
            CkKeyValuePair pair = new CkKeyValuePair();
            obj.valuesList.Add(pair);

            pair.whitespaceBeforeValue = peek().ignoredTextBeforeToken;
            pop();
            pair.valueObject = new CkObject();
            parseObjectBody(pair.valueObject);
            if (pop().type != Token.Type.CloseBrace)
                throw new Exception("expected }");
        }
        else
        {
            throw new Exception("unexpected");
        }
    }

    private void parseItemRest(CkObject obj, Token keyToken)
    {
        CkKeyValuePair pair = new CkKeyValuePair();

        if (peek().type == Token.Type.Less ||
            peek().type == Token.Type.Greater ||
            peek().type == Token.Type.LessOrEqual ||
            peek().type == Token.Type.GreaterOrEqual ||
            peek().type == Token.Type.QuestionEqual ||
            peek().type == Token.Type.Assign ||
            peek().type == Token.Type.Equals)
        {
            pair.whitespaceBeforeKeyName = keyToken.ignoredTextBeforeToken;
            pair.key = keyToken.stringValue;

            parseOperator(pair);
            parseAfterOperator(pair);
        }
        else // TODO: assert
        {
            pair.whitespaceBeforeValue = keyToken.ignoredTextBeforeToken;
            pair.valueString = keyToken.stringValue;
            pair.operatorString = null;
        }

        obj.valuesList.Add(pair);
    }

    private void parseOperator(CkKeyValuePair pair)
    {
        if (peek().type == Token.Type.Assign)
        {
            pair.operatorString = "=";
            pair.whitespaceBeforeOperator = peek().ignoredTextBeforeToken;
            pop();
        }
        else if (peek().type == Token.Type.GreaterOrEqual)
        {
            pair.operatorString = ">=";
            pair.whitespaceBeforeOperator = peek().ignoredTextBeforeToken;
            pop();
        }
        else if (peek().type == Token.Type.LessOrEqual)
        {
            pair.operatorString = "<=";
            pair.whitespaceBeforeOperator = peek().ignoredTextBeforeToken;
            pop();
        }
        else if (peek().type == Token.Type.QuestionEqual)
        {
            pair.operatorString = "?=";
            pair.whitespaceBeforeOperator = peek().ignoredTextBeforeToken;
            pop();
        }
        else if (peek().type == Token.Type.Less)
        {
            pair.operatorString = "<";
            pair.whitespaceBeforeOperator = peek().ignoredTextBeforeToken;
            pop();
        }
        else if (peek().type == Token.Type.Greater)
        {
            pair.operatorString = ">";
            pair.whitespaceBeforeOperator = peek().ignoredTextBeforeToken;
            pop();
        }
        else if (peek().type == Token.Type.Equals)
        {
            pair.operatorString = "==";
            pair.whitespaceBeforeOperator = peek().ignoredTextBeforeToken;
            pop();
        }
        else
        {
            throw new Exception("expected valid operator");
        }
    }

    private void parseAfterOperator(CkKeyValuePair pair)
    {
        if (peek().type == Token.Type.String)
        {
            pair.whitespaceBeforeValue = peek().ignoredTextBeforeToken;

            Token startToken = pop();
            parseMaybeObject(startToken, pair);
        }
        else if (peek().type == Token.Type.OpenBrace)
        {
            pair.whitespaceBeforeValue = peek().ignoredTextBeforeToken;
            pop();
            pair.valueObject = new CkObject();
            parseObjectBody(pair.valueObject);
            if (pop().type != Token.Type.CloseBrace)
                throw new Exception("expected }");
        }
        else
        {
            throw new Exception("expected String or {");
        }
    }

    private void parseMaybeObject(Token startToken, CkKeyValuePair pair)
    {
        if (peek().type == Token.Type.OpenBrace)
        {
            pair.whitespaceBeforeTypeTag = startToken.ignoredTextBeforeToken;
            pair.typeTag = startToken.stringValue;

            pair.whitespaceBeforeValue = peek().ignoredTextBeforeToken;
            if (pop().type != Token.Type.OpenBrace)
                throw new Exception("expected {");

            pair.valueObject = new CkObject();
            parseObjectBody(pair.valueObject);

            if (pop().type != Token.Type.CloseBrace)
                throw new Exception("expected }");
        }
        else // todo: assert
        {
            pair.whitespaceBeforeValue = startToken.ignoredTextBeforeToken;
            pair.valueString = startToken.stringValue;
        }
    }
}