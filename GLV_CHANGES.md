# DarkGroupedListView (GLV) — LATEST Changes Only

File: `DarkUI/Controls/DarkGroupedListView.cs`
Class: `DarkGroupedListView : UserControl`

This is a summary of the **most recent feature additions only**. The
control pre-existing behavior (two-pass scrollbar layout, group
rebuild-sorting, GroupHeaderClicked, GetGroupFilePaths, etc.) is
unchanged. Do not reimplement or remove it.

---

## Feature 1: Arrow-key navigation never stops on a group header row

**Problem:** Group headers are regular DataGridView rows. Arrow keys,
Home/End, PageUp/PageDown landed the current cell on header rows.

**Solution:** `_base.KeyDown` intercepts navigation keys and repoints the
current cell before DGV's default behavior runs. The jump happens before
`CurrentCellChanged` sync, so the synced path is always an item row.

- **↓ / ↑** → `NextVisibleRowExpanding(row, ±1)`
- **Home / End** → first / last item row (never a header)
- **PageDown / PageUp** → page jump via `NextNonGroupRow` (skips headers
  and collapsed groups; no auto-expand)
- If no valid move exists (`target == row`), the key is consumed with
  `e.Handled = true` so DGV can't land on a header.

Design rule: **headers are never a landing spot.**

## Feature 2: Auto-expand collapsed groups during ↓/↑ navigation

**Problem:** Pressing ↓ from the last item of an expanded group jumped
over a collapsed group entirely (invisible rows were skipped).

**Solution:** `NextVisibleRowExpanding(fromRow, delta)`:
- ↓ from the last item of group A → the collapsed group B below is
  **auto-expanded** and the cursor lands on B's **first item**.
- ↑ from the first item of group B → the collapsed group A above is
  **auto-expanded** and the cursor lands on A's **last item** (nearest to
  the cursor).

Helpers added: `IsGroupCollapsed(hdrRow)` (header text starts with "▶"),
`ExpandGroup(hdrRow)` (expand-only via existing `ToggleGroup`),
`FindGroupHeader(itemRow)`, `LastItemOfGroup(hdrRow)`.

## Feature 3: Group header highlights when its content is highlighted

**Problem:** No visual link between a selected item and its group.

**Solution:** `_base.CellFormatting` — a group header row renders
highlighted when the current cell's row belongs to that group (between
this header and the next header, or the header itself is current):

