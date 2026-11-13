using System;
using System.Collections.Generic;
using System.Linq;
using AwesomeTaskManager.Data;
using AwesomeTaskManager.Editor;
using AwesomeTaskManager.Next;
using AwesomeTaskManager.UI;
using UnityEditor;
using UnityEngine;

namespace AwesomeTaskManager.Next.UI
{
    /// <summary>
    /// In-Editor AI Kanban & Notes Assistant Window for Awesome Task Manager Next.
    /// Provides AI task generation, subtask breakdown, devlog notes creation, sprint health metrics, and agent configuration.
    /// </summary>
    public class ATMNextAssistantWindow : EditorWindow
    {
        private enum Tab
        {
            Welcome = 0,
            TaskGenerator = 1,
            SubtaskBreakdown = 2,
            DevlogWriter = 3,
            SprintHealth = 4,
            AgentSetup = 5
        }

        private Tab _currentTab = Tab.Welcome;
        private Vector2 _scrollPos;
        private Vector2 _tabBarScrollPos;
        private Vector2 _agentSetupScroll;
        private string _toolSearchQuery = "";
        private SaveData _data;

        private static readonly (string CategoryName, (string Name, string Description)[] Tools)[] _availableToolCatalog = new[]
        {
            ("📋 Boards & Columns", new[]
            {
                ("atm_get_boards", "Inspect all boards, columns, and cards"),
                ("atm_create_board", "Create new task boards"),
                ("atm_delete_board", "Delete boards (requires confirm)"),
                ("atm_create_column", "Create new columns in boards"),
                ("atm_move_column", "Move columns across boards or reorder"),
                ("atm_clone_column", "Clone/duplicate columns to boards"),
                ("atm_reorder_column", "Reorder columns left/right/first/last"),
                ("atm_delete_column", "Delete columns (requires confirm)"),
                ("atm_set_active_board", "Switch the active board in the editor"),
                ("atm_set_filter", "Filter tasks by category/assignee/priority/search"),
                ("atm_get_filter", "Inspect active filter settings on the board"),
                ("atm_delete_all_tasks_in_column", "Bulk clear columns (requires confirm)"),
                ("atm_move_all_tasks_between_boards", "Bulk move tasks (requires confirm)")
            }),
            ("🃏 Tasks & Cards", new[]
            {
                ("atm_create_task", "Create new task cards with checklists"),
                ("atm_bulk_create_tasks", "Import dozens of cards at once"),
                ("atm_update_task", "Update task properties or mark complete"),
                ("atm_bulk_update_tasks", "Bulk update multiple task properties/assignees"),
                ("atm_move_task", "Move cards across columns/boards"),
                ("atm_bulk_move_tasks", "Bulk move multiple cards at once"),
                ("atm_clone_task", "Clone/duplicate a card to column/board"),
                ("atm_bulk_clone_tasks", "Bulk duplicate cards across columns/boards"),
                ("atm_reorder_task", "Reorder cards up/down in column"),
                ("atm_delete_task", "Remove task cards (requires confirm)"),
                ("atm_bulk_delete_tasks", "Bulk delete task cards (requires confirm)"),
                ("atm_manage_checklist", "Add, toggle, or remove checklist items"),
                ("atm_search", "Search tasks, checklists, notes, categories, assignees, boards, columns, and ATM tools")
            }),
            ("🏷 Categories & Assignees", new[]
            {
                ("atm_create_category", "Create new category tags with colors"),
                ("atm_get_categories", "List all categories and color indices"),
                ("atm_delete_category", "Delete a category (requires confirm)"),
                ("atm_create_assignee", "Create team members / assignees"),
                ("atm_get_assignees", "List all assignees with IDs and styles"),
                ("atm_delete_assignee", "Delete an assignee (requires confirm)")
            }),
            ("🌿 Hierarchical Subtasks & Epics", new[]
            {
                ("atm_create_subtask", "Create child tasks linked to a parent"),
                ("atm_link_subtask", "Link existing card as child to parent"),
                ("atm_unlink_subtask", "Remove parent-child link between cards"),
                ("atm_convert_checklist_to_subtasks", "Convert checklist items to cards"),
                ("atm_get_subtasks", "Query parent/child relationships"),
                ("atm_create_epic_hierarchy", "Create parent + multiple children at once")
            }),
            ("📝 Notes & Devlogs", new[]
            {
                ("atm_get_notes", "List all Quick Notes and folders"),
                ("atm_create_note", "Write devlogs and architecture notes"),
                ("atm_bulk_create_notes", "Batch create notes across folders"),
                ("atm_update_note", "Edit existing Quick Notes"),
                ("atm_bulk_update_notes", "Bulk update content, folders, tags, colors"),
                ("atm_bulk_move_notes", "Bulk move notes to target folders"),
                ("atm_delete_note", "Delete notes (requires confirm)"),
                ("atm_bulk_delete_notes", "Bulk delete notes (requires confirm)"),
                ("atm_generate_devlog", "Auto-compile 'Done' tasks into a note"),
                ("atm_link_item", "Link notes, URLs, or assets to cards"),
                ("atm_bulk_link_items", "Bulk link multiple notes/URLs/assets to cards")
            }),
            ("📊 Sprint Analytics & Health", new[]
            {
                ("atm_get_project_summary", "Get sprint completion analytics")
            })
        };

        private void OnEnable()
        {
            wantsMouseMove = true;
            LoadData();
            if (!EditorPrefs.HasKey("ATMNext_FirstRun"))
            {
                _currentTab = Tab.Welcome;
                EditorPrefs.SetBool("ATMNext_FirstRun", true);
            }
        }

        public void LoadData()
        {
            _data = ATMNextService.GetData();
            if (_data != null && _data.themes != null && _data.themes.Count > 0)
            {
                int themeIdx = Mathf.Clamp(_data.currentThemeIndex, 0, _data.themes.Count - 1);
                TBStyles.ApplyTheme(_data.themes[themeIdx]);
                TBStyles.InvalidateCache();
            }
            Repaint();
        }

        // Task Generator fields
        private string _selectedBoardName = "";
        private string _selectedColumnName = "";
        private string _manualTaskTitle = "";
        private string _manualTaskDesc = "";
        private string _manualTaskCategory = "Code";
        private int _manualTaskPriority = 2; // Medium
        private string _manualChecklistRaw = "";

        // Subtask Breakdown fields
        private int _selectedCardIndex = 0;
        private string _newSubtaskText = "";
        private string _newSubtaskCardDesc = "";
        private string _newSubtaskCardCategory = "Code";
        private int _newSubtaskCardPriority = 2;

        // Devlog Writer fields
        private string _devlogTitle = "Sprint Update";
        private string _devlogContent = "";
        private string _devlogFolderName = "AI Devlogs";
        private string _devlogTags = "Devlog, Sprint";

        // AI Importer fields
        private string _aiResponseJson = "";
        private string _aiChecklistInput = "";

