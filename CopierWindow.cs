using System.Linq;
using System.Numerics;
using Content.Shared.Copier;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;

namespace Content.Client.Copier;

public sealed class CopierWindow : DefaultWindow
{
    private static readonly Color WindowBackground = Color.FromHex("#1a1a2e");
    private static readonly Color CardBackground = Color.FromHex("#1a1a2e");
    private static readonly Color CardBorder = Color.FromHex("#3a3a6a");
    private static readonly Color HeaderText = Color.FromHex("#aaddff");
    private static readonly Color ProgressBg = Color.FromHex("#111122");
    private static readonly Color ProgressFill = Color.FromHex("#44cc44");
    private static readonly Color ProgressBorder = Color.FromHex("#66ee66");
    private static readonly Color LabelText = Color.FromHex("#cccccc");
    private static readonly Color ButtonDanger = Color.FromHex("#cc3333");
    private static readonly Color ButtonDangerBorder = Color.FromHex("#ff5555");
    private static readonly Color ModeActive = Color.FromHex("#2a6a2a");
    private static readonly Color ModeActiveBorder = Color.FromHex("#44cc44");
    private static readonly Color ModeInactive = CardBackground;
    private static readonly Color ModeInactiveBorder = CardBorder;

    [ViewVariables] public string? SelectedTemplateId;

    public int CopiesToPrint = 1;
    public CopierMode SelectedMode = CopierMode.Print;

    public readonly ProgressBar TonerBar;
    public readonly Label TonerLabel;
    public readonly Button PrintButton;
    public readonly Button CopyButton;
    public readonly Button PrintModeButton;
    public readonly Button CopyModeButton;
    public readonly LineEdit SearchBar;

    private readonly BoxContainer _departmentList;
    private readonly BoxContainer _templateList;

    private CopierBoundUserInterfaceState? _lastState;
    private string? _selectedDepartment;
    private Label? _copiesLabel;

    public CopierWindow()
    {
        Title = "Копировальный аппарат";
        SetSize = new Vector2(800, 560);

        var bg = new PanelContainer
        {
            PanelOverride = new StyleBoxFlat { BackgroundColor = WindowBackground },
            HorizontalExpand = true,
            VerticalExpand = true
        };
        Contents.AddChild(bg);

        var mainContainer = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            HorizontalExpand = true,
            VerticalExpand = true,
            Margin = new Thickness(8)
        };

        PanelContainer MakeCard(string title)
        {
            var panel = new PanelContainer
            {
                PanelOverride = new StyleBoxFlat
                {
                    BackgroundColor = CardBackground,
                    BorderColor = CardBorder,
                    BorderThickness = new Thickness(1),
                    Padding = new Thickness(8)
                },
                Margin = new Thickness(0, 0, 0, 6),
                HorizontalExpand = true,
                VerticalExpand = true
            };

            var content = new BoxContainer
            {
                Orientation = BoxContainer.LayoutOrientation.Vertical,
                HorizontalExpand = true,
                VerticalExpand = true
            };

            if (!string.IsNullOrEmpty(title))
            {
                content.AddChild(new Label
                {
                    Text = title,
                    FontColorOverride = HeaderText,
                    Margin = new Thickness(0, 0, 0, 6)
                });
            }

            panel.AddChild(content);
            return panel;
        }

