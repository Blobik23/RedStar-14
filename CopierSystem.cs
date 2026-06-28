using System.Linq;
using System.Threading;
using Content.Server.Paper;
using Content.Shared.Copier;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Interaction;
using Content.Shared.Paper;
using Content.Shared.Popups;
using Content.Shared.UserInterface;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server.Copier;

/// <summary>
///     Handles copier interactions: toner refill, paper insertion/removal,
///     category-based printing and document copying with sequential output.
/// </summary>
public sealed class CopierSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly PaperSystem _paper = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CopierComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<CopierComponent, CopierPrintMessage>(OnPrintMessage);
        SubscribeLocalEvent<CopierComponent, CopierCopyMessage>(OnCopyMessage);
        SubscribeLocalEvent<CopierComponent, CopierSetModeMessage>(OnSetModeMessage);
        SubscribeLocalEvent<CopierComponent, ActivatableUIOpenAttemptEvent>(OnUIOpenAttempt);
        SubscribeLocalEvent<CopierComponent, BoundUIOpenedEvent>(OnUIOpened);
        SubscribeLocalEvent<CopierComponent, EntInsertedIntoContainerMessage>(OnItemInserted);
        SubscribeLocalEvent<CopierComponent, EntRemovedFromContainerMessage>(OnItemRemoved);
    }

    private void OnUIOpenAttempt(EntityUid uid, CopierComponent copier, ActivatableUIOpenAttemptEvent args)
    {
        if (args.Cancelled) return;
    }

    private void OnUIOpened(EntityUid uid, CopierComponent copier, BoundUIOpenedEvent args)
    {
        UpdateUIState(uid, copier);
    }

    private void OnPrintMessage(EntityUid uid, CopierComponent copier, CopierPrintMessage args)
    {
        TryPrintDocument(uid, copier, args.DocId, args.Copies, args.Actor);
        UpdateUIState(uid, copier);
    }

    private void OnCopyMessage(EntityUid uid, CopierComponent copier, CopierCopyMessage args)
    {
        TryCopyDocument(uid, copier, args.Copies, args.Actor);
        UpdateUIState(uid, copier);
    }

    private void OnSetModeMessage(EntityUid uid, CopierComponent copier, CopierSetModeMessage args)
    {
        copier.Mode = args.Mode;
        Dirty(uid, copier);
        UpdateUIState(uid, copier);
    }

    private void OnInteractUsing(EntityUid uid, CopierComponent copier, InteractUsingEvent args)
    {
        if (TryComp<TonerCartridgeComponent>(args.Used, out var cartridge))
            RefillToner(uid, copier, cartridge, args);
    }

    private void RefillToner(EntityUid uid, CopierComponent copier, TonerCartridgeComponent cartridge, InteractUsingEvent args)
    {
        if (copier.TonerAmount >= copier.MaxTonerAmount)
        {
            _popup.PopupEntity(Loc.GetString("copier-toner-full"), uid, args.User);
            args.Handled = true;
            return;
        }

        copier.TonerAmount = Math.Min(copier.TonerAmount + cartridge.TonerAmount, copier.MaxTonerAmount);

        Del(args.Used);
        _popup.PopupEntity(Loc.GetString("copier-toner-added"), uid, args.User);
        Dirty(uid, copier);
        UpdateUIState(uid, copier);
        args.Handled = true;
    }

    private void OnItemInserted(EntityUid uid, CopierComponent copier, EntInsertedIntoContainerMessage args)
    {
        if (args.Container.ID != "PaperSlot")
            return;

        if (TryComp<PaperComponent>(args.Entity, out var paper))
            copier.CopiedText = paper.Content;

        Dirty(uid, copier);
        UpdateUIState(uid, copier);
    }

    private void OnItemRemoved(EntityUid uid, CopierComponent copier, EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID != "PaperSlot")
            return;

        copier.CopiedText = string.Empty;
        Dirty(uid, copier);
        UpdateUIState(uid, copier);
    }

    /// <summary>
    ///     Prints a document by its entity ID.
    /// </summary>
    public bool TryPrintDocument(EntityUid uid, CopierComponent copier, string docId, int copies, EntityUid user)
    {
        return StartPrintJob(uid, copier, docId, copies, user);
    }

    /// <summary>
    ///     Copies the currently inserted document.
    /// </summary>
    public bool TryCopyDocument(EntityUid uid, CopierComponent copier, int copies, EntityUid user)
    {
        if (_itemSlots.GetItemOrNull(uid, "PaperSlot") == null)
        {
            _popup.PopupEntity(Loc.GetString("copier-no-paper-inserted"), uid, user);
            return false;
        }

        if (string.IsNullOrEmpty(copier.CopiedText))
        {
            _popup.PopupEntity(Loc.GetString("copier-empty-document"), uid, user);
            return false;
        }

        return StartPrintJob(uid, copier, null, copies, user, copier.CopiedText);
    }

    /// <summary>
    ///     Starts a print job with sequential output and cooldown.
    /// </summary>
    private bool StartPrintJob(EntityUid uid, CopierComponent copier, string? docId, int copies, EntityUid user, string? copyContent = null)
    {
        var curTime = _timing.CurTime;

        if (curTime < copier.NextPrintTime)
        {
            var remaining = (int)(copier.NextPrintTime - curTime).TotalSeconds;
            _popup.PopupEntity(Loc.GetString("copier-cooldown", ("seconds", remaining)), uid, user);
            return false;
        }

        if (copier.TonerAmount < copies)
        {
            _popup.PopupEntity(Loc.GetString("copier-no-toner"), uid, user);
            return false;
        }

        copier.TonerAmount -= copies;
        copier.NextPrintTime = curTime + TimeSpan.FromSeconds(copies);
        Dirty(uid, copier);

        _appearance.SetData(uid, CopierVisualLayers.Working, true);
        _audio.PlayPvs(copier.PrintSound, uid);

        var xform = Transform(uid);
        var printed = 0;
        var cts = new CancellationTokenSource();

        uid.SpawnRepeatingTimer(TimeSpan.FromSeconds(1), () =>
        {
            if (copyContent != null)
            {
                var paper = Spawn("Paper", xform.Coordinates);
                _paper.SetContent(paper, copyContent);
            }
            else if (docId != null)
            {
                Spawn(docId, xform.Coordinates);
            }

            printed++;

            if (printed >= copies)
            {
                _appearance.SetData(uid, CopierVisualLayers.Working, false);
                _popup.PopupEntity(Loc.GetString("copier-printing-complete", ("count", copies)), uid, user);
                cts.Cancel();
            }
        }, cts.Token);

        return true;
    }

    private void UpdateUIState(EntityUid uid, CopierComponent copier)
    {
        var categories = new List<CopierCategoryInfo>();

        foreach (var catId in copier.AvailableCategories)
        {
            if (!_prototype.TryIndex<CopierCategoryPrototype>(catId, out var catProto))
                continue;

            var docs = new List<CopierDocInfo>();

            foreach (var docId in catProto.Documents)
            {
                string docName = docId;
                if (_prototype.TryIndex<EntityPrototype>(docId, out var docProto))
                    docName = docProto.Name ?? docId;

                docs.Add(new CopierDocInfo(docId, docName));
            }

            categories.Add(new CopierCategoryInfo(catId, catProto.Name, docs));
        }

        var state = new CopierBoundUserInterfaceState(
            copier.TonerAmount,
            copier.MaxTonerAmount,
            copier.Mode,
            categories);

        _uiSystem.SetUiState(uid, CopierUiKey.Key, state);
    }
}
