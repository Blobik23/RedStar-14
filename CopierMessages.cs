using Robust.Shared.Serialization;

namespace Content.Shared.Copier;

[Serializable, NetSerializable]
public sealed class CopierBoundUserInterfaceState : BoundUserInterfaceState
{
    public int TonerAmount;
    public int MaxTonerAmount;
    public CopierMode Mode;
    public List<CopierCategoryInfo> Categories;

    public CopierBoundUserInterfaceState(int toner, int maxToner, CopierMode mode, List<CopierCategoryInfo> categories)
    {
        TonerAmount = toner;
        MaxTonerAmount = maxToner;
        Mode = mode;
        Categories = categories;
    }
}

[Serializable, NetSerializable]
public sealed class CopierCategoryInfo
{
    public string Id;
    public string Name;
    public List<CopierDocInfo> Documents;

    public CopierCategoryInfo(string id, string name, List<CopierDocInfo> documents)
    {
        Id = id;
        Name = name;
        Documents = documents;
    }
}

[Serializable, NetSerializable]
public sealed class CopierDocInfo
{
    public string Id;
    public string Name;

    public CopierDocInfo(string id, string name)
    {
        Id = id;
        Name = name;
    }
}

[Serializable, NetSerializable]
public sealed class CopierPrintMessage : BoundUserInterfaceMessage
{
    public string DocId;
    public int Copies;

    public CopierPrintMessage(string docId, int copies)
    {
        DocId = docId;
        Copies = copies;
    }
}

[Serializable, NetSerializable]
public sealed class CopierCopyMessage : BoundUserInterfaceMessage
{
    public int Copies;
    public CopierCopyMessage(int copies) => Copies = copies;
}

[Serializable, NetSerializable]
public sealed class CopierSetModeMessage : BoundUserInterfaceMessage
{
    public CopierMode Mode;
    public CopierSetModeMessage(CopierMode mode) => Mode = mode;
}
