public static class Tests
{
    public static void run()
    {
        TestDataParser.run();
        TestLocalisationParser.run();
    }
}

public static class TestDataParser
{
    public static void run()
    {
        testNoSpaceBeforeCloseBracket();
        testOneString();
        testArray();
        testMixedArrayDict();
        testArrayInArray();
        testTypeTag();
        testMissingCloseBrace();
        testExtraCloseBrace();
    }

    private static void assert(bool val)
    {
        if (!val)
            throw new Exception("test failed");
    }

    private static void testNoSpaceBeforeCloseBracket()
    {
        string input = "scope:target = { exists = var:relic_religion}\n";
        TokeniserOutput tokens = Tokeniser.tokenise(input);

        assert(tokens.tokens.Count == 8);
        assert(tokens.tokens[0].type == Token.Type.String && tokens.tokens[0].stringValue == "scope:target");
        assert(tokens.tokens[1].type == Token.Type.Assign);
        assert(tokens.tokens[2].type == Token.Type.OpenBrace);
        assert(tokens.tokens[3].type == Token.Type.String && tokens.tokens[3].stringValue == "exists");
        assert(tokens.tokens[4].type == Token.Type.Assign);
        assert(tokens.tokens[5].type == Token.Type.String && tokens.tokens[5].stringValue == "var:relic_religion");
        assert(tokens.tokens[6].type == Token.Type.CloseBrace);
        assert(tokens.tokens[7].type == Token.Type.FileEnd);

        Parser parser = new Parser();
        parser.tokens = tokens;

        CkObject data = parser.parseRoot();

        assert(data.valuesList.Count == 1);
        assert(data.valuesList[0].key == "scope:target");

        CkObject subObject = data.valuesList[0].valueObject;
        assert(subObject.valuesList.Count == 1);
        assert(subObject.valuesList[0].key == "exists");
        assert(subObject.valuesList[0].valueString == "var:relic_religion");

        assert(data.serialise() == input);
    }

    private static void testOneString()
    {
        string input = "abc";
        CkObject data = Parser.parse(input);
        assert(data.valuesList.Count == 1);
        assert(data.valuesList[0].valueString == "abc");

        assert(data.serialise() == input);
    }

    private static void testArray()
    {
        string input = "arr = { a \"b\" 1.5 }";
        CkObject data = Parser.parse(input);
        assert(data.valuesList.Count == 1);

        CkObject array = data.valuesList[0].valueObject;
        assert(array.valuesList.Count == 3);
        assert(array.valuesList[0].valueString == "a");
        assert(array.valuesList[1].valueString == "\"b\"");
        assert(array.valuesList[2].valueString == "1.5");

        assert(data.serialise() == input);
    }

    private static void testMixedArrayDict()
    {
        string input = "arr = { a b x = 10 c }";
        CkObject data = Parser.parse(input);
        assert(data.valuesList.Count == 1);

        CkObject items = data.valuesList[0].valueObject;
        assert(items.valuesList.Count == 4);
        assert(items.valuesList[0].valueString == "a");
        assert(items.valuesList[1].valueString == "b");
        assert(items.valuesList[2].key == "x" && items.valuesList[2].valueString == "10");
        assert(items.valuesList[3].valueString == "c");

        assert(data.serialise() == input);
    }

    private static void testArrayInArray()
    {
        string input = "arr = { {a b} {c d} }";
        CkObject data = Parser.parse(input);
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

        assert(data.serialise() == input);
    }

    private static void testTypeTag()
    {
        string input = "color =  rgb   { 100 200 150 }";
        CkObject data = Parser.parse(input);
        assert(data.valuesList.Count == 1);
        assert(data.valuesList[0].key == "color");
        assert(data.valuesList[0].typeTag == "rgb");

        CkObject color = data.valuesList[0].valueObject;
        assert(color.valuesList.Count == 3);
        assert(color.valuesList[0].valueString == "100");
        assert(color.valuesList[1].valueString == "200");
        assert(color.valuesList[2].valueString == "150");

        assert(data.serialise() == input);
    }

    private static void testMissingCloseBrace()
    {
        string input = "arr = { {a b} {c d";
        CkObject data = Parser.parse(input);
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

        // we may have to parse broken files, but we shall *not* produce them
        assert(data.serialise() == input + "}}");
    }

