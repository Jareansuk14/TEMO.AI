namespace TEMO.AI;

internal sealed record FieldDef(
    string Id,
    string Label,
    string Section,
    string DataFile,
    string DataConst,
    string Key,
    bool Multi = false,
    int ArrayIndex = -1)
{
    public bool IsArray => ArrayIndex >= 0;
    public bool IsBrand => Id == "brand";
}
