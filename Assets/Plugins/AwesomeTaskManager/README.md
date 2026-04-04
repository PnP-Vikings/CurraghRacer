# 🎯 Awesome Task Manager

**A beautiful, feature-rich Kanban board & Quick Notes System built right inside the Unity Editor.**

Stop alt-tabbing to Trello, Notion, or sticky notes. *Awesome Task Manager* keeps your tasks, ideas, and notes exactly where you need them — inside Unity.

---

## 🚀 Quick Start

1. **Import** the package into your Unity project
2. Open via the menu: **Tools → Awesome Task Manager** (or press `Ctrl+Alt+T`)
3. Your first board is already created — start adding cards!
4. Switch to the **Notes** tab to start writing — use `![[image.png]]` to embed images inline

---

## ✨ Key Features

### 📋 Kanban Task Board
- **Unlimited boards** — organize tasks by feature, milestone, sprint, or however you like
- **Customizable columns** — add, rename, reorder, and delete columns (default: To Do → In Progress → Done)
- **Powerful task cards** with:
  - 📝 Title & rich description
  - 🏷 **Category tags** with customizable default colors (Audio, Art, Code, Bug, etc.)
  - 🎨 **8 color labels** — None, Green, Blue, Yellow, Orange, Red, Purple, Teal
  - 🔥 **Priority levels** — None, Low, Medium, High, Urgent (with emoji indicators)
  - 📅 **Smart due dates** — quick-set buttons (Today, +1d, +3d, +1w, +2w, +1m), overdue/due-today/due-soon alerts with color-coded indicators right on the card
  - ☑ **Checklists** with progress tracking
  - 📦 Archive cards to declutter without deleting
- **Drag & drop** cards between columns with Trello-style ghost card visual
- **Reorder** cards within columns (▲/▼)
- **Search & filter** by text or category across all columns
- **Full card detail editor** popup with inline category management
- **Category Manager** — add, rename, recolor, and delete categories globally
- **🖼 Image & GIF attachments** — attach images (PNG, JPG, GIF, BMP, TGA, PSD, TIFF) to any card with inline preview
- **🎞 Animated GIF playback** — GIFs animate live on the board and in card detail views

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
- **Color-code** notes with 8 label colors
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

---

## 📄 Changelog

### v1.2.0
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
