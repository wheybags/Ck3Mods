

using System.Text;

public class CkObject
{
    public List<CkKeyValuePair> valuesList = new List<CkKeyValuePair>();
    public string whitespaceAfterLastValue = " ";

    public CkKeyValuePair findFirstWithName(string name)
    {
        foreach (CkKeyValuePair item in valuesList)
        {
            if (item.key == name)
                return item;
        }

        return null;
    }

    public static string serialise(CkObject obj)
    {
        StringBuilder stringBuilder = new StringBuilder();
        serialise(stringBuilder, obj);
        return stringBuilder.ToString();
    }

    public static void serialise(StringBuilder stringBuilder, CkObject obj)
    {
        foreach (CkKeyValuePair pair in obj.valuesList)
        {
            if (pair.key != null)
            {
                stringBuilder.Append(pair.whitespaceBeforeKeyName);
                stringBuilder.Append(pair.key);
                stringBuilder.Append(pair.whitespaceBeforeOperator);
                stringBuilder.Append(pair.operatorString);
            }

            if (pair.valueIsString)
            {
                stringBuilder.Append(pair.whitespaceBeforeValue);

                stringBuilder.Append(pair.valueString);
            }
            else
            {
                if (pair.typeTag != null)
                {
                    stringBuilder.Append(pair.whitespaceBeforeTypeTag);
                    stringBuilder.Append(pair.typeTag);
                }

                stringBuilder.Append(pair.whitespaceBeforeValue);
                stringBuilder.Append("{");
                serialise(stringBuilder, pair.valueObject);
                stringBuilder.Append("}");
            }
        }

        stringBuilder.Append(obj.whitespaceAfterLastValue);
    }
}

public class CkKeyValuePair
{
    public string whitespaceBeforeKeyName = " ";
    public string whitespaceBeforeOperator = " ";
    public string whitespaceBeforeTypeTag = " ";
    public string whitespaceBeforeValue = " ";

    public string typeTag = null;
    public string key = null;
    public string operatorString = "=";

    public bool valueIsString { get; private set; }

    private string _valueString;
    public string valueString {
        get
        {
            if (!valueIsString)
                throw new Exception("not a string");
            return _valueString!;
        }
        set
        {
            valueIsString = true;
            _valueString = value;
        }
    }

    private CkObject _valueObject;
    public CkObject valueObject {
        get
        {
            if (valueIsString)
                throw new Exception("not a CkObject");
            return _valueObject!;
        }
        set
        {
            valueIsString = false;
            _valueObject = value;
        }
    }
}