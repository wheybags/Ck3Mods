public static class Test
{
    public static void run()
    {
        testNoSpaceBeforeCloseBracket();
        testOneString();
        testArray();
        testMixedArrayDict();
        testArrayInArray();
    }

    private static void assert(bool val)
    {
        if (!val)
            throw new Exception("test failed");
    }

    private static void testNoSpaceBeforeCloseBracket()
    {
        List<Token> tokens = Tokeniser.tokenise("scope:target = { exists = var:relic_religion}\n");

        assert(tokens.Count == 8);
        assert(tokens[0].type == Token.Type.String && tokens[0].stringValue == "scope:target");
        assert(tokens[1].type == Token.Type.Assign);
        assert(tokens[2].type == Token.Type.OpenBrace);
        assert(tokens[3].type == Token.Type.String && tokens[3].stringValue == "exists");
        assert(tokens[4].type == Token.Type.Assign);
        assert(tokens[5].type == Token.Type.String && tokens[5].stringValue == "var:relic_religion");
        assert(tokens[6].type == Token.Type.CloseBrace);
        assert(tokens[7].type == Token.Type.FileEnd);

        Parser parser = new Parser();
        parser.tokens = tokens;

        CkObject data = parser.parseRoot();

        assert(data.valuesList.Count == 1);
        assert(data.valuesList[0].key == "scope:target");

        CkObject subObject = data.valuesList[0].valueObject;
        assert(subObject.valuesList.Count == 1);
        assert(subObject.valuesList[0].key == "exists");
        assert(subObject.valuesList[0].valueString == "var:relic_religion");
    }

    private static void testOneString()
    {
        CkObject data = Parser.parse("abc");
        assert(data.valuesList.Count == 1);
        assert(data.valuesList[0].valueString == "abc");
    }

    private static void testArray()
    {
        CkObject data = Parser.parse("arr = { a \"b\" 1.5 }");
        assert(data.valuesList.Count == 1);

        CkObject array = data.valuesList[0].valueObject;
        assert(array.valuesList.Count == 3);
        assert(array.valuesList[0].valueString == "a");
        assert(array.valuesList[1].valueString == "\"b\"");
        assert(array.valuesList[2].valueString == "1.5");
    }

    private static void testMixedArrayDict()
    {
        CkObject data = Parser.parse("arr = { a b x = 10 c }");
        assert(data.valuesList.Count == 1);

        CkObject items = data.valuesList[0].valueObject;
        assert(items.valuesList.Count == 4);
        assert(items.valuesList[0].valueString == "a");
        assert(items.valuesList[1].valueString == "b");
        assert(items.valuesList[2].key == "x" && items.valuesList[2].valueString == "10");
        assert(items.valuesList[3].valueString == "c");
    }

    private static void testArrayInArray()
    {
        CkObject data = Parser.parse("arr = { {a b} {c d} }");
        assert(data.valuesList.Count == 1);

        CkObject array = data.valuesList[0].valueObject;
        assert(array.valuesList.Count == 2);

        CkObject subArray1 = array.valuesList[0].valueObject;
        assert(subArray1.valuesList.Count == 2);
        assert(subArray1.valuesList[0].valueString == "a");
        assert(subArray1.valuesList[1].valueString == "b");

        CkObject subArray2 = array.valuesList[1].valueObject;
        assert(subArray2.valuesList.Count == 2);
        assert(subArray2.valuesList[0].valueString == "c");
        assert(subArray2.valuesList[1].valueString == "d");
    }
}