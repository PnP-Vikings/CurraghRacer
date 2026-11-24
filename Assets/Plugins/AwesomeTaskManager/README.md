# 🎯 Awesome Task Manager

**A beautiful, feature-rich Kanban board & Quick Notes System built right inside the Unity Editor.**

Stop alt-tabbing to Trello, Notion, or sticky notes. *Awesome Task Manager* keeps your tasks, ideas, and notes exactly where you need them — inside Unity.

---

## 🚀 Quick Start

1. **Import** the package into your Unity project
2. Open via the menu: **Tools → Awesome Task Manager** (or press `Ctrl+Alt+T`)
3. **New Shortcuts**: Press `Alt+Shift+C` to quickly create a card on the active board or `Ctrl+Alt+N` for a new note from anywhere in Unity.
4. Your first board is already created — start adding cards!
5. Switch between **Board**, **Notes**, and **Style** tabs to manage tasks, write notes, or customize your workspace theme
6. Switch to the **Notes** tab to start writing — use `![[image.png]]` to embed images inline, `![[clip.mp4]]` for videos, or paste a YouTube link on its own line for a preview card

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
- **📤 Beta (Export/Import) — CSV & Excel Support** — seamlessly move boards between projects, ClickUp, or Excel. Supports both industry-standard CSV and Excel (.xlsx/.xml) formats. ClickUp-compatible headers ensure your data maps perfectly.
- **📦 Beta (Export/Import) — Advanced JSON Portability** — move boards and cards between projects or team members using standalone JSON files (.atb/.atc) for full structure preservation.
- **📊 Detailed ClickUp & Excel Mapping** — supports Task Names, Content, Statuses, Priorities, Assignees, Tags, Due Dates, and Checklists during CSV and Excel operations.
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
- **🏷 Titled Links** — give card URLs an optional title when adding them, and edit the URL or title later with the ✎ button; YouTube links show the video title automatically when they have no custom title
- **🔃 Unified Linked Items** — assets, scene objects, notes, and URLs are combined into a single list with ▲/▼ reordering
- **🚀 Smart Navigation** — click a linked note to open it instantly, or a URL to open it in your browser (the confirmation shows the link title along with the URL)
- **✨ Auto-Link URLs** — pasted links in notes are automatically converted into clickable buttons in Preview mode
- **🚀 Smart Scene Navigation** — clicking a linked scene object pings it in the Hierarchy and automatically switches scenes if needed (with confirmation)
- **👤 Assignee Filtering** — filter the board to show only tasks assigned to specific members; hover over icons on a card to see full names
- **🔥 Priority Filtering** — quickly filter the board by priority level to focus on what matters most

### 📝 Quick Notes System
- **Markdown-powered notepad** with Edit / Preview toggle
- **Inline image embeds** — use `![[image.png]]` syntax to embed images directly in your notes, just like Obsidian
- **Remote image embeds** — `![](https://…/image.gif)` downloads the image once, caches it in `AttachedImages/`, and renders it inline (animated GIFs included)
- **📺 YouTube preview cards** — a YouTube link on its own line (or `![](youtube-url)`) renders a card with the thumbnail, a ▶ overlay and the video title; click to open it in your browser (timestamps are kept)
- **🎬 Inline video player** — `![[clip.mp4]]` or `![](https://…/clip.mp4)` plays inside the note with play/pause, stop, seek, time, mute and loop controls (`.mp4`, `.webm`, `.mov`, `.m4v`, `.ogv`)
- **Markdown preview** renders:
  - `# H1`, `## H2`, `### H3` headers with sizing and underlines
  - `- [ ]` / `- [x]` interactive checkboxes (toggleable in preview!)
  - `- item` / `* item` bullet points
  - `1. item` numbered lists
  - `---` / `***` / `___` horizontal rules
  - `**bold**`, `*italic*`, `` `code` `` inline formatting
- **Clipboard image paste** — screenshot or copy an image, then press **Ctrl+V** or click the Paste button to embed it instantly
- **Drag & drop images & videos** — drag image or video files from Explorer directly into notes
- **Browser GIF paste** — copy a GIF in your browser (or copy a `.gif` URL) and press **Ctrl+V** to embed it as an animated GIF
- **Folder organization** — create folders, drag notes between them, "Unfiled" for loose notes
- **Pin** important notes to the top
- **Color-code** notes with 17 label colors
- **Pop-out** any note into its own floating editor window 
- **Search** notes by title or content
- **Word & character count** — live stats in the editor
- **Export** individual notes or entire folders as Markdown (.md) files via the **Beta (Export/Import)** menu.
- **📥 Import** text files (.md, .txt, .rtf, .log, .csv, .json, .xml, .yaml, etc.) as notes using the **Beta (Export/Import)** button or menu.

### 🎞 GIF Support
- **Built-in GIF decoder** — no external dependencies, parses animated GIFs natively
- **Animated playback** in both the board view and card detail panels
- **Supports up to 200 frames** and files up to 50 MB
- **One-time decode, session-cached** — zero ongoing performance cost
- **Interlaced & transparent GIFs** — interlaced GIFs decode correctly and transparent frames/disposal behave like in web browsers
- **Paste from the web** — GIFs copied from a browser, `.gif` URLs, and `data:image/gif` sources are downloaded and saved to `AttachedImages/`

