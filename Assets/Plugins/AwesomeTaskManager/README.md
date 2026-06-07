# 🎯 Awesome Task Manager

**A beautiful, feature-rich Kanban board & Quick Notes System built right inside the Unity Editor.**

Stop alt-tabbing to Trello, Notion, or sticky notes. *Awesome Task Manager* keeps your tasks, ideas, and notes exactly where you need them — inside Unity.

---

## 🚀 Quick Start

1. **Import** the package into your Unity project
2. Open via the menu: **Tools → Awesome Task Manager** (or press `Ctrl+Alt+T`)
3. **New Shortcuts**: Press `Alt+Shift+C` to quickly create a card on the active board or `Ctrl+Alt+N` for a new note from anywhere in Unity.
4. Your first board is already created — start adding cards!
5. Switch to the **Notes** tab to start writing — use `![[image.png]]` to embed images inline

---

## ✨ Key Features

### 📋 Kanban Task Board
- **Unlimited boards** — organize tasks by feature, milestone, sprint, or however you like
- **Board Templates** — jumpstart your project with premade templates (Agile, Game Design, Bug Tracker, Simple To-Do) or save your own custom layouts
- **Customizable columns** — add, rename, reorder, and delete columns (default: To Do → In Progress → Done)
- **Powerful task cards** with:
  - 📝 Title & rich description
  - 🏷 **Category tags** with customizable default colors (Audio, Art, Code, Bug, etc.)
  - 🎨 **17 color labels** — expanded palette including Grey, Green, Blue, Yellow, Orange, Red, Purple, Teal, Pink, Lime, Indigo, Cyan, Amber, Deep Orange, Deep Purple, Blue Grey, and Brown
  - 🔥 **Priority levels** — None, Low, Medium, High, Urgent (with emoji indicators)
  - 📅 **Smart due dates** — quick-set buttons (Today, +1d, +3d, +1w, +2w, +1m), overdue/due-today/due-soon alerts with color-coded indicators right on the card
  - ☑ **Checklists** with progress tracking — reorderable items (▲/▼) visible on the board with per-card **▾/▸ toggle** to show/hide tasks, plus toolbar **Show All / Hide All** buttons
  - 🌳 **Subtask Hierarchy** — link cards directly to checklist items; completion status synchronizes automatically between parent and child tasks
- 💡 **Interactive Highlighting** — click the 🌳 (Master) or 🌿 (Subtask) icons to instantly highlight all related cards across the board
  - 📦 Archive cards to declutter with visual markers and quick toggle
- **Drag & drop** cards between columns with Trello-style ghost card visual
- **Reorder** cards within columns (▲/▼)
- **Card Duplication & Copy-Paste** — instantly duplicate cards or copy them across different boards to maintain consistency between workflows
- **Search & filter** by text or category across all columns
- **Full card detail editor** popup with inline category management
- **Category Manager** — add, rename, recolor, and delete categories globally
- **👥 Assignees** — manage project members globally and assign them to cards with circular profile icons (initials or custom images) and customizable border colors
- **🖼 Image & GIF attachments** — attach images (PNG, JPG, GIF, BMP, TGA, PSD, TIFF) to any card with inline preview
- **🔍 Large Image Preview** — click any image or GIF in the card details or note preview to open a larger, dedicated viewer window
- **🎞 Animated GIF playback** — GIFs animate live on the board and in card detail views
- **🔗 Note & URL Linking** — link Quick Notes and web URLs directly to task cards for easy access
- **🔃 Unified Linked Items** — assets, scene objects, notes, and URLs are combined into a single list with ▲/▼ reordering
- **🚀 Smart Navigation** — click a linked note to open it instantly, or a URL to open it in your browser (with confirmation)
- **✨ Auto-Link URLs** — pasted links in notes are automatically converted into clickable buttons in Preview mode
- **🚀 Smart Scene Navigation** — clicking a linked scene object pings it in the Hierarchy and automatically switches scenes if needed (with confirmation)
- **👤 Assignee Filtering** — filter the board to show only tasks assigned to specific members; hover over icons on a card to see full names
- **🔥 Priority Filtering** — quickly filter the board by priority level to focus on what matters most

### 📝 Quick Notes System
- **Markdown-powered notepad** with Edit / Preview toggle
- **Inline image embeds** — use `![[image.png]]` syntax to embed images directly in your notes, just like Obsidian
- **Markdown preview** renders:
  - `# H1`, `## H2`, `### H3` headers with sizing and underlines
  - `- [ ]` / `- [x]` interactive checkboxes (toggleable in preview!)
  - `- item` / `* item` bullet points
  - `1. item` numbered lists
  - `---` / `***` / `___` horizontal rules
  - `**bold**`, `*italic*`, `` `code` `` inline formatting
