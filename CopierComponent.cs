using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Copier;

/// <summary>
///     Component for the copier machine.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CopierComponent : Component
{
    [DataField, AutoNetworkedField]
    public int TonerAmount = 25;

    [DataField, AutoNetworkedField]
    public int MaxTonerAmount = 25;

    [DataField, AutoNetworkedField]
    public CopierMode Mode = CopierMode.Print;

    [DataField, AutoNetworkedField]
    public string CopiedText = string.Empty;

    [DataField]
    public List<string> AvailableCategories = new();

    [DataField]
    public SoundSpecifier PrintSound = new SoundPathSpecifier("/Audio/Machines/scanning.ogg");

    [DataField, AutoNetworkedField]
    public TimeSpan NextPrintTime;
}

[Serializable, NetSerializable]
public enum CopierMode : byte
{
    Print,
    Copy
}
