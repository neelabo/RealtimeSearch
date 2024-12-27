namespace NeeLaboratory.Input
{
    public abstract class StringConverter
    {
        public static StringConverter Default { get; } = new DefaultStringConverter();
        public abstract string Convert(string value);
    }


    public class DefaultStringConverter : StringConverter
    {
        public override string Convert(string value)
        {
            return value;
        }
    }
}
