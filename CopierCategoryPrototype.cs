using Robust.Shared.Prototypes;

namespace Content.Shared.Copier;

/// <summary>
///     Prototype for a category of printable documents in the copier.
/// </summary>
[Prototype("copierCategory")]
public sealed partial class CopierCategoryPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public string Name = string.Empty;

    /// <summary>
    ///     Entity IDs of the documents in this category.
    /// </summary>
    [DataField(required: true)]
    public List<EntProtoId> Documents = new();
}