- **Clipboard image paste** — screenshot or copy an image, then press **Ctrl+V** or click the Paste button to embed it instantly
- **Drag & drop images** — drag image files from Explorer directly into notes
- **Folder organization** — create folders, drag notes between them, "Unfiled" for loose notes
- **Pin** important notes to the top
- **Color-code** notes with 17 label colors
- **Pop-out** any note into its own floating editor window 
- **Search** notes by title or content
- **Word & character count** — live stats in the editor
- **Export** individual notes or entire folders as Markdown (.md) files
- **📥 Import** text files (.md, .txt, .rtf, .log, .csv, .json, .xml, .yaml, etc.) as notes

### 🎞 GIF Support
- **Built-in GIF decoder** — no external dependencies, parses animated GIFs natively
- **Animated playback** in both the board view and card detail panels
- **Supports up to 200 frames** and files up to 50 MB
- **One-time decode, session-cached** — zero ongoing performance cost

### 🎨 Polish & UX
- Clean, modern UI that fits both **Light** and **Dark** editor themes
- Status bar with live card count, overdue alerts, and column count
- Tab interface: switch between Board and Notes instantly
- Keyboard shortcut: **Ctrl+Alt+T** to open
- Keyboard shortcut: **Alt+Shift+C** to quick-create a Task Card from anywhere (works in background)
- Keyboard shortcut: **Ctrl+Alt+N** to quick-create a Note from anywhere (works in background)
- **Ctrl+V** to paste clipboard images directly into notes
- Drag-and-drop with visual feedback (banner, drop zones, folder highlights)
- All data auto-saves on every change — no "Save" button needed

## 📂 Data Storage

All data is saved as JSON at:
```
ProjectSettings/AwesomeTaskManager/AwesomeTaskManager.json
```

Attached images are stored in:
```
Assets/Plugins/AwesomeTaskManager/Editor/AttachedImages/
```

- **Per-project** — each project has its own boards and notes
- **Version-control friendly** — commit the JSON file and AttachedImages folder to share boards with your team
- Editor-only — zero impact on your game builds

---

## 📦 Installation

### From Unity Asset Store
1. Purchase/download from the Unity Asset Store
2. Import into your project via Package Manager
3. Done! The assembly definition ensures zero conflicts with your game code.

### Manual / From .unitypackage
1. Drag the `.unitypackage` into your Unity project
2. Import all files
3. Open via **Tools → Awesome Task Manager**

---

## 🗂 Folder Structure

```
Assets/Plugins/AwesomeTaskManager/
├── README.md
└── Editor/
    ├── AwesomeTaskManager.Editor.asmdef
    ├── AttachedImages/           — Pasted/browsed images stored here
    ├── Data/
    │   ├── TaskBoardData.cs      — All data models (boards, cards, notes, folders)
    │   └── Persistence.cs        — JSON save/load to ProjectSettings
    └── UI/
        ├── TBStyles.cs           — Colors, GUIStyles, visual constants
        ├── TaskBoardWindow.cs    — Main editor window (board + notes views)
        ├── CardDetailWindow.cs   — Card detail / creation popup with date picker
        ├── CategoryEditorWindow.cs — Global category manager
        ├── NotePopupWindow.cs    — Pop-out note editor window
        └── GifDecoder.cs         — Built-in animated GIF parser & decoder
```

---

## 🔧 Compatibility

| Requirement | Version |
|-------------|---------|
| Unity | **2021.3 LTS** or newer (including Unity 6) |
| Render Pipeline | Any (SRP, URP, HDRP, Built-in) — it's editor-only! |
| Platform | All — editor tool, no runtime dependencies |
| Theme | ✅ Light & Dark editor themes |

**Zero dependencies.** No third-party plugins required.

---

## 💡 Tips & Tricks

