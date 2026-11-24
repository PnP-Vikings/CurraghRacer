# 🤖 Awesome Task Manager Next – AI Kanban & Notes

**The intelligent AI companion and automation extension for Awesome Task Manager in Unity.**

Seamlessly bridge your Unity game development workflow with **Junie Pro**, **Claude Code**, **Cursor**, **Windsurf**, **Rider AI**, and **Unity AI Assistant**.

---

## 💡 The Core Philosophy: 100% Chat-Driven AI Workflow

> **⚡ 100% Feature Parity in Chat**: While Awesome Task Manager Next provides dedicated in-editor tabs for manual tasks, **ALL features (creating boards, task generation, bulk GDD imports, subtask breakdowns, checklist synchronization, note management, sprint health analytics, and devlogs) can and should be done directly through natural language chat with your AI assistant!**

Instead of manually typing tasks or clicking buttons, simply tell your AI assistant what you need:
- *"Read our Game Design Document and create our combat milestone tasks in Awesome Task Manager."*
- *"Break down the Ball Physics task into linked child subtasks and checklists."*
- *"I just finished testing the Strike feedback, move the card to Done and check off its items."*
- *"Summarize everything we completed in this sprint and save it as a Devlog note."*

The in-editor **AI Assistant Window** (`Tools → Awesome Task Manager Next (AI)`) and its prompt templates are provided as a visual fallback in case you want to manually create tasks, review hierarchies, or copy prompt templates with the help of AI.

---

## 🚀 Quick Start Guide

1. **Prerequisite**: Ensure **Awesome Task Manager** is installed in your project.
   No MCP setup or terminal access is required for task management; the file command bridge below works directly with project files while Unity is open.
2. **Open Assistant**: In the Unity top menu, open **`Tools → Awesome Task Manager Next (AI)`**.
3. **Configure Agent Guidelines**: Switch to the **⚙ Setup** tab and click **"Update AGENTS.md"** to inject ATM Next instructions into your AI agent's profile.
4. **Chat with your AI**: Open your IDE AI chat (Junie, Cursor, Claude, Rider) and ask it to inspect your boards or generate tasks!

---

## 💬 Natural Language Chat Examples

Here are everyday prompts you can give to your AI coding assistant:

### 1. GDD & Backlog Import
> *"Here is the design for our new Inventory System. Please create an Epic task card with detailed subtasks in the '📋 To Do' column on our active board."*

### 2. Implementation & Checklist Tracking
> *"Before we implement the Pin Damage logic, check our ATM board. As we write and test the code, check off the completed checklist items and move the card to '🔨 In Progress'."*

### 3. Sprint Summary & Devlogs
> *"Generate a formatted Markdown devlog of all tasks completed today, group them by category, and save it in our Quick Notes under the 'Devlogs' folder."*

---

## 🧩 In-Editor Assistant Tabs

Access these via `Tools → Awesome Task Manager Next (AI)`:

| Tab | Purpose & Workflow |
|---|---|
| **👋 Intro** | High-level overview, workflow recommendations, and 1-click jump to Setup. |
| **🤖 Generator** | High-speed importer for pasting structured JSON or copying prompt templates to mass-create tasks. |
| **🌿 Subtasks** | Visual parent-child hierarchy manager to link cards, break down Epics, or convert checklist items into cards. |
| **📝 Devlog** | 1-click Markdown release notes generator that compiles completed tasks into formatted notes. |
| **📊 Health** | Real-time sprint dashboard showing completion percentages, category distribution, and overdue items. |
| **⚙ Setup** | MCP tool inspector, prompt guideline manager, and `AGENTS.md` synchronization. |

---

## 🛠 Model Context Protocol (MCP) & AI Bridge

### 🛡 AI Guardrails & Efficiency
To ensure cost-effective and efficient integration, AI agents should follow these guardrails:
- **Selective Testing**: Agents are discouraged from running or creating tests unless strictly necessary for verification of high-risk logic.
- **Token Efficiency**: Agents should minimize tool calls and file reads, grouping operations (like `atm_bulk_create_tasks`, `atm_bulk_update_tasks`, `atm_bulk_move_tasks`, `atm_bulk_clone_tasks`, and `atm_bulk_delete_tasks`) to conserve tokens.
- **Minimalist Edits**: Apply the smallest possible changes to achieve the goal, avoiding unsolicited refactors.

