// Все модификации и наработки в ss14-wega под тегом Corvax-Wega и директориях _Wega лицензированы под GNU GPL v3.
// https://github.com/corvax-team/ss14-wega/blob/master/LICENSE.TXT

using System.Numerics;
using Content.Shared._Wega.Lavaland.Components;
using Content.Shared.Projectiles;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared._Wega.Lavaland.Systems;

public sealed class HierophantTrophySystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<HierophantTrophyComponent, MeleeHitEvent>(OnMeleeHit);
        SubscribeLocalEvent<HierophantTrophyComponent, GunShotEvent>(OnGunShot);
        SubscribeLocalEvent<HierophantTrophyProjectileComponent, ProjectileHitEvent>(OnProjectileHit);
    }

    private void OnMeleeHit(Entity<HierophantTrophyComponent> ent, ref MeleeHitEvent args)
    {
        if (!_net.IsServer || args.HitEntities.Count == 0 || !TryActivate(ent.Comp))
            return;

        var direction = args.Direction;
        if (direction is null && args.HitEntities.Count > 0)
        {
            var delta = _transform.GetWorldPosition(args.HitEntities[0]) - _transform.GetWorldPosition(args.User);
            direction = delta;
        }

        SpawnWalls(ent.Comp, Transform(args.User).Coordinates, direction ?? Vector2.UnitX);
    }

    private void OnGunShot(Entity<HierophantTrophyComponent> ent, ref GunShotEvent args)
    {
        if (!_net.IsServer)
            return;

        foreach (var (ammo, _) in args.Ammo)
        {
            if (ammo is not { } projectile || !HasComp<ProjectileComponent>(projectile))
                continue;

            EnsureComp<HierophantTrophyProjectileComponent>(projectile).Upgrade = ent.Owner;
        }
    }

    private void OnProjectileHit(Entity<HierophantTrophyProjectileComponent> ent, ref ProjectileHitEvent args)
    {
        if (!_net.IsServer
            || !TryComp<HierophantTrophyComponent>(ent.Comp.Upgrade, out var trophy)
            || !TryActivate(trophy))
            return;

        var targetCoordinates = Transform(args.Target).Coordinates;
        var direction = Transform(ent).LocalRotation.ToWorldVec();
        if (args.Shooter is { } shooter && Exists(shooter))
        {
            var delta = _transform.GetWorldPosition(args.Target) - _transform.GetWorldPosition(shooter);
            if (delta != Vector2.Zero)
                direction = delta.Normalized();
        }

        SpawnWalls(trophy, targetCoordinates, direction);
    }

    private bool TryActivate(HierophantTrophyComponent component)
    {
        if (_timing.CurTime < component.NextActivation)
            return false;

        component.NextActivation = _timing.CurTime + component.Cooldown;
        return true;
    }

    private void SpawnWalls(HierophantTrophyComponent component, EntityUid user)
    {
        if (!Exists(user))
            return;

        var transform = Transform(user);
        SpawnWalls(component, transform.Coordinates, transform.LocalRotation.ToWorldVec());
    }

    private void SpawnWalls(
        HierophantTrophyComponent component,
        EntityCoordinates coordinates,
        Vector2 direction)
    {
        if (direction == Vector2.Zero)
            direction = Vector2.UnitX;

        direction = direction.Normalized();
        var perpendicular = new Vector2(-direction.Y, direction.X);

        for (var i = -1; i <= 1; i++)
            Spawn(component.WallPrototype, coordinates.Offset(perpendicular * i));
    }
}