- **Inline images in notes** — Type `![[filename.png]]` in Edit mode, then switch to Preview to see it rendered inline
- **Ctrl+V image paste** — Screenshot something, switch to a note, press Ctrl+V — the image is auto-embedded
- **Quick-set due dates** — In the card editor, use the Today / +1d / +3d / +1w / +2w / +1m buttons instead of typing dates manually
- **Overdue alerts** — Cards past their due date show a red "🔴 Overdue" badge right on the board
- **Drag cards** — Click and hold any card, then drag it to another column. A ghost card follows your cursor and columns highlight as drop targets
- **Column highlights during drag** — Source column = green, hovered target = blue, others = subtle grey
- **Drag notes to folders** — Click and drag a note onto any folder in the sidebar (source folder = green, hovered = blue)
- **Pop-out notes** — Click ↗ on any note to open it in a standalone resizable window
- **Export notes** — Click 📤 to export a note as Markdown, or use the folder ⋮ menu to export all notes in a folder
- **Import notes** — Click 📥 to import .md, .txt, or other text files directly as notes
- **Animated GIFs** — Attach a .gif to a card or note and it will animate live in the editor
- **Category colors** — Set a default color per category in the Category Manager (🏷 button in the toolbar); new cards auto-inherit the color
- **Scene Linking Navigation** — Click a linked scene object on a task card to find it instantly in the Hierarchy. If it's in a different scene, a confirmation dialog will help you switch and navigate to it automatically.
- **Link Notes to Cards** — Drag a note from the Notes list onto a task card on the board to link them. Or use the "Link Note" button in the card detail editor.
- **Auto-Link in Preview** — Paste any `http://` or `https://` URL into a note. In Preview mode, it will be rendered as a clickable blue link.
- **Background Shortcuts** — Use `Alt+Shift+C` or `Ctrl+Alt+N` to quickly capture ideas or tasks without having the main window open. They save to your most recent folder/primary board.
- **Show/Hide checklists** — Use the **▾ Show All** / **▸ Hide All** toolbar buttons to expand or collapse all card checklists at once, or toggle individual cards with the **▾/▸** button on each card
- **Large Image Previews** — Click on any image or GIF attachment in the card details or markdown preview to open a larger, dedicated preview window.
- **👥 Assignees** — Add members via the 👥 button in the toolbar. Assign them in the card detail editor. They appear as overlapping initials bubbles or profile icons with custom borders on the board.
- **Link Note Submenus** — When linking a note to a card, notes are now organized by their folders in the context menu for easier navigation.
- **URL Confirmation** — Clicking a linked URL now shows a confirmation dialog before opening your web browser, preventing accidental exits from Unity.
- **Note Popup Preview** — You can now toggle between Edit and Preview modes even in standalone note windows. It also supports image pasting (Ctrl+V) and drag-and-drop just like the main window.
- **Board Templates** — Use the "Board Options" dropdown to save your current board layout as a template. You can choose to include existing cards or just the column structure.
- **Subtask Highlighting** — If a card is both a master and a subtask, it will show both 🌳 and 🌿 icons. Click 🌳 to see its children, or 🌿 to see its parents.
- **Transfer Cards** — Use the ⋮ menu on a card to "Copy to Board >" and select any other board for an instant transfer.

---

## 📄 Changelog

### v1.6.0

- **Added** 🌳 **Subtask Hierarchy** — link task cards directly to checklist items to create multi-level parent-child relationships.
- **Added** 🔄 **Automatic Completion Sync** — checking off a linked subtask card automatically updates the corresponding checklist item on the parent card, and vice-versa.
- **Added** ↕ **Checklist Reordering** — added ▲/▼ buttons to checklist items in the card detail window for custom task sorting.
- **Added** 💡 **Interactive Relationship Highlighting** — cards now feature 🌳 (Master) and 🌿 (Subtask) icons that, when clicked, highlight all related tasks across the board.
- **Improved** 🛡 **Data Integrity** — implemented a robust "Load-Merge-Save" pattern and enhanced multi-window notification logic to ensure perfect data synchronization even during Play Mode transitions.

### v1.5.3

- **Improved** 🔃 **Global Data Synchronization** — implemented a coordinated "Reload All Open Windows" system that ensures every open task board, card editor, and management window stays perfectly in sync, preventing stale data from overwriting fresh changes.

- **Improved** 🛡 **Stability & Reliability** — fixed critical bugs where new cards, assignees, or categories could be lost during domain reloads (Play Mode) by implementing robust state serialization and disk-based fallback saving across all windows.

- **Improved** 📱 **Responsive UI** — redesigned the toolbar to intelligently hide labels on smaller screens and added compact ▾/▸ icons for checklist toggles to maximize usable space on narrow layouts.

- **Added** 💡 **On-Hover Tooltips** — added descriptive tooltips to all filter and selection dropdowns (Board, Category, Assignee, Priority, etc.) for improved guidance and discoverability.

- **Improved** ⌨ **Shortcut Reliability** — improved the "New Note" shortcut to ensure proper data persistence even when the main Task Board window is closed.

