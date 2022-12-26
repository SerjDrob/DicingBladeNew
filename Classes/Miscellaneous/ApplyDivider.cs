namespace DicingBlade.Classes.Miscellaneous
{
    static class ApplyDivider
    {
        public static T DivideDoubles<T>(this T div, int divider)
        {
            foreach (var property in typeof(T).GetProperties())
            {
                if (property.PropertyType == typeof(double))
                {
                    var value = (double)property.GetValue(div) / divider;
                    property.SetValue(div, value);
                }
            }
            return div;
        }
    }
}