        private void ProcessAiJsonImport()
        {
            if (string.IsNullOrWhiteSpace(_aiResponseJson)) return;

            string json = _aiResponseJson.Trim();
            try
            {
                if (json.StartsWith("["))
                {
                    // It's a list of tasks
                    var wrapped = JsonUtility.FromJson<AiTaskListWrapper>("{\"items\":" + json + "}");
                    if (wrapped.items != null)
                    {
                        foreach (var t in wrapped.items)
                        {
                            ATMNextService.CreateCard(_selectedBoardName, t.column, t.title, t.description, t.category, t.priority, checklist: t.checklist);
                        }
                        ShowNotification(new GUIContent($"Imported {wrapped.items.Count} tasks!"));
                        _aiResponseJson = "";
                    }
                }
                else if (json.StartsWith("{"))
                {
                    // It's an Epic
                    var epic = JsonUtility.FromJson<AiEpicData>(json);
                    if (!string.IsNullOrEmpty(epic.epicTitle))
                    {
                        var subs = epic.subtasks?.Select(s => new ATMNextService.SubtaskDefinition
                        {
                            title = s.title,
                            description = s.description,
                            category = s.category,
                            priority = s.priority,
                            checklist = s.checklist
                        }).ToList() ?? new List<ATMNextService.SubtaskDefinition>();
                        ATMNextService.CreateHierarchicalTask(_selectedBoardName, _selectedColumnName, epic.epicTitle, epic.description, epic.category, epic.priority, subs);
                        ShowNotification(new GUIContent("Imported Epic and subtasks!"));
                        _aiResponseJson = "";
                    }
                }
                else
                {
                    ShowNotification(new GUIContent("Invalid JSON format. Expected { or ["));
                }
            }
            catch (Exception e)
            {
                Debug.LogError("[ATM Next] Failed to parse AI JSON: " + e.Message);
                ShowNotification(new GUIContent("Import failed. Check Console."));
            }
        }

        [Serializable]
        private struct AiTaskData
        {
            public string title;
            public string column;
            public string category;
            public int priority;
            public string description;
            public List<string> checklist;
        }

        [Serializable]
        private struct AiTaskListWrapper
        {
            public List<AiTaskData> items;
        }

        [Serializable]
        private struct AiEpicData
        {
            public string epicTitle;
            public string category;
            public int priority;
            public string description;
            public List<AiTaskData> subtasks;
        }

        [MenuItem("Tools/Awesome Task Manager Next (AI)", false, 20)]
        public static void ShowWindow()
        {
            var window = GetWindow<ATMNextAssistantWindow>("ATM Next (AI)");
            window.minSize = new Vector2(480, 520);
            window.Show();
        }

        private void OnGUI()
        {
            if (_data == null) LoadData();

            if (Event.current.type == EventType.Repaint)
            {
                TBStyles.DrawCanvasBackground(new Rect(0, 0, position.width, position.height), TBStyles.CardDetailBg, true);
            }

            DrawHeader();
            DrawTabBar();

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
            EditorGUILayout.Space(10);

            switch (_currentTab)
            {
                case Tab.Welcome:
                    DrawWelcomeTab();
                    break;
                case Tab.TaskGenerator:
                    DrawTaskGeneratorTab();
                    break;
                case Tab.SubtaskBreakdown:
                    DrawSubtaskBreakdownTab();
                    break;
                case Tab.DevlogWriter:
                    DrawDevlogWriterTab();
                    break;
                case Tab.SprintHealth:
                    DrawSprintHealthTab();
                    break;
                case Tab.AgentSetup:
                    DrawAgentSetupTab();
                    break;
            }

            EditorGUILayout.EndScrollView();
            DrawFooter();

            ThemedTooltip.Draw(this);
        }

