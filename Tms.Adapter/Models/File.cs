namespace Tms.Adapter.Models;

public class File
{
    public string Name { get; set; } = null!;
    public string Content { get; set; } = null!;
    public string CallerMemberName { get; set; } = null!;
    public string PathToFile { get; set; } = null!;
}