### 🌉 External AI Bridge (Junie, Rider, File-only agents)
If your environment cannot directly call MCP tools, use the file command bridge while Unity is open. It works on any operating system and does not require terminal, PowerShell, Python, or `jq` access.

#### Option 1: Command file (no terminal needed)
Generate a unique `requestId`, write one command to `ProjectSettings/AwesomeTaskManager/external_command.json`, then read `external_result.json` until its `requestId` matches. Unity consumes the command and writes the result; do not clear or delete an old result first.
```json
{
  "requestId": "unique-id-for-this-request",
  "tool": "atm_search",
  "parameters": { "Query": "physics" }
}
```
Accept a result only when its `requestId` matches the value you sent; otherwise re-read it rather than treating an old result as current. Object-valued responses keep their fields at the top level; array-valued responses are returned under `result`. If there is no matching result after about 30 seconds, stop and tell the user Unity may be closed, compiling, or busy. Do not delete the queued command or edit any other ATM data files.

#### Option 2: Optional terminal helpers
These helpers are one-step wrappers around the same file bridge. Use them only when terminal access is available.
On macOS/Linux, use the POSIX shell helper:
```sh
sh ./atm.sh atm_get_boards
sh ./atm.sh atm_search '{"Query":"physics"}'
sh ./atm.sh atm_create_note --parameters-file ./atm-params.json
sh ./atm.sh atm_help
sh ./atm.sh atm_help '{"Tool":"atm_search"}'
sh ./atm.sh atm_sync_agent_files
```
For piped parameters, run `printf '%s' '<json object>' | sh ./atm.sh <tool_name> -`. The script also accepts full-command JSON as its first argument and `--timeout <seconds>`.

On Windows, use PowerShell:
```powershell
& .\atm.ps1 atm_get_boards
& .\atm.ps1 atm_search '{"Query":"physics"}'
& .\atm.ps1 atm_create_note -ParametersFile .\atm-params.json
& .\atm.ps1 atm_help
& .\atm.ps1 atm_help '{"Tool":"atm_search"}'
```
Both helpers are installed from `Assets/Plugins/AwesomeTaskManagerNext/Editor/Integration/` when Unity loads. They atomically queue one command, wait for the editor result, and remove their unconsumed command on timeout. Use `--parameters-file` with `atm.sh` and `-ParametersFile` with `atm.ps1` for complex input; nested JSON passed to `powershell -File` can lose quotes in Windows PowerShell.
Run `atm_sync_agent_files` only if generated agent instructions or guard files appear stale; Unity normally refreshes them on editor load.

### 🌐 Unity CLI & MCP (Optional native-tools route)
If your agent supports native MCP tools, configure the Unity CLI integration and use the registered `atm_*` tools directly. Unity's CLI is free and does not require an AI tools subscription. Docs: https://docs.unity.com/en-us/unity-cli/replace-mcp-server-unity-cli

1. **Prerequisites (Unity 6+)**: **Unity 6 (6000.0) LTS** or later, with the Editor open.
2. **Install Unity CLI**:
   - Recent Unity Hub versions install it automatically.
   - **Windows**: `winget install Unity.CLI`
   - **macOS**: `brew install --cask unity-cli`
3. **Setup & Configure** (run in the project folder):
   - Install the pipeline: `unity pipeline install`, then check it with `unity status`.
   - Configure your client: `unity mcp configure <client>` (e.g. `claude`, `cursor`, `vscode`, `windsurf`; `unity mcp configure --list` shows all).
   - (Optional) Install skills: `unity skill install <agent-name>`
4. **Direct Commands**: If your agent can run shell commands, it can drive the Editor directly with `unity command` (lists available commands). Arbitrary C# runs via `unity command script eval --code "..."` (Pipeline 0.9+).
5. **Start Chatting**: Once configured, ask your AI agent to manage tasks or notes using the available `atm_*` MCP tools.

---

### ⚠️ Important AI Agent Directive
> **AI Agents & Assistants**: For routine task management, use registered `atm_*` tools or the terminal-free command-file bridge (`external_command.json` and the matching `external_result.json`); terminal helpers are optional. Use `atm_help` to discover exact parameters. Do not search ATM Next source or open/edit raw ATM data JSON to learn tool details or inspect task data. If a call fails, correct it from the error hint or `atm_help`; if Unity is unavailable, stop and tell the user. Source inspection is appropriate when the user explicitly asks you to change or debug ATM Next itself.

