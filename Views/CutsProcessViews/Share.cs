namespace DicingBlade.Views.CutsProcessViews;

public class Share
{
    public Share(int part, int total)
    {
        Part = part;
        Total = total;
    }
    public int Part
    {
        get; set;
    }
    public int Total
    {
        get; set;
    }
    public static bool IsChanged
    {
        get; set;
    }
}