### 👥 Team Collaboration & Version Control
- **Modular Multi-File Storage** — boards (`Boards/{board_id}.json`) and notes (`Notes/{note_id}.json`) are saved into granular separate files, eliminating monolithic JSON merge conflicts when teammates work concurrently
- **User State Isolation** — local filters, search queries, active board index, and view toggles are isolated into `UserSettings/AwesomeTaskManager/AwesomeTaskManagerUser.json` (ignored by Unity `.gitignore`), ensuring zero dirty Git diffs on board navigation
- **Live Disk-Change Detection & VCS Sync** — automatically monitors data file timestamps and auto-reloads when teammate changes are pulled (`git pull`), branches are switched, or stashes are popped
- **Human-Readable Task IDs (e.g., `ATM-42`)** — tasks receive sequential task numbers and short codes (`taskCode`) with a configurable prefix (`taskPrefix`) and 1-click badge copying
- **Git Branch Naming Helper** — 1-click "Copy Git Branch Name" action in the card context menu and detail header (e.g., `feature/ATM-42-fix-pin-collision`) for easy branch/PR linking
- **Git Author Auto-Detection** — automatically detects local author names via `git config user.name` with `Environment.UserName` fallback
- **"My Tasks" View (👤)** — 1-click toolbar filter button to isolate tasks assigned to or created by the current developer
- **Card Audit Metadata** — tracks `createdBy`, `lastModifiedBy`, and `lastModifiedDate` timestamps and authors on every card
- **Transparent Legacy Migration** — automatically detects pre-existing monolithic `AwesomeTaskManager.json` or older `TheUltimateTaskBoard.json` files and migrates them safely

### 🎨 Themes & Visual Customization
- **Full Theme Engine & Style Tab** — complete visual customization hub to tailor the look and feel of Awesome Task Manager
- **13 Built-in Theme Presets** — choose from classic, retro, vintage console, and modern styles, including Fruit Company-inspired translucent glassmorphism:
  - **Vintage 8-Bit** — Classic 8-bit vintage console style with console gray casing, controller crimson red buttons, dark d-pad accents, and retro 8-bit gaming icons (🎮, 📜, 🪙, ⭐, ⚡, 👑, 🏁, 💀)
  - **Retro Synthwave** — 80s arcade synthwave with neon magenta, cyan, laser pink, and arcade gaming icons (🕹️, 💾, 📼, 👾, 📟)
  - **Fruit Company Glassmorphism** — macOS Big Sur/Monterey frosted translucent glass with vibrant system accents
  - **Aurora Glow Glass** — Nordic arctic translucent night with glowing aurora emerald & violet glass panes
  - **Cupertino Twilight** — Warm California sunset frosted peach, lavender, and coral glass tones
  - **Frosted Ice** — Ultra-clean crystal ice glass with subtle frost overlays
  - **Default** — Balanced modern dark & light theme
  - **Dark Slate** — Refined deep navy and slate grey tones
  - **Cyberpunk Neon** — High-contrast neon cyan, magenta, and deep purple
  - **Forest Emerald** — Natural earthy greens and sage accents
  - **Pastel Dream** — Soft soothing lavender, peach, and mint pastels
  - **Sunset Warm** — Rich amber, terracotta, and warm dusk hues
  - **Monochrome Minimal** — Ultra-clean grayscale typography and borders
- **Independent Theme Storage for Teams** — theme preferences and custom styles are stored in a separate dedicated file (`AwesomeTaskManagerTheme.json`), completely decoupled from board and task data. Team members can each use their preferred visual theme without creating Git conflicts or modifying shared board data.
- **Full Opacity & Alpha Channel Control** — every single color picker in the Style tab supports the alpha/opacity slider, enabling translucent overlays, subtle tints, and frosted glass aesthetics.
- **Interactive Live Preview** — real-time preview mockup displays immediate feedback as you modify theme colors, backgrounds, opacity, and icons
- **Dual Skin Support** — customize separate color palettes for Unity Pro Skin (Dark Mode) and Personal Skin (Light Mode) within each theme
- **Comprehensive Color & Opacity Customization**:
  - 17 customizable Label & Tag colors with alpha channel control
  - Board canvas background and Top Bar / Tab Bar background colors
  - Tab headers (active, inactive, hover) and button/dropdown colors with custom hover states
  - Column header text, column background & alternating backgrounds, card backgrounds (`pro_CardBg` / `personal_CardBg`), borders, card titles, badges, and "+ Add Card" buttons
  - Note view sidebar, note editor, note title text, text area input background & text colors, note card list items, and popout window backgrounds
  - Card Detail and New Card window backgrounds
  - Delete button and destructive action styling
  - Dropdown menu options and modal popup background tones
- **Customizable Icons** — customize priority icons, due date progression markers, tab icons, and UI action/navigation icons
- **📤 Export & 📥 Import Themes (.attheme)** — export individual themes or multi-theme packs as JSON files to easily share styling across projects and team members
- **Granular Reset Controls** — quickly reset colors, icons, or full themes back to default presets
- **Themed Modal Dialogs (`ThemedDialog`)** — custom modal dialogs that fully respect active theme colors, borders, icons, and typography
- **Searchable Themed Dropdowns (`ThemedDropdownPopup`)** — dropdown menus styled to the theme with real-time text search filtering and checkmark indicators

### 🎨 Polish & UX
- Clean, modern UI that fits both **Light** and **Dark** editor themes with complete custom theme support
- Status bar with live card count, completion stats, due date notification intervals (due today, 1d, 2d, 3d, <1 week, >1 week, overdue), and column count
- Tab interface: switch between **Board**, **Notes**, and **Style** customizer tabs instantly
- Themed modal confirmation and alert dialogs (`ThemedDialog`) replacing native OS dialogs
- Themed dropdown menus with real-time search filtering across all selectors
- Keyboard shortcut: **Ctrl+Alt+T** to open
- Keyboard shortcut: **Alt+Shift+C** to quick-create a Task Card from anywhere (works in background)
- Keyboard shortcut: **Ctrl+Alt+N** to quick-create a Note from anywhere (works in background)
- **Ctrl+V** to paste clipboard images directly into notes
- Drag-and-drop with visual feedback (banner, drop zones, folder highlights)
- All data auto-saves on every change — no "Save" button needed

## 📂 Data Storage