### v1.5.2

**Added** 💾 **Preview & Save Workflow** — editing an existing card now works on a temporary clone; changes are only applied to the board when you click "Save Changes"

- **Added** ⚠️ **Unsaved Changes Protection** — closing a card window (new or existing) via the default 'X' button now prompts to save or discard changes if there are unsaved edits

- **Added** 🚫 **Title Validation** — preventing saving of new cards without a title, with clear error messaging in both the editor and the close prompt

- **Improved** ⌨ **Checklist Enter & Focus** — support for pressing Enter to quickly add checklist items, with fixed deferred focus management to ensure the input field reliably regains focus for rapid entries without resetting window focus

- **Improved** 🎯 **Automatic Field Selection** — new and existing cards now automatically focus the title field when opened for faster data entry

- **Added** 🔥 **Priority Filtering** — Added a new dropdown to the board toolbar to filter tasks by priority (None, Low, Medium, High, Urgent).

- **Improved Filter Persistence** — All filters (Search, Category, Assignee, Priority) now reset correctly when creating new cards via shortcuts or switching boards.

### v1.5.1
- **Improved** **Board Title** — Added more responsive UI to accommodate bigger board names without breaking the layout

### v1.5.0
- **Added** 👥 **Assignees System** — manage project members globally and assign them to specific task cards
- **Added** 🎨 **Member Initials & Profile Icons** — assignees are displayed as overlapping circular icons (initials or custom images) with customizable border colors
- **Added** 👤 **Assignee Filtering** — new toolbar filter allows you to show only tasks assigned to specific members
- **Added** 🔍 **Hover-Expand Assignees** — hover over profile icons on a card to see all assigned members and their full names
- **Added** 👥 **Assignee Manager** — a dedicated window to manage team members, supporting profile image drag-and-drop, clipboard pasting, and external file browsing
- **Added** 📋 **Board Templates System** — create new boards using premade layouts (Agile, Game Design, Bug Tracker, Simple To-Do) or save your own custom templates
- **Added** 💾 **Template Options** — choose whether to include cards or just the column structure when saving a new template
- **Added** 👯 **Card Duplication** — quickly duplicate any card within a column with one click
- **Added** 📋 **Cross-Board Copy-Paste** — copy a card to the internal clipboard and paste it into any column or even transfer it to a completely different board
- **Added** ✨ **Success Notifications** — added a sleek success popup that appears when moving cards between columns or notes between folders
- **Added** 📁 **Archiving System** — you can now archive completed tasks to declutter your board without permanently deleting them
- **Added** 📂 **Archived Toggle** — new toolbar button (📦/🗃️) allows you to toggle the visibility of archived cards instantly
- **Added** 🏷 **Archived Marker** — archived cards display a "📦 ARCHIVED" badge on the board for easy identification
- **Added** ⚙ **Card Context Menu** — added a dedicated ⋮ options button to every card for quick access to Archiving and Deletion
- **Improved** 🏗 **Card Dragging** — archived cards now have a distinct visual style (grey border) when being dragged to distinguish them from active tasks
- **Improved** ⌨ **Toolbar Tooltips** — icon-only buttons now display their names as tooltips when hovered for better discoverability
- **Improved** 🖼 **Flexible Image Handling** — profile icons now support the same powerful pasting and drag-and-drop logic as notes and cards
- **Improved** 👥 **Responsive UI** — redesigned the Assignee Manager with a more flexible layout that remains visible on smaller windows

### v1.4.0
- **Added** 🔍 **Large Image Preview** — click any image or GIF in card details or notes to open it in a larger viewer window
- **Added** 👁 **Note Popup Preview** — standalone note windows now feature a Preview/Edit toggle with full Markdown support
- **Added** 🎨 **Expanded Colors** — increased available label colors from 8 to 17, including Pink, Lime, Indigo, Cyan, Amber, Brown, and more
- **Added** 📁 **Link Note Submenus** — the "Link Note" context menu now groups notes by folder for better organization
- **Added** ⚠️ **URL Confirmation** — clicking external links now prompts for confirmation to prevent accidental browser opens
- **Improved** 🖼 **Image Handling** — centralized Markdown rendering and image pasting logic; added clipboard image support to note popups
- **Improved** 📏 **UI & Layout** — increased image preview size in card details and optimized note list item layout to prevent text overlap

