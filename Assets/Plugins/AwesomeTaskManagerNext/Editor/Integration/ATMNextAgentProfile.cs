using System.IO;
using UnityEngine;

namespace AwesomeTaskManager.Next
{
    /// <summary>
    /// Configuration guidelines and instructions profile for AI agents (Junie Pro, Claude Code, Cursor, Rider AI).
    /// </summary>
    public static class ATMNextAgentProfile
    {
        public const string AgentGuideline = @"# Awesome Task Manager Next – AI Agent Integration Guidelines

This project uses **Awesome Task Manager Next** for task management and project notes.
All board and note data is stored in `ProjectSettings/AwesomeTaskManager/AwesomeTaskManager.json`.

## ⚠️ CRITICAL DIRECTIVE FOR AI AGENTS:
When asked to perform any task management, card inspection, note creation, backlog ranking, or board operations:
1. **ALWAYS check if ATM Next AI tools are available** (via MCP tools `atm_*`, the C# API `AwesomeTaskManager.Next.ATMNextService`, or the external command bridge).
2. **NEVER manually edit or manipulate `AwesomeTaskManager.json` directly.** Direct JSON file edits bypass validation, risk reference corruption, cause data loss, and miss live editor UI updates.
3. **Use Bulk Tools**: Use bulk tools (like `atm_bulk_create_tasks`, `atm_bulk_update_tasks`, `atm_bulk_move_tasks`, `atm_bulk_clone_tasks`, and `atm_bulk_delete_tasks` for cards, or `atm_bulk_create_notes`, `atm_bulk_update_notes`, `atm_bulk_move_notes`, `atm_bulk_delete_notes`, and `atm_bulk_link_items` for notes and linking) to perform multi-item operations efficiently in a single step with single user confirmation.

## Agent Responsibilities:
1. **Hierarchical Task Structuring**: Avoid creating massive flat task lists. Structure complex features into a parent Epic task with linked child subtask cards (using `atm_create_subtask`, `atm_link_subtask`, or `atm_create_epic_hierarchy`).
2. **Task Consultation & Search**: When starting work on a feature or bug fix, check ATM tasks and subtasks to understand requirements, or use `atm_search` to quickly find relevant cards, notes, checklists, categories, assignees, boards, columns, or ATM tool capabilities.
3. **Checklist & Subtask Synchronization**: When implementing components, check off completed checklist items or mark child cards as completed.
4. **Status Updates & Card Movement**: Move cards to `🔨 In Progress` when starting (or use `atm_bulk_move_tasks` / `atm_bulk_update_tasks` for batch updates), and `✅ Done` when verified and tested.
5. **Card & Column Ordering & Navigation**: Use `atm_reorder_task` to position cards up/down in a column, `atm_clone_task` or `atm_bulk_clone_tasks` to duplicate cards across columns/boards, `atm_move_column` or `atm_clone_column` to move/clone columns across boards, `atm_reorder_column` to reorder columns, `atm_set_active_board` to switch boards, and `atm_set_filter` to apply category/assignee/priority/search filters.
6. **Categories & Team Members**: Use `atm_create_category` and `atm_create_assignee` when new tags or assignees are needed.
7. **Documentation & Bulk Note Operations**: Write architecture notes, devlogs, or bug investigation summaries to Quick Notes under folder `AI Notes`. Use `atm_bulk_create_notes`, `atm_bulk_update_notes`, `atm_bulk_move_notes`, and `atm_bulk_link_items` to manage documentation in bulk.
8. **Programmatic API & MCP**: Use `AwesomeTaskManager.Next.ATMNextService` for C# operations or call registered MCP tools.

## Available MCP Tools:
- **Boards/Columns**: `atm_get_boards`, `atm_create_board`, `atm_delete_board`*, `atm_create_column`, `atm_move_column`, `atm_clone_column`, `atm_reorder_column`, `atm_delete_column`*, `atm_set_active_board`, `atm_set_filter`, `atm_get_filter`, `atm_delete_all_tasks_in_column`*, `atm_move_all_tasks_between_boards`*
- **Tasks/Cards**: `atm_create_task`, `atm_bulk_create_tasks`, `atm_update_task`, `atm_bulk_update_tasks`, `atm_move_task`, `atm_bulk_move_tasks`, `atm_clone_task`, `atm_bulk_clone_tasks`, `atm_reorder_task`, `atm_delete_task`*, `atm_bulk_delete_tasks`*, `atm_manage_checklist`, `atm_search`
- **Categories & Assignees**: `atm_create_category`, `atm_get_categories`, `atm_delete_category`*, `atm_create_assignee`, `atm_get_assignees`, `atm_delete_assignee`*
- **Subtasks & Epics**: `atm_create_subtask`, `atm_link_subtask`, `atm_unlink_subtask`, `atm_convert_checklist_to_subtasks`, `atm_get_subtasks`, `atm_create_epic_hierarchy`
- **Notes & Devlogs**: `atm_get_notes`, `atm_create_note`, `atm_bulk_create_notes`, `atm_update_note`, `atm_bulk_update_notes`, `atm_bulk_move_notes`, `atm_delete_note`*, `atm_bulk_delete_notes`*, `atm_generate_devlog`, `atm_link_item`, `atm_bulk_link_items`
- **Analytics & Health**: `atm_get_project_summary`

## Efficiency & Testing Guardrails:
1. **Selective Testing**: Do not run or create tests unless explicitly required by the user or if the changes are high-risk. Avoid broad test suites if a targeted check suffices.
2. **Credit Efficiency**: Minimize tool calls and file reads. Group related operations (e.g., using `atm_bulk_create_tasks`, `atm_bulk_update_tasks`, `atm_bulk_move_tasks`, `atm_bulk_update_notes`, and `atm_bulk_link_items` instead of multiple single calls) to conserve agent tokens and credits.
3. **Minimalism**: Apply the smallest possible change to achieve the goal. Avoid unnecessary refactors or stylistic sweeps unless asked.

## Unity CLI & MCP Integration (Recommended):
If you are using **Unity AI Assistant**, **Claude Code**, **Cursor**, **Windsurf**, or another MCP-capable tool, you should connect via the **Unity CLI**. It is free and does not require an AI tools subscription.

1. **Prerequisites (Unity 6+)**:
   - Ensure you are using **Unity 6 (6000.0)** or later.
   - Install the **Unity Pipeline** package in your project.
2. **Install Unity CLI**:
   - Open a terminal (PowerShell on Windows) and run:
     `curl -fsSL https://unity.com/install.sh | bash`
3. **Setup & Configure**:
   - Install the pipeline: `unity pipeline install`
   - Configure your client: `unity mcp configure <client>` (replace `<client>` with `claude`, `cursor`, `vscode`, or `windsurf`).
   - (Optional) Install skills: `unity skill install <agent-name>`
4. **Direct Commands**: If your agent can run shell commands, it can drive the Editor directly using `unity command` or `unity eval`, which is faster and uses fewer tokens than MCP.
5. **Start Chatting**: Once configured, simply ask your AI agent to manage your tasks or notes, and it will use the available `atm_` MCP tools automatically.

---

## External AI Bridge (Non-MCP fallback):
If your environment cannot directly call MCP tools (e.g. Rider, VS Code), you can invoke them via the file bridge:
1. Write a JSON file to `ProjectSettings/AwesomeTaskManager/external_command.json`.
   Format: `{ ""tool"": ""tool_name"", ""parameters"": { ... } }`
2. Wait for `ProjectSettings/AwesomeTaskManager/external_result.json` to appear (and `external_command.json` to be deleted).
3. The bridge is polled by Unity while the Editor is open.

*Destructive operations require `confirm: true` parameter to execute.
";

        /// <summary>
        /// Appends or updates the ATM Next guidelines in AGENTS.md in the project root.
        /// </summary>
        public static bool EnsureAgentsMdUpdated()
        {
            try
            {
                string projectRoot = Path.GetDirectoryName(Application.dataPath);
                if (string.IsNullOrEmpty(projectRoot)) return false;

                string agentsPath = Path.Combine(projectRoot, "AGENTS.md");
                string content = File.Exists(agentsPath) ? File.ReadAllText(agentsPath) : "";

                string markerStart = "# Awesome Task Manager Next – AI Agent Integration Guidelines";
                
                if (content.Contains(markerStart))
                {
                    // Find where the old section starts and try to replace it or just leave it if we can't easily find the end.
                    // For simplicity, we'll just check if it's identical.
                    if (content.Contains(AgentGuideline)) return false;

                    // If it contains the header but not the exact current version, we'll try a simple replacement if it's at the end.
                    int index = content.IndexOf(markerStart);
                    content = content.Substring(0, index).TrimEnd() + "\n\n" + AgentGuideline;
                }
                else
                {
                    content = content.TrimEnd() + "\n\n" + AgentGuideline;
                }

                File.WriteAllText(agentsPath, content.Trim());
                Debug.Log("[AwesomeTaskManagerNext] Updated AGENTS.md with AI agent guidelines.");
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[AwesomeTaskManagerNext] Failed to update AGENTS.md: " + e.Message);
                return false;
            }
        }
    }
}
