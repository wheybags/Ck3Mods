// Root                 = ObjectBody $End
// ObjectBody           = ObjectBody2 | Nil
// ObjectBody2          = Item ObjectBody
// Item                 = $String ItemRest
// ItemRest             = Operator AfterOperator | Nil
// Operator             = "=" | ">=" | "?=" | "<=" | "==" | "<" | ">"
// AfterOperator        = $String | "{" ObjectBody "}"

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
        if (peek().type == Token.Type.String)
            parseObjectBody2(obj);
        else if (peek().type == Token.Type.FileEnd || peek().type == Token.Type.CloseBrace)
            obj.whitespaceAfterLastValue = peek().ignoredTextBeforeToken;
        else
            throw new Exception("unexpected");
    }

    private void parseObjectBody2(CkObject obj)
    {
        if (peek().type == Token.Type.String)
        {
            parseItem(obj);
            parseObjectBody(obj);
        }
        else
        {
            throw new Exception("expected String");
        }
    }

    private void parseItem(CkObject obj)
    {
        if (peek().type != Token.Type.String)
            throw new Exception("expected BareString");

        Token keyToken = pop();
        parseItemRest(obj, keyToken);
    }

    private void parseItemRest(CkObject obj, Token keyToken)
    {
        CkKeyValuePair pair = new CkKeyValuePair();
        pair.whitespaceBeforeKeyName = keyToken.ignoredTextBeforeToken;

        if (peek().type == Token.Type.Less ||
            peek().type == Token.Type.Greater ||
            peek().type == Token.Type.LessOrEqual ||
            peek().type == Token.Type.GreaterOrEqual ||
            peek().type == Token.Type.QuestionEqual ||
            peek().type == Token.Type.Assign ||
            peek().type == Token.Type.Equals)
        {
            pair.key = keyToken.stringValue;

            parseOperator(pair);
            parseAfterOperator(pair);
        }
        else // TODO: assert
        {
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
            pair.valueString = pop().stringValue;
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
}