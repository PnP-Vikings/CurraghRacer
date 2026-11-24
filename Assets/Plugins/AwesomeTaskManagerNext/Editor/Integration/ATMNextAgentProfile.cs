using System;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace AwesomeTaskManager.Next
{
    /// <summary>
    /// Configuration guidelines and instructions profile for AI agents (Junie Pro, Claude Code, Cursor, Windsurf, Copilot, Rider AI).
    /// </summary>
    public static class ATMNextAgentProfile
    {
        public const string AgentGuideline = @"# Awesome Task Manager Next – AI Agent Integration Guidelines

> For routine ATM task-management requests, use the registered `atm_*` tools or the terminal-free file command bridge first; optional terminal helpers (`atm.sh` / `atm.ps1`) are available when preferred. Do not open ATM source or data files to learn tool details or inspect task data.

This project uses **Awesome Task Manager Next (ATM Next)** for task management, Kanban boards, checklists, subtasks, and Quick Notes.
All ATM data lives in `ProjectSettings/AwesomeTaskManager/` and is owned by the running Unity Editor.

## ⚠️ CRITICAL DIRECTIVE FOR AI AGENTS (READ BEFORE ANY TASK OR NOTE WORK)
This applies to **any** request involving tasks, cards, to-dos, bugs to track, backlog, sprints, Kanban boards, columns, checklists, subtasks, epics, categories, assignees, Quick Notes, devlogs, or ""the board"", even if the user never says ""ATM"".

1. **NEVER manually edit or modify JSON files directly.** Never inspect or edit ATM Next source files or raw ATM data files for routine task-management requests. Do not search/read plugin source, generated code, or data JSON to learn tool names, parameters, boards, cards, or notes; use this instruction section and ATM tools instead. This does not prohibit inspecting relevant source when the user explicitly asks you to implement or debug ATM Next itself. Never directly create, edit, overwrite, rename, or delete ATM data files except writing `external_command.json` for the file bridge; read `external_result.json` only to retrieve its response.
   - ATM data is owned by the Unity Editor under `ProjectSettings/AwesomeTaskManager/`, including `AwesomeTaskManager.json`, its backups/theme, and everything under `Boards/`, `Notes/`, and `TheUltimateTaskBoard/`.
   - Use only the registered ATM tools or the terminal-free file command bridge. The `atm.sh`/`atm.ps1` scripts are optional wrappers. Never fall back to direct data-file edits if a tool call fails.
2. **Choose the available channel in this order:**
   1. **Native ATM MCP tools**: use `atm_*` tools directly when present. Prefer bulk tools for multiple operations.
   2. **File command bridge (no terminal required)**: write one command to `ProjectSettings/AwesomeTaskManager/external_command.json`, then read `external_result.json` and confirm its `requestId` matches. Use the protocol below; do not clear old results or edit any other ATM data files.
   3. **Optional terminal helpers**: use `atm.sh` on macOS/Linux or `atm.ps1` on Windows as a one-step wrapper around the same file bridge. Examples are below.
   4. **C# API**: only from Unity Editor code/tests, via `AwesomeTaskManager.Next.ATMNextService`.
3. If the editor bridge times out or reports an error, fix the call using the returned hint or `atm_help`; if Unity is closed/compiling or no channel works, stop and tell the user. **Never switch to direct JSON edits.**
4. For requests to manage tasks or notes, consult existing task data through `atm_search`, `atm_get_boards`, `atm_get_notes`, or the relevant ATM tool—not by opening raw data files.
5. Prefer bulk tools for multiple operations (`atm_bulk_create_tasks`, `atm_bulk_update_tasks`, `atm_bulk_move_tasks`, `atm_bulk_clone_tasks`, `atm_bulk_delete_tasks`, `atm_bulk_create_notes`, `atm_bulk_update_notes`, `atm_bulk_move_notes`, `atm_bulk_delete_notes`, `atm_bulk_link_items`) to reduce calls and saves.

## 🌉 File Command Bridge & CLI Helper Protocol (when `atm_*` MCP tools are not available)
The file command bridge works on any operating system and needs no terminal or PowerShell. When you can read and write project files, use it directly; the `atm.sh` and `atm.ps1` helpers below are optional wrappers.

### Recommended: command file (no terminal needed)
Unity checks `ProjectSettings/AwesomeTaskManager/external_command.json` while the Editor is open and not compiling/importing. Write one command with a unique `requestId`; Unity consumes it and writes the result to `ProjectSettings/AwesomeTaskManager/external_result.json`.
1. Generate a unique `requestId` for this request. Do not delete or clear an existing `external_result.json`.
2. Write exactly one command to `ProjectSettings/AwesomeTaskManager/external_command.json`:
   ```json
   {
     ""requestId"": ""<unique-id>"",
     ""tool"": ""atm_search"",
     ""parameters"": { ""Query"": ""physics"" }
   }
   ```
3. Read `external_result.json`. Accept it only when its `requestId` matches the one you sent; if it does not match yet, re-read it rather than using a stale result. Unity removes the command when consumed and replaces the result. Array-valued tool results are placed under the result's `result` field; object-valued results keep their fields at the top level.
4. If there is no matching result after about 30 seconds, stop and tell the user Unity may be closed, compiling, or busy. Do not delete the queued command or fall back to editing ATM data files.
5. Send only one command at a time; use bulk tools for multiple operations. Check `success`, `message`, and any `hint`/`validParameters`; use `atm_help` to correct a failed request.
6. Bridge-only `atm_refresh` runs `AssetDatabase.Refresh()`.

### Optional: macOS/Linux shell helper

Run from the project root with the system POSIX shell:
```sh
sh ./atm.sh atm_get_boards
sh ./atm.sh atm_search '{""Query"":""physics""}'
sh ./atm.sh atm_create_note --parameters-file ./atm-params.json
sh ./atm.sh atm_help
sh ./atm.sh atm_help '{""Tool"":""atm_search""}'
sh ./atm.sh atm_sync_agent_files
```
For parameters from a pipe, use `printf '%s' '<json object>' | sh ./atm.sh <tool_name> -`. The script also accepts a full command JSON object as its first argument, a timeout (`--timeout <seconds>`), and the PowerShell-compatible aliases `-ParametersFile` and `-TimeoutSeconds`. Parameter-file paths are relative to the calling directory; the script locates the project relative to itself.

### Optional: Windows PowerShell helper
Run from the project root using PowerShell's call operator:
```powershell
& .\atm.ps1 atm_get_boards
& .\atm.ps1 atm_search '{""Query"":""physics""}'
& .\atm.ps1 atm_create_note -ParametersFile .\atm-params.json
& .\atm.ps1 atm_help
& .\atm.ps1 atm_help '{""Tool"":""atm_search""}'
& .\atm.ps1 atm_sync_agent_files
```
For `-ParametersFile`, point to a JSON file containing only the parameter object. Do not use nested JSON quoting with `powershell -File`; Windows PowerShell may strip embedded quotes. If a call fails, use the JSON error hint or `atm_help`, correct the call, and do not switch to raw data files.
Use `atm_sync_agent_files` only if the generated instruction or guard files appear stale; Unity also synchronizes them when the editor loads.

### Parameter Rules (bridge and MCP)
- `""tool""` is the exact tool name (e.g. `atm_move_task`). `""parameters""` is a JSON object whose keys are the tool's parameter names from the reference below.
- Parameter names are case-insensitive (`Title` = `title`). Unknown keys are reported with the valid parameter names and a correction hint; fix the call rather than trying to inspect implementation files.
- **Cards** are referenced by `CardIdOrTitle`: the card's `id`, its exact title (case-insensitive), or its task code (e.g. `ATM-12`). **Notes** use `NoteIdOrTitle`. **Boards and columns** accept a name or an id.
- If `Board` is omitted, ATM uses the active board. Default columns are `📋 To Do`, `🔨 In Progress`, and `✅ Done`. Call `atm_get_boards` to get the real board and column titles before moving cards.
- `Priority` is `0`-`4`, `ColorLabel` is `0`-`16`, and `DueDate` is a string.
- Tools marked `*` are destructive and do nothing unless `""Confirm"": true` is passed. Ask the user before sending it.

### Command Bridge Examples
- **Get Boards**: `{ ""tool"": ""atm_get_boards"", ""parameters"": {} }`
- **Search**: `{ ""tool"": ""atm_search"", ""parameters"": { ""Query"": ""physics"" } }`
- **Create Task**: `{ ""tool"": ""atm_create_task"", ""parameters"": { ""Title"": ""New Task"", ""Column"": ""📋 To Do"", ""Description"": ""Task details"", ""Category"": ""Gameplay"", ""Priority"": 2, ""Checklist"": [""Step 1"", ""Step 2""] } }`
- **Update Task**: `{ ""tool"": ""atm_update_task"", ""parameters"": { ""CardIdOrTitle"": ""New Task"", ""NewDescription"": ""Updated details"", ""Completed"": true } }`
- **Move Task**: `{ ""tool"": ""atm_move_task"", ""parameters"": { ""CardIdOrTitle"": ""ATM-1"", ""TargetColumn"": ""✅ Done"" } }`
- **Check a Checklist Item**: `{ ""tool"": ""atm_manage_checklist"", ""parameters"": { ""CardIdOrTitle"": ""ATM-1"", ""Action"": ""toggle"", ""ItemIndex"": 0, ""IsChecked"": true } }`
- **Bulk Create Tasks**: `{ ""tool"": ""atm_bulk_create_tasks"", ""parameters"": { ""DefaultColumn"": ""📋 To Do"", ""Tasks"": [ { ""Title"": ""Task A"" }, { ""Title"": ""Task B"", ""Category"": ""UI"" } ] } }`
- **Bulk Move Tasks**: `{ ""tool"": ""atm_bulk_move_tasks"", ""parameters"": { ""CardIdsOrTitles"": [""ATM-1"", ""ATM-2""], ""DefaultTargetColumn"": ""🔨 In Progress"" } }`
- **Create Epic + Subtasks**: `{ ""tool"": ""atm_create_epic_hierarchy"", ""parameters"": { ""Title"": ""Scoring Epic"", ""Subtasks"": [ { ""Title"": ""Strike logic"" }, { ""Title"": ""Spare logic"" } ] } }`
- **Create Note**: `{ ""tool"": ""atm_create_note"", ""parameters"": { ""Title"": ""Architecture Note"", ""Content"": ""Markdown content..."", ""FolderName"": ""AI Notes"" } }`
- **Link Note to Card**: `{ ""tool"": ""atm_link_item"", ""parameters"": { ""CardIdOrTitle"": ""ATM-1"", ""LinkType"": ""note"", ""Target"": ""Architecture Note"" } }`
- **Delete Task**: `{ ""tool"": ""atm_delete_task"", ""parameters"": { ""CardIdOrTitle"": ""ATM-1"", ""Confirm"": true } }`

## Tool & Parameter Reference
Required parameters are **bold**. `[]` = JSON array. `{...}[]` = array of objects with the listed keys.

**Boards & Columns**
- `atm_get_boards`: no parameters. Returns boards, columns, and cards with ids, task codes, and checklists.
- `atm_create_board`: **`BoardName`**
- `atm_delete_board`*: **`BoardNameOrId`**, `Confirm`
- `atm_set_active_board`: **`BoardNameOrId`**
- `atm_create_column`: `BoardNameOrId`, **`ColumnTitle`**
- `atm_move_column`: **`ColumnNameOrId`**, `TargetBoard`, `SourceBoard`, `TargetIndex`
- `atm_clone_column`: **`ColumnNameOrId`**, `TargetBoard`, `SourceBoard`, `NewTitle`, `IncludeCards` (default true), `TargetIndex`
- `atm_reorder_column`: **`ColumnNameOrId`**, `Direction` (`left`/`right`/`first`/`last`), `TargetIndex`, `Board`
- `atm_delete_column`*: **`ColumnNameOrId`**, `BoardNameOrId`, `Confirm`
- `atm_delete_all_tasks_in_column`*: `BoardNameOrId`, **`ColumnNameOrId`**, `Confirm`
- `atm_move_all_tasks_between_boards`*: **`SourceBoardNameOrId`**, **`TargetBoardNameOrId`**, `Confirm`
- `atm_set_filter`: `Category`, `Assignee`, `Priority`, `PriorityName`, `SearchQuery`, `ResetAll`
- `atm_get_filter`: no parameters

**Tasks / Cards**
- `atm_create_task`: **`Title`**, `Board`, `Column` (default `📋 To Do`), `Description`, `Category`, `Priority`, `DueDate`, `ColorLabel`, `Checklist[]`, `Assignees[]`
- `atm_update_task`: **`CardIdOrTitle`**, `NewTitle`, `NewDescription`, `NewCategory`, `NewPriority`, `NewDueDate`, `NewColorLabel`, `Completed`, `Archived`, `Assignees[]`
- `atm_move_task`: **`CardIdOrTitle`**, **`TargetColumn`**, `TargetBoard`
- `atm_clone_task`: **`CardIdOrTitle`**, `TargetColumn`, `TargetBoard`, `SourceBoard`, `NewTitle`, `TargetIndex`
- `atm_reorder_task`: **`CardIdOrTitle`**, `Direction` (`up`/`down`/`top`/`bottom`), `TargetIndex`, `Board`
- `atm_delete_task`*: **`CardIdOrTitle`**, `Confirm`
- `atm_manage_checklist`: **`CardIdOrTitle`**, `Action` (`add` default, `toggle`, `remove`), `ItemText` (add), `ItemIndex` (toggle/remove, 0-based), `IsChecked` (toggle sets this exact state), `Confirm` (required for remove)
- `atm_search`: **`Query`**, `Board`, `Column`, `Category`, `Assignee`, `Scope` (default `all`), `IncludeCompleted` (default true), `IncludeArchived` (default false), `Limit`
- `atm_bulk_create_tasks`: `Board`, `DefaultColumn`, **`Tasks`** `{Title, Description, Column, Category, Priority, Checklist[], Assignees[], ColorLabel}[]`
- `atm_bulk_update_tasks`: `Updates` `{CardIdOrTitle, NewTitle, NewDescription, NewCategory, NewPriority, NewDueDate, NewColorLabel, Completed, Archived, Assignees[]}[]`, **or** `CardIdsOrTitles[]` with `DefaultCategory`, `DefaultPriority`, `DefaultColorLabel`, `DefaultAssignees[]`
- `atm_bulk_move_tasks`: `Moves` `{CardIdOrTitle, TargetColumn, TargetBoard, SourceBoard}[]`, **or** `CardIdsOrTitles[]` with `DefaultTargetColumn`, `DefaultTargetBoard`, `DefaultSourceBoard`
- `atm_bulk_clone_tasks`: `Clones` `{CardIdOrTitle, TargetColumn, TargetBoard, SourceBoard, NewTitle}[]`, **or** `CardIdsOrTitles[]` with `DefaultTargetColumn`, `DefaultTargetBoard`, `DefaultSourceBoard`
- `atm_bulk_delete_tasks`*: **`CardIdsOrTitles[]`**, `Confirm`

**Subtasks & Epics**
- `atm_create_subtask`: **`ParentCardIdOrTitle`**, **`Title`**, `Description`, `Category`, `Priority`, `DueDate`, `Column`, `ColorLabel`, `Checklist[]`, `Assignees[]`
- `atm_link_subtask`: **`ParentCardIdOrTitle`**, **`ChildCardIdOrTitle`**, `ChecklistItemTitle`
- `atm_unlink_subtask`: **`ParentCardIdOrTitle`**, **`ChildCardIdOrTitle`**, `RemoveChecklistItem`
- `atm_get_subtasks`: **`CardIdOrTitle`**
- `atm_convert_checklist_to_subtasks`: **`CardIdOrTitle`**, `Column`, `Category`
- `atm_create_epic_hierarchy`: **`Title`**, `Board`, `Column`, `Description`, `Category`, `Priority`, `DueDate`, `ColorLabel`, `Subtasks` `{Title, Description, Category, Priority, DueDate, Column, Checklist[]}[]`

**Categories & Assignees**
- `atm_create_category`: **`Name`**, `ColorIndex`
- `atm_get_categories`: no parameters
- `atm_delete_category`*: **`Category`**, `Confirm`
- `atm_create_assignee`: **`Name`**, `ColorIndex`, `BorderColorIndex`, `ProfileImageGuid`
- `atm_get_assignees`: no parameters
- `atm_delete_assignee`*: **`AssigneeIdOrName`**, `Confirm`

**Quick Notes, Links & Devlogs**
- `atm_get_notes`: no parameters
- `atm_create_note`: **`Title`**, `Content`, `FolderName` (default `AI Notes`), `Tags[]`, `Pinned`, `ColorIndex`
- `atm_update_note`: **`NoteIdOrTitle`**, `NewTitle`, `NewContent`, `NewFolderName`, `NewTags[]`, `Pinned`, `ColorIndex`
- `atm_delete_note`*: **`NoteIdOrTitle`**, `Confirm`
- `atm_bulk_create_notes`: `DefaultFolderName` (default `AI Notes`), `DefaultTags[]`, **`Notes`** `{Title, Content, FolderName, Tags[], Pinned, ColorIndex}[]`
- `atm_bulk_update_notes`: `Updates` `{NoteIdOrTitle, NewTitle, NewContent, AppendContent, NewFolderName, NewTags[], AddTags[], RemoveTags[], Pinned, ColorIndex}[]`, **or** `NoteIdsOrTitles[]` with `DefaultFolderName`, `DefaultTags[]`, `AddTags[]`, `RemoveTags[]`, `DefaultPinned`, `DefaultColorIndex`, `AppendContent`
- `atm_bulk_move_notes`: `Moves` `{NoteIdOrTitle, TargetFolderName}[]`, **or** `NoteIdsOrTitles[]` with `TargetFolderName`
- `atm_bulk_delete_notes`*: **`NoteIdsOrTitles[]`**, `Confirm`
- `atm_link_item`: **`CardIdOrTitle`**, **`Target`** (note id/title, URL, or asset path/GUID), `LinkType` (`note` default, `url`, `asset`), `DisplayName`
- `atm_bulk_link_items`: `Links` `{CardIdOrTitle, Target, LinkType, DisplayName}[]`, **or** `CardIdOrTitle` / `CardIdsOrTitles[]` with `Target` / `Targets[]`, `LinkType`, `DisplayName`
- `atm_generate_devlog`: `BoardName`, `Title` (default `Sprint Devlog`), `FolderName` (default `Devlogs`), `CreateQuickNote` (default true), `Tags[]`

**Analytics**
- `atm_get_project_summary`: no parameters

## Agent Responsibilities
1. **Hierarchical Task Structuring**: Avoid creating massive flat task lists. Structure complex features into a parent Epic task with linked child subtask cards (using `atm_create_subtask`, `atm_link_subtask`, or `atm_create_epic_hierarchy`).
2. **Task Consultation & Search**: When starting work on a feature or bug fix, check ATM tasks and subtasks to understand requirements, or use `atm_search` to quickly find relevant cards, notes, checklists, categories, assignees, boards, columns, or ATM tool capabilities.
3. **Checklist & Subtask Synchronization**: When implementing components, check off completed checklist items (`atm_manage_checklist`) or mark child cards as completed (`atm_update_task` with `Completed: true`).
4. **Status Updates & Card Movement**: Move cards to `🔨 In Progress` when starting (or use `atm_bulk_move_tasks` / `atm_bulk_update_tasks` for batch updates), and `✅ Done` when verified and tested. If no ATM channel is available, mention the needed board update to the user instead of editing files.
5. **Card & Column Ordering & Navigation**: Use `atm_reorder_task` to position cards up/down in a column, `atm_clone_task` or `atm_bulk_clone_tasks` to duplicate cards across columns/boards, `atm_move_column` or `atm_clone_column` to move/clone columns across boards, `atm_reorder_column` to reorder columns, `atm_set_active_board` to switch boards, and `atm_set_filter` to apply category/assignee/priority/search filters.
6. **Categories & Team Members**: Use `atm_create_category` and `atm_create_assignee` when new tags or assignees are needed.
7. **Documentation & Bulk Note Operations**: Write architecture notes, devlogs, or bug investigation summaries to Quick Notes under folder `AI Notes`. Use `atm_bulk_create_notes`, `atm_bulk_update_notes`, `atm_bulk_move_notes`, and `atm_bulk_link_items` to manage documentation in bulk.
8. **Programmatic API**: Use `AwesomeTaskManager.Next.ATMNextService` only from C# Editor code (scripts/tests), never as a reason to touch the JSON files.

## Efficiency & Testing Guardrails
1. **Selective Testing**: Do not run or create tests unless explicitly required by the user or if the changes are high-risk. Avoid broad test suites if a targeted check suffices.
2. **Credit Efficiency**: Minimize tool calls and file reads. Group related operations (e.g., using `atm_bulk_create_tasks`, `atm_bulk_update_tasks`, `atm_bulk_move_tasks`, `atm_bulk_update_notes`, and `atm_bulk_link_items` instead of multiple single calls) to conserve agent tokens and credits.
3. **Minimalism**: Apply the smallest possible change to achieve the goal. Avoid unnecessary refactors or stylistic sweeps unless asked.

## Unity CLI & MCP Integration (Optional)
MCP-capable clients (**Claude Code**, **Cursor**, **Windsurf**, **VS Code/Copilot**, **Unity AI Assistant**) can connect to the Unity Editor through the free **Unity CLI**. No Unity AI subscription is required. Docs: https://docs.unity.com/en-us/unity-cli/replace-mcp-server-unity-cli

1. **Prerequisites**: Unity 6 (6000.0) LTS or later, with the Editor open.
2. **Install the Unity CLI**: recent Unity Hub versions install it automatically. Otherwise run `winget install Unity.CLI` (Windows) or `brew install --cask unity-cli` (macOS).
3. **Set up the project**:
   - Add the Unity Pipeline package: `unity pipeline install` (run in the project folder), then check with `unity status`.
   - Configure your client: `unity mcp configure <client>` (e.g. `claude`, `cursor`, `vscode`, `windsurf`; `unity mcp configure --list` shows all).
   - (Optional) Install skills: `unity skill install <agent-name>`
4. **Direct Commands**: agents that can run shell commands can drive the Editor with `unity command` (lists available commands). Arbitrary C# runs via `unity command script eval --code ""...""` (Pipeline 0.9+).
5. **Verify**: after connecting, check that `atm_*` tools appear in your tool list. If they don't, use the File Command Bridge above. Never edit the JSON files.

*Destructive operations require the `Confirm: true` parameter to execute.
";

        /// <summary>
        /// Heading that marks the start of the ATM Next section inside an instruction file.
        /// </summary>
        public const string GuidelineMarker = "# Awesome Task Manager Next – AI Agent Integration Guidelines";

        private const string GuardBlockStart = "# BEGIN ATM NEXT MANAGED RULES";
        private const string GuardBlockEnd = "# END ATM NEXT MANAGED RULES";
        private const string IgnoreRules = GuardBlockStart + @"
ProjectSettings/AwesomeTaskManager/AwesomeTaskManager.json*
ProjectSettings/AwesomeTaskManager/AwesomeTaskManagerTheme.json*
ProjectSettings/AwesomeTaskManager/Boards/**
ProjectSettings/AwesomeTaskManager/Notes/**
ProjectSettings/AwesomeTaskManager/TheUltimateTaskBoard/**
" + GuardBlockEnd;

        private const string CliScriptRelativePath = "Assets/Plugins/AwesomeTaskManagerNext/Editor/Integration/atm.ps1";
        private const string CliScriptRootPath = "atm.ps1";
        private const string ShellScriptRelativePath = "Assets/Plugins/AwesomeTaskManagerNext/Editor/Integration/atm.sh";
        private const string ShellScriptRootPath = "atm.sh";
        private static bool _shellScriptExecutablePermissionAttempted;

        private static readonly string[] ClaudeDeniedPaths =
        {
            "./ProjectSettings/AwesomeTaskManager/AwesomeTaskManager.json",
            "./ProjectSettings/AwesomeTaskManager/AwesomeTaskManager.json.bak",
            "./ProjectSettings/AwesomeTaskManager/AwesomeTaskManagerTheme.json",
            "./ProjectSettings/AwesomeTaskManager/AwesomeTaskManagerTheme.json.bak",
            "./ProjectSettings/AwesomeTaskManager/Boards/**",
            "./ProjectSettings/AwesomeTaskManager/Notes/**",
            "./ProjectSettings/AwesomeTaskManager/TheUltimateTaskBoard/**"
        };

        [InitializeOnLoadMethod]
        private static void AutoSyncAgentFiles()
        {
            if (Application.isBatchMode) return;

            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            if (string.IsNullOrEmpty(projectRoot)) return;

            foreach (string relativeFilePath in AgentFilePaths)
            {
                string fullPath = Path.Combine(projectRoot, relativeFilePath);
                if (!File.Exists(fullPath)) continue;

                try
                {
                    if (File.ReadAllText(fullPath).Contains(GuidelineMarker))
                        UpdateAgentFile(relativeFilePath);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[AwesomeTaskManagerNext] Could not inspect {relativeFilePath} for agent guideline synchronization: {e.Message}");
                }
            }

            EnsureGuardFilesUpdated();
        }

        /// <summary>
        /// Project-relative paths of every AI instruction file managed by ATM Next.
        /// </summary>
        public static readonly string[] AgentFilePaths =
        {
            "AGENTS.md",
            "CLAUDE.md",
            ".cursorrules",
            ".windsurfrules",
            Path.Combine(".github", "copilot-instructions.md")
        };

        public enum AgentFileUpdateResult
        {
            UpToDate,
            Updated,
            Failed
        }

        /// <summary>
        /// Appends or updates the ATM Next guidelines in AGENTS.md in the project root.
        /// </summary>
        public static bool EnsureAgentsMdUpdated()
        {
            return UpdateAgentFile("AGENTS.md") == AgentFileUpdateResult.Updated;
        }

        /// <summary>
        /// Creates or updates CLAUDE.md in the project root with the AI integration guidelines.
        /// </summary>
        public static bool EnsureClaudeMdUpdated()
        {
            return UpdateAgentFile("CLAUDE.md") == AgentFileUpdateResult.Updated;
        }

        /// <summary>
        /// Creates or updates .cursorrules in the project root with the AI integration guidelines.
        /// </summary>
        public static bool EnsureCursorRulesUpdated()
        {
            return UpdateAgentFile(".cursorrules") == AgentFileUpdateResult.Updated;
        }

        /// <summary>
        /// Creates or updates .windsurfrules in the project root with the AI integration guidelines.
        /// </summary>
        public static bool EnsureWindsurfRulesUpdated()
        {
            return UpdateAgentFile(".windsurfrules") == AgentFileUpdateResult.Updated;
        }

        /// <summary>
        /// Creates or updates .github/copilot-instructions.md with the AI integration guidelines.
        /// </summary>
        public static bool EnsureCopilotInstructionsUpdated()
        {
            return UpdateAgentFile(Path.Combine(".github", "copilot-instructions.md")) == AgentFileUpdateResult.Updated;
        }

        /// <summary>
        /// Updates all standard AI instruction files (AGENTS.md, CLAUDE.md, .cursorrules, .windsurfrules, Copilot) at once.
        /// </summary>
        public static int EnsureAllAgentFilesUpdated()
        {
            return EnsureAllAgentFilesUpdated(out _);
        }

        /// <summary>
        /// Updates all standard AI instruction files and reports how many could not be written.
        /// </summary>
        public static int EnsureAllAgentFilesUpdated(out int failedCount)
        {
            int count = 0;
            failedCount = 0;
            foreach (string relativeFilePath in AgentFilePaths)
            {
                AgentFileUpdateResult result = UpdateAgentFile(relativeFilePath);
                if (result == AgentFileUpdateResult.Updated) count++;
                else if (result == AgentFileUpdateResult.Failed) failedCount++;
            }
            EnsureGuardFilesUpdated();
            return count;
        }

        /// <summary>
        /// Installs the bridge helper and managed AI rules that protect ATM data files.
        /// </summary>
        public static void EnsureGuardFilesUpdated()
        {
            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            if (string.IsNullOrEmpty(projectRoot)) return;

            foreach (string fileName in new[] { ".aiignore", ".cursorignore", ".codeiumignore" })
                UpdateManagedTextFile(projectRoot, fileName, IgnoreRules);

            UpdateClaudePermissions(projectRoot);
            InstallCliScript(projectRoot);
        }

        private static void UpdateManagedTextFile(string projectRoot, string relativePath, string managedBlock)
        {
            try
            {
                string path = Path.Combine(projectRoot, relativePath);
                string original = File.Exists(path) ? File.ReadAllText(path) : "";
                string newline = original.Contains("\r\n") ? "\r\n" : "\n";
                string content = NormalizeLineEndings(original);
                string block = NormalizeLineEndings(managedBlock);
                int start = content.IndexOf(GuardBlockStart, StringComparison.Ordinal);
                if (start >= 0)
                {
                    int end = content.IndexOf(GuardBlockEnd, start + GuardBlockStart.Length, StringComparison.Ordinal);
                    if (end < 0)
                    {
                        Debug.LogWarning($"[AwesomeTaskManagerNext] Skipping {relativePath}: its ATM Next managed block has no end marker.");
                        return;
                    }
                    end += GuardBlockEnd.Length;
                    string before = content.Substring(0, start).TrimEnd();
                    string after = content.Substring(end).TrimStart('\n');
                    content = (before.Length > 0 ? before + "\n\n" : "") + block + (after.Length > 0 ? "\n\n" + after : "");
                }
                else
                {
                    content = string.IsNullOrWhiteSpace(content) ? block : content.TrimEnd() + "\n\n" + block;
                }

                string output = (content.TrimEnd() + "\n").Replace("\n", newline);
                if (output != original)
                    File.WriteAllText(path, output, new UTF8Encoding(false));
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[AwesomeTaskManagerNext] Failed to update {relativePath}: {e.Message}");
            }
        }

        private static void UpdateClaudePermissions(string projectRoot)
        {
            const string relativePath = ".claude/settings.json";
            try
            {
                string path = Path.Combine(projectRoot, relativePath);
                JObject settings = File.Exists(path) ? JObject.Parse(File.ReadAllText(path)) : new JObject();
                JToken permissionsToken = settings["permissions"];
                JObject permissions;
                if (permissionsToken == null)
                {
                    permissions = new JObject();
                    settings["permissions"] = permissions;
                }
                else if (permissionsToken is JObject existingPermissions)
                {
                    permissions = existingPermissions;
                }
                else
                {
                    Debug.LogWarning($"[AwesomeTaskManagerNext] Skipping {relativePath}: 'permissions' must be a JSON object.");
                    return;
                }

                JToken denyToken = permissions["deny"];
                JArray deny;
                if (denyToken == null)
                {
                    deny = new JArray();
                    permissions["deny"] = deny;
                }
                else if (denyToken is JArray existingDeny)
                {
                    deny = existingDeny;
                }
                else
                {
                    Debug.LogWarning($"[AwesomeTaskManagerNext] Skipping {relativePath}: 'permissions.deny' must be a JSON array.");
                    return;
                }

                bool changed = false;
                foreach (string deniedPath in ClaudeDeniedPaths)
                {
                    foreach (string operation in new[] { "Read", "Edit" })
                    {
                        string rule = $"{operation}({deniedPath})";
                        if (deny.Any(token => string.Equals(token.ToString(), rule, StringComparison.OrdinalIgnoreCase))) continue;
                        deny.Add(rule);
                        changed = true;
                    }
                }

                if (changed || !File.Exists(path))
                {
                    string directory = Path.GetDirectoryName(path);
                    if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);
                    File.WriteAllText(path, settings.ToString(Newtonsoft.Json.Formatting.Indented) + "\n", new UTF8Encoding(false));
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[AwesomeTaskManagerNext] Failed to update {relativePath}: {e.Message}");
            }
        }

        private static void InstallCliScript(string projectRoot)
        {
            InstallCliScript(projectRoot, CliScriptRelativePath, CliScriptRootPath, false);
            InstallCliScript(projectRoot, ShellScriptRelativePath, ShellScriptRootPath, true);
        }

        private static void InstallCliScript(string projectRoot, string relativeSourcePath, string relativeDestinationPath, bool normalizeLineEndings)
        {
            try
            {
                string sourcePath = Path.Combine(projectRoot, relativeSourcePath);
                string destinationPath = Path.Combine(projectRoot, relativeDestinationPath);
                if (!File.Exists(sourcePath))
                {
                    Debug.LogWarning($"[AwesomeTaskManagerNext] Canonical ATM bridge script was not found at {relativeSourcePath}.");
                    return;
                }

                string source = File.ReadAllText(sourcePath);
                if (normalizeLineEndings) source = NormalizeLineEndings(source);
                string destination = File.Exists(destinationPath) ? File.ReadAllText(destinationPath) : null;
                if (normalizeLineEndings) destination = NormalizeLineEndings(destination);
                if (!string.Equals(destination, source, StringComparison.Ordinal))
                    File.WriteAllText(destinationPath, source, new UTF8Encoding(false));

                if (relativeDestinationPath == ShellScriptRootPath)
                    EnsureShellScriptExecutable(destinationPath);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[AwesomeTaskManagerNext] Failed to install {relativeDestinationPath}: {e.Message}");
            }
        }

        private static void EnsureShellScriptExecutable(string scriptPath)
        {
            if (_shellScriptExecutablePermissionAttempted || Application.platform == RuntimePlatform.WindowsEditor) return;
            _shellScriptExecutablePermissionAttempted = true;

            try
            {
                using (var process = new System.Diagnostics.Process())
                {
                    process.StartInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "/bin/chmod",
                        Arguments = "+x \"" + scriptPath.Replace("\"", "\\\"") + "\"",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };
                    process.Start();
                    process.WaitForExit(2000);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[AwesomeTaskManagerNext] Could not mark {ShellScriptRootPath} executable: {e.Message}");
            }
        }

        /// <summary>
        /// Creates or updates a single AI instruction file (path relative to the project root).
        /// </summary>
        public static AgentFileUpdateResult UpdateAgentFile(string relativeFilePath)
        {
            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            if (string.IsNullOrEmpty(projectRoot)) return AgentFileUpdateResult.Failed;
            return UpdateOrCreateFile(projectRoot, relativeFilePath, AgentGuideline, GuidelineMarker);
        }

        internal static AgentFileUpdateResult UpdateOrCreateFile(string projectRoot, string relativeFilePath, string guidelineContent, string markerStart)
        {
            try
            {
                string fullPath = Path.Combine(projectRoot, relativeFilePath);
                string directory = Path.GetDirectoryName(fullPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                string original = File.Exists(fullPath) ? File.ReadAllText(fullPath) : "";
                // Keep the file's existing line-ending style; compare with normalized line endings so
                // a CRLF/LF checkout difference between this script and the file never triggers a rewrite.
                string newLine = original.Contains("\r\n") ? "\r\n" : "\n";
                string content = NormalizeLineEndings(original);
                string guideline = NormalizeLineEndings(guidelineContent).Trim();
                string marker = NormalizeLineEndings(markerStart);

                int index = content.IndexOf(marker, StringComparison.Ordinal);
                if (index >= 0)
                {
                    if (content.Contains(guideline)) return AgentFileUpdateResult.UpToDate;

                    // Replace only the ATM section: it ends at the next top-level "# " heading (if any),
                    // so user content placed after the section is preserved.
                    string before = content.Substring(0, index).TrimEnd();
                    string after = "";
                    int nextHeading = content.IndexOf("\n# ", index + marker.Length, StringComparison.Ordinal);
                    if (nextHeading >= 0) after = content.Substring(nextHeading + 1).Trim();

                    content = (before.Length > 0 ? before + "\n\n" : "") + guideline + (after.Length > 0 ? "\n\n" + after : "");
                }
                else
                {
                    content = string.IsNullOrWhiteSpace(content) ? guideline : (content.TrimEnd() + "\n\n" + guideline);
                }

                string output = (content.Trim() + "\n").Replace("\n", newLine);
                if (output == original) return AgentFileUpdateResult.UpToDate;

                File.WriteAllText(fullPath, output, new UTF8Encoding(false));
                Debug.Log($"[AwesomeTaskManagerNext] Updated {relativeFilePath} with AI agent guidelines.");
                return AgentFileUpdateResult.Updated;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[AwesomeTaskManagerNext] Failed to update {relativeFilePath}: {e.Message}");
                return AgentFileUpdateResult.Failed;
            }
        }

        private static string NormalizeLineEndings(string text)
        {
            return (text ?? "").Replace("\r\n", "\n").Replace("\r", "\n");
        }
    }
}
