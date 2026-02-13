using UnityEngine;
using UnityEngine.UIElements;

public class SudokuGridViewPopupHandler
{
    private const int GRID_SIZE = 9;

    private VisualElement root;
    private VisualElement popupOverlay;
    private VisualElement popupContainer;
    private Vector2 popupOffset;

    public SudokuGridViewPopupHandler(VisualElement root, VisualElement popupOverlay, VisualElement popupContainer, Vector2 popupOffset)
    {
        this.root = root;
        this.popupOverlay = popupOverlay;
        this.popupContainer = popupContainer;
        this.popupOffset = popupOffset;
    }

    public void ShowPopup(SudokuCell cell, System.Action<int> onFillNumber, System.Action<int, VisualElement> onToggleNote, System.Action onErase)
    {
        this.popupContainer.Clear();
        this.popupOverlay.RemoveFromClassList("popup-overlay--hidden");

        this.popupContainer.RegisterCallbackOnce<GeometryChangedEvent>(evt =>
            this.PositionPopup(cell));

        VisualElement fillRow = new VisualElement();
        fillRow.AddToClassList("popup-row");

        for (int i = 1; i <= GRID_SIZE; i++)
        {
            int number = i;
            VisualElement button = new VisualElement();
            button.AddToClassList("popup-fill-button");

            Label label = new Label(number.ToString());
            label.AddToClassList("popup-fill-label");
            button.Add(label);

            button.RegisterCallback<ClickEvent>(evt =>
            {
                evt.StopPropagation();
                onFillNumber?.Invoke(number);
            });

            fillRow.Add(button);
        }

        this.popupContainer.Add(fillRow);

        VisualElement separator = new VisualElement();
        separator.AddToClassList("popup-separator");
        this.popupContainer.Add(separator);

        VisualElement noteRow = new VisualElement();
        noteRow.AddToClassList("popup-row");

        for (int i = 1; i <= GRID_SIZE; i++)
        {
            int number = i;
            VisualElement button = new VisualElement();
            button.AddToClassList("popup-note-button");

            if (cell.HasNote(number))
                button.AddToClassList("popup-note-button--active");

            Label label = new Label(number.ToString());
            label.AddToClassList("popup-note-label");
            button.Add(label);

            button.RegisterCallback<ClickEvent>(evt =>
            {
                evt.StopPropagation();
                onToggleNote?.Invoke(number, button);
            });

            noteRow.Add(button);
        }

        this.popupContainer.Add(noteRow);

        VisualElement eraseButton = new VisualElement();
        eraseButton.AddToClassList("popup-erase-button");

        Label eraseLabel = new Label("\uf12d");
        eraseLabel.AddToClassList("popup-erase-label");
        eraseButton.Add(eraseLabel);

        eraseButton.RegisterCallback<ClickEvent>(evt =>
        {
            evt.StopPropagation();
            onErase?.Invoke();
        });

        this.popupContainer.Add(eraseButton);
    }

    private void PositionPopup(SudokuCell cell)
    {
        Rect cellBound = cell.Element.worldBound;
        float popupWidth = this.popupContainer.resolvedStyle.width;
        float popupHeight = this.popupContainer.resolvedStyle.height;
        float rootWidth = this.root.resolvedStyle.width;
        float rootHeight = this.root.resolvedStyle.height;

        float x = cellBound.center.x - popupWidth / 2f + this.popupOffset.x;
        float y = cellBound.yMin - popupHeight + this.popupOffset.y;

        if (y < 0)
            y = cellBound.yMax - this.popupOffset.y;

        if (x < 4f) x = 4f;
        if (x + popupWidth > rootWidth - 4f) x = rootWidth - popupWidth - 4f;

        if (y + popupHeight > rootHeight - 4f) y = rootHeight - popupHeight - 4f;

        this.popupContainer.style.left = x;
        this.popupContainer.style.top = y;
    }

    public void HidePopup()
    {
        this.popupOverlay.AddToClassList("popup-overlay--hidden");
        this.popupContainer.Clear();
    }
}
