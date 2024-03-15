using System.Text;

public class CkObjectRoot : CkObject
{
    public bool linesHaveCarriageReturns;
}

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

    public string serialise()
    {
        return CkObjectSerialiser.serialise(this);
    }
}

public class CkKeyValuePair
{
    public string whitespaceBeforeKeyName = null;
    public string whitespaceBeforeOperator = null;
    public string whitespaceBeforeTypeTag = null;
    public string whitespaceBeforeValue = null;

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