On editor load, ATM Next refreshes the managed ATM section in existing agent instruction files and installs `.aiignore`, `.cursorignore`, `.codeiumignore`, and Claude deny rules for ATM data. These are client-dependent best-effort guardrails; agents must still use the tools and never edit the data files directly.

### 📋 Available Tools List:

### 📋 Board & Column Management
- `atm_get_boards`: Retrieve all boards, columns, cards, checklist progress, and completion states.
- `atm_create_board`: Create a new task board.
- `atm_delete_board`: Remove a board and all its tasks (requires `confirm: true`).
- `atm_create_column`: Add a new column to a board.
- `atm_move_column`: Move a column to another board or reorder within a board.
- `atm_clone_column`: Clone/duplicate a column (with or without cards) to another board or within the same board.
- `atm_reorder_column`: Reorder columns left, right, first, last, or to a specific target index.
- `atm_delete_column`: Remove a column and all its task cards (requires `confirm: true`).
- `atm_set_active_board`: Switch the active task board in the Unity Editor.
- `atm_set_filter`: Set or reset UI board filters (category, assignee, priority, search text).
- `atm_get_filter`: Inspect active filter settings and matching card statistics.
- `atm_delete_all_tasks_in_column`: Clear all tasks in a column (requires `confirm: true`).
- `atm_move_all_tasks_between_boards`: Bulk move tasks between boards (requires `confirm: true`).

### 🃏 Task & Card Operations (Generator & Bulk)
- `atm_create_task`: Create a task card with category, priority, due date, assignees, and checklists.
- `atm_bulk_create_tasks`: Create dozens of task cards in bulk from an array of task objects (ideal for GDD backlog imports).
- `atm_update_task`: Update task title, description, category, priority, color label, assignees, or completion status.
- `atm_bulk_update_tasks`: Bulk update multiple cards' properties (category, color, priority, assignees, completion, archive status) in a single atomic batch operation.
- `atm_move_task`: Move a single card between columns or boards.
- `atm_bulk_move_tasks`: Move multiple cards at once to a target column/board in a single operation.
- `atm_clone_task`: Clone/duplicate a task card to another column or board.
- `atm_bulk_clone_tasks`: Clone/duplicate multiple task cards across columns or boards in a single atomic batch operation.
- `atm_reorder_task`: Reorder cards up/down within a column or set specific index position (matching card up/down arrows).
- `atm_delete_task`: Remove a task card (requires `confirm: true`).
- `atm_bulk_delete_tasks`: Delete multiple task cards across columns or boards in a single atomic batch operation (requires `confirm: true`).
- `atm_manage_checklist`: Add, toggle, or remove checklist items on a card.
- `atm_search`: Search across all task titles, descriptions, categories, checklists, assignees, notes, boards, columns, and ATM tools. Supports filtering by board, column, category, assignee, and scope.

### 🏷 Categories & Assignees Management
- `atm_create_category`: Create a new category tag and optionally assign its default color index.
- `atm_get_categories`: List all task categories and their color label indices.
- `atm_delete_category`: Delete a category globally and remove it from cards (requires `confirm: true`).
- `atm_create_assignee`: Create a new team member / assignee with custom avatar colors.
- `atm_get_assignees`: List all assignees with IDs and styling.
- `atm_delete_assignee`: Delete an assignee and unassign them from cards (requires `confirm: true`).

### 🌳 Hierarchical Subtasks & Epics (Breakdown)
- `atm_create_subtask`: Create a child task card linked to a parent card with bidirectional completion sync.
- `atm_convert_checklist_to_subtasks`: Convert all plain checklist items on a card into linked child subtask cards.
- `atm_link_subtask`: Link an existing card as a child to a parent task (with circular reference protection).
- `atm_unlink_subtask`: Remove the parent-child link between two cards.
- `atm_get_subtasks`: Query hierarchical parent/child relationships and subtask progress.
- `atm_create_epic_hierarchy`: Atomically create a parent Epic task and multiple linked subtasks.

