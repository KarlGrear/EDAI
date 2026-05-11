namespace EDAI.Core.Models;

public sealed record CategoryModel(int Id, string Name)
{
    public static readonly CategoryModel All = new(-1, "(All)");
}