    private static void testExtraCloseBrace()
    {
        string input = "{a b}}} {c d}";
        CkObject array = Parser.parse(input);
        assert(array.valuesList.Count == 2);

        CkObject subArray1 = array.valuesList[0].valueObject;
        assert(subArray1.valuesList.Count == 2);
        assert(subArray1.valuesList[0].valueString == "a");
        assert(subArray1.valuesList[1].valueString == "b");

        CkObject subArray2 = array.valuesList[1].valueObject;
        assert(subArray2.valuesList.Count == 2);
        assert(subArray2.valuesList[0].valueString == "c");
        assert(subArray2.valuesList[1].valueString == "d");

        // we may have to parse broken files, but we shall *not* produce them
        assert(array.serialise() == "{a b} {c d}");
    }
}

public static class TestLocalisationParser
{
   public static void run()
   {
      testBasic();
      testEOFReserialise();
      testEmbeddedQuotes();
      testCreateKey();
      testCreateFromScratch();
      testTrailingSpace();
      testCommentAfterEntry();
   }

   private static void assert(bool val)
   {
      if (!val)
         throw new Exception("test failed");
   }

   private static void testBasic()
   {
      string input = "l_english:\n\n #############################################\n#General names:\n baron:0 \"Baron\"";
      LocalisationFileData data = LocalisationParser.parse(input);
      assert(data.sections.Count == 1);
      assert(data.sections[0].key == "l_english");
      assert(data.sections[0].entries.Count == 1);
      assert(data.sections[0].entries[0].key == "baron");
      assert(data.sections[0].entries[0].number == 0);
      assert(data.sections[0].entries[0].value == "Baron");

      string serialised = LocalisationSerialiser.serialise(data);
      assert(serialised == input);
   }

   private static void testEOFReserialise()
   {
      string input = "abc:\n count_herder_female: \"Shepherdess\"\n";
      LocalisationFileData data = LocalisationParser.parse(input);
      assert(data.sections.Count == 1);
      assert(data.sections[0].entries.Count == 1);
      string serialised = LocalisationSerialiser.serialise(data);
      assert(serialised == input);
   }

   private static void testEmbeddedQuotes()
   {
      string input = "abc:\n key: \"value with \"quotes\" in it\"";
      LocalisationFileData data = LocalisationParser.parse(input);
      assert(data.sections.Count == 1);
      assert(data.sections[0].entries.Count == 1);
      assert(data.sections[0].entries[0].key == "key");
      assert(data.sections[0].entries[0].value == "value with \"quotes\" in it");
   }

   private static void testCreateKey()
   {
      string input = "abc:\n a: \"A\"";
      LocalisationFileData data = LocalisationParser.parse(input);
      data.sections[0].set("b", "B");
      string serialised = LocalisationSerialiser.serialise(data);
      assert(serialised == "abc:\n a: \"A\"\n b: \"B\"");
   }

   private static void testCreateFromScratch()
   {
      LocalisationFileData data = new LocalisationFileData();
      LocalisationSection section = new LocalisationSection();
      section.key = "abc";
      data.sections.Add(section);
      section.set("a", "A");
      section.set("b", "B");
      string serialised = LocalisationSerialiser.serialise(data);
      assert(serialised == "abc:\n a: \"A\"\n b: \"B\"");
   }

   private static void testTrailingSpace()
   {
       string input = "l_english:\n barony_feudal_rimmen: \"Seat\" \n baron_feudal_male_rimmen: \"Master\"";

       LocalisationFileData data = LocalisationParser.parse(input);
       assert(data.sections.Count == 1);
       assert(data.sections[0].key == "l_english");
       assert(data.sections[0].entries.Count == 2);

       assert(data.sections[0].entries[0].key == "barony_feudal_rimmen");
       assert(data.sections[0].entries[0].number == null);
       assert(data.sections[0].entries[0].value == "Seat");

       assert(data.sections[0].entries[1].key == "baron_feudal_male_rimmen");
       assert(data.sections[0].entries[1].number == null);
       assert(data.sections[0].entries[1].value == "Master");

       string serialised = LocalisationSerialiser.serialise(data);
       assert(serialised == input);
   }

   private static void testCommentAfterEntry()
   {
       string input = "l_english:\n empire_mercenary: \"Grand Army\" #of the Republic\n a: \"b\"";

       LocalisationFileData data = LocalisationParser.parse(input);
       assert(data.sections.Count == 1);
       assert(data.sections[0].key == "l_english");
       assert(data.sections[0].entries.Count == 2);

       assert(data.sections[0].entries[0].key == "empire_mercenary");
       assert(data.sections[0].entries[0].number == null);
       assert(data.sections[0].entries[0].value == "Grand Army");

       assert(data.sections[0].entries[1].key == "a");
       assert(data.sections[0].entries[1].number == null);
       assert(data.sections[0].entries[1].value == "b");

       string serialised = LocalisationSerialiser.serialise(data);
       assert(serialised == input);
   }
}