- active → `BackColor`/`SelectionBackColor` = `Colors.GreySelection`
  (dimmed vs item rows' `BlueSelection`)
- inactive → `Colors.MediumBackground`

`IsGroupActive(hdrRow)` walks `_groupRows` to find which header owns the
current row index. `CurrentCellChanged` calls `_base.Invalidate()` so the
highlight tracks arrow navigation in real time.

## Feature 4: Right-click on a group header selects the header row

**Problem:** Right-click fired `GroupHeaderClicked` and showed the menu,
but the row never rendered as selected.

**Solution:** In the `CellMouseClick` right-click branch for group rows:

```csharp
_base.CurrentCell = _base.Rows[e.RowIndex].Cells[0];
_base.Rows[e.RowIndex].Selected = true;
```

…before firing `GroupHeaderClicked`. Since the header is now the current
row, `IsGroupActive` returns true and Feature 3 renders it highlighted.

---

## Where the code lives (line anchors in current file)

- `_base.KeyDown` handler — right after the `CurrentCellChanged` handler
- `_base.CellFormatting` handler — same region
- `NextVisibleRowExpanding` / `NextNonGroupRow` / `IsGroupCollapsed` /
  `ExpandGroup` / `FindGroupHeader` / `LastItemOfGroup` — helper methods
  above `ToggleGroup`
- `IsGroupActive` — helper method, same region
- Right-click selection — inside the `CellMouseClick` handler's group-row
  right-click branch

## Behavior matrix to verify

| Action | Expected |
|---|---|
| ↓ from last item of group A, group B below collapsed | B expands, cursor on B's first item |
| ↑ from first item of group B, group A above collapsed | A expands, cursor on A's last item |
| ↓/↑ across expanded groups | Header skipped, cursor moves item-to-item |
| Home / End | First/last item row, never a header |
| PageUp / PageDown | Page jump, never a header |
| Cursor on any item | Its group header shows grey highlight |
| Right-click group header | Row highlights + GroupHeaderClicked fires |
| Right-click item row | Normal selection + app context menu (unchanged) |

---

# ⚠️ PENDING FIXES — right-click behavior (requested 2026-08-03)

The PS4 PKG Tool app (consumer of this control) reports two remaining
right-click defects. **Do not mark this file done until both are landed
and verified.** App-side logic is already correct; these are the last two.

## Pending Fix A: group-header right-click wipes the item selection

**Problem:** Feature 4's `_base.Rows[e.RowIndex].Selected = true;` runs
with `MultiSelect = false`, so selecting the header row **deselects the
previously highlighted item row**. Consequence (observed in the app):
right-clicking a group header, then right-clicking any item, the item's
context menu shows an empty selection (label shows "PKG" instead of the
PKG title) and the menu has nothing to act on.

**Fix:** set the current cell (drives the `IsGroupActive` header highlight)
but preserve the item selection. Replace the right-click branch in
`CellMouseClick` (~lines 190-205):

```csharp
else if (e.Button == MouseButtons.Right)
{
    // Highlight the header via the current cell WITHOUT clearing the item selection.
    int prevRow = _base.CurrentCellAddress.Y;
    int prevCol = _base.CurrentCellAddress.X;
    if (e.RowIndex < _base.Rows.Count && _base.Columns.Count > 0)
    {
        _base.CurrentCell = _base.Rows[e.RowIndex].Cells[0]; // drives IsGroupActive highlight
        if (prevRow >= 0 && prevRow < _base.Rows.Count && _base.Columns.Count > 0
            && prevRow != e.RowIndex && _base.Rows[prevRow].Tag != null)
            _base.Rows[prevRow].Selected = true;             // restore the item selection
    }
    string raw = _base.Rows[e.RowIndex].Cells[0].Value?.ToString() ?? "";
    string groupName = raw;
    if (groupName.StartsWith("▼ ") || groupName.StartsWith("▶ "))
        groupName = groupName.Substring(2);
    int paren = groupName.LastIndexOf("  (");
    if (paren > 0) groupName = groupName.Substring(0, paren);
    GroupHeaderClicked?.Invoke(e.RowIndex, groupName, e);
}
```

## Pending Fix B: right-click on an item moves the current cell

**Problem:** WinForms DataGridView moves `CurrentCell` on **any** mouse
click, including right-click. On an item right-click this fires
`CurrentCellChanged` → the app's sync runs (PKG re-read) and the group
header highlight follows the click. Right-click must be view-only.

**Fix:** add a `_base.MouseDown` handler next to the `CellMouseClick` hook
that restores the current cell/selection after the click:

```csharp
// WinForms DGV moves CurrentCell on any click; on right-click that would fire
// CurrentCellChanged → group highlight + app sync. Restore the cell after the click.
_base.MouseDown += (s, e) =>
{
    if (e.Button != MouseButtons.Right) return;
    int keepRow = _base.CurrentCellAddress.Y;
    int keepCol = _base.CurrentCellAddress.X;
    _base.BeginInvoke((MethodInvoker)(() =>
    {
        if (keepRow >= 0 && keepRow < _base.Rows.Count && _base.Columns.Count > 0
            && _base.Rows[keepRow].Tag != null) // never restore onto a header row
        {
            _base.CurrentCell = _base.Rows[keepRow].Cells[Math.Max(0, keepCol)];
            _base.Rows[keepRow].Selected = true;
        }
    }));
};
```

## Acceptance (verify in PS4 PKG Tool)

| Action | Expected |
|---|---|
| Right-click group header, then right-click an item | Menu label shows the item's **title** (not "PKG"); highlight untouched |
| Right-click item | No group-highlight movement, no PKG re-read |
| Right-click several group headers in a row | Fresh count each time (`Group Action (N)` = packages in that group) |
| Right-click group header | Header still highlights + `GroupHeaderClicked` fires (Feature 4 preserved) |

---

# ⚠️ PENDING FIX C — left-click a group header selects the group's top PKG

Requested 2026-08-03. App-side menu label no longer uses group counts —
it always shows the selected/highlighted PKG's title. So group clicks
must establish a selection.

**Problem:** left-clicking a group header only toggles expand/collapse
(`ToggleGroup`); nothing gets selected, so there is no "highlighted PKG"
after interacting with a group.

**Fix:** in `CellMouseClick`, the group-row **left-click** branch —
after `ToggleGroup(e.RowIndex)` — select the group's **first item row**:

```csharp
if (e.Button == MouseButtons.Left)
{
    ToggleGroup(e.RowIndex);
    // Auto-select the top PKG of this group so the highlight + app sync follow.
    if (!IsGroupCollapsed(e.RowIndex))   // only when the group is expanded
    {
        for (int r = e.RowIndex + 1; r < _base.Rows.Count && !_groupRows.Contains(r); r++)
        {
            if (_base.Rows[r].Tag != null)
            {
                _base.CurrentCell = _base.Rows[r].Cells[0];  // fires CurrentCellChanged → sync (intended)
                _base.Rows[r].Selected = true;
                break;
            }
        }
    }
}
```

Notes:
- The `CurrentCellChanged` sync MUST fire (do NOT suppress) — the app uses
  it to highlight the same PKG in its grid and update `SelectedFilePath`.
- `IsGroupCollapsed` check avoids selecting rows hidden by the toggle
  (selecting a hidden row throws in DataGridView).
- Empty groups → no selection change.

**Acceptance:** left-click a group header → group toggles as before AND
its top PKG becomes highlighted (grid follows); the GLV menu label then
shows that PKG's title on any subsequent right-click.
