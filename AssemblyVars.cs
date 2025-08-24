namespace YouToot
{
    [AttributeUsage(AttributeTargets.Assembly)]
    public class BuildDateTimeAttribute : Attribute
    {
        public string Date { get; }

        public BuildDateTimeAttribute(string date)
        {
            Date = date;
        }
    }
}