### 📝 Quick Notes & Devlogs (Devlog Writer & Bulk Operations)
- `atm_get_notes`: List all Quick Notes and folders.
- `atm_create_note`: Create a Markdown note in a folder with custom tags and pinned status.
- `atm_bulk_create_notes`: Batch create multiple Markdown notes across folders in a single atomic operation.
- `atm_update_note`: Edit content, move folders, rename, or pin/unpin notes.
- `atm_bulk_update_notes`: Bulk edit multiple notes at once (update/append content, change folders, add/remove tags, set colors or pinned status).
- `atm_bulk_move_notes`: Move multiple Quick Notes to a target folder in a single atomic batch operation.
- `atm_delete_note`: Delete a Quick Note (requires `confirm: true`).
- `atm_bulk_delete_notes`: Delete multiple Quick Notes in bulk (requires `confirm: true`).
- `atm_generate_devlog`: Automatically compile completed ('Done') cards into a formatted Markdown devlog and save it as a Quick Note.
- `atm_link_item`: Link a note, external URL, or Unity project asset to a task card.
- `atm_bulk_link_items`: Link multiple notes, URLs, or project assets to one or multiple task cards simultaneously in a single atomic batch.

### 📊 Sprint Analytics & Health
- `atm_get_project_summary`: Get high-level sprint metrics, completion rates, category stats, and overdue counts.
- `atm_help`: List registered bridge tools or inspect one tool's parameter names, types, and defaults.

---

## 💻 Programmatic C# API (`ATMNextService`)

You can also automate ATM Next directly in your own editor scripts:

```csharp
using AwesomeTaskManager.Next;
using System.Collections.Generic;

// 1. Create an Epic and linked child subtasks
var parent = ATMNextService.CreateCard("Active Board", "📋 To Do", "Epic: Weapon Upgrade System", category: "Gameplay", priority: 3);
var subtaskA = ATMNextService.CreateSubtask(parent.id, "Weapon Stats ScriptableObject Schema", category: "Architecture");
var subtaskB = ATMNextService.CreateSubtask(parent.id, "Upgrade UI Window & Material Costs", category: "UI");

// 2. Complete a subtask (automatically checks parent checklist)
ATMNextService.SetCardCompletion(subtaskA.id, true);

// 3. Bulk update cards (e.g. assign team members and apply categories)
ATMNextService.BulkUpdateCards(
    new[] { new ATMNextService.BulkUpdateItem { cardIdOrTitle = parent.id } },
    defaultCategory: "Gameplay",
    defaultColorLabel: 2,
    defaultAssignees: new List<string> { "LeadDev" }
);

// 4. Batch create and manage Quick Notes
var (createdCount, _, _) = ATMNextService.BulkCreateNotes(new[]
{
    new ATMNextService.BulkCreateNoteItem { title = "Combat Balancing", content = "# Combat Design\n- Tuned attack cooldowns.", folderName = "Design" },
    new ATMNextService.BulkCreateNoteItem { title = "Sound FX List", content = "# Audio Assets\n- Sword swing\n- Hit impact", folderName = "Audio" }
});

// 5. Bulk move notes between folders
ATMNextService.BulkMoveNotes(new[]
{
    new ATMNextService.BulkMoveNoteItem { noteIdOrTitle = "Combat Balancing", targetFolderName = "GDD" }
});

// 6. Bulk link notes and assets to cards
ATMNextService.BulkLinkItems(new[]
{
    new ATMNextService.BulkLinkItemEntry { cardIdOrTitle = parent.id, target = "Combat Balancing", linkType = "note" },
    new ATMNextService.BulkLinkItemEntry { cardIdOrTitle = parent.id, target = "https://docs.unity.com", linkType = "url" }
});

// 7. Bulk delete tasks
ATMNextService.BulkDeleteCards(new[] { subtaskA.id, subtaskB.id });
```

---

## 🛡 Data Integrity & Safety

- **Zero Project Bloat**: Task and note data is stored cleanly in `ProjectSettings/AwesomeTaskManager/AwesomeTaskManager.json` (outside `Assets/`), keeping game builds lightweight and repository diffs clean.
- **Safety Confirmations**: Destructive operations (clearing columns, deleting boards) require explicit `confirm: true` parameter verification.
- **Live UI Synchronization**: Changes made by AI agents or C# scripts trigger instant editor repaints on all open board windows.