### v1.3.1
- **Added** 📝 **Note Linking** — link any note to a task card via drag-and-drop or the "Link Note" button in the card editor
- **Added** 🔗 **URL Linking** — add web links to task cards for quick access to documentation, Trello, or other external resources
- **Added** ✨ **Auto-Linking** — URLs pasted into notes are now automatically detected and rendered as clickable links in Preview mode
- **Added** 📂 **Shared Dialogs** — extracted `EditorInputDialog` to a shared utility for reuse across different windows
- **Improved** 🖼 **Linked Item Icons** — unique icons for Assets, Scene Objects, Notes, and URLs to help distinguish them at a glance
- **Improved** 🖱 **Drag & Drop** — enhanced card drop logic to support multiple item types (Assets, Scene, Notes) simultaneously

### v1.3.0
- **Added** 🔗 **Scene Object Linking** — link GameObjects and scene assets directly to task cards via drag-and-drop
- **Added** 🚀 **Smart Scene Navigation** — clicking a linked scene object now prompts to open the correct scene (if needed) and automatically pings/selects the object in the Hierarchy
- **Added** 🏷 **Scene Context Labels** — linked scene objects are labeled as `[SceneName] ObjectName` for better clarity
- **Added** 🔃 **Unified Item Reordering** — assets and scene objects are now combined into a single "Linked Items" list with ▲/▼ buttons for custom reordering
- **Added** ⌨ **Global Shortcuts** — new shortcuts to create items without focusing the main window:
  - `Alt+Shift+C` — New Task Card
  - `Ctrl+Alt+N` — New Note
- **Improved** 🏗 **Background Workflow** — shortcuts now work even when the main window is closed, spawning a standalone popup editor and saving data to the primary board/folder
- **Improved** 🔄 **Data Migration** — automatic migration of legacy linked assets and scene objects into the new unified list format on first load

### v1.2.0
- **Added** ☑ **Checklist visibility controls** — per-card ▾/▸ toggle to show or hide checklist tasks on the board, plus **Show All / Hide All** toolbar buttons for batch control
- **Added** 🚫 **Cancel confirmation** — when creating a new card, pressing Cancel with unsaved changes now prompts you to confirm
- **Added** 🎞 Animated GIF playback — GIFs now animate live on the board and in card detail panels via a built-in GIF decoder (no external dependencies)
- **Added** 📝 Obsidian-style markdown notes — Edit/Preview toggle with inline image rendering via `![[image.png]]` syntax
- **Added** Markdown preview renderer — headers, bullet lists, numbered lists, interactive checkboxes, horizontal rules, bold/italic/code formatting
- **Added** ⌨ **Ctrl+V clipboard image paste** — paste screenshots and copied images directly into notes with auto-embed
- **Added** 🖱 Drag & drop image files from Explorer into notes
- **Added** GIF decoder with support for up to 200 frames and 50 MB files
- **Fixed** GIF files no longer crash Unity when attached (deferred AssetDatabase.Refresh, frame/size limits, error handling)
- **Fixed** GUIClips errors from unbalanced Begin/End layout groups during image gallery deletion
- **Fixed** Note text not updating when switching between notes in Edit mode (GUI focus release)
- **Fixed** Pasted images now update the text editor immediately (GUI focus clear after paste)
- **Improved** Image insert workflow — Browse, Paste, and Drag & Drop all auto-insert `![[filename]]` into note content
- **Changed** Renamed from "The Ultimate Task Board" to "Awesome Task Manager"

### v1.1.0
- **Fixed** card drag-and-drop hitbox — drop anywhere on a column, not just the bottom zone
- **Improved** Trello-style card dragging with floating ghost card visual and column highlighting
- **Fixed** pop-out ↗ button overflow in note list items
- **Added** 📥 Import feature for notes — import .md, .txt, .rtf, .log, .csv, and other text files
- **Added** 🖼 Image attachments for both cards and notes with inline preview
- **Improved** folder highlighting during note drag (source = green, hovered = blue, others = grey)

### v1.0.0
- Initial release
- Kanban board with multiple boards, columns, cards
- Smart due dates with overdue/today/soon indicators and quick-set buttons
- Category system with global color defaults and Category Manager window
- Quick Notes with folders, drag-to-folder, pop-out editor, Markdown export
- Drag & drop cards between columns
- Search & filter by text and category
- 8 color labels, 4 priority levels, checklists
- Light & Dark theme support
- Status bar with live stats

---

## 📬 Support

Found a bug or have a feature request? Please reach out via the **Asset Store** page or open an issue on the support channel.

---

*Made with ❤️ for game developers who want to stay organized without leaving Unity.*
