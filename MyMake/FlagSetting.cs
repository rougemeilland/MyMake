namespace MyMake
{
    class FlagSetting
    {
        public FlagSetting(string value, string on)
        {
            Value = value;
            On = on;
        }
        public string Value { get; private set; }
        public string On { get; private set; }
    }

}
