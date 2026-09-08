namespace skestock.Domain.Entities;

public sealed class CategoryIcon
{
    private CategoryIcon()
    {
    }

    public CategoryIcon(string name, string fileName, string path)
    {
        Name = name;
        FileName = fileName;
        Path = path;
    }

    public string Name { get; private set; } = null!;

    public string FileName { get; private set; } = null!;

    public string Path { get; private set; } = null!;
}
