// Все модификации и наработки в ss14-wega под тегом Corvax-Wega и директориях _Wega лицензированы под GNU GPL v3.
// https://github.com/corvax-team/ss14-wega/blob/master/LICENSE.TXT

using Content.Server._Wega.Lavaland.Mobs.Components;
using Content.Shared._Wega.Lavaland.Events;
using Content.Shared.Body.Part;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.SSDIndicator;
using Robust.Shared.Audio.Systems;

namespace Content.Server._Wega.Lavaland.Mobs;

public sealed partial class BloodDrunkMinerSystem : EntitySystem
{
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BloodDrunkMinerComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<BloodDrunkMinerComponent, BloodDrunkMinerDashAction>(OnDash);
        SubscribeLocalEvent<BloodDrunkMinerComponent, BodyPartRemovedEvent>(OnBodyPartRemoved);
    }

    private void OnMapInit(Entity<BloodDrunkMinerComponent> ent, ref MapInitEvent args)
    {
        RemComp<SSDIndicatorComponent>(ent);
    }

    private void OnDash(Entity<BloodDrunkMinerComponent> ent, ref BloodDrunkMinerDashAction args)
    {
        args.Handled = true;
        _transform.SetCoordinates(ent, args.Target);
        _audio.PlayPvs(args.DashSound, ent);
    }

    private void OnBodyPartRemoved(Entity<BloodDrunkMinerComponent> ent, ref BodyPartRemovedEvent args)
    {
        if (args.Part.Comp.PartType == BodyPartType.Head)
            _mobState.ChangeMobState(ent, MobState.Dead);
    }
}
