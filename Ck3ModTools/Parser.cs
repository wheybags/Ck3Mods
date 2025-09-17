
// See https://pdx.tools/blog/a-tour-of-pds-clausewitz-syntax

// What is not supported?
// 1: operators as key names: =="bar"
// here '=' "should" be the key and 'bar' the value
//
// 2: objects with implicit operator: foo{bar=qux}
// this should be equivalent to foo={bar=qux}
//
// 3: this [[]] syntax is not supported at all (I think it's not in ck3 though)
// generate_advisor = {
//   [[scaled_skill]
//   $scaled_skill$
//       ]
//   [[!skill] if = {} ]
// }
//
// 4: semicolons
// Apparently you can put semicolons in certain places and they should be ignored. This isn't supported
//
// 5: this "list" syntax:
// simple_cross_flag = {
//     pattern = list "christian_emblems_list"
//     color1 = list "normal_colors"
// }
// because I haven't seen a real example, and I can't tell from this one how it should work


public class Parser
{
    // This is a simple handwritten recursive descent parser
    //
    // Grammar:
    //     Root                 = ObjectBody $End
    //     ObjectBody           = Item ObjectBody | Nil
    //     Item                 = $String ItemRest | "{" ObjectBody CloseBrace
    //     ItemRest             = Operator AfterOperator | Nil
    //     Operator             = "=" | ">=" | "?=" | "<=" | "==" | "<" | ">"
    //     AfterOperator        = $String MaybeObject | "{" ObjectBody CloseBrace
    //     MaybeObject          = "{" ObjectBody CloseBrace | Nil
    //     CloseBrace           = "}" // this rule exists so we can insert some special logic to handle extra / missing "}" in the top level ObjectBody
    //
    // Firsts:
    //     Root          = $String "{" $End
    //     ObjectBody    = $String "{" |
    //     Item          = $String | "{"
    //     ItemRest      = "=" ">=" "?=" "<=" "==" "<" ">" |
    //     Operator      = "=" | ">=" | "?=" | "<=" | "==" | "<" | ">"
    //     AfterOperator = $String | "{"
    //     MaybeObject   = "{" |
    //     CloseBrace    = "}"
    //
    // Follows:
    //     Root          =
    //     ObjectBody    = "}" $End
    //     Item          = "{" "}" $End $String
    //     ItemRest      = "{" "}" $End $String
    //     Operator      = "{" $String
    //     AfterOperator = "{" "}" $End $String
    //     MaybeObject   = "{" "}" $End $String
    //     CloseBrace    = "{" "}" $End $String



    public static CkObject parse(string inputString)
    {
        Parser parser = new Parser();
        parser.tokens = Tokeniser.tokenise(inputString);
        return parser.parseRoot();
    }

    public TokeniserOutput tokens;
    public bool allowBadBraces = true;

    private int tokenIndex = 0;
    private int objectDepth = 0;

    Token peek()
    {
        return tokens.tokens[tokenIndex];
    }

    Token pop()
    {
        Token retval = tokens.tokens[tokenIndex];
        tokenIndex++;
        return retval;
    }

    public CkObjectRoot parseRoot()
    {
        CkObjectRoot root = new CkObjectRoot();
        parseObjectBody(root);
        if (peek().type != Token.Type.FileEnd)
            throw new Exception("expected file end");

        root.linesHaveCarriageReturns = tokens.linesHaveCarriageReturn;
        return root;
    }

    private void parseObjectBody(CkObject obj)
    {
        objectDepth++;

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

        objectDepth--;
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
            parseCloseBrace();
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
        else if (peek().type == Token.Type.OpenBrace ||
                 peek().type == Token.Type.CloseBrace ||
                 peek().type == Token.Type.FileEnd ||
                 peek().type == Token.Type.String)
        {
            pair.whitespaceBeforeValue = keyToken.ignoredTextBeforeToken;
            pair.valueString = keyToken.stringValue;
            pair.operatorString = null;
        }
        else
        {
            throw new Exception("unexpected");
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
            parseCloseBrace();
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
            parseCloseBrace();
        }
        else if (peek().type == Token.Type.OpenBrace ||
                 peek().type == Token.Type.CloseBrace ||
                 peek().type == Token.Type.FileEnd ||
                 peek().type == Token.Type.String)
        {
            pair.whitespaceBeforeValue = startToken.ignoredTextBeforeToken;
            pair.valueString = startToken.stringValue;
        }
        else
        {
            throw new Exception("unexpected");
        }
    }

    private void parseCloseBrace()
    {
        if (allowBadBraces)
        {
            // Paradox has shipped (working in-game) game files with broken close braces, so we have to support them

            // This allows missing close brace(s) at the end of file, eg:
            // "x = {{1 2} {3 4"
            if (peek().type == Token.Type.FileEnd)
                return;

            if (peek().type == Token.Type.CloseBrace)
            {
                pop();

                // This allows extra close brace(s) after top level objects, eg:
                // "x = { a b }} y = { c d }"
                if (objectDepth == 1)
                {
                    while (peek().type == Token.Type.CloseBrace)
                        pop();
                }

                return;
            }
        }
        else
        {
            if (pop().type != Token.Type.CloseBrace)
                throw new Exception("expected }");
        }
    }
}