        private void DrawHeader()
        {
            var headerRect = EditorGUILayout.BeginVertical(GUILayout.Height(56));
            if (Event.current.type == EventType.Repaint)
            {
                TBStyles.DrawGlassPanel(headerRect, TBStyles.TopBarBg, EditorGUIUtility.isProSkin ? new Color(1f, 1f, 1f, 0.08f) : new Color(1f, 1f, 1f, 0.35f), false);
                EditorGUI.DrawRect(new Rect(headerRect.x, headerRect.yMax - 1, headerRect.width, 1), EditorGUIUtility.isProSkin ? new Color(0f, 0f, 0f, 0.35f) : new Color(0f, 0f, 0f, 0.12f));
            }

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(10);
            
            // Robot Icon
            EditorGUILayout.BeginVertical(GUILayout.Width(32));
            GUILayout.Space(13);
            GUILayout.Label("🤖", GUILayout.Width(30), GUILayout.Height(30));
            EditorGUILayout.EndVertical();
            
            // Title and Subtitle
            EditorGUILayout.BeginVertical();
            GUILayout.Space(8);
            ThemedTooltip.Label(new GUIContent("Awesome Task Manager Next", "Advanced AI-powered task management extension for Awesome Task Manager."), TBStyles.AssistantHeader);
            GUILayout.Label("AI-Powered Kanban Boards, Smart Checklists & Markdown Notes", EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
            GUILayout.Space(2);
        }

        private void DrawTabBar()
        {
            var tabScope = EditorGUILayout.BeginVertical(GUILayout.Height(32));
            if (Event.current.type == EventType.Repaint)
            {
                TBStyles.DrawGlassPanel(tabScope, TBStyles.TopBarBg, EditorGUIUtility.isProSkin ? new Color(1f, 1f, 1f, 0.05f) : new Color(1f, 1f, 1f, 0.2f), false);
                EditorGUI.DrawRect(new Rect(tabScope.x, tabScope.yMax - 1, tabScope.width, 1), EditorGUIUtility.isProSkin ? new Color(0f, 0f, 0f, 0.3f) : new Color(0f, 0f, 0f, 0.1f));
            }

            _tabBarScrollPos = EditorGUILayout.BeginScrollView(_tabBarScrollPos, false, false, GUIStyle.none, GUIStyle.none, GUIStyle.none, GUILayout.Height(32));
            using (new EditorGUILayout.HorizontalScope(GUILayout.ExpandWidth(true)))
            {
                GUILayout.Space(5);
                if (ThemedTooltip.Button(new GUIContent("👋 Intro", "Overview of AI Assistant capabilities and how it integrates with your workflow."), _currentTab == Tab.Welcome ? TBStyles.TabActive : TBStyles.TabInactive, GUILayout.Height(26)))
                {
                    _currentTab = Tab.Welcome;
                    GUIUtility.ExitGUI();
                }
                if (ThemedTooltip.Button(new GUIContent($"{TBStyles.BoardTabIcon} Generator", "Create new task cards manually or copy AI prompt templates to generate structured task lists."), _currentTab == Tab.TaskGenerator ? TBStyles.TabActive : TBStyles.TabInactive, GUILayout.Height(26)))
                {
                    _currentTab = Tab.TaskGenerator;
                    GUIUtility.ExitGUI();
                }
                if (ThemedTooltip.Button(new GUIContent($"{TBStyles.ChildLinkIcon} Subtasks", "Break down large tasks into smaller subtasks, link cards into hierarchies, and manage checklists."), _currentTab == Tab.SubtaskBreakdown ? TBStyles.TabActive : TBStyles.TabInactive, GUILayout.Height(26)))
                {
                    _currentTab = Tab.SubtaskBreakdown;
                    GUIUtility.ExitGUI();
                }
                if (ThemedTooltip.Button(new GUIContent($"{TBStyles.NotesTabIcon} Devlog", "Write devlogs, release notes, or architecture summaries. Use AI prompts to summarize your sprint."), _currentTab == Tab.DevlogWriter ? TBStyles.TabActive : TBStyles.TabInactive, GUILayout.Height(26)))
                {
                    _currentTab = Tab.DevlogWriter;
                    GUIUtility.ExitGUI();
                }
                if (ThemedTooltip.Button(new GUIContent($"{TBStyles.PriorityFilterIcon} Health", "View overall sprint progress, completion percentages, and board statistics."), _currentTab == Tab.SprintHealth ? TBStyles.TabActive : TBStyles.TabInactive, GUILayout.Height(26)))
                {
                    _currentTab = Tab.SprintHealth;
                    GUIUtility.ExitGUI();
                }
                if (ThemedTooltip.Button(new GUIContent($"{TBStyles.StyleTabIcon} Setup", "Configure AI agent integration, update guidelines, and view available MCP tools."), _currentTab == Tab.AgentSetup ? TBStyles.TabActive : TBStyles.TabInactive, GUILayout.Height(26)))
                {
                    _currentTab = Tab.AgentSetup;
                    GUIUtility.ExitGUI();
                }
                GUILayout.Space(5);
            }
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawWelcomeTab()
        {
            GUILayout.Label("👋 Welcome to Awesome Task Manager Next!", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("This extension bridges Unity with AI Coding Agents (Junie, Claude, Cursor) and provides smart automation tools for your Kanban boards.", MessageType.Info);

            EditorGUILayout.Space(8);
            EditorGUILayout.HelpBox("💬 100% Chat-Driven Workflow:\nALL features (creating boards, task generation, bulk GDD imports, subtask breakdowns, checklist synchronization, note writing, sprint health analytics, and devlogs) can and should be done directly through chat with your AI assistant!\n\nSimply ask your AI in plain language to manage your boards, break down features, or write notes. This works via Unity CLI & MCP (Recommended) or via the External AI Bridge (Junie, Cursor, Rider) when the Editor is open.\n\nThe manual Generator, Subtasks, and Devlog tabs in this window are provided here in case you want to manually create tasks, inspect hierarchies, or copy prompt templates with the help of AI.", MessageType.Info);

            EditorGUILayout.Space(10);
            GUILayout.Label("❓ In-Editor Assistant Tabs Overview:", EditorStyles.boldLabel);
            
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                BulletPoint("🤖 AI Task Generator", "A high-speed importer for Game Design Documents (GDD) or feature lists. Use this tab if you want to paste structured JSON or copy prompt templates manually.");
                BulletPoint("🌿 Subtask Breakdown", "An interactive hierarchy manager to break down large tasks into child cards or convert checklists into subtask hierarchies.");
                BulletPoint("📝 Devlog & Docs Writer", "A documentation assistant that drafts Markdown release notes from your completed tasks with 1-click Quick Note saving.");
                BulletPoint("🤝 AI Agents & MCP Setup", "Configures Model Context Protocol (MCP) tool endpoints so your IDE AI agent can read/write tasks and notes directly.");
            }

            EditorGUILayout.Space(10);
            GUILayout.Label("🚀 Getting Started", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("1. Open 'Setup' tab to configure your AI Agent profile and guidelines.\n2. Ask your AI Assistant in chat to read tasks and start planning your sprint.\n3. (Optional) Use the 'Generator', 'Subtasks', or 'Devlog' tabs if you prefer manual UI editing with AI prompts.", MessageType.None);

            EditorGUILayout.Space(8);
            using (new EditorGUILayout.VerticalScope(TBStyles.CardBox))
            {
                GUILayout.Label("🔌 Unity CLI & MCP Integration (Recommended)", EditorStyles.miniBoldLabel);
                EditorGUILayout.HelpBox("Unity now recommends using the Unity CLI for MCP integration. It is free and does not require an AI tools subscription.\n\n" +
                                        "1. Prerequisites (Unity 6+):\n" +
                                        "   - Ensure you are using Unity 6 (6000.0) or later.\n" +
                                        "   - Install the Unity Pipeline package in your project.\n" +
                                        "2. Install Unity CLI:\n" +
                                        "   - Windows (PowerShell): irm https://public-cdn.cloud.unity3d.com/hub/prod/cli/install.ps1 | iex\n" +
                                        "   - macOS / Linux: curl -fsSL https://unity.com/install.sh | bash\n" +
                                        "3. Setup & Configure:\n" +
                                        "   - Install pipeline: unity pipeline install\n" +
                                        "   - Configure client: unity mcp configure <client>\n" +
                                        "   - (Optional) Install skills: unity skill install <agent-name>\n" +
                                        "4. Direct Commands: If your agent can run shell commands, it can drive the Editor directly using 'unity command' or 'unity eval', which is faster and uses fewer tokens than MCP.\n" +
                                        "5. Start Chatting: Once configured, simply ask your AI agent to manage your tasks or notes, and it will use the available 'atm_' MCP tools automatically.", MessageType.None);
            }
            
            EditorGUILayout.Space(4);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (ThemedTooltip.Button(new GUIContent("⚙ Go to Setup Screen", "Open the Setup tab to configure your AI Agent profile, inspect MCP tools, and update prompt guidelines."), TBStyles.StandardButton, GUILayout.Height(30)))
                {
                    _currentTab = Tab.AgentSetup;
                    GUIUtility.ExitGUI();
                }

                GUILayout.Space(6);

                if (ThemedTooltip.Button(new GUIContent("📋 Open Board UI (Alt+Shift+C)", "Open the main Kanban board window."), TBStyles.StandardButton, GUILayout.Height(30)))
                {
                    EditorApplication.ExecuteMenuItem("Tools/Awesome Task Manager");
                }
            }
            
            GUILayout.Space(10);
            EditorGUILayout.HelpBox("New here? Check the README.md in the package folder for the complete AI guide.", MessageType.Info);
        }

        private void BulletPoint(string title, string description)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(" • ", GUILayout.Width(15));
            EditorGUILayout.BeginVertical();
            GUILayout.Label(title, EditorStyles.boldLabel);
            GUILayout.Label(description, EditorStyles.wordWrappedLabel);
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(4);
        }

        private void DrawTaskGeneratorTab()
        {
            GUILayout.Label("🤖 AI Task & Feature Generator", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("💡 AI Chat Tip: You can ask your AI Assistant (Junie, Cursor, Claude) directly in chat to create cards or bulk-generate tasks from your Game Design Document (GDD)!\n\nThis Generator tab and its prompt templates are provided here in case you want to manually create tasks or copy prompts to import tasks with the help of AI.", MessageType.Info);
            var data = _data;
            var boardNames = data.boards.Select(b => b.name).ToArray();

            if (boardNames.Length == 0)
            {
                EditorGUILayout.HelpBox("No boards found. Create a board in Awesome Task Manager first.", MessageType.Info);
                if (ThemedTooltip.Button(new GUIContent("Create Default Board", "Create a new 'My First Board' task board to get started."), TBStyles.StandardButton))
                {
                    ATMNextService.CreateBoard("My First Board");
                }
                return;
            }

            EditorGUILayout.BeginHorizontal();
            ThemedTooltip.Label(new GUIContent("Target Board", "Choose which board to add tasks to."), null, GUILayout.Width(EditorGUIUtility.labelWidth));
            int currentBoardIdx = Array.IndexOf(boardNames, _selectedBoardName);
            if (currentBoardIdx < 0) currentBoardIdx = 0;
            TBStyles.DrawThemedDropdown(currentBoardIdx, boardNames, (idx) => { _selectedBoardName = boardNames[idx]; }, TBStyles.StandardDropdown, "Choose which board to add tasks to.");
            EditorGUILayout.EndHorizontal();

            var targetBoard = data.boards.FirstOrDefault(b => b.name == _selectedBoardName) ?? data.boards[0];
            var colNames = targetBoard.columns.Select(c => c.title).ToArray();
            
            if (colNames.Length > 0)
            {
                EditorGUILayout.BeginHorizontal();
                ThemedTooltip.Label(new GUIContent("Target Column", "Choose which column in the board to add tasks to."), null, GUILayout.Width(EditorGUIUtility.labelWidth));
                int currentColIdx = Array.IndexOf(colNames, _selectedColumnName);
                if (currentColIdx < 0) currentColIdx = 0;
                TBStyles.DrawThemedDropdown(currentColIdx, colNames, (idx) => { _selectedColumnName = colNames[idx]; }, TBStyles.StandardDropdown, "Choose which column in the board to add tasks to.");
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space(8);
            GUILayout.Label("Quick Task Creator / AI Importer", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            ThemedTooltip.Label(new GUIContent("Task Title", "Enter a concise title for the new task."), null, GUILayout.Width(EditorGUIUtility.labelWidth));
            _manualTaskTitle = TBStyles.DrawThemedTextField(_manualTaskTitle);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            ThemedTooltip.Label(new GUIContent("Category", "Specify a category (e.g., Coding, Art, Design) for better organization."), null, GUILayout.Width(EditorGUIUtility.labelWidth));
            _manualTaskCategory = TBStyles.DrawThemedTextField(_manualTaskCategory);
            EditorGUILayout.EndHorizontal();
            
            _manualTaskPriority = TBStyles.DrawThemedIntSlider(new GUIContent("Priority (0-4)"), _manualTaskPriority, 0, 4, "Set the importance of this task. 0 is lowest, 4 is highest.");

            ThemedTooltip.Label(new GUIContent("Description:", "Detailed notes or requirements for this task."));
            using (var scope = new EditorGUILayout.VerticalScope())
            {
                if (Event.current.type == EventType.Repaint)
                {
                    var borderColor = EditorGUIUtility.isProSkin ? new Color(1f, 1f, 1f, 0.25f) : new Color(0f, 0f, 0f, 0.3f);
                    TBStyles.DrawGlassPanel(scope.rect, TBStyles.DropdownBg, borderColor, false);
                }
                
                GUILayout.Space(2);
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(2);
                _manualTaskDesc = EditorGUILayout.TextArea(_manualTaskDesc, TBStyles.CardTextArea, GUILayout.Height(50));
                GUILayout.Space(2);
                EditorGUILayout.EndHorizontal();
                GUILayout.Space(2);
            }

            ThemedTooltip.Label(new GUIContent("Checklist Items (one per line):", "AI can automatically break these down into child cards or you can use them as simple checklist items."));
            using (var scope = new EditorGUILayout.VerticalScope())
            {
                if (Event.current.type == EventType.Repaint)
                {
                    var borderColor = EditorGUIUtility.isProSkin ? new Color(1f, 1f, 1f, 0.25f) : new Color(0f, 0f, 0f, 0.3f);
                    TBStyles.DrawGlassPanel(scope.rect, TBStyles.DropdownBg, borderColor, false);
                }
                
                GUILayout.Space(2);
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(2);
                _manualChecklistRaw = EditorGUILayout.TextArea(_manualChecklistRaw, TBStyles.CardTextArea, GUILayout.Height(60));
                GUILayout.Space(2);
                EditorGUILayout.EndHorizontal();
                GUILayout.Space(2);
            }

            if (ThemedTooltip.Button(new GUIContent($"{TBStyles.NewCardIcon} Add Card to Board", "Create a new task card with the specified title, category, and priority on the target board."), TBStyles.StandardButton, GUILayout.Height(28)))
            {
                if (!string.IsNullOrWhiteSpace(_manualTaskTitle))
                {
                    var checklist = _manualChecklistRaw
                        .Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => s.Trim().TrimStart('-', '*', '[', ']', ' '))
                        .Where(s => !string.IsNullOrEmpty(s))
                        .ToList();

                    ATMNextService.CreateCard(
                        _selectedBoardName,
                        _selectedColumnName,
                        _manualTaskTitle,
                        _manualTaskDesc,
                        _manualTaskCategory,
                        _manualTaskPriority,
                        checklist: checklist
                    );

                    _manualTaskTitle = "";
                    _manualTaskDesc = "";
                    _manualChecklistRaw = "";
                    GUI.FocusControl(null);
                    ShowNotification(new GUIContent("Task card created!"));
                }
            }

            
            EditorGUILayout.Space(14);
            EditorGUILayout.LabelField("Game Feature / GDD Prompt Templates", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Copy these prompt templates into your LLM or Junie Pro to generate structured task lists or hierarchical Epics.", MessageType.None);
            
            EditorGUILayout.BeginHorizontal();
            if (ThemedTooltip.Button(new GUIContent($"{TBStyles.BoardTabIcon} Copy GDD-to-Tasks Prompt", "Copy a prompt template to your clipboard that helps an AI turn a Game Design Document into a task list."), TBStyles.StandardButton))
            {
                GUIUtility.systemCopyBuffer = ATMNextPromptTemplates.GddToKanbanPrompt;
                ShowNotification(new GUIContent("Prompt template copied to clipboard!"));
            }
            if (ThemedTooltip.Button(new GUIContent($"{TBStyles.BoardTabIcon} Copy Epic & Subtasks Prompt", "Copy a prompt template to your clipboard that helps an AI break down a complex feature into a hierarchical Epic."), TBStyles.StandardButton))
            {
                GUIUtility.systemCopyBuffer = ATMNextPromptTemplates.EpicToSubtasksPrompt;
                ShowNotification(new GUIContent("Epic prompt template copied to clipboard!"));
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(14);
            GUILayout.Label("🤖 Bulk AI Importer (JSON)", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Paste the structured JSON response from an AI (using the prompt templates below) to import multiple tasks or an Epic hierarchy at once.", MessageType.None);
            
            using (var scope = new EditorGUILayout.VerticalScope())
            {
                if (Event.current.type == EventType.Repaint)
                {
                    var borderColor = EditorGUIUtility.isProSkin ? new Color(1f, 1f, 1f, 0.25f) : new Color(0f, 0f, 0f, 0.3f);
                    TBStyles.DrawGlassPanel(scope.rect, TBStyles.DropdownBg, borderColor, false);
                }
                
                GUILayout.Space(2);
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(2);
                _aiResponseJson = EditorGUILayout.TextArea(_aiResponseJson, TBStyles.CardTextArea, GUILayout.Height(80));
                GUILayout.Space(2);
                EditorGUILayout.EndHorizontal();
                GUILayout.Space(2);
            }

            if (ThemedTooltip.Button(new GUIContent($"{TBStyles.BoardTabIcon} Process & Import AI Response", "Parse the pasted JSON and create all tasks/epics on the target board."), TBStyles.StandardButton, GUILayout.Height(28)))
            {
                ProcessAiJsonImport();
            }

           
           
        }

        private void DrawSubtaskBreakdownTab()
        {
            GUILayout.Label("🔗 Hierarchical Task Management", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("💡 AI Chat Tip: You can ask your AI Assistant directly in chat to break down complex features into Epics, subtasks, or convert checklists into child task cards!\n\nThis Subtasks tab and its prompt templates are provided here in case you want to manually manage or convert subtasks with the help of AI.", MessageType.Info);
            var allCards = ATMNextService.GetCards();

            if (allCards.Count == 0)
            {
                EditorGUILayout.HelpBox("No task cards found across boards.", MessageType.Info);
                return;
            }

            var cardTitles = allCards.Select(c => $"[{c.category}] {c.title}").ToArray();
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(new GUIContent("Select Task Card", "Select a task card to view its subtasks and checklist."), GUILayout.Width(EditorGUIUtility.labelWidth));
            _selectedCardIndex = Mathf.Clamp(_selectedCardIndex, 0, allCards.Count - 1);
            TBStyles.DrawThemedDropdown(_selectedCardIndex, cardTitles, (idx) => { _selectedCardIndex = idx; }, TBStyles.StandardDropdown, "Select a task card to view its subtasks and checklist.");
            EditorGUILayout.EndHorizontal();

            var card = allCards[_selectedCardIndex];
            EditorGUILayout.Space(6);

            EditorGUILayout.BeginVertical(TBStyles.CardBox);
            GUILayout.Label(new GUIContent($"{TBStyles.BoardHeaderIcon} Card: {card.title}", "Currently selected task card details."), TBStyles.CardTitle);
            if (!string.IsNullOrEmpty(card.description))
                GUILayout.Label(new GUIContent($"Description: {card.description}", card.description), EditorStyles.wordWrappedMiniLabel);
            GUILayout.Label($"{TBStyles.CategoryIcon} Category: {card.category} | {TBStyles.PriorityFilterIcon} Priority: {card.priority} | Status: {(card.completed ? TBStyles.CompletedIcon + " Completed" : "⏳ In Progress")}", EditorStyles.miniLabel);

            var parents = ATMNextService.GetParentTasks(card.id);
            if (parents.Count > 0)
            {
                GUILayout.Label($"{TBStyles.ParentLinkIcon} Parent Card(s): {string.Join(", ", parents.Select(p => p.title))}", EditorStyles.miniBoldLabel);
            }

            var hierarchy = ATMNextService.GetTaskHierarchy(card.id);
            if (hierarchy != null && hierarchy.totalSubtasks > 0)
            {
                GUILayout.Label($"{TBStyles.ChildLinkIcon} Linked Subtasks: {hierarchy.completedSubtasks}/{hierarchy.totalSubtasks} completed ({hierarchy.completionPercent:0}%)", EditorStyles.boldLabel);
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(8);
            GUILayout.Label("Current Checklist Items & Linked Subtasks:", EditorStyles.boldLabel);

            if (card.checklistItems == null || card.checklistItems.Count == 0)
            {
                GUILayout.Label("No checklist items yet.", EditorStyles.miniLabel);
            }
            else
            {
                for (int i = 0; i < card.checklistItems.Count; i++)
                {
                    EditorGUILayout.BeginHorizontal();
                    bool state = i < card.checklistStates.Count && card.checklistStates[i];
                    bool newState = TBStyles.DrawThemedCheckbox(state, "Mark this checklist item as completed or pending.", GUILayout.Width(20));
                    if (newState != state)
                    {
                        ATMNextService.SetChecklistItemState(card.id, i, newState);
                    }

                    string itemText = card.checklistItems[i] ?? "";
                    string linkedSubId = (card.checklistLinkedCardIds != null && i < card.checklistLinkedCardIds.Count) ? card.checklistLinkedCardIds[i] : null;

                    if (!string.IsNullOrEmpty(linkedSubId))
                    {
                        var subCard = ATMNextService.GetCard(linkedSubId);
                        string subTitle = subCard != null ? subCard.title : "Linked Card";
                        GUILayout.Label($"{TBStyles.ChildLinkIcon} {itemText} [Card: {subTitle}]", EditorStyles.boldLabel);

                        if (ThemedTooltip.Button(new GUIContent("Unlink", "Remove the link between this checklist item and its subtask card."), TBStyles.StandardButton, GUILayout.Width(52)))
                        {
                            ATMNextService.UnlinkChildTask(card.id, linkedSubId);
                            break;
                        }
                    }
                    else
                    {
                        GUILayout.Label(itemText);
                        if (ThemedTooltip.Button(new GUIContent($"{TBStyles.UrlIcon} Convert to Card", "Convert this checklist item into a dedicated task card on the board and link it as a subtask."), TBStyles.StandardButton, GUILayout.Width(115)))
                        {
                            ATMNextService.CreateSubtask(card.id, itemText, category: card.category, priority: card.priority);
                            break;
                        }
                    }

                    if (ThemedTooltip.Button(new GUIContent(TBStyles.CancelIcon, "Permanently remove this item from the checklist."), TBStyles.DeleteIconButton, GUILayout.Width(22)))
                    {
                        ATMNextService.RemoveChecklistItem(card.id, i);
                        break;
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }

            EditorGUILayout.Space(8);
            EditorGUILayout.BeginHorizontal();
            ThemedTooltip.Label(new GUIContent("Quick Checklist Item", "Type a short description for a new checklist item."), null, GUILayout.Width(EditorGUIUtility.labelWidth));
            _newSubtaskText = TBStyles.DrawThemedTextField(_newSubtaskText);
            if (ThemedTooltip.Button(new GUIContent("Add", "Add a new item to the task's checklist."), TBStyles.StandardButton, GUILayout.Width(50)) && !string.IsNullOrWhiteSpace(_newSubtaskText))
            {
                ATMNextService.AddChecklistItem(card.id, _newSubtaskText.Trim());
                _newSubtaskText = "";
                GUI.FocusControl(null);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(12);
            EditorGUILayout.BeginVertical(TBStyles.CardBox);
            GUILayout.Label($"{TBStyles.NewCardIcon} Create & Link Dedicated Child Subtask Card", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Creates a full child card on the board and links it into this card's checklist. This keeps your board organized without creating huge overwhelming flat lists.", MessageType.None);
            EditorGUILayout.BeginHorizontal();
            ThemedTooltip.Label(new GUIContent("Subtask Title", "Enter the title for the new child card."), null, GUILayout.Width(EditorGUIUtility.labelWidth));
            _newSubtaskText = TBStyles.DrawThemedTextField(_newSubtaskText);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            ThemedTooltip.Label(new GUIContent("Description", "Briefly describe what needs to be done in this subtask."), null, GUILayout.Width(EditorGUIUtility.labelWidth));
            _newSubtaskCardDesc = TBStyles.DrawThemedTextField(_newSubtaskCardDesc);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            ThemedTooltip.Label(new GUIContent("Category", "Subtasks usually inherit the parent's category, but you can override it here."), null, GUILayout.Width(EditorGUIUtility.labelWidth));
            _newSubtaskCardCategory = TBStyles.DrawThemedTextField(_newSubtaskCardCategory);
            EditorGUILayout.EndHorizontal();
            _newSubtaskCardPriority = TBStyles.DrawThemedIntSlider(new GUIContent("Priority"), _newSubtaskCardPriority, 0, 4, "Set the priority for this subtask.");

            if (ThemedTooltip.Button(new GUIContent($"{TBStyles.ChildLinkIcon} Create & Link Child Subtask", "Create a new dedicated task card and automatically link it to this parent card."), TBStyles.StandardButton, GUILayout.Height(26)))
            {
                if (!string.IsNullOrWhiteSpace(_newSubtaskText))
                {
                    ATMNextService.CreateSubtask(
                        card.id,
                        _newSubtaskText.Trim(),
                        _newSubtaskCardDesc,
                        category: _newSubtaskCardCategory,
                        priority: _newSubtaskCardPriority
                    );
                    _newSubtaskText = "";
                    _newSubtaskCardDesc = "";
                    GUI.FocusControl(null);
                    ShowNotification(new GUIContent("Child subtask card created & linked!"));
                }
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginHorizontal();
            if (ThemedTooltip.Button(new GUIContent($"{TBStyles.ChecklistIcon} Copy Subtask Checklist Prompt", "Copy a prompt template to help an AI generate a detailed checklist for this specific task."), TBStyles.StandardButton))
            {
                string prompt = ATMNextPromptTemplates.SubtaskBreakdownPrompt
                    .Replace("{TITLE}", card.title)
                    .Replace("{DESCRIPTION}", card.description ?? "")
                    .Replace("{CATEGORY}", card.category ?? "");

                GUIUtility.systemCopyBuffer = prompt;
                ShowNotification(new GUIContent("Subtask prompt copied to clipboard!"));
            }

            if (ThemedTooltip.Button(new GUIContent($"{TBStyles.ParentLinkIcon} Copy Epic & Subtasks Prompt", "Copy a prompt template to help an AI break down this task into a full hierarchical Epic with child subtasks."), TBStyles.StandardButton))
            {
                string prompt = ATMNextPromptTemplates.EpicToSubtasksPrompt
                    .Replace("{INPUT}", $"{card.title}\n{card.description}");

                GUIUtility.systemCopyBuffer = prompt;
                ShowNotification(new GUIContent("Epic hierarchy prompt copied to clipboard!"));
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(10);
            GUILayout.Label("🌿 AI Checklist Importer", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Paste a list of checklist items from an AI (one per line) to bulk-add them to this card.", MessageType.None);
            
            using (var scope = new EditorGUILayout.VerticalScope())
            {
                if (Event.current.type == EventType.Repaint)
                {
                    var borderColor = EditorGUIUtility.isProSkin ? new Color(1f, 1f, 1f, 0.25f) : new Color(0f, 0f, 0f, 0.3f);
                    TBStyles.DrawGlassPanel(scope.rect, TBStyles.DropdownBg, borderColor, false);
                }
                
                GUILayout.Space(2);
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(2);
                _aiChecklistInput = EditorGUILayout.TextArea(_aiChecklistInput, TBStyles.CardTextArea, GUILayout.Height(60));
                GUILayout.Space(2);
                EditorGUILayout.EndHorizontal();
                GUILayout.Space(2);
            }

         
            
            if (ThemedTooltip.Button(new GUIContent($"{TBStyles.ChecklistIcon} Add AI Items to Checklist", "Parse the pasted text and add each line as a new checklist item."), TBStyles.StandardButton, GUILayout.Height(28)))
            {
                if (!string.IsNullOrWhiteSpace(_aiChecklistInput))
                {
                    var items = _aiChecklistInput
                        .Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => s.Trim().TrimStart('-', '*', '[', ']', ' '))
                        .Where(s => !string.IsNullOrEmpty(s))
                        .ToList();

                    foreach (var item in items)
                    {
                        ATMNextService.AddChecklistItem(card.id, item);
                    }
                    _aiChecklistInput = "";
                    ShowNotification(new GUIContent($"Added {items.Count} items to checklist!"));
                }
            }

            EditorGUILayout.Space(10);
          
        }

        private void DrawDevlogWriterTab()
        {
            GUILayout.Label("📝 Devlog & AI Summary Writer", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("💡 AI Chat Tip: You can ask your AI Assistant directly in chat to summarize your sprint, compile completed tasks into release notes, or create formatted devlog notes automatically!\n\nThis Devlog tab and its prompt templates are provided here in case you want to manually draft, format, or review devlogs with the help of AI.", MessageType.Info);
            EditorGUILayout.BeginHorizontal();
            ThemedTooltip.Label(new GUIContent("Devlog Title", "The title of your new devlog or architecture note."), null, GUILayout.Width(EditorGUIUtility.labelWidth));
            _devlogTitle = TBStyles.DrawThemedTextField(_devlogTitle);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            ThemedTooltip.Label(new GUIContent("Note Folder", "Optional folder name to organize your notes."), null, GUILayout.Width(EditorGUIUtility.labelWidth));
            _devlogFolderName = TBStyles.DrawThemedTextField(_devlogFolderName);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            ThemedTooltip.Label(new GUIContent("Tags (comma-separated)", "Add descriptive tags to help filter and find this note later."), null, GUILayout.Width(EditorGUIUtility.labelWidth));
            _devlogTags = TBStyles.DrawThemedTextField(_devlogTags);
            EditorGUILayout.EndHorizontal();

            ThemedTooltip.Label(new GUIContent("Markdown Content:", "Write your developer log or release notes here using Markdown."));
            using (var scope = new EditorGUILayout.VerticalScope())
            {
                if (Event.current.type == EventType.Repaint)
                {
                    var borderColor = EditorGUIUtility.isProSkin ? new Color(1f, 1f, 1f, 0.25f) : new Color(0f, 0f, 0f, 0.3f);
                    TBStyles.DrawGlassPanel(scope.rect, TBStyles.DropdownBg, borderColor, false);
                }
                
                GUILayout.Space(2);
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(2);
                _devlogContent = EditorGUILayout.TextArea(_devlogContent, TBStyles.CardTextArea, GUILayout.Height(160));
                GUILayout.Space(2);
                EditorGUILayout.EndHorizontal();
                GUILayout.Space(2);
            }

            EditorGUILayout.BeginHorizontal();
            if (ThemedTooltip.Button(new GUIContent($"{TBStyles.NotesTabIcon} Auto-Generate Draft", "Scan all 'Done' tasks and generate a Markdown draft of release notes."), TBStyles.StandardButton, GUILayout.Height(28)))
            {
                var doneCards = ATMNextService.GetCards().Where(c => c.completed).ToList();
                var inProgress = ATMNextService.GetCards().Where(c => !c.completed).ToList();
                
                var sb = new System.Text.StringBuilder();
                sb.AppendLine($"# 🚀 Devlog Update: {_devlogTitle}");
                sb.AppendLine();
                sb.AppendLine("## ✨ Completed Features & Improvements");
                if (doneCards.Count == 0) sb.AppendLine("- No tasks completed yet.");
                foreach (var c in doneCards) sb.AppendLine($"- **{c.title}** ({c.category})");
                
                sb.AppendLine();
                sb.AppendLine("## 🛠 In Progress & Next Steps");
                if (inProgress.Count == 0) sb.AppendLine("- All tasks completed!");
                foreach (var c in inProgress.Take(5)) sb.AppendLine($"- {c.title}");
                
                _devlogContent = sb.ToString();
                ShowNotification(new GUIContent("Draft generated!"));
            }

            if (ThemedTooltip.Button(new GUIContent($"{TBStyles.NewCardIcon} Create Quick Note", "Save your devlog or release notes as a Markdown Quick Note in the project."), TBStyles.StandardButton, GUILayout.Height(28)))
            {
                if (!string.IsNullOrWhiteSpace(_devlogTitle))
                {
                    var tags = _devlogTags.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(t => t.Trim()).ToList();
                    var note = ATMNextService.CreateNote(_devlogTitle, _devlogContent, _devlogFolderName, tags);
                    if (note != null)
                    {
                        ShowNotification(new GUIContent($"Created Note '{note.title}'!"));
                    }
                }
            }

            if (ThemedTooltip.Button(new GUIContent($"{TBStyles.PriorityFilterIcon} Copy Sprint Summary Prompt", "Generate and copy a prompt that helps an AI write a sprint summary based on your current task status."), TBStyles.StandardButton, GUILayout.Height(28)))
            {
                var cards = ATMNextService.GetCards().Select(c => $"- [{c.category}] {c.title}: {(c.completed ? "DONE" : "IN_PROGRESS")}").ToList();
                string prompt = ATMNextPromptTemplates.SprintSummaryPrompt
                    .Replace("{TITLE}", _devlogTitle)
                    .Replace("{TASKS}", string.Join("\n", cards.ToArray()));

                GUIUtility.systemCopyBuffer = prompt;
                ShowNotification(new GUIContent("Sprint Summary prompt copied!"));
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawSprintHealthTab()
        {
            GUILayout.Label("📈 Sprint Health & Analytics", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Monitor your sprint's health with completion percentages, board-wide statistics, and category distributions.", MessageType.Info);
            var data = _data;
            var allCards = ATMNextService.GetCards();

            int total = allCards.Count;
            int done = allCards.Count(c => c.completed);
            int pending = total - done;
            float progress = total > 0 ? (float)done / total : 0f;

            EditorGUILayout.BeginVertical(TBStyles.CardBox);
            GUILayout.Label($"{TBStyles.PriorityFilterIcon} Sprint Progress", EditorStyles.boldLabel);
            var progressRect = EditorGUILayout.GetControlRect(false, 20);
            EditorGUI.ProgressBar(progressRect, progress, $"{done} / {total} Tasks ({Mathf.RoundToInt(progress * 100)}%)");
            ThemedTooltip.SetTooltip(progressRect, $"Overall completion across all boards: {done} tasks done out of {total} total.");
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(8);
            GUILayout.Label("Board Statistics:", EditorStyles.boldLabel);
            ThemedTooltip.LabelField("Total Boards:", data.boards.Count.ToString(), $"Total number of Kanban boards: {data.boards.Count}");
            ThemedTooltip.LabelField("Total Task Cards:", total.ToString(), $"Total number of task cards across all boards: {total}");
            ThemedTooltip.LabelField("Completed Cards:", done.ToString(), $"Number of cards marked as completed: {done}");
            ThemedTooltip.LabelField("Pending Tasks:", pending.ToString(), $"Number of cards still to be done: {pending}");
            ThemedTooltip.LabelField("Quick Notes:", (data.notes?.Count ?? 0).ToString(), $"Total number of Quick Notes: {data.notes?.Count ?? 0}");
            ThemedTooltip.LabelField("Note Folders:", (data.noteFolders?.Count ?? 0).ToString(), $"Total number of note folders: {data.noteFolders?.Count ?? 0}");

            EditorGUILayout.Space(10);
            GUILayout.Label("Categories Breakdown:", EditorStyles.boldLabel);
            foreach (var cat in data.categories)
            {
                int count = allCards.Count(c => string.Equals(c.category, cat, StringComparison.OrdinalIgnoreCase));
                if (count > 0)
                {
                    ThemedTooltip.LabelField($"• {cat}:", $"{count} cards", $"{cat}: {count} cards in this category.");
                }
            }
        }

        private void DrawAgentSetupTab()
        {
            GUILayout.Label("🤖 AI Agent & MCP Integration", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Configure Junie Pro, Cursor, Claude Code, or Unity AI Assistant to automatically consult, update, and manage your Kanban boards and notes.", MessageType.Info);

            EditorGUILayout.Space(8);
            using (new EditorGUILayout.VerticalScope(TBStyles.CardBox))
            {
                GUILayout.Label("🔌 Unity CLI & MCP Integration (Recommended)", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox("Unity now recommends using the Unity CLI for MCP integration. It is free and does not require an AI tools subscription.", MessageType.None);
                
                EditorGUILayout.Space(2);
                GUILayout.Label("1. Prerequisites (Unity 6+)", EditorStyles.miniBoldLabel);
                EditorGUILayout.HelpBox("• Ensure you are using Unity 6 (6000.0) or later.\n" +
                                        "• Install the Unity Pipeline package in your project.", MessageType.None);

                EditorGUILayout.Space(2);
                GUILayout.Label("2. Install Unity CLI", EditorStyles.miniBoldLabel);
                EditorGUILayout.HelpBox("• Windows (PowerShell):\n  irm https://public-cdn.cloud.unity3d.com/hub/prod/cli/install.ps1 | iex\n" +
                                        "• macOS / Linux:\n  curl -fsSL https://unity.com/install.sh | bash", MessageType.None);
                
                EditorGUILayout.Space(2);
                GUILayout.Label("3. Setup & Configure", EditorStyles.miniBoldLabel);
                EditorGUILayout.HelpBox("• Install pipeline: unity pipeline install\n" +
                                        "• Configure client: unity mcp configure <client>\n" +
                                        "• (Optional) Install skills: unity skill install <agent-name>", MessageType.None);

                EditorGUILayout.Space(2);
                GUILayout.Label("4. Direct Commands", EditorStyles.miniBoldLabel);
                EditorGUILayout.HelpBox("If your agent can run shell commands, it can drive the Editor directly using 'unity command' or 'unity eval', which is faster and uses fewer tokens than MCP.", MessageType.None);

                EditorGUILayout.Space(2);
                GUILayout.Label("5. Start Chatting", EditorStyles.miniBoldLabel);
                EditorGUILayout.HelpBox("Once configured, simply ask your AI agent to manage your tasks or notes, and it will use the available 'atm_' MCP tools automatically.", MessageType.None);
            }

            EditorGUILayout.Space(8);
            using (new EditorGUILayout.VerticalScope(TBStyles.CardBox))
            {
                GUILayout.Label("🌉 External AI Bridge", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox("If your AI (Junie, Cursor, Rider) cannot call MCP tools directly, it can use the file-based bridge:\n\n" +
                                        "1. AI writes to: ProjectSettings/AwesomeTaskManager/external_command.json\n" +
                                        "2. Unity processes it and writes to: external_result.json\n\n" +
                                        "This bridge is active as long as this Unity Editor is open.", MessageType.None);
            }

            EditorGUILayout.Space(8);
            if (ThemedTooltip.Button(new GUIContent($"{TBStyles.NotesTabIcon} Append Guidelines to AGENTS.md", "Update the AGENTS.md file in your project root with the latest instructions for AI agents."), TBStyles.StandardButton, GUILayout.Height(30)))
            {
                bool updated = ATMNextAgentProfile.EnsureAgentsMdUpdated();
                ShowNotification(new GUIContent(updated ? "AGENTS.md updated!" : "AGENTS.md already contains guidelines."));
            }

            if (ThemedTooltip.Button(new GUIContent($"{TBStyles.BoardTabIcon} Copy Agent Guidelines to Clipboard", "Copy the full AI integration guidelines to your clipboard for use in external LLM tools."), TBStyles.StandardButton, GUILayout.Height(30)))
            {
                GUIUtility.systemCopyBuffer = ATMNextAgentProfile.AgentGuideline;
                ShowNotification(new GUIContent("Agent guidelines copied!"));
            }

            EditorGUILayout.Space(12);
            GUILayout.Label($"Available Tools ({_availableToolCatalog.Sum(c => c.Tools.Length)} MCP & Bridge Endpoints):", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            using (new EditorGUILayout.HorizontalScope())
            {
                ThemedTooltip.Label("🔍", "Filter available tools by name or description", null, GUILayout.Width(18), GUILayout.Height(20));
                _toolSearchQuery = TBStyles.DrawThemedTextField(_toolSearchQuery, TBStyles.ThemedSearchField, GUILayout.Height(20));
                if (!string.IsNullOrEmpty(_toolSearchQuery))
                {
                    if (ThemedTooltip.Button("✕", "Clear search filter", TBStyles.ToolbarButton, GUILayout.Width(22), GUILayout.Height(20)))
                    {
                        _toolSearchQuery = "";
                        GUI.FocusControl(null);
                    }
                }
            }

            EditorGUILayout.Space(6);

            float originalLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 240;

            _agentSetupScroll = EditorGUILayout.BeginScrollView(_agentSetupScroll, GUILayout.Height(240));

            int totalMatches = 0;
            string query = (_toolSearchQuery ?? "").Trim();
            bool hasFilter = !string.IsNullOrEmpty(query);
            bool renderedAnyCategory = false;

            for (int i = 0; i < _availableToolCatalog.Length; i++)
            {
                var cat = _availableToolCatalog[i];
                var matchingTools = hasFilter
                    ? cat.Tools.Where(t => t.Name.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 ||
                                           t.Description.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0).ToArray()
                    : cat.Tools;

                if (matchingTools.Length > 0)
                {
                    totalMatches += matchingTools.Length;
                    if (renderedAnyCategory) EditorGUILayout.Space(4);
                    renderedAnyCategory = true;

                    GUILayout.Label($"{cat.CategoryName} ({matchingTools.Length})", EditorStyles.miniBoldLabel);

                    foreach (var tool in matchingTools)
                    {
                        ThemedTooltip.LabelField($"• {tool.Name}", tool.Description, tool.Description);
                    }
                }
            }

            if (hasFilter && totalMatches == 0)
            {
                EditorGUILayout.HelpBox($"No tools found matching \"{query}\".", MessageType.Info);
            }

            EditorGUILayout.EndScrollView();

            EditorGUIUtility.labelWidth = originalLabelWidth;
        }

        private void DrawFooter()
        {
            using (var scope = new EditorGUILayout.HorizontalScope(GUILayout.Height(24)))
            {
                if (Event.current.type == EventType.Repaint)
                {
                    TBStyles.DrawGlassPanel(scope.rect, TBStyles.TopBarBg, EditorGUIUtility.isProSkin ? new Color(1f, 1f, 1f, 0.05f) : new Color(1f, 1f, 1f, 0.2f), false);
                }

                GUILayout.Label(" Awesome Task Manager Next – AI Edition", EditorStyles.miniLabel);
                GUILayout.FlexibleSpace();
                if (ThemedTooltip.Button(new GUIContent("Open Main Task Board", "Open the standard Awesome Task Manager Kanban board window."), TBStyles.ToolbarButton))
                {
                    TaskBoardWindow.Open();
                }
                GUILayout.Space(4);
            }
        }
    }
}