Project board, note, category, and assignee data is saved in a modular, version-control-friendly structure:
```
ProjectSettings/AwesomeTaskManager/
├── AwesomeTaskManager.json        # Shared manifest, categories, assignees, templates & task prefix
├── AwesomeTaskManagerTheme.json   # Theme presets & custom visual styles
├── Boards/
│   └── {board_id}.json            # Granular modular board files
└── Notes/
    └── {note_id}.json             # Modular quick note files
```

Local developer session state (active filters, search query, selected board) is isolated in:
```
UserSettings/AwesomeTaskManager/AwesomeTaskManagerUser.json
```

Attached images are stored in:
```
Assets/Plugins/AwesomeTaskManager/Editor/AttachedImages/
```

- **Per-project** — each project has its own boards and notes
- **Team & Version-control friendly** — boards and notes are stored as separate modular JSON files, preventing monolithic Git merge conflicts. User session state is decoupled in `UserSettings/` (ignored by Unity `.gitignore`), ensuring zero dirty Git diffs when simply navigating or filtering.
- **External Disk Sync** — automatically detects external changes from `git pull` or branch switching and reloads data cleanly.
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
    │   ├── TaskBoardData.cs      — All data models (boards, cards, notes, folders, themes)
    │   └── Persistence.cs        — JSON save/load to ProjectSettings
    └── UI/
        ├── TBStyles.cs           — Colors, dynamic GUIStyles, theme engine
        ├── TaskBoardWindow.cs    — Main editor window (board, notes, style views)
        ├── CardDetailWindow.cs   — Card detail / creation popup with date picker
        ├── CategoryEditorWindow.cs — Global category manager
        ├── AssigneeManagerWindow.cs — Global team member & assignee manager
        ├── ImportFieldMappingWindow.cs — CSV/Excel import column mapping window
        ├── ThemedDialog.cs       — Themed modal confirmation and alert dialogs
        ├── ThemedDropdownPopup.cs — Themed searchable dropdown popup menus
        ├── ThemedTooltip.cs      — Themed tooltips & truncated text hover popups
        ├── ImageLargePreviewWindow.cs — Large image / GIF viewer window
        ├── NotePopupWindow.cs    — Pop-out note editor window
        ├── NoteClipboardController.cs — Shared note selection, right-click menu & clipboard logic
        ├── ThemedContextMenu.cs  — Themed right-click context menu popup
        ├── MarkdownRenderer.cs   — Markdown parsing and rich text renderer
        ├── EditorInputDialog.cs  — Shared modal text input dialog (single or two-field)
        ├── NoteVideoPlayer.cs    — Inline VideoPlayer widget for note video embeds
        ├── NoteVideoImportSettings.cs — VP8/audio import settings for note videos
        └── GifDecoder.cs         — Built-in animated GIF parser & decoder
```

---

## 🔧 Compatibility

| Requirement | Version |
|-------------|---------|
| Unity | **Unity 6** or newer |
| Render Pipeline | Any (SRP, URP, HDRP, Built-in) — it's editor-only! |
| Platform | All — editor tool, no runtime dependencies |
| Theme | ✅ Light & Dark editor themes |

**Zero dependencies.** No third-party plugins required.

---

## 💡 Tips & Tricks

- **🎨 Customize Themes & Colors** — Open the `🎨 Style` tab to switch between 13 built-in presets (including Vintage 8-Bit, Retro Synthwave, Fruit Company Glassmorphism, and Aurora Glow Glass) or create your own custom theme. Customize board canvas backgrounds, top bars, buttons, dropdowns, note editors, card backgrounds, and 17 label colors with full opacity/alpha control for both Dark and Light skins.
- **✨ Frosted Glass & Translucency** — Use the alpha channel slider on colors in the Style tab to create modern translucent / glassmorphic interfaces with layered depth.
- **📤 Export & Share Themes** — In the Style tab, click "Export Theme" or "Export All Themes" to save `.attheme` JSON files that teammates can import directly.
- **🏷 Customize Icons** — Change emoji or text icons for priority levels, due date status badges, tabs, and action buttons in the Style tab.
- **🔍 Searchable Dropdowns** — Type directly into the search bar inside any dropdown menu (board, category, assignee, priority, theme) to instantly filter options.
- **🪟 Themed Dialog Prompts** — All deletion, navigation, and confirmation dialogs now match your active theme colors and support Enter (confirm) and Escape (cancel) keyboard shortcuts.
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
- **GIFs from the web** — Right-click a GIF in your browser → Copy Image (or copy its `.gif` URL), then press Ctrl+V in a note
- **YouTube cards** — Put a YouTube link on its own line in a note; Preview shows a thumbnail card with the video title
- **Videos in notes** — Drag an `.mp4`/`.mov`/`.webm` into a note, pick it with 📎 Browse, or type `![[clip.mp4]]`, then press ▶ Play video in Preview. Videos are transcoded once on import (this can take a moment for long clips); if a video added earlier is silent or logs decoder warnings, click **Convert for editor playback** under the player
- **Link titles** — When adding a URL to a card, fill in "Title (optional)" so the card shows a readable name instead of the raw URL
- **Category colors** — Set a default color per category in the Category Manager (🏷 button in the toolbar); new cards auto-inherit the color
- **Scene Linking Navigation** — Click a linked scene object on a task card to find it instantly in the Hierarchy. If it's in a different scene, a confirmation dialog will help you switch and navigate to it automatically.
- **Link Notes to Cards** — Drag a note from the Notes list onto a task card on the board to link them. Or use the "Link Note" button in the card detail editor.
- **Auto-Link in Preview** — Paste any `http://` or `https://` URL, or write `[label](url)`, into a note. In Preview mode, it will be rendered as a clickable link.
- **Copy table cells** — In Preview mode, drag across table cells (or Shift+click) to select a range, then press Ctrl+C or right-click → Copy to copy it as tab-separated text.
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
- **👤 "My Tasks" Quick Filter** — Click the 👤 button in the board toolbar to instantly isolate cards assigned to or created by you.
- **🏷 Short Task IDs (`ATM-42`) & Git Branch Helper** — Every card receives a human-readable task code in its details footer (e.g., `ATM-42`). Click the task code badge to copy it to your clipboard, or right-click any card (or use the ⋮ menu) and choose **"Copy Git Branch Name"** (e.g., `feature/ATM-42-card-title`) for fast branch and PR naming.
- **👥 Team Collaboration & Git Sync** — Boards and quick notes are automatically stored in separate granular files under `ProjectSettings/AwesomeTaskManager/Boards/` and `Notes/` to prevent merge conflicts. Navigating boards and changing filters is saved in `UserSettings/` to keep your Git working tree clean, and external changes from `git pull` auto-reload smoothly.

