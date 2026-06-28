using Robust.Shared.GameStates;

namespace Content.Shared.Copier;

/// <summary>
///     Toner cartridge used to refill copier machines.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TonerCartridgeComponent : Component
{
    [DataField, AutoNetworkedField]
    public int TonerAmount = 25;

    [DataField, AutoNetworkedField]
    public int MaxTonerAmount = 25;
}