        // ---- Left panel ----
        var leftPanel = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            MinWidth = 210,
            Margin = new Thickness(0, 0, 8, 0)
        };

        // Toner.
        var tonerCard = MakeCard("ТОНЕР");
        tonerCard.VerticalExpand = false;
        var tonerContent = (BoxContainer)tonerCard.GetChild(0);

        TonerBar = new ProgressBar
        {
            MinHeight = 22,
            Margin = new Thickness(0, 0, 0, 2),
            BackgroundStyleBoxOverride = new StyleBoxFlat { BackgroundColor = ProgressBg },
            ForegroundStyleBoxOverride = new StyleBoxFlat
            {
                BackgroundColor = ProgressFill,
                BorderColor = ProgressBorder,
                BorderThickness = new Thickness(1)
            }
        };
        tonerContent.AddChild(TonerBar);

        TonerLabel = new Label
        {
            Text = "0 / 25",
            HorizontalAlignment = HAlignment.Center,
            FontColorOverride = LabelText
        };
        tonerContent.AddChild(TonerLabel);
        leftPanel.AddChild(tonerCard);

        // Mode.
        var modeCard = MakeCard("РЕЖИМ");
        modeCard.VerticalExpand = false;
        var modeContent = (BoxContainer)modeCard.GetChild(0);

        PrintModeButton = new Button
        {
            Text = "Печать",
            HorizontalExpand = true,
            MinHeight = 28,
            Margin = new Thickness(0, 0, 0, 4)
        };
        modeContent.AddChild(PrintModeButton);

        CopyModeButton = new Button
        {
            Text = "Копирование",
            HorizontalExpand = true,
            MinHeight = 28
        };
        modeContent.AddChild(CopyModeButton);
        leftPanel.AddChild(modeCard);

        // Copies.
        var copiesCard = MakeCard("КОЛИЧЕСТВО КОПИЙ");
        copiesCard.VerticalExpand = false;
        var copiesContent = (BoxContainer)copiesCard.GetChild(0);

        var copiesRow = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            HorizontalExpand = true
        };

        var minusBtn = new Button { Text = "−", MinWidth = 36, MinHeight = 26 };
        minusBtn.OnPressed += _ => ChangeCopies(-1);
        copiesRow.AddChild(minusBtn);

        _copiesLabel = new Label
        {
            Text = $"{CopiesToPrint}",
            HorizontalExpand = true,
            HorizontalAlignment = HAlignment.Center,
            FontColorOverride = LabelText,
            VerticalAlignment = VAlignment.Center
        };
        copiesRow.AddChild(_copiesLabel);

        var plusBtn = new Button { Text = "+", MinWidth = 36, MinHeight = 26 };
        plusBtn.OnPressed += _ => ChangeCopies(1);
        copiesRow.AddChild(plusBtn);

        copiesContent.AddChild(copiesRow);
        leftPanel.AddChild(copiesCard);

        // Action buttons.
        var actionContainer = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            MinHeight = 100,
            VerticalExpand = true,
            VerticalAlignment = VAlignment.Top
        };

        PrintButton = new Button
        {
            Text = "ПЕЧАТЬ",
            MinHeight = 48,
            HorizontalExpand = true,
            Margin = new Thickness(0, 6, 0, 4),
            StyleBoxOverride = new StyleBoxFlat
            {
                BackgroundColor = ButtonDanger,
                BorderColor = ButtonDangerBorder,
                BorderThickness = new Thickness(2)
            }
        };
        actionContainer.AddChild(PrintButton);

        CopyButton = new Button
        {
            Text = "КОПИРОВАТЬ",
            MinHeight = 48,
            HorizontalExpand = true,
            Margin = new Thickness(0, 6, 0, 4),
            StyleBoxOverride = new StyleBoxFlat
            {
                BackgroundColor = ButtonDanger,
                BorderColor = ButtonDangerBorder,
                BorderThickness = new Thickness(2)
            }
        };
        actionContainer.AddChild(CopyButton);

        leftPanel.AddChild(actionContainer);

        // ---- Center panel: departments ----
        var deptCard = MakeCard("ОТДЕЛЫ");
        deptCard.MinWidth = 150;
        var deptContent = (BoxContainer)deptCard.GetChild(0);

        var deptScroll = new ScrollContainer
        {
            HorizontalExpand = true,
            VerticalExpand = true,
            HScrollEnabled = false
        };
        _departmentList = new BoxContainer { Orientation = BoxContainer.LayoutOrientation.Vertical };
        deptScroll.AddChild(_departmentList);
        deptContent.AddChild(deptScroll);

        // ---- Right panel: search + templates ----
        var rightPanel = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            HorizontalExpand = true
        };

        var templateCard = MakeCard("ШАБЛОНЫ");
        templateCard.HorizontalExpand = true;
        templateCard.VerticalExpand = true;
        var templateContent = (BoxContainer)templateCard.GetChild(0);

        SearchBar = new LineEdit { PlaceHolder = "Поиск...", Margin = new Thickness(0, 0, 0, 6) };
        SearchBar.OnTextChanged += _ => UpdateTemplateList();
        templateContent.AddChild(SearchBar);

        var templateScroll = new ScrollContainer
        {
            HorizontalExpand = true,
            VerticalExpand = true,
            HScrollEnabled = false
        };
        _templateList = new BoxContainer { Orientation = BoxContainer.LayoutOrientation.Vertical };
        templateScroll.AddChild(_templateList);
        templateContent.AddChild(templateScroll);

        rightPanel.AddChild(templateCard);

        // ---- Final assembly ----
        mainContainer.AddChild(leftPanel);
        mainContainer.AddChild(deptCard);
        mainContainer.AddChild(rightPanel);
        bg.AddChild(mainContainer);

        UpdateModeButtonStyles();
        UpdateModeVisibility();
    }

    public void SetMode(CopierMode mode)
    {
        SelectedMode = mode;
        UpdateModeButtonStyles();
        UpdateModeVisibility();
    }

    private void UpdateModeButtonStyles()
    {
        var active = new StyleBoxFlat
        {
            BackgroundColor = ModeActive,
            BorderColor = ModeActiveBorder,
            BorderThickness = new Thickness(1)
        };
        var inactive = new StyleBoxFlat
        {
            BackgroundColor = ModeInactive,
            BorderColor = ModeInactiveBorder,
            BorderThickness = new Thickness(1)
        };

        PrintModeButton.StyleBoxOverride = SelectedMode == CopierMode.Print ? active : inactive;
        CopyModeButton.StyleBoxOverride = SelectedMode == CopierMode.Copy ? active : inactive;
    }

    private void UpdateModeVisibility()
    {
        PrintButton.Visible = SelectedMode == CopierMode.Print;
        CopyButton.Visible = SelectedMode == CopierMode.Copy;
    }

    private void ChangeCopies(int delta)
    {
        CopiesToPrint = Math.Clamp(CopiesToPrint + delta, 1, 5);
        _copiesLabel!.Text = $"{CopiesToPrint}";
    }

    public void UpdateState(CopierBoundUserInterfaceState state)
    {
        _lastState = state;

        TonerBar.MaxValue = state.MaxTonerAmount;
        TonerBar.Value = state.TonerAmount;
        TonerLabel.Text = $"{state.TonerAmount} / {state.MaxTonerAmount}";

        _departmentList.RemoveAllChildren();

        var allBtn = new Button { Text = "Все", ToggleMode = true };
        allBtn.OnPressed += _ =>
        {
            _selectedDepartment = null;
            UpdateDepartmentButtons();
            UpdateTemplateList();
        };
        _departmentList.AddChild(allBtn);

        foreach (var cat in state.Categories)
        {
            var btn = new Button { Text = cat.Name, ToggleMode = true };
            btn.OnPressed += _ =>
            {
                _selectedDepartment = cat.Id;
                UpdateDepartmentButtons();
                UpdateTemplateList();
            };
            _departmentList.AddChild(btn);
        }

        UpdateDepartmentButtons();
        UpdateTemplateList();
    }

    private void UpdateDepartmentButtons()
    {
        foreach (var child in _departmentList.Children)
        {
            if (child is Button btn)
                btn.Pressed = _selectedDepartment == null
                    ? btn.Text == "Все"
                    : btn.Text == _lastState?.Categories.FirstOrDefault(c => c.Id == _selectedDepartment)?.Name;
        }
    }

    private void UpdateTemplateList()
    {
        if (_lastState == null) return;

        _templateList.RemoveAllChildren();
        var search = SearchBar.Text?.ToLower() ?? "";

        var docs = _selectedDepartment == null
            ? _lastState.Categories.SelectMany(c => c.Documents).GroupBy(d => d.Id).Select(g => g.First())
            : _lastState.Categories.FirstOrDefault(c => c.Id == _selectedDepartment)?.Documents
              ?? Enumerable.Empty<CopierDocInfo>();

        var filtered = docs
            .Where(d => string.IsNullOrEmpty(search) || d.Name.ToLower().Contains(search))
            .OrderBy(d => d.Name);

        foreach (var d in filtered)
        {
            var btn = new Button { Text = d.Name, ToggleMode = true };
            btn.OnPressed += _ =>
            {
                foreach (var child in _templateList.Children)
                    if (child is Button b) b.Pressed = false;
                btn.Pressed = true;
                SelectedTemplateId = d.Id;
            };
            _templateList.AddChild(btn);
        }
    }
}