---

## 📄 Changelog

### v2.6 (Team Collaboration, Modular Multi-File Persistence, Git Identity, Short Task IDs & Live VCS Sync)
- **Added** 📂 **Modular Multi-File Persistence** — boards and quick notes are now persisted as individual granular files (`ProjectSettings/AwesomeTaskManager/Boards/{board_id}.json` and `ProjectSettings/AwesomeTaskManager/Notes/{note_id}.json`), allowing concurrent team edits without monolithic JSON merge conflicts.
- **Added** 🔄 **Seamless Legacy Data Migration** — automatically detects pre-existing monolithic `AwesomeTaskManager.json` or legacy `TheUltimateTaskBoard.json` files and migrates them transparently without data loss.
- **Added** 👤 **Decoupled User State Isolation** — local workspace preferences (`lastBoardIndex`, active filters, search query, my-tasks filter) are now isolated into `UserSettings/AwesomeTaskManager/AwesomeTaskManagerUser.json` (ignored by Unity `.gitignore`), completely eliminating dirty Git diffs when navigating boards or toggling filters.
- **Added** 🔄 **Live Disk-Change Detection & VCS Sync** — automatically monitors data file timestamps on disk to detect changes from `git pull`, branch switching, or external edits, refreshing the board seamlessly without clobbering remote changes.
- **Added** 🏷 **Human-Readable Task IDs (e.g., `ATM-42`)** — tasks receive sequential task numbers and short codes (`taskCode`) with a configurable prefix (`taskPrefix`), 1-click badge copying in the Card Details footer, and context menu actions across board and card detail views.
- **Added** 🌿 **Git Branch Helper** — 1-click "Copy Git Branch Name" action in the card context menu and Card Details view (e.g., `feature/ATM-42-card-title`) for frictionless Git workflow linking.
- **Added** 👤 **Git Author Auto-Detection & Audit Trail** — automatically detects local author identity via `git config user.name` (with `Environment.UserName` fallback) and records `createdBy`, `lastModifiedBy`, and `lastModifiedDate` audit metadata on cards.
- **Added** 👤 **"My Tasks" View** — 1-click toolbar filter button (👤) to quickly isolate cards assigned to or created by the current developer.
- **Improved** 📐 **Card Details & Linked Items Layout** — refined "Add URL" and "Link Note" button sizing to span the full container width with no layout overflow or gaps, and aligned task code and creator metadata cleanly in the footer.

### v2.5 (Reliable Note Clipboard, Themed Context Menus, Clickable Links, Markdown Tables, GIFs, YouTube & Video Embeds)
- **Added** 🎞 **GIF Paste from the Web** — GIFs copied in a browser (read from the clipboard's HTML `<img src>`), pasted `.gif` URLs, and `data:image/gif;base64` sources are downloaded, validated by file signature, saved to `AttachedImages/`, and embedded as animated GIFs. Downloads show a cancelable progress bar with a 20s timeout.
- **Added** 🌐 **Remote Image Embeds** — `![](https://…)` images download asynchronously, are cached by URL (`remote_<hash>.<ext>`), show a "Loading image…" state, and display a warning if the download fails.
- **Fixed** 🧩 **Interlaced & Transparent GIFs** — `GifDecoder` now maps interlaced rows correctly and starts frames transparent, with disposal-to-background restoring transparency like web browsers.
- **Added** 📺 **YouTube Preview Cards** — standalone YouTube links (`watch?v=`, `youtu.be/`, `shorts/`, `embed/`, `live/`, mobile/music hosts) render a clickable card with the thumbnail, ▶ overlay and the oEmbed title; clicking opens the original link (timestamp included) in the browser.
- **Added** 🎬 **Inline Video Player (`NoteVideoPlayer`)** — `![[clip.mp4]]` and direct video URLs (`.mp4`, `.webm`, `.mov`, `.m4v`, `.ogv`) play inside the note Preview without entering Play Mode, with play/pause, stop, seek, time, mute and loop controls. Players are cleaned up on cache clear and assembly reload; codec support depends on the platform.
- **Fixed** 🔊 **Note Video Audio in Edit Mode** — the editor's audio engine never consumed `VideoPlayer` audio outside Play Mode (silent playback plus `AudioSampleProvider buffer overflow` warnings). `NoteVideoPlayer` now runs the `VideoPlayer` in `APIOnly` mode and streams the decoded samples into a looping streaming `AudioClip` on a hidden `AudioSource`, so `.mov` and `.mp4` notes play with sound; a hidden `AudioListener` is added when the open scene has none.
- **Improved** 🎞 **Warning-Free Note Videos** — videos dropped, pasted or browsed into `AttachedImages/` are imported with VP8/Vorbis transcoding and audio import (`NoteVideoImportSettings` + `NoteVideoImportPostprocessor`), so playback uses Unity's built-in decoder instead of Windows Media Foundation and no longer logs the `Unexpected timestamp values` / `Color primaries ... unknown` messages. Videos imported earlier show a one-click **Convert for editor playback** button.
- **Improved** 🖱 **Video Drag & Drop / File Paste** — video files can be dragged or pasted into notes just like images.
- **Improved** 📎 **Browse for Videos** — the note 📎 Browse button (Notes tab and pop-out window) now accepts videos too, with "Images & Videos", "Images", "Videos" and "All files" filters.
- **Added** 🏷 **Titled Card Links** — the Add Link dialog has an optional "Title" field, and a new ✎ Edit Link button changes the URL or title. Cards and tooltips show the title, with the URL in the tooltip.
- **Improved** 🔗 **Open URL Confirmation** — the confirmation dialog shows the link title (or the fetched YouTube video title) along with the URL.
- **Added** 🪟 **Two-Field Input Dialog** — `EditorInputDialog.ShowWithSecondField` for prompts that need a second value.
- **Added** 🧠 **Shared Note Clipboard Controller** — the Notes tab and pop-out note windows now share one `NoteClipboardController` for selection tracking, right-click handling, and Cut / Copy / Paste / Smart Paste, so both views behave identically.
- **Fixed** ✂ **Cut / Copy / Paste Reliability** — Cut and Copy now act on exactly the highlighted text, Copy and Cut stay enabled when right-clicking inside a selection, and stale selections are cleared when the caret moves via keyboard, typing, or clicking.
- **Fixed** 📍 **Paste at the Cursor** — Paste, Smart Paste, and Ctrl+V image paste insert at the current caret or replace the current selection. Right-click hit-testing now uses the character under the pointer (multi-line, wrapped, padded, and scrolled text), fixing pastes landing one line below.
- **Fixed** 🛡 **No More Accidental Note Wipes** — after a context-menu action the caret/selection is restored correctly instead of the whole note being selected, preventing a following paste from replacing the entire note.
- **Improved** 🖍 **Selection Stays Visible** — the highlighted selection remains drawn while the right-click menu is open, and focus, caret, and selection are restored when the menu closes (item picked or dismissed) so typing and shortcuts work immediately.
- **Fixed** 🖼 **Smart Paste Caret** — after Smart Paste inserts an image tag, the caret is placed right after the inserted tag rather than at the clipboard text length.
- **Improved** 📋 **Toolbar Paste & Browse at Caret** — the toolbar 📋 Paste and 📎 Browse buttons now insert images at the current cursor position (or replace the selection), matching Smart Paste.
- **Improved** 🎨 **Themed Right-Click Menus** — the note editor now always shows the styled `ThemedContextMenu` instead of Unity's plain default menu, with a new `OnClosed` callback and an opt-in fit-to-content mode (no scrollbar) for short menus.
- **Improved** 👁 **Preview-Mode Context Menu** — right-clicking in Preview shows only Copy, Copy Entire Note, Switch to Edit Mode, Popout Note, and Export Note; Edit mode keeps the full action list. Preview Copy now reliably reads the highlighted passage and strips rich-text formatting tags.
- **Improved** 🗒 **Simplified Quick Notes Row Menu** — removed "Cut Note Content" and "Smart Paste" from the note list right-click menu and removed its scrollbar.
- **Improved** 📐 **Smooth Quick Notes Resizing** — note cards are laid out within the visible sidebar width (accounting for the vertical scrollbar, no horizontal overflow), titles wrap inside the card, and the ↗ popout button and date/word-count info line scale and fade smoothly instead of disappearing abruptly.
- **Fixed** 📭 **Quick Notes List Visibility** — fixed a layout-pass issue that could make the note list appear empty.
- **Added** 🔗 **Clickable Markdown Links** — `[label](url)` links and raw URLs in Preview are rendered as underlined, clickable links; trailing punctuation is kept outside raw URLs and unsafe href characters are escaped.
- **Added** 📊 **Redesigned Markdown Tables** — tables render as a proper grid with a header row, accent header line, striped rows, borders, per-column alignment (`:--`, `:-:`, `--:`), escaped pipes (`\|`), word-wrapping, and responsive column widths. Fixed separator-row detection so valid tables are no longer shown as raw `| a | b |` text.
- **Added** 🔲 **Multi-Cell Table Selection** — drag across cells or Shift+click to select a range of table cells, then Copy via right-click or Ctrl+C (copied as tab-separated text). Click outside the table or press Escape to clear.
- **Improved** 🌈 **Themed Tables** — table backgrounds, stripes, grid lines, and selection colors follow the active Awesome Task Manager theme.
- **Added** 🧪 **EditMode Tests** — NUnit coverage for clipboard range/insert helpers, rich-text stripping, context-menu composition, Quick Notes layout helpers, link formatting, table parsing, alignment, column fitting, and selection text, GIF URL/HTML source/image signature detection, interlaced GIF decoding, YouTube ID parsing, video path detection, oEmbed titles, and link title/confirmation messages.

### v2.4 (Context Menu Selection, Smooth Sidebar & Responsive Layouts)
- **Added** 📋 **Copy/Cut Selection for Notes** — added "Copy Selection" (Preview & Edit modes) and "Cut Selection" (Edit mode) to the right-click context menu in both the main window and pop-out note windows.
- **Fixed** 📜 **Note Cut & Undo Reliability** — resolved a critical bug where "Cut Selection" could wipe note content and fixed an issue where "Undo" was immediately overwritten by stale UI state; added automatic focus clearing and strengthened safety checks for all content-modifying actions.
- **Improved** 📏 **Smooth Sidebar Resizing & Animation** — implemented smooth width transitions for the Quick Notes sidebar with percentage-based layout thresholds (15% for ultra-narrow, 20% for tooltips) for better responsiveness across various window sizes.
- **Added** 💡 **Automatic Tooltips for Sidebar Items** — headers, folders, and note titles now automatically show tooltips when the sidebar is narrowed (below 20% width) and text becomes clipped.
- **Improved** 🔄 **Sidebar Reset & Persistence** — the Quick Notes sidebar now resets to a more compact default width (200px) when switching from the Board tab back to the Notes tab.
- **Improved** ➕ **Dynamic "+ Note" Button** — the "+ Note" button automatically collapses to a simple "+" icon when the sidebar width drops below 100px to maintain a clean layout.
- **Fixed** 📜 **Note Scrolling & Layout Exceptions** — resolved ArgumentException control group mismatches during sidebar animation and restored native scrolling in Note Edit mode by switching to auto-expanding text areas.

### v2.35 (Enhanced Due Date Status Bar Notifications)
- **Added** ⏰ **Expanded Due Date Status Indicators** — bottom status bar now provides real-time counts and notifications for tasks due in **1 day**, **2 days**, **3 days**, **less than a week** (4–6 days), and **over a week** (7+ days), alongside completed, overdue, and due today counts.
- **Improved** 📊 **Dynamic Status Bar Breakdown** — due date indicators automatically render with their corresponding theme-styled icons and accent colors whenever matching non-zero task counts exist.
- **Added** 🧩 **`DueDateSummary` Model & Helper API** — added `DueDateSummary` struct in `TaskBoardData.cs` and `CalculateDueDateSummary` / `BuildStatusBarText` in `TaskBoardWindow.cs` for reliable programmatic due date aggregation and reporting.

### v2.3 (Markdown Rich Text, Code Blocks & Syntax Highlighting)
- **Added** ✨ **Full Markdown Inline Formatting (`MarkdownRenderer`)** — added rich text formatting for bold (`**text**` / `__text__`), italic (`*text*` / `_text_`), combined bold+italic (`***text***`), inline code (` `code` `), strikethrough (`~~text~~`), and markdown web links (`[text](url)`).
- **Added** 💻 **Code Blocks with Syntax Highlighting** — multi-line code blocks (```` ```csharp ````, ```` ```json ````, ```` ```shader ````) now render in a container with syntax coloring and an interactive **"📋 Copy"** button that temporarily changes to **"✓ Copied!"**.
- **Added** 💬 **Blockquotes Rendering** — added visual callout styling for Markdown blockquotes (`> text`).
- **Added** 📊 **Markdown Tables** — added support for Markdown tables (`| Header 1 | Header 2 |`) with aligned columns and alternating row shading.
- **Improved** 📝 **Rich Text Headers & Lists** — headers (`#`, `##`, `###`), checklists (`- [ ]`, `- [x]`), bullet points (`-`, `*`, `+`), and numbered lists (`1.`) now support inline rich text formatting.

### v2.2 (URL Attachment, Input Focus & Category Management Fixes)
- **Fixed** 🔗 **URL Attachment on Cards (`EditorInputDialog`)** — resolved a bug where clicking "OK" in the URL input dialog failed to attach the link to task cards because dialog lifecycle cleanup (`OnDisable` / `OnDestroy`) was prematurely clearing the return value upon window close.
- **Improved** 🌐 **URL Input Sanitization** — automatically trims whitespace and prepends `https://` if the protocol prefix is omitted when adding external web links to cards.
- **Fixed** 🏷 **Category Input & Auto-Commit (`CardDetailWindow`)** — resolved an issue where typed new categories were lost or reverted to the first list item when clicking off or closing. Typed categories and checklist items are now automatically committed on close, save, or field defocus, with Enter key submission support.
- **Fixed** 🧹 **Input Field Focus Bleed & Leftover Text** — resolved an issue where Unity IMGUI's recycled text editor retained leftover text (such as deleted category names or previously typed inputs) and leaked them into the title field when creating a new card. All input fields across Card Detail, Category Editor, Assignee Manager, Note Popup, and Dialogs now use dynamic session-scoped control names with complete keyboard focus and text editing buffer resets.
- **Fixed** 🗑 **Delete Category in Card Detail** — resolved an issue where clicking the delete category button in the Card Detail window failed to delete the category. It now prompts for confirmation, cleanses the category from global save data and all existing cards, persists changes immediately, and refreshes open board windows.

### v2.1 (Unity 6.6+ Compatibility Fix)
- **Fixed** 🛠 **Unity 6.6+ Compatibility (CS0619 Obsolete GetInstanceID)** — resolved compilation errors (`error CS0619: 'Object.GetInstanceID()' is obsolete: 'Use GetEntityId instead.'`) in `ThemedTooltip.cs` when building on Unity 6.6+ (`UNITY_6000_3_OR_NEWER`) by safely resolving editor window identifiers using `GetEntityId()`, while maintaining backwards compatibility with `GetInstanceID()` on earlier Unity versions.

### v2 (Custom Themes & Styling Engine, Opacity Controls, Frosted Glass Like Themes, Themed Dialogs)

#### 🎨 Custom Themes & Styling Engine
- **Added** 🎨 **Dedicated Style Customizer Tab** — new `🎨 Style` tab in the main window providing complete visual customization of the entire Awesome Task Manager.
- **Added** 🎛 **13 Built-in Theme Presets** — choose from classic, retro, and translucent presets:
  - **Vintage 8-Bit** (Classic 8-bit vintage console style with console gray casing, controller crimson buttons, and retro gaming icons)
  - **Retro Synthwave** (80s arcade synthwave with neon magenta, cyan, laser pink, and arcade gaming icons)
  - **Fruit Company Glassmorphism** (macOS Big Sur/Monterey frosted translucent glass with vibrant system accents)
  - **Aurora Glow Glass** (Nordic arctic translucent night with glowing aurora emerald & violet glass panes)
  - **Cupertino Twilight** (Warm California sunset frosted peach, lavender, and coral glass tones)
  - **Frosted Ice** (Ultra-clean crystal ice glass with subtle frost overlays)
  - **Default** (Balanced modern dark & light theme)
  - **Dark Slate** (Refined deep navy and slate grey tones)
  - **Cyberpunk Neon** (High-contrast neon cyan, magenta, and deep purple)
  - **Forest Emerald** (Natural earthy greens and sage accents)
  - **Pastel Dream** (Soft soothing lavender, peach, and mint pastels)
  - **Sunset Warm** (Rich amber, terracotta, and warm dusk hues)
  - **Monochrome Minimal** (Ultra-clean grayscale typography and borders)
- **Added** 💾 **Independent Theme Storage for Teams** — theme preferences and custom styles are stored in a dedicated `AwesomeTaskManagerTheme.json` file, decoupled from `AwesomeTaskManager.json`. Teammates can each use distinct visual styles without creating Git conflicts or modifying board data.
- **Added** ✨ **Full Opacity & Alpha Channel Controls** — enabled alpha/opacity sliders across all theme color pickers to allow translucent overlays, floating glass cards, and frosted glass styling.
- **Added** 🪄 **Custom Theme Management** — create, duplicate, rename, customize, and delete custom user themes.
- **Added** 🌓 **Dual-Skin Theme Support** — configure distinct color palettes for Unity Pro Skin (Dark Mode) and Personal Skin (Light Mode) within each theme.
- **Added** 🌈 **Comprehensive Color & Opacity Customization**:
  - **17 Label Colors** — full palette control over every category and tag color swatch with alpha opacity support.
  - **Canvas & Header Backgrounds** — customize Board canvas background and Top Bar / Tab Bar background colors.
  - **Tab Headers & Navigation** — customizable active, inactive, and hover background and text colors for Board, Notes, and Style tab buttons.
  - **Card & Column Styling** — customize column header text, board header text, column backgrounds & alternating backgrounds, card backgrounds (`pro_CardBg` / `personal_CardBg`), borders, card titles, card body text/badges, card details metadata text, card category tags (`pro_CardCategoryTag` / `personal_CardCategoryTag`), card task checklist text, and "+ Add Card" button colors with hover states.
  - **Definite Card States & Task Counts** — customize dedicated colors for Overdue (`pro_StatusOverdue`), Due Today (`pro_StatusDueToday`), Due Soon (`pro_StatusDueSoon`), Completed (`pro_StatusCompleted`), and Tasks Completed Count (`pro_TasksCompletedCount`) across board cards and detail editors.
  - **Tooltips & Truncated Text Hover Popups** — customize dedicated colors for Tooltip Background (`pro_TooltipBg` / `personal_TooltipBg`), Tooltip Text (`pro_TooltipText` / `personal_TooltipText`), and Tooltip Border (`pro_TooltipBorder` / `personal_TooltipBorder`) with opacity and glass specular highlight support.
  - **Notes & Editor Styling** — customize note sidebar background, note editor background, note markdown textarea input background & text colors, note title text color, note folders text color, "+ Note" button colors, "📥" import note button colors, note cards (normal, selected, hover), and note popout window backgrounds.
  - **Card Details & Popouts** — customize Card Detail and New Card window backgrounds, description textarea input styling, themed year and day numeric date input fields, text colors, and section headers.
  - **Buttons & Controls** — customizable normal and on-hover backgrounds and text colors for all buttons and toolbar dropdowns.
  - **Delete Actions** — customizable delete buttons and destructive action styling across all windows.
  - **Dropdown Menus & Popups** — customize background, text, hover background, and hover text colors for dropdown menus and popup dialogs.
- **Added** 🖼 **Live Interactive Preview Mockup** — real-time preview panel in the Style tab displays immediate visual feedback for cards, notes, dropdowns, opacity, and buttons as you tweak theme properties.
- **Added** 📤 **Theme Export & Import (.attheme)** — export individual themes or multi-theme packs as JSON files and import them into any project to share styles with team members.
- **Added** 🔄 **Granular Reset Controls** — easily reset individual color sections, interface icons, or entire themes back to default presets.

#### 🏷 Customizable Icons & Status Indicators
- **Added** 🔥 **Custom Priority Icons** — customize the visual icons for all 5 priority levels (None, Low, Medium, High, Urgent).
- **Added** 📅 **Custom Due Date Progression Icons** — customize status indicators for Overdue, Due Today, Due Soon, and Due Date badges.
- **Added** 🧭 **Custom Interface & Navigation Icons** — customize icons for Board, Notes, Style tabs, Category, Assignee, Link, Archive, Unarchive, Detail, New Card, Checklists, Attachments, URLs, Save, Cancel, Move Up/Down, and Delete actions.

#### 🪟 Themed Dialogs, Dropdowns & Tooltips Engine
- **Added** 💬 **Custom Themed Tooltips & Truncated Text Popups (`ThemedTooltip`)** — replaced Unity's default plain black box tooltips and hover popups with theme-styled floating overlays supporting custom background colors, borders, typography, opacity, specular sheen, soft drop shadows, and automatic screen edge clamping.
- **Added** 🪟 **Custom Themed Dialogs (`ThemedDialog`)** — replaced native OS modal dialogs (`EditorUtility.DisplayDialog` and `DisplayDialogComplex`) across all windows with customizable themed dialog windows matching the active theme's styling, colors, borders, icons, and keyboard navigation (Enter/Escape).
- **Added** 🔍 **Searchable Themed Dropdown Menus (`ThemedDropdownPopup`)** — replaced native OS popups with themed dropdown menus featuring real-time text search filtering, checkmark indicators, color swatches, and accurate anchor positioning below trigger buttons.

#### 🛠 Board & Card Improvements & Fixes
- **Added** 🛡 **Unsaved Card Creation Guard** — pressing "+ Add Card" or opening another card while an existing card with unsaved edits is open now prompts with a styled confirmation dialog (`ThemedDialog`) to discard or keep changes, preventing unintended loss of work.
- **Fixed** 📌 **Active Board Selection Retention** — adding a new card or saving changes in the card detail window now reliably preserves the active board without switching to a different board.
- **Fixed** 🧹 **Board Selector Deduplication** — normalized board lists and cleansed backup duplicates from the board selector dropdown.
- **Fixed** 📐 **Delete Board Button Alignment** — styled and aligned the toolbar delete board button to prevent vertical offset and layout overlapping.

### v1.7 (Beta Export/Import + Import Mapping Profiles)

#### Export & Import
- **Added** 📤 **CSV & Excel Board/Column/Card Export & Import** — comprehensive ClickUp-compatible export and import for boards, columns, and individual cards in CSV and Excel (.xlsx/.xml) formats.
- **Added** 📦 **Advanced JSON Export/Import** — native JSON formats (.atb, .atcl, .atc) moved to an 'Advanced' submenu for full data preservation.
- **Added** 🌐 **Trello CSV Import** — full Trello export mapping (Card Name → Status, Members → Assignees, Labels → Tags, etc.) with optional custom-field priority parsing.
- **Improved** 📂 **Organized Menu Structure** — all export and import actions grouped under a dedicated **Beta (Export or Import)** submenu.
- **Improved** 🔄 **Unified Data Mapping** — centralized mapping system ensures consistent handling of Status, Priority, Assignees, Tags, and Checklists across all formats.
- **Fixed** 📦 **Empty Column Round-Trip** — boards now preserve their full column structure in export placeholders, and import correctly restores empty columns even if they contained no cards.
- **Fixed** 🔄 **Data Synchronization & Refresh** — automatic filter resets, data normalization, and forced GUI redraws after every import operation.
- **Fixed** 📊 **Import Column Ordering** — case-insensitive column mapping and automatic sorting (To Do → In Progress → Review → Done → Archived) for ClickUp-preset imports.
- **Fixed** 🛡 **ExitGUIException** — `ExitGUIException` is now silently swallowed inside import callbacks and no longer logged as a false CSV/Excel import error.

#### Import Field Mapping UI
- **Added** 🗺 **Interactive Import Mapping Window** — every CSV/Excel board, column, and card import now opens a field-mapping dialog before committing, letting you match any incoming column to any Task Manager field.
- **Added** 🎛 **Preset Selector** — one-click presets (Generic, ClickUp, Trello) auto-populate the mapping for known services.
- **Added** 💾 **Saved Import Profiles** — name and save any mapping configuration as a reusable profile, persisted in the project's task manager save file so nothing is lost between sessions.
- **Added** ♻️ **Remember Mapping by File Pattern** — enable "Remember last mapping for matching files" and provide a wildcard pattern (e.g. `trello-export*.csv`) to automatically pre-fill the mapping next time a matching file is opened.
- **Added** 🔬 **Header-Signature Matching** — profiles automatically detect the best match by comparing the exact set of column headers from the incoming file, even when the filename changes.
- **Added** 📁 **Scoped Profiles** — profiles are separated by import type (Board / Column / Card) so board profiles never appear in a column import dialog and vice versa.
- **Added** 🗂 **Built-in Default Profiles** — pre-configured, non-deletable profiles for the most common services:
  - **AwesomeTaskManager CSV** (round-trip your own exports)
  - **ClickUp Export**
  - **Trello Export**
  - **Jira Export**
  - **Asana Export**
  - **Monday.com Export**
  - **Generic Spreadsheet**
- **Added** 🖊 **Profile Rename & Update** — edit the profile name in the dialog and hit Import to update an existing profile in place.
- **Added** 🗑 **Profile Delete** — delete any user-saved profile directly from the mapping dialog (built-in profiles are protected and cannot be deleted).

#### Data Persistence & Stability
- **Fixed** 🛡 **Critical: Boards no longer reset on project open** — `Persistence.Load()` now returns `null` for existing-but-unreadable save files instead of silently returning fresh defaults, preventing destructive overwrites.
- **Fixed** 🔄 **Stale EditorWindow State** — all editor windows (`TaskBoardWindow`, `CardDetailWindow`, `CategoryEditorWindow`, `AssigneeManagerWindow`, `NotePopupWindow`) now store `SaveData` as a non-serialized field and always reload fresh from disk on `OnEnable`, eliminating stale in-memory state after domain reloads or Play Mode transitions.
- **Fixed** 🎨 **Styling Loads Immediately** — colors, column order, and styling now display correctly the very first time a board opens or is imported, without needing to switch away and back. Achieved via `TBStyles.InvalidateCache()` and a deterministic post-load visual pipeline applied on every `OnEnable` and after every import.
- **Fixed** 📋 **Column Order Preserved** — `SaveData.Normalize()` now removes null columns and cards while preserving existing list order, ensuring columns stay in the order you arranged them after save/load cycles.

### v1.6.1
- **Fixed** 🛡 **Added More Robust Checks when loading and saving** — Added more robust checks when loading and saving data to prevent potential data loss or corruption.

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

## 🤖 AI Integration & Awesome Task Manager Next (Optional Upgrade)

Awesome Task Manager can be upgraded with the optional **Awesome Task Manager Next** AI companion.

- **For AI Agents & Coding Assistants** (Junie, Claude Code, Cursor, Windsurf, Rider AI, Unity AI Assistant):
  - If **Awesome Task Manager Next** is installed in the project, **always use the registered ATM Next MCP tools (`atm_*`) or the C# API (`AwesomeTaskManager.Next.ATMNextService`)** when asked to view, create, rank, move, or manage tasks, notes, categories, and assignees.
  - **DO NOT edit `ProjectSettings/AwesomeTaskManager/AwesomeTaskManager.json` directly**, as direct JSON file edits bypass validation and live UI synchronization.
  - When available, access the AI Assistant via top menu: **`Tools → Awesome Task Manager Next (AI)`** or check `Assets/Plugins/AwesomeTaskManagerNext/README.md` for full MCP tool documentation and available commands.

---

## 📬 Support

Found a bug or have a feature request? Please reach out via the **Asset Store** page.

---

*Made with ❤️ for game developers who want to stay organized without leaving Unity.*
