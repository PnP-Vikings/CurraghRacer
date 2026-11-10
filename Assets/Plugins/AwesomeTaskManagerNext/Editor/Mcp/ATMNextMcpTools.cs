using System;
using System.Collections.Generic;
using System.Linq;
using AwesomeTaskManager.Data;
using AwesomeTaskManager.Next;

#if ATM_NEXT_UNITYAI_MCP_PRESENT || ATM_NEXT_MCP_PRESENT
using Unity.AI.MCP.Editor.ToolRegistry;
#endif

namespace AwesomeTaskManager.Next.Mcp
{
    /// <summary>
    /// Model Context Protocol (MCP) tool registrations for Awesome Task Manager Next.
    /// Exposes task board, Kanban, checklist, and quick note management to Unity AI Assistant and IDE AI Agents.
    /// </summary>
    public static class ATMNextMcpTools
    {
        #region Parameter Classes

        public class CreateTaskParams
        {
            public string Board { get; set; }
            public string Column { get; set; } = "📋 To Do";
            public string Title { get; set; }
            public string Description { get; set; }
            public string Category { get; set; }
            public int Priority { get; set; }
            public string DueDate { get; set; }
            public int ColorLabel { get; set; }
            public string[] Checklist { get; set; }
            public string[] Assignees { get; set; }
        }

        public class UpdateTaskParams
        {
            public string CardIdOrTitle { get; set; }
            public string NewTitle { get; set; }
            public string NewDescription { get; set; }
            public string NewCategory { get; set; }
            public int? NewPriority { get; set; }
            public string NewDueDate { get; set; }
            public int? NewColorLabel { get; set; }
            public bool? Completed { get; set; }
            public bool? Archived { get; set; }
            public string[] Assignees { get; set; }
        }

        public class MoveTaskParams
        {
            public string CardIdOrTitle { get; set; }
            public string TargetColumn { get; set; }
            public string TargetBoard { get; set; }
        }

        public class DeleteTaskParams
        {
            public string CardIdOrTitle { get; set; }
            public bool Confirm { get; set; }
        }

        public class ManageChecklistParams
        {
            public string CardIdOrTitle { get; set; }
            public string Action { get; set; } = "add"; // "add", "toggle", "remove"
            public string ItemText { get; set; }
            public int ItemIndex { get; set; }
            public bool IsChecked { get; set; }
            public bool Confirm { get; set; }
        }

        public class CreateNoteParams
        {
            public string Title { get; set; }
            public string Content { get; set; }
            public string FolderName { get; set; } = "AI Notes";
            public string[] Tags { get; set; }
            public bool Pinned { get; set; }
            public int ColorIndex { get; set; }
        }

        public class UpdateNoteParams
        {
            public string NoteIdOrTitle { get; set; }
            public string NewTitle { get; set; }
            public string NewContent { get; set; }
            public string NewFolderName { get; set; }
            public string[] NewTags { get; set; }
            public bool? Pinned { get; set; }
            public int? ColorIndex { get; set; }
        }

        public class DeleteNoteParams
        {
            public string NoteIdOrTitle { get; set; }
            public bool Confirm { get; set; }
        }

        public class BulkCreateNoteItem
        {
            public string Title { get; set; }
            public string Content { get; set; }
            public string FolderName { get; set; }
            public string[] Tags { get; set; }
            public bool Pinned { get; set; }
            public int ColorIndex { get; set; }
        }

        public class BulkCreateNotesParams
        {
            public string DefaultFolderName { get; set; } = "AI Notes";
            public string[] DefaultTags { get; set; }
            public BulkCreateNoteItem[] Notes { get; set; }
        }

        public class BulkUpdateNoteItem
        {
            public string NoteIdOrTitle { get; set; }
            public string NewTitle { get; set; }
            public string NewContent { get; set; }
            public string AppendContent { get; set; }
            public string NewFolderName { get; set; }
            public string[] NewTags { get; set; }
            public string[] AddTags { get; set; }
            public string[] RemoveTags { get; set; }
            public bool? Pinned { get; set; }
            public int? ColorIndex { get; set; }
        }

        public class BulkUpdateNotesParams
        {
            public string DefaultFolderName { get; set; }
            public string[] DefaultTags { get; set; }
            public string[] AddTags { get; set; }
            public string[] RemoveTags { get; set; }
            public bool? DefaultPinned { get; set; }
            public int? DefaultColorIndex { get; set; }
            public string AppendContent { get; set; }
            public BulkUpdateNoteItem[] Updates { get; set; }
            public string[] NoteIdsOrTitles { get; set; }
            public string[] Notes { get; set; }
        }

        public class BulkMoveNoteItem
        {
            public string NoteIdOrTitle { get; set; }
            public string TargetFolderName { get; set; }
        }

        public class BulkMoveNotesParams
        {
            public string TargetFolderName { get; set; }
            public BulkMoveNoteItem[] Moves { get; set; }
            public string[] NoteIdsOrTitles { get; set; }
            public string[] Notes { get; set; }
        }

        public class BulkDeleteNotesParams
        {
            public string[] NoteIdsOrTitles { get; set; }
            public string[] Notes { get; set; }
            public bool Confirm { get; set; }
        }

        public class LinkItemParams
        {
            public string CardIdOrTitle { get; set; }
            public string LinkType { get; set; } = "note"; // "note", "url", "asset"
            public string Target { get; set; } // Note ID/Title, URL, or Asset Path/GUID
            public string DisplayName { get; set; }
        }

        public class BulkLinkItemEntry
        {
            public string CardIdOrTitle { get; set; }
            public string Target { get; set; }
            public string LinkType { get; set; } = "note";
            public string DisplayName { get; set; }
        }

        public class BulkLinkItemsParams
        {
            public string CardIdOrTitle { get; set; }
            public string[] CardIdsOrTitles { get; set; }
            public string[] Tasks { get; set; }
            public string Target { get; set; }
            public string[] Targets { get; set; }
            public string LinkType { get; set; } = "note"; // "note", "url", "asset"
            public string DisplayName { get; set; }
            public BulkLinkItemEntry[] Links { get; set; }
        }

        public class SearchParams
        {
            public string Query { get; set; }
            public string Keyword { get; set; }
            public string Search { get; set; }
            public string Term { get; set; }
            public string Text { get; set; }
            public string Board { get; set; }
            public string Column { get; set; }
            public string Category { get; set; }
            public string Assignee { get; set; }
            public string Scope { get; set; } = "all";
            public bool IncludeCompleted { get; set; } = true;
            public bool IncludeArchived { get; set; } = false;
            public int? Limit { get; set; }
        }

        public class CreateSubtaskParams
        {
            public string ParentCardIdOrTitle { get; set; }
            public string Title { get; set; }
            public string Description { get; set; }
            public string Category { get; set; }
            public int? Priority { get; set; }
            public string DueDate { get; set; }
            public string Column { get; set; }
            public int? ColorLabel { get; set; }
            public string[] Checklist { get; set; }
            public string[] Assignees { get; set; }
        }

        public class LinkSubtaskParams
        {
            public string ParentCardIdOrTitle { get; set; }
            public string ChildCardIdOrTitle { get; set; }
            public string ChecklistItemTitle { get; set; }
        }

        public class UnlinkSubtaskParams
        {
            public string ParentCardIdOrTitle { get; set; }
            public string ChildCardIdOrTitle { get; set; }
            public bool RemoveChecklistItem { get; set; }
        }

        public class GetSubtasksParams
        {
            public string CardIdOrTitle { get; set; }
        }

        public class CreateBoardParams
        {
            public string BoardName { get; set; }
        }

        public class DeleteBoardParams
        {
            public string BoardNameOrId { get; set; }
            public bool Confirm { get; set; }
        }

        public class CreateColumnParams
        {
            public string BoardNameOrId { get; set; }
            public string ColumnTitle { get; set; }
        }

        public class MoveColumnParams
        {
            public string ColumnNameOrId { get; set; }
            public string TargetBoard { get; set; }
            public string SourceBoard { get; set; }
            public int? TargetIndex { get; set; }
        }

        public class CloneColumnParams
        {
            public string ColumnNameOrId { get; set; }
            public string TargetBoard { get; set; }
            public string SourceBoard { get; set; }
            public string NewTitle { get; set; }
            public bool IncludeCards { get; set; } = true;
            public int? TargetIndex { get; set; }
        }

        public class ReorderColumnParams
        {
            public string ColumnNameOrId { get; set; }
            public string Direction { get; set; } // "left", "right", "first", "last", "up", "down"
            public int? TargetIndex { get; set; }
            public string Board { get; set; }
        }

        public class DeleteColumnParams
        {
            public string ColumnNameOrId { get; set; }
            public string BoardNameOrId { get; set; }
            public bool Confirm { get; set; }
        }

        public class DeleteAllTasksInColumnParams
        {
            public string BoardNameOrId { get; set; }
            public string ColumnNameOrId { get; set; }
            public bool Confirm { get; set; }
        }

        public class MoveAllTasksBetweenBoardsParams
        {
            public string SourceBoardNameOrId { get; set; }
            public string TargetBoardNameOrId { get; set; }
            public bool Confirm { get; set; }
        }

        public class SubtaskItemParam
        {
            public string Title { get; set; }
            public string Description { get; set; }
            public string Category { get; set; }
            public int Priority { get; set; }
            public string DueDate { get; set; }
            public string Column { get; set; }
            public string[] Checklist { get; set; }
        }

        public class CreateEpicHierarchyParams
        {
            public string Board { get; set; }
            public string Column { get; set; } = "📋 To Do";
            public string Title { get; set; }
            public string Description { get; set; }
            public string Category { get; set; }
            public int Priority { get; set; }
            public string DueDate { get; set; }
            public int ColorLabel { get; set; }
            public SubtaskItemParam[] Subtasks { get; set; }
        }

        public class BulkTaskItem
        {
            public string Title { get; set; }
            public string Description { get; set; }
            public string Column { get; set; }
            public string Category { get; set; }
            public int? Priority { get; set; }
            public string[] Checklist { get; set; }
            public string[] Assignees { get; set; }
            public int? ColorLabel { get; set; }
        }

        public class BulkCreateTasksParams
        {
            public string Board { get; set; }
            public string DefaultColumn { get; set; } = "📋 To Do";
            public BulkTaskItem[] Tasks { get; set; }
        }

        public class ConvertChecklistParams
        {
            public string CardIdOrTitle { get; set; }
            public string Column { get; set; }
            public string Category { get; set; }
        }

        public class GenerateDevlogParams
        {
            public string BoardName { get; set; }
            public string Title { get; set; } = "Sprint Devlog";
            public string FolderName { get; set; } = "Devlogs";
            public bool CreateQuickNote { get; set; } = true;
            public string[] Tags { get; set; }
        }

        public class CloneTaskParams
        {
            public string CardIdOrTitle { get; set; }
            public string TargetColumn { get; set; }
            public string TargetBoard { get; set; }
            public string SourceBoard { get; set; }
            public string NewTitle { get; set; }
            public int? TargetIndex { get; set; }
        }

        public class BulkMoveItem
        {
            public string CardIdOrTitle { get; set; }
            public string TargetColumn { get; set; }
            public string TargetBoard { get; set; }
            public string SourceBoard { get; set; }
        }

        public class BulkMoveTasksParams
        {
            public string DefaultTargetColumn { get; set; }
            public string DefaultTargetBoard { get; set; }
            public string DefaultSourceBoard { get; set; }
            public BulkMoveItem[] Moves { get; set; }
            public string[] CardIdsOrTitles { get; set; }
            public string[] Tasks { get; set; }
        }

        public class BulkCloneItem
        {
            public string CardIdOrTitle { get; set; }
            public string TargetColumn { get; set; }
            public string TargetBoard { get; set; }
            public string SourceBoard { get; set; }
            public string NewTitle { get; set; }
        }

        public class BulkCloneTasksParams
        {
            public string DefaultTargetColumn { get; set; }
            public string DefaultTargetBoard { get; set; }
            public string DefaultSourceBoard { get; set; }
            public BulkCloneItem[] Clones { get; set; }
            public string[] CardIdsOrTitles { get; set; }
            public string[] Tasks { get; set; }
        }

        public class BulkUpdateItem
        {
            public string CardIdOrTitle { get; set; }
            public string NewTitle { get; set; }
            public string NewDescription { get; set; }
            public string NewCategory { get; set; }
            public int? NewPriority { get; set; }
            public string NewDueDate { get; set; }
            public int? NewColorLabel { get; set; }
            public bool? Completed { get; set; }
            public bool? Archived { get; set; }
            public string[] Assignees { get; set; }
        }

        public class BulkUpdateTasksParams
        {
            public string DefaultCategory { get; set; }
            public int? DefaultColorLabel { get; set; }
            public int? DefaultPriority { get; set; }
            public string[] DefaultAssignees { get; set; }
            public BulkUpdateItem[] Updates { get; set; }
            public string[] CardIdsOrTitles { get; set; }
            public string[] Tasks { get; set; }
        }

        public class BulkDeleteTasksParams
        {
            public string[] CardIdsOrTitles { get; set; }
            public string[] Tasks { get; set; }
            public bool Confirm { get; set; }
        }

        public class ReorderTaskParams
        {
            public string CardIdOrTitle { get; set; }
            public string Direction { get; set; } // "up", "down", "top", "bottom"
            public int? TargetIndex { get; set; }
            public string Board { get; set; }
        }

        public class SetActiveBoardParams
        {
            public string BoardNameOrId { get; set; }
        }

        public class SetFilterParams
        {
            public string Category { get; set; }
            public string Assignee { get; set; }
            public int? Priority { get; set; }
            public string PriorityName { get; set; }
            public string SearchQuery { get; set; }
            public bool ResetAll { get; set; }
        }

        public class GetFilterParams
        {
        }

        public class CreateCategoryParams
        {
            public string Name { get; set; }
            public int? ColorIndex { get; set; }
        }

        public class GetCategoriesParams
        {
        }

        public class DeleteCategoryParams
        {
            public string Category { get; set; }
            public bool Confirm { get; set; }
        }

        public class CreateAssigneeParams
        {
            public string Name { get; set; }
            public int? ColorIndex { get; set; }
            public int? BorderColorIndex { get; set; }
            public string ProfileImageGuid { get; set; }
        }

        public class GetAssigneesParams
        {
        }

        public class DeleteAssigneeParams
        {
            public string AssigneeIdOrName { get; set; }
            public bool Confirm { get; set; }
        }

        #endregion

        #region Tool Methods

#if ATM_NEXT_UNITYAI_MCP_PRESENT || ATM_NEXT_MCP_PRESENT
        [McpTool("atm_get_boards", "Returns all Kanban boards, columns, task cards, and their completion status in Awesome Task Manager.", Groups = new[] { "Task Manager/Board" })]
#endif
        public static object GetBoards()
        {
            var data = ATMNextService.GetData();
            return data.boards.Select(b => new
            {
                id = b.id,
                name = b.name,
                columns = b.columns.Select(col => new
                {
                    id = col.id,
                    title = col.title,
                    cards = col.cards.Select(c => new
                    {
                        id = c.id,
                        title = c.title,
                        description = c.description,
                        category = c.category,
                        priority = c.priority,
                        dueDate = c.dueDate,
                        completed = c.completed,
                        archived = c.archived,
                        isParent = c.checklistLinkedCardIds != null && c.checklistLinkedCardIds.Any(subId => !string.IsNullOrEmpty(subId)),
                        subtasksCount = c.checklistLinkedCardIds != null ? c.checklistLinkedCardIds.Count(subId => !string.IsNullOrEmpty(subId)) : 0,
                        linkedSubtaskCardIds = c.checklistLinkedCardIds != null ? c.checklistLinkedCardIds.Where(subId => !string.IsNullOrEmpty(subId)).ToList() : new List<string>(),
                        checklistCount = c.checklistItems?.Count ?? 0,
                        completedChecklists = c.checklistStates?.Count(s => s) ?? 0,
                        checklist = c.checklistItems?.Select((item, idx) => new
                        {
                            item,
                            done = idx < c.checklistStates.Count && c.checklistStates[idx],
                            linkedCardId = idx < c.checklistLinkedCardIds.Count ? c.checklistLinkedCardIds[idx] : string.Empty
                        }).ToList(),
                        linkedItemsCount = c.linkedItems?.Count ?? 0
                    }).ToList()
                }).ToList()
            }).ToList();
        }

#if ATM_NEXT_UNITYAI_MCP_PRESENT || ATM_NEXT_MCP_PRESENT
        [McpTool("atm_create_task", "Creates a new task card on a specified board and column in Awesome Task Manager.", Groups = new[] { "Task Manager/Card" })]
#endif
        public static object CreateTask(CreateTaskParams p)
        {
            if (p == null || string.IsNullOrWhiteSpace(p.Title))
                return new { success = false, message = "Task title is required." };

            var card = ATMNextService.CreateCard(
                p.Board,
                p.Column,
                p.Title,
                p.Description ?? "",
                p.Category ?? "",
                p.Priority,
                p.DueDate ?? "",
                p.ColorLabel,
                p.Checklist != null ? p.Checklist.ToList() : null,
                p.Assignees != null ? p.Assignees.ToList() : null
            );

            if (card == null)
                return new { success = false, message = "Failed to create task card." };

            return new
            {
                success = true,
                message = $"Created task '{card.title}' successfully.",
                cardId = card.id,
                title = card.title,
                category = card.category,
                priority = card.priority
            };
        }

#if ATM_NEXT_UNITYAI_MCP_PRESENT || ATM_NEXT_MCP_PRESENT
        [McpTool("atm_update_task", "Updates an existing task card in Awesome Task Manager.", Groups = new[] { "Task Manager/Card" })]
#endif
        public static object UpdateTask(UpdateTaskParams p)
        {
            if (p == null || string.IsNullOrWhiteSpace(p.CardIdOrTitle))
                return new { success = false, message = "CardIdOrTitle is required." };

            var card = ATMNextService.UpdateCard(
                p.CardIdOrTitle,
                p.NewTitle,
                p.NewDescription,
                p.NewCategory,
                p.NewPriority,
                p.NewDueDate,
                p.NewColorLabel,
                p.Completed,
                p.Archived,
                p.Assignees != null ? new List<string>(p.Assignees) : null
            );

            if (card == null)
                return new { success = false, message = $"Task '{p.CardIdOrTitle}' not found." };

            return new
            {
                success = true,
                message = $"Updated task '{card.title}' successfully.",
                cardId = card.id,
                title = card.title,
                completed = card.completed
            };
        }

#if ATM_NEXT_UNITYAI_MCP_PRESENT || ATM_NEXT_MCP_PRESENT
        [McpTool("atm_move_task", "Moves a task card to another column or board (e.g., from 'To Do' to 'Done').", Groups = new[] { "Task Manager/Card" })]
#endif
        public static object MoveTask(MoveTaskParams p)
        {
            if (p == null || string.IsNullOrWhiteSpace(p.CardIdOrTitle) || string.IsNullOrWhiteSpace(p.TargetColumn))
                return new { success = false, message = "CardIdOrTitle and TargetColumn are required." };

            bool moved = ATMNextService.MoveCard(p.CardIdOrTitle, p.TargetColumn, p.TargetBoard);
            if (!moved)
                return new { success = false, message = $"Failed to move task '{p.CardIdOrTitle}' to column '{p.TargetColumn}'." };

            return new { success = true, message = $"Moved task '{p.CardIdOrTitle}' to '{p.TargetColumn}'." };
        }

#if ATM_NEXT_UNITYAI_MCP_PRESENT || ATM_NEXT_MCP_PRESENT
        [McpTool("atm_delete_task", "Deletes a task card from Awesome Task Manager.", Groups = new[] { "Task Manager/Card" })]
#endif
        public static object DeleteTask(DeleteTaskParams p)
        {
            if (p == null || string.IsNullOrWhiteSpace(p.CardIdOrTitle))
                return new { success = false, message = "CardIdOrTitle is required." };

            if (!p.Confirm)
                return new { success = false, message = $"Are you sure you want to delete task '{p.CardIdOrTitle}'? This action cannot be undone. Please call again with 'confirm: true' to proceed." };

            bool deleted = ATMNextService.DeleteCard(p.CardIdOrTitle);
            return new { success = deleted, message = deleted ? $"Deleted task '{p.CardIdOrTitle}'." : "Task not found." };
        }

#if ATM_NEXT_UNITYAI_MCP_PRESENT || ATM_NEXT_MCP_PRESENT
        [McpTool("atm_manage_checklist", "Adds, toggles, or removes checklist items on a task card.", Groups = new[] { "Task Manager/Checklist" })]
#endif
        public static object ManageChecklist(ManageChecklistParams p)
        {
            if (p == null || string.IsNullOrWhiteSpace(p.CardIdOrTitle))
                return new { success = false, message = "CardIdOrTitle is required." };

            string action = (p.Action ?? "add").ToLowerInvariant();

            if (action == "add")
            {
                if (string.IsNullOrWhiteSpace(p.ItemText))
                    return new { success = false, message = "ItemText is required for adding checklist items." };

                bool added = ATMNextService.AddChecklistItem(p.CardIdOrTitle, p.ItemText);
                return new { success = added, message = added ? $"Added checklist item to '{p.CardIdOrTitle}'." : "Card not found." };
            }
            else if (action == "toggle" || action == "check")
            {
                bool toggled = ATMNextService.SetChecklistItemState(p.CardIdOrTitle, p.ItemIndex, p.IsChecked);
                return new { success = toggled, message = toggled ? $"Updated checklist item {p.ItemIndex} state to {p.IsChecked}." : "Failed to update item." };
            }
            else if (action == "remove" || action == "delete")
            {
                if (!p.Confirm)
                    return new { success = false, message = $"Are you sure you want to remove checklist item {p.ItemIndex} from '{p.CardIdOrTitle}'? Please call again with 'confirm: true' to proceed." };

                bool removed = ATMNextService.RemoveChecklistItem(p.CardIdOrTitle, p.ItemIndex);
                return new { success = removed, message = removed ? $"Removed checklist item {p.ItemIndex}." : "Failed to remove item." };
            }

            return new { success = false, message = $"Unknown action '{p.Action}'. Supported: add, toggle, remove." };
        }

#if ATM_NEXT_UNITYAI_MCP_PRESENT || ATM_NEXT_MCP_PRESENT
        [McpTool("atm_get_notes", "Retrieves Quick Notes and folders from Awesome Task Manager.", Groups = new[] { "Task Manager/Notes" })]
#endif
        public static object GetNotes()
        {
            var data = ATMNextService.GetData();
            return new
            {
                folders = data.noteFolders?.Select(f => new { id = f.id, name = f.name }).ToList(),
                notes = data.notes?.Select(n => new
                {
                    id = n.id,
                    title = n.title,
                    content = n.content,
                    folderId = n.folderId,
                    tags = n.tags,
                    pinned = n.pinned,
                    modifiedDate = n.modifiedDate
                }).ToList()
            };
        }

#if ATM_NEXT_UNITYAI_MCP_PRESENT || ATM_NEXT_MCP_PRESENT
        [McpTool("atm_create_note", "Creates a new Markdown Quick Note in Awesome Task Manager.", Groups = new[] { "Task Manager/Notes" })]
#endif
        public static object CreateNote(CreateNoteParams p)
        {
            if (p == null || string.IsNullOrWhiteSpace(p.Title))
                return new { success = false, message = "Note title is required." };

            var note = ATMNextService.CreateNote(
                p.Title,
                p.Content ?? "",
                p.FolderName ?? "AI Notes",
                p.Tags != null ? p.Tags.ToList() : null,
                p.Pinned,
                p.ColorIndex
            );

            if (note == null)
                return new { success = false, message = "Failed to create note." };

            return new
            {
                success = true,
                message = $"Created note '{note.title}' in folder '{p.FolderName}'.",
                noteId = note.id,
                title = note.title
            };
        }

#if ATM_NEXT_UNITYAI_MCP_PRESENT || ATM_NEXT_MCP_PRESENT
        [McpTool("atm_update_note", "Updates an existing Quick Note in Awesome Task Manager.", Groups = new[] { "Task Manager/Notes" })]
#endif
        public static object UpdateNote(UpdateNoteParams p)
        {
            if (p == null || string.IsNullOrWhiteSpace(p.NoteIdOrTitle))
                return new { success = false, message = "NoteIdOrTitle is required." };

            var note = ATMNextService.UpdateNote(
                p.NoteIdOrTitle,
                p.NewTitle,
                p.NewContent,
                p.NewFolderName,
                p.NewTags != null ? p.NewTags.ToList() : null,
                p.Pinned,
                p.ColorIndex
            );

            if (note == null)
                return new { success = false, message = $"Note '{p.NoteIdOrTitle}' not found." };

            return new { success = true, message = $"Updated note '{note.title}' successfully.", noteId = note.id };
        }

#if ATM_NEXT_UNITYAI_MCP_PRESENT || ATM_NEXT_MCP_PRESENT
        [McpTool("atm_delete_note", "Deletes a Quick Note from Awesome Task Manager.", Groups = new[] { "Task Manager/Notes" })]
#endif
        public static object DeleteNote(DeleteNoteParams p)
        {
            if (p == null || string.IsNullOrWhiteSpace(p.NoteIdOrTitle))
                return new { success = false, message = "NoteIdOrTitle is required." };

            if (!p.Confirm)
                return new { success = false, message = $"Are you sure you want to delete note '{p.NoteIdOrTitle}'? This action cannot be undone. Please call again with 'confirm: true' to proceed." };

            bool deleted = ATMNextService.DeleteNote(p.NoteIdOrTitle);
            return new { success = deleted, message = deleted ? $"Deleted note '{p.NoteIdOrTitle}'." : "Note not found." };
        }

#if ATM_NEXT_UNITYAI_MCP_PRESENT || ATM_NEXT_MCP_PRESENT
        [McpTool("atm_bulk_create_notes", "Creates multiple Markdown Quick Notes in bulk across folders in a single atomic batch.", Groups = new[] { "Task Manager/Notes" })]
#endif
        public static object BulkCreateNotes(BulkCreateNotesParams p)
        {
            if (p == null || p.Notes == null || p.Notes.Length == 0)
                return new { success = false, message = "No notes provided to create." };

            var items = p.Notes.Select(n => new ATMNextService.BulkCreateNoteItem
            {
                title = n.Title,
                content = n.Content,
                folderName = n.FolderName,
                tags = n.Tags != null ? new List<string>(n.Tags) : null,
                pinned = n.Pinned,
                colorIndex = n.ColorIndex
            });

            var (successCount, totalCount, results) = ATMNextService.BulkCreateNotes(
                items,
                p.DefaultFolderName ?? "AI Notes",
                p.DefaultTags != null ? new List<string>(p.DefaultTags) : null
            );

            return new
            {
                success = successCount > 0,
                createdCount = successCount,
                totalRequested = totalCount,
                message = $"Bulk note creation completed: {successCount}/{totalCount} notes created.",
                results = results.Select(r => new
                {
                    title = r.title,
                    noteId = r.noteId,
                    success = r.success,
                    message = r.message
                }).ToList()
            };
        }

#if ATM_NEXT_UNITYAI_MCP_PRESENT || ATM_NEXT_MCP_PRESENT
        [McpTool("atm_bulk_update_notes", "Updates multiple Quick Notes in bulk (content, folder, tags, colors, pinned status) in a single atomic batch.", Groups = new[] { "Task Manager/Notes" })]
#endif
        public static object BulkUpdateNotes(BulkUpdateNotesParams p)
        {
            if (p == null)
                return new { success = false, message = "Parameters required for bulk note update." };

            var items = new List<ATMNextService.BulkUpdateNoteItem>();

            if (p.Updates != null && p.Updates.Length > 0)
            {
                foreach (var u in p.Updates)
                {
                    if (u == null || string.IsNullOrWhiteSpace(u.NoteIdOrTitle)) continue;
                    items.Add(new ATMNextService.BulkUpdateNoteItem
                    {
                        noteIdOrTitle = u.NoteIdOrTitle,
                        newTitle = u.NewTitle,
                        newContent = u.NewContent,
                        appendContent = u.AppendContent,
                        newFolderName = u.NewFolderName,
                        newTags = u.NewTags != null ? new List<string>(u.NewTags) : null,
                        addTags = u.AddTags != null ? new List<string>(u.AddTags) : null,
                        removeTags = u.RemoveTags != null ? new List<string>(u.RemoveTags) : null,
                        pinned = u.Pinned,
                        colorIndex = u.ColorIndex
                    });
                }
            }

            var noteList = p.NoteIdsOrTitles ?? p.Notes;
            if (noteList != null && noteList.Length > 0)
            {
                foreach (var titleOrId in noteList)
                {
                    if (string.IsNullOrWhiteSpace(titleOrId)) continue;
                    if (!items.Any(x => string.Equals(x.noteIdOrTitle, titleOrId, StringComparison.OrdinalIgnoreCase)))
                    {
                        items.Add(new ATMNextService.BulkUpdateNoteItem
                        {
                            noteIdOrTitle = titleOrId
                        });
                    }
                }
            }

            if (items.Count == 0)
                return new { success = false, message = "No valid notes or update specifications provided." };

            var (successCount, totalCount, results) = ATMNextService.BulkUpdateNotes(
                items,
                p.DefaultFolderName,
                p.DefaultTags != null ? new List<string>(p.DefaultTags) : null,
                p.AddTags != null ? new List<string>(p.AddTags) : null,
                p.RemoveTags != null ? new List<string>(p.RemoveTags) : null,
                p.DefaultPinned,
                p.DefaultColorIndex,
                p.AppendContent
            );

            return new
            {
                success = successCount > 0,
                updatedCount = successCount,
                totalRequested = totalCount,
                message = $"Bulk note update completed: {successCount}/{totalCount} notes updated.",
                results = results.Select(r => new
                {
                    noteIdOrTitle = r.noteIdOrTitle,
                    noteTitle = r.noteTitle,
                    noteId = r.noteId,
                    success = r.success,
                    message = r.message
                }).ToList()
            };
        }

#if ATM_NEXT_UNITYAI_MCP_PRESENT || ATM_NEXT_MCP_PRESENT
        [McpTool("atm_bulk_move_notes", "Moves multiple Quick Notes to a target folder in a single atomic batch operation.", Groups = new[] { "Task Manager/Notes" })]
#endif
        public static object BulkMoveNotes(BulkMoveNotesParams p)
        {
            if (p == null)
                return new { success = false, message = "Parameters required for bulk note move." };

            var items = new List<ATMNextService.BulkMoveNoteItem>();

            if (p.Moves != null && p.Moves.Length > 0)
            {
                foreach (var m in p.Moves)
                {
                    if (m == null || string.IsNullOrWhiteSpace(m.NoteIdOrTitle)) continue;
                    items.Add(new ATMNextService.BulkMoveNoteItem
                    {
                        noteIdOrTitle = m.NoteIdOrTitle,
                        targetFolderName = m.TargetFolderName
                    });
                }
            }

            var noteList = p.NoteIdsOrTitles ?? p.Notes;
            if (noteList != null && noteList.Length > 0)
            {
                foreach (var titleOrId in noteList)
                {
                    if (string.IsNullOrWhiteSpace(titleOrId)) continue;
                    if (!items.Any(x => string.Equals(x.noteIdOrTitle, titleOrId, StringComparison.OrdinalIgnoreCase)))
                    {
                        items.Add(new ATMNextService.BulkMoveNoteItem
                        {
                            noteIdOrTitle = titleOrId,
                            targetFolderName = p.TargetFolderName
                        });
                    }
                }
            }

            if (items.Count == 0)
                return new { success = false, message = "No valid notes specified to move." };

            var (successCount, totalCount, results) = ATMNextService.BulkMoveNotes(
                items,
                p.TargetFolderName ?? ""
            );

            return new
            {
                success = successCount > 0,
                movedCount = successCount,
                totalRequested = totalCount,
                message = $"Bulk note move completed: {successCount}/{totalCount} notes moved.",
                results = results.Select(r => new
                {
                    noteIdOrTitle = r.noteIdOrTitle,
                    noteTitle = r.noteTitle,
                    noteId = r.noteId,
                    targetFolderName = r.targetFolderName,
                    success = r.success,
                    message = r.message
                }).ToList()
            };
        }

#if ATM_NEXT_UNITYAI_MCP_PRESENT || ATM_NEXT_MCP_PRESENT
        [McpTool("atm_bulk_delete_notes", "Deletes multiple Quick Notes in bulk (requires confirm: true).", Groups = new[] { "Task Manager/Notes" })]
#endif
        public static object BulkDeleteNotes(BulkDeleteNotesParams p)
        {
            if (p == null)
                return new { success = false, message = "Parameters required for bulk note deletion." };

            var noteList = p.NoteIdsOrTitles ?? p.Notes;
            if (noteList == null || noteList.Length == 0)
                return new { success = false, message = "No note IDs or titles specified to delete." };

            if (!p.Confirm)
                return new { success = false, message = $"Are you sure you want to delete {noteList.Length} notes? This action cannot be undone. Please call again with 'confirm: true' to proceed." };

            var (successCount, totalCount, results) = ATMNextService.BulkDeleteNotes(noteList);

            return new
            {
                success = successCount > 0,
                deletedCount = successCount,
                totalRequested = totalCount,
                message = $"Bulk note deletion completed: {successCount}/{totalCount} notes deleted.",
                results = results.Select(r => new
                {
                    noteIdOrTitle = r.noteIdOrTitle,
                    noteTitle = r.noteTitle,
                    noteId = r.noteId,
                    success = r.success,
                    message = r.message
                }).ToList()
            };
        }

#if ATM_NEXT_UNITYAI_MCP_PRESENT || ATM_NEXT_MCP_PRESENT
        [McpTool("atm_link_item", "Links a Quick Note, external URL, or Unity project asset to a task card.", Groups = new[] { "Task Manager/Link" })]
#endif
        public static object LinkItem(LinkItemParams p)
        {
            if (p == null || string.IsNullOrWhiteSpace(p.CardIdOrTitle) || string.IsNullOrWhiteSpace(p.Target))
                return new { success = false, message = "CardIdOrTitle and Target are required." };

            string type = (p.LinkType ?? "note").ToLowerInvariant();
            bool linked = false;

            if (type == "note")
            {
                linked = ATMNextService.LinkNoteToCard(p.CardIdOrTitle, p.Target);
            }
            else if (type == "url")
            {
                linked = ATMNextService.LinkUrlToCard(p.CardIdOrTitle, p.Target, p.DisplayName ?? "");
            }
            else if (type == "asset")
            {
                linked = ATMNextService.LinkAssetToCard(p.CardIdOrTitle, p.Target);
            }

            return new { success = linked, message = linked ? $"Successfully linked {type} to '{p.CardIdOrTitle}'." : $"Failed to link {type}." };
        }

#if ATM_NEXT_UNITYAI_MCP_PRESENT || ATM_NEXT_MCP_PRESENT
        [McpTool("atm_bulk_link_items", "Links multiple Quick Notes, external URLs, or Unity project assets to one or multiple task cards in a single atomic batch.", Groups = new[] { "Task Manager/Link" })]
#endif
        public static object BulkLinkItems(BulkLinkItemsParams p)
        {
            if (p == null)
                return new { success = false, message = "Parameters required for bulk item linking." };

            var entries = new List<ATMNextService.BulkLinkItemEntry>();

            if (p.Links != null && p.Links.Length > 0)
            {
                foreach (var l in p.Links)
                {
                    if (l == null) continue;
                    entries.Add(new ATMNextService.BulkLinkItemEntry
                    {
                        cardIdOrTitle = l.CardIdOrTitle,
                        target = l.Target,
                        linkType = l.LinkType,
                        displayName = l.DisplayName
                    });
                }
            }

            var cardList = new List<string>();
            if (!string.IsNullOrWhiteSpace(p.CardIdOrTitle)) cardList.Add(p.CardIdOrTitle);
            var extraCards = p.CardIdsOrTitles ?? p.Tasks;
            if (extraCards != null)
            {
                foreach (var c in extraCards)
                {
                    if (!string.IsNullOrWhiteSpace(c) && !cardList.Contains(c))
                        cardList.Add(c);
                }
            }

            var targetList = new List<string>();
            if (!string.IsNullOrWhiteSpace(p.Target)) targetList.Add(p.Target);
            if (p.Targets != null)
            {
                foreach (var t in p.Targets)
                {
                    if (!string.IsNullOrWhiteSpace(t) && !targetList.Contains(t))
                        targetList.Add(t);
                }
            }

            if (cardList.Count > 0 && targetList.Count > 0)
            {
                foreach (var c in cardList)
                {
                    foreach (var t in targetList)
                    {
                        if (!entries.Any(e => string.Equals(e.cardIdOrTitle, c, StringComparison.OrdinalIgnoreCase) && string.Equals(e.target, t, StringComparison.OrdinalIgnoreCase)))
                        {
                            entries.Add(new ATMNextService.BulkLinkItemEntry
                            {
                                cardIdOrTitle = c,
                                target = t,
                                linkType = p.LinkType ?? "note",
                                displayName = p.DisplayName
                            });
                        }
                    }
                }
            }

            if (entries.Count == 0)
                return new { success = false, message = "No valid links specified to create." };

            var (successCount, totalCount, results) = ATMNextService.BulkLinkItems(
                entries,
                p.CardIdOrTitle,
                p.LinkType ?? "note"
            );

            return new
            {
                success = successCount > 0,
                linkedCount = successCount,
                totalRequested = totalCount,
                message = $"Bulk item linking completed: {successCount}/{totalCount} links established.",
                results = results.Select(r => new
                {
                    cardIdOrTitle = r.cardIdOrTitle,
                    cardTitle = r.cardTitle,
                    cardId = r.cardId,
                    target = r.target,
                    linkType = r.linkType,
                    success = r.success,
                    message = r.message
                }).ToList()
            };
        }

#if ATM_NEXT_UNITYAI_MCP_PRESENT || ATM_NEXT_MCP_PRESENT
        [McpTool("atm_search", "Searches across all task cards, checklists, categories, assignees, boards, columns, quick notes, and ATM tools.", Groups = new[] { "Task Manager/Search" })]
#endif
        public static object Search(SearchParams p)
        {
            p ??= new SearchParams();
            string effectiveQuery = p.Query ?? p.Keyword ?? p.Search ?? p.Term ?? p.Text ?? "";

            return ATMNextService.SearchDetailed(
                query: effectiveQuery,
                boardNameOrId: p.Board,
                columnNameOrId: p.Column,
                category: p.Category,
                assignee: p.Assignee,
                scope: p.Scope ?? "all",
                includeCompleted: p.IncludeCompleted,
                includeArchived: p.IncludeArchived,
                limit: p.Limit
            );
        }

#if ATM_NEXT_UNITYAI_MCP_PRESENT || ATM_NEXT_MCP_PRESENT
        [McpTool("atm_get_project_summary", "Generates high-level metrics on task completion, sprint health, and notes.", Groups = new[] { "Task Manager/Analytics" })]
#endif
        public static object GetProjectSummary()
        {
            return ATMNextService.GetProjectSummary();
        }

#if ATM_NEXT_UNITYAI_MCP_PRESENT || ATM_NEXT_MCP_PRESENT
        [McpTool("atm_create_subtask", "Creates a new child task card and links it as a subtask in the parent card's checklist.", Groups = new[] { "Task Manager/Card", "Task Manager/Subtasks" })]
#endif
        public static object CreateSubtask(CreateSubtaskParams p)
        {
            if (p == null || string.IsNullOrWhiteSpace(p.ParentCardIdOrTitle) || string.IsNullOrWhiteSpace(p.Title))
                return new { success = false, message = "ParentCardIdOrTitle and Title are required." };

            var childCard = ATMNextService.CreateSubtask(
                p.ParentCardIdOrTitle,
                p.Title,
                p.Description ?? "",
                p.Column,
                p.Category,
                p.Priority,
                p.DueDate,
                p.ColorLabel,
                p.Checklist != null ? p.Checklist.ToList() : null,
                p.Assignees != null ? p.Assignees.ToList() : null
            );

            if (childCard == null)
                return new { success = false, message = $"Failed to create subtask: parent card '{p.ParentCardIdOrTitle}' not found." };

            return new
            {
                success = true,
                message = $"Created subtask '{childCard.title}' linked to parent '{p.ParentCardIdOrTitle}'.",
                cardId = childCard.id,
                title = childCard.title,
                parentCardId = p.ParentCardIdOrTitle
            };
        }

#if ATM_NEXT_UNITYAI_MCP_PRESENT || ATM_NEXT_MCP_PRESENT
        [McpTool("atm_link_subtask", "Links an existing task card as a child subtask under a parent card.", Groups = new[] { "Task Manager/Card", "Task Manager/Subtasks" })]
#endif
        public static object LinkSubtask(LinkSubtaskParams p)
        {
            if (p == null || string.IsNullOrWhiteSpace(p.ParentCardIdOrTitle) || string.IsNullOrWhiteSpace(p.ChildCardIdOrTitle))
                return new { success = false, message = "ParentCardIdOrTitle and ChildCardIdOrTitle are required." };

            bool linked = ATMNextService.LinkChildTask(p.ParentCardIdOrTitle, p.ChildCardIdOrTitle, p.ChecklistItemTitle);
            if (!linked)
                return new { success = false, message = "Failed to link subtask: verify that cards exist and linking does not create a circular dependency." };

            return new
            {
                success = true,
                message = $"Successfully linked child '{p.ChildCardIdOrTitle}' to parent '{p.ParentCardIdOrTitle}'."
            };
        }

#if ATM_NEXT_UNITYAI_MCP_PRESENT || ATM_NEXT_MCP_PRESENT
        [McpTool("atm_unlink_subtask", "Unlinks a child subtask card from its parent task card.", Groups = new[] { "Task Manager/Card", "Task Manager/Subtasks" })]
#endif
        public static object UnlinkSubtask(UnlinkSubtaskParams p)
        {
            if (p == null || string.IsNullOrWhiteSpace(p.ParentCardIdOrTitle) || string.IsNullOrWhiteSpace(p.ChildCardIdOrTitle))
                return new { success = false, message = "ParentCardIdOrTitle and ChildCardIdOrTitle are required." };

            bool unlinked = ATMNextService.UnlinkChildTask(p.ParentCardIdOrTitle, p.ChildCardIdOrTitle, p.RemoveChecklistItem);
            if (!unlinked)
                return new { success = false, message = "Failed to unlink subtask: link not found." };

            return new
            {
                success = true,
                message = $"Successfully unlinked child '{p.ChildCardIdOrTitle}' from parent '{p.ParentCardIdOrTitle}'."
            };
        }

#if ATM_NEXT_UNITYAI_MCP_PRESENT || ATM_NEXT_MCP_PRESENT
        [McpTool("atm_get_subtasks", "Returns hierarchical parent and child subtask relationships for a task card.", Groups = new[] { "Task Manager/Card", "Task Manager/Subtasks" })]
#endif
        public static object GetSubtasks(GetSubtasksParams p)
        {
            if (p == null || string.IsNullOrWhiteSpace(p.CardIdOrTitle))
                return new { success = false, message = "CardIdOrTitle is required." };

            var hierarchy = ATMNextService.GetTaskHierarchy(p.CardIdOrTitle);
            if (hierarchy == null || hierarchy.card == null)
                return new { success = false, message = $"Card '{p.CardIdOrTitle}' not found." };

            return new
            {
                success = true,
                cardId = hierarchy.card.id,
                title = hierarchy.card.title,
                isCompleted = hierarchy.card.completed,
                isParent = hierarchy.children.Count > 0,
                isSubtask = hierarchy.parents.Count > 0,
                totalSubtasks = hierarchy.totalSubtasks,
                completedSubtasks = hierarchy.completedSubtasks,
                completionPercent = hierarchy.completionPercent,
                parents = hierarchy.parents.Select(parent => new
                {
                    id = parent.id,
                    title = parent.title,
                    completed = parent.completed
                }).ToList(),
                subtasks = hierarchy.children.Select(child => new
                {
                    id = child.id,
                    title = child.title,
                    completed = child.completed,
                    category = child.category,
                    priority = child.priority
                }).ToList()
            };
        }

#if ATM_NEXT_UNITYAI_MCP_PRESENT || ATM_NEXT_MCP_PRESENT
        [McpTool("atm_create_epic_hierarchy", "Creates a high-level Epic / Parent task along with multiple linked child subtask cards in one atomic operation.", Groups = new[] { "Task Manager/Card", "Task Manager/Subtasks" })]
#endif
        public static object CreateEpicHierarchy(CreateEpicHierarchyParams p)
        {
            if (p == null || string.IsNullOrWhiteSpace(p.Title))
                return new { success = false, message = "Title is required for the Epic / Parent task." };

            var subtaskDefs = new List<ATMNextService.SubtaskDefinition>();
            if (p.Subtasks != null && p.Subtasks.Length > 0)
            {
                foreach (var s in p.Subtasks)
                {
                    if (s == null || string.IsNullOrWhiteSpace(s.Title)) continue;
                    subtaskDefs.Add(new ATMNextService.SubtaskDefinition
                    {
                        title = s.Title,
                        description = s.Description ?? "",
                        category = s.Category,
                        priority = s.Priority,
                        dueDate = s.DueDate,
                        column = s.Column,
                        checklist = s.Checklist != null ? s.Checklist.ToList() : null
                    });
                }
            }

            var (parent, children) = ATMNextService.CreateHierarchicalTask(
                p.Board,
                p.Column,
                p.Title,
                p.Description ?? "",
                p.Category ?? "Feature",
                p.Priority,
                subtaskDefs,
                p.DueDate,
                p.ColorLabel
            );

            if (parent == null)
                return new { success = false, message = "Failed to create hierarchical task." };

            return new
            {
                success = true,
                message = $"Created Epic '{parent.title}' with {children.Count} linked child subtasks.",
                parentId = parent.id,
                parentTitle = parent.title,
                subtasksCount = children.Count,
                subtasks = children.Select(c => new { id = c.id, title = c.title, category = c.category, priority = c.priority }).ToList()
            };
        }

#if ATM_NEXT_UNITYAI_MCP_PRESENT || ATM_NEXT_MCP_PRESENT
        [McpTool("atm_create_board", "Creates a new task board in Awesome Task Manager.", Groups = new[] { "Task Manager/Board" })]
#endif
        public static object CreateBoard(CreateBoardParams p)
        {
            if (p == null || string.IsNullOrWhiteSpace(p.BoardName))
                return new { success = false, message = "BoardName is required." };

            var board = ATMNextService.CreateBoard(p.BoardName);
            return new { success = board != null, message = board != null ? $"Created board '{board.name}'." : "Failed to create board." };
        }

#if ATM_NEXT_UNITYAI_MCP_PRESENT || ATM_NEXT_MCP_PRESENT
        [McpTool("atm_delete_board", "Deletes a task board from Awesome Task Manager.", Groups = new[] { "Task Manager/Board" })]
#endif
        public static object DeleteBoard(DeleteBoardParams p)
        {
            if (p == null || string.IsNullOrWhiteSpace(p.BoardNameOrId))
                return new { success = false, message = "BoardNameOrId is required." };

            if (!p.Confirm)
                return new { success = false, message = $"Are you sure you want to delete board '{p.BoardNameOrId}' and all its tasks? This action cannot be undone. Please call again with 'confirm: true' to proceed." };

            bool deleted = ATMNextService.DeleteBoard(p.BoardNameOrId);
            return new { success = deleted, message = deleted ? $"Deleted board '{p.BoardNameOrId}'." : "Failed to delete board. Ensure it exists and is not the last remaining board." };
        }

#if ATM_NEXT_UNITYAI_MCP_PRESENT || ATM_NEXT_MCP_PRESENT
        [McpTool("atm_create_column", "Creates a new Kanban column in a board.", Groups = new[] { "Task Manager/Column" })]
#endif
        public static object CreateColumn(CreateColumnParams p)
        {
            if (p == null || string.IsNullOrWhiteSpace(p.ColumnTitle))
                return new { success = false, message = "ColumnTitle is required." };

            var col = ATMNextService.CreateColumn(p.BoardNameOrId, p.ColumnTitle);
            return new { success = col != null, message = col != null ? $"Created column '{col.title}'." : "Failed to create column." };
        }

#if ATM_NEXT_UNITYAI_MCP_PRESENT || ATM_NEXT_MCP_PRESENT
        [McpTool("atm_move_column", "Moves a column to another board or moves/reorders it within a board.", Groups = new[] { "Task Manager/Board", "Task Manager/Column" })]
#endif
        public static object MoveColumn(MoveColumnParams p)
        {
            if (p == null || string.IsNullOrWhiteSpace(p.ColumnNameOrId))
                return new { success = false, message = "ColumnNameOrId is required." };

            bool success = ATMNextService.MoveColumn(p.ColumnNameOrId, p.TargetBoard, p.SourceBoard, p.TargetIndex ?? -1);
            return new
            {
                success = success,
                message = success
                    ? $"Moved column '{p.ColumnNameOrId}' successfully."
                    : $"Failed to move column '{p.ColumnNameOrId}'. Ensure the column and boards exist."
            };
        }

#if ATM_NEXT_UNITYAI_MCP_PRESENT || ATM_NEXT_MCP_PRESENT
        [McpTool("atm_clone_column", "Clones/duplicates a column (including or excluding its cards) to another board or within the same board.", Groups = new[] { "Task Manager/Board", "Task Manager/Column" })]
#endif
        public static object CloneColumn(CloneColumnParams p)
        {
            if (p == null || string.IsNullOrWhiteSpace(p.ColumnNameOrId))
                return new { success = false, message = "ColumnNameOrId is required." };

            var clonedCol = ATMNextService.CloneColumn(p.ColumnNameOrId, p.TargetBoard, p.SourceBoard, p.NewTitle, p.IncludeCards, p.TargetIndex ?? -1);
            return new
            {
                success = clonedCol != null,
                message = clonedCol != null
                    ? $"Successfully cloned column '{clonedCol.title}' with {clonedCol.cards?.Count ?? 0} cards."
                    : $"Failed to clone column '{p.ColumnNameOrId}'. Ensure the source column and target board exist.",
                columnId = clonedCol?.id,
                columnTitle = clonedCol?.title,
                cardsCount = clonedCol?.cards?.Count ?? 0
            };
        }

#if ATM_NEXT_UNITYAI_MCP_PRESENT || ATM_NEXT_MCP_PRESENT
        [McpTool("atm_reorder_column", "Reorders a column left, right, first, last, or to a specific index in a board.", Groups = new[] { "Task Manager/Board", "Task Manager/Column" })]
#endif
        public static object ReorderColumn(ReorderColumnParams p)
        {
            if (p == null || string.IsNullOrWhiteSpace(p.ColumnNameOrId))
                return new { success = false, message = "ColumnNameOrId is required." };

            bool success = ATMNextService.ReorderColumn(p.ColumnNameOrId, p.Direction, p.TargetIndex, p.Board);
            return new
            {
                success = success,
                message = success
                    ? $"Reordered column '{p.ColumnNameOrId}' successfully."
                    : $"Failed to reorder column '{p.ColumnNameOrId}'."
            };
        }

#if ATM_NEXT_UNITYAI_MCP_PRESENT || ATM_NEXT_MCP_PRESENT
        [McpTool("atm_delete_column", "Deletes a column and all its task cards from a board.", Groups = new[] { "Task Manager/Board", "Task Manager/Column" })]
#endif
        public static object DeleteColumn(DeleteColumnParams p)
        {
            if (p == null || string.IsNullOrWhiteSpace(p.ColumnNameOrId))
                return new { success = false, message = "ColumnNameOrId is required." };

            if (!p.Confirm)
                return new { success = false, message = $"Are you sure you want to delete column '{p.ColumnNameOrId}' and all its tasks? This action cannot be undone. Please call again with 'confirm: true' to proceed." };

            bool success = ATMNextService.DeleteColumn(p.BoardNameOrId, p.ColumnNameOrId);
            return new
            {
                success = success,
                message = success
                    ? $"Deleted column '{p.ColumnNameOrId}'."
                    : $"Failed to delete column '{p.ColumnNameOrId}'."
            };
        }

#if ATM_NEXT_UNITYAI_MCP_PRESENT || ATM_NEXT_MCP_PRESENT
        [McpTool("atm_delete_all_tasks_in_column", "Deletes all task cards within a specific column.", Groups = new[] { "Task Manager/Card", "Task Manager/Column" })]
#endif
        public static object DeleteAllTasksInColumn(DeleteAllTasksInColumnParams p)
        {
            if (p == null || string.IsNullOrWhiteSpace(p.ColumnNameOrId))
                return new { success = false, message = "ColumnNameOrId is required." };

            if (!p.Confirm)
                return new { success = false, message = $"Are you sure you want to delete ALL tasks in column '{p.ColumnNameOrId}'? This action cannot be undone. Please call again with 'confirm: true' to proceed." };

            bool success = ATMNextService.DeleteAllCardsInColumn(p.BoardNameOrId, p.ColumnNameOrId);
            return new { success = success, message = success ? $"Deleted all tasks in column '{p.ColumnNameOrId}'." : "Failed to delete tasks." };
        }

#if ATM_NEXT_UNITYAI_MCP_PRESENT || ATM_NEXT_MCP_PRESENT
        [McpTool("atm_move_all_tasks_between_boards", "Moves all tasks from one board to another, preserving column names where possible.", Groups = new[] { "Task Manager/Board", "Task Manager/Card" })]
#endif
        public static object MoveAllTasksBetweenBoards(MoveAllTasksBetweenBoardsParams p)
        {
            if (p == null || string.IsNullOrWhiteSpace(p.SourceBoardNameOrId) || string.IsNullOrWhiteSpace(p.TargetBoardNameOrId))
                return new { success = false, message = "SourceBoardNameOrId and TargetBoardNameOrId are required." };

            if (!p.Confirm)
                return new { success = false, message = $"Are you sure you want to move ALL tasks from board '{p.SourceBoardNameOrId}' to board '{p.TargetBoardNameOrId}'? Please call again with 'confirm: true' to proceed." };

            bool success = ATMNextService.MoveAllCardsBetweenBoards(p.SourceBoardNameOrId, p.TargetBoardNameOrId);
            return new { success = success, message = success ? $"Successfully moved all tasks to '{p.TargetBoardNameOrId}'." : "Failed to move tasks." };
        }

#if ATM_NEXT_UNITYAI_MCP_PRESENT || ATM_NEXT_MCP_PRESENT
        [McpTool("atm_bulk_create_tasks", "Creates multiple task cards in bulk from an array of task objects (ideal for GDD feature lists or sprint backlog imports).", Groups = new[] { "Task Manager/Card" })]
#endif
        public static object BulkCreateTasks(BulkCreateTasksParams p)
        {
            if (p == null || p.Tasks == null || p.Tasks.Length == 0)
                return new { success = false, message = "Tasks array is required." };

            var createdCards = new List<object>();
            foreach (var t in p.Tasks)
            {
                if (t == null || string.IsNullOrWhiteSpace(t.Title)) continue;
                string col = string.IsNullOrWhiteSpace(t.Column) ? p.DefaultColumn : t.Column;
                var card = ATMNextService.CreateCard(
                    p.Board,
                    col,
                    t.Title,
                    t.Description ?? "",
                    t.Category ?? "",
                    t.Priority ?? 0,
                    null,
                    t.ColorLabel ?? 0,
                    t.Checklist != null ? t.Checklist.ToList() : null,
                    t.Assignees != null ? t.Assignees.ToList() : null
                );
                if (card != null)
                {
                    createdCards.Add(new { id = card.id, title = card.title, column = col });
                }
            }

            return new
            {
                success = true,
                message = $"Successfully created {createdCards.Count} tasks in bulk.",
                count = createdCards.Count,
                tasks = createdCards
            };
        }

#if ATM_NEXT_UNITYAI_MCP_PRESENT || ATM_NEXT_MCP_PRESENT
        [McpTool("atm_convert_checklist_to_subtasks", "Converts all plain checklist items on a task card into linked child subtask cards.", Groups = new[] { "Task Manager/Card" })]
#endif
        public static object ConvertChecklistToSubtasks(ConvertChecklistParams p)
        {
            if (p == null || string.IsNullOrWhiteSpace(p.CardIdOrTitle))
                return new { success = false, message = "CardIdOrTitle is required." };

            var parent = ATMNextService.GetCard(p.CardIdOrTitle);
            if (parent == null)
                return new { success = false, message = $"Card '{p.CardIdOrTitle}' not found." };

            int convertedCount = 0;
            var createdSubtaskIds = new List<string>();

            if (parent.checklistItems != null)
            {
                for (int i = 0; i < parent.checklistItems.Count; i++)
                {
                    string itemText = parent.checklistItems[i];
                    if (string.IsNullOrWhiteSpace(itemText)) continue;

                    // If already linked, skip
                    if (parent.checklistLinkedCardIds != null && i < parent.checklistLinkedCardIds.Count && !string.IsNullOrEmpty(parent.checklistLinkedCardIds[i]))
                        continue;

                    var child = ATMNextService.CreateSubtask(
                        parent.id,
                        itemText.Trim(),
                        "",
                        p.Column,
                        !string.IsNullOrEmpty(p.Category) ? p.Category : parent.category,
                        parent.priority
                    );

                    if (child != null)
                    {
                        convertedCount++;
                        createdSubtaskIds.Add(child.id);
                    }
                }
            }

            return new
            {
                success = true,
                message = $"Converted {convertedCount} checklist items into linked child subtasks for card '{parent.title}'.",
                convertedCount = convertedCount,
                subtaskIds = createdSubtaskIds
            };
        }

#if ATM_NEXT_UNITYAI_MCP_PRESENT || ATM_NEXT_MCP_PRESENT
        [McpTool("atm_generate_devlog", "Generates a structured Markdown devlog / release notes summary from completed ('Done') cards and optionally saves it as a Quick Note.", Groups = new[] { "Task Manager/Notes" })]
#endif
        public static object GenerateDevlog(GenerateDevlogParams p = null)
        {
            p ??= new GenerateDevlogParams();
            var cards = ATMNextService.GetCards(p.BoardName);
            var doneCards = cards.Where(c => c.completed || (c.checklistStates != null && c.checklistStates.Count > 0 && c.checklistStates.All(s => s))).ToList();

            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"# 🚀 {p.Title}");
            sb.AppendLine($"*Generated on {System.DateTime.Now:yyyy-MM-dd HH:mm}*");
            sb.AppendLine();
            sb.AppendLine("---");
            sb.AppendLine();
            sb.AppendLine("## 📊 Sprint Overview");
            sb.AppendLine($"- **Total Cards in Project**: {cards.Count}");
            sb.AppendLine($"- **Completed Cards**: {doneCards.Count}");
            sb.AppendLine($"- **Completion Rate**: {(cards.Count > 0 ? (doneCards.Count * 100 / cards.Count) : 0)}%");
            sb.AppendLine();
            sb.AppendLine("## ✅ Completed Features & Fixes");

            if (doneCards.Count == 0)
            {
                sb.AppendLine("*(No completed tasks found in Done column)*");
            }
            else
            {
                var grouped = doneCards.GroupBy(c => string.IsNullOrEmpty(c.category) ? "General" : c.category);
                foreach (var grp in grouped)
                {
                    sb.AppendLine($"### 🏷 {grp.Key}");
                    foreach (var card in grp)
                    {
                        sb.AppendLine($"- **{card.title}**");
                        if (!string.IsNullOrWhiteSpace(card.description))
                        {
                            sb.AppendLine($"  > {card.description.Replace("\n", " ")}");
                        }
                        if (card.checklistItems != null && card.checklistItems.Count > 0)
                        {
                            for (int ci = 0; ci < card.checklistItems.Count; ci++)
                            {
                                bool isDone = ci < card.checklistStates.Count && card.checklistStates[ci];
                                sb.AppendLine($"  - [{(isDone ? "x" : " ")}] {card.checklistItems[ci]}");
                            }
                        }
                    }
                    sb.AppendLine();
                }
            }

            string markdown = sb.ToString();
            QuickNote note = null;
            if (p.CreateQuickNote)
            {
                note = ATMNextService.CreateNote(
                    p.Title,
                    markdown,
                    p.FolderName ?? "Devlogs",
                    p.Tags != null ? p.Tags.ToList() : new List<string> { "Devlog", "ReleaseNotes", "AI-Generated" }
                );
            }

            return new
            {
                success = true,
                message = note != null ? $"Generated devlog and created Quick Note '{note.title}'." : "Generated devlog markdown successfully.",
                devlogMarkdown = markdown,
                noteId = note?.id,
                completedTaskCount = doneCards.Count
            };
        }

#if ATM_NEXT_UNITYAI_MCP_PRESENT || ATM_NEXT_MCP_PRESENT
        [McpTool("atm_bulk_move_tasks", "Moves multiple task cards across columns or boards in a single atomic batch operation.", Groups = new[] { "Task Manager/Card" })]
#endif
        public static object BulkMoveTasks(BulkMoveTasksParams p)
        {
            if (p == null)
                return new { success = false, message = "Parameters required for bulk move." };

            var items = new List<ATMNextService.BulkMoveItem>();

            if (p.Moves != null && p.Moves.Length > 0)
            {
                foreach (var m in p.Moves)
                {
                    if (m != null && !string.IsNullOrWhiteSpace(m.CardIdOrTitle))
                    {
                        items.Add(new ATMNextService.BulkMoveItem
                        {
                            cardIdOrTitle = m.CardIdOrTitle,
                            targetColumn = m.TargetColumn,
                            targetBoard = m.TargetBoard,
                            sourceBoard = m.SourceBoard
                        });
                    }
                }
            }

            var cardList = p.CardIdsOrTitles ?? p.Tasks;
            if (cardList != null && cardList.Length > 0)
            {
                foreach (var c in cardList)
                {
                    if (!string.IsNullOrWhiteSpace(c))
                    {
                        items.Add(new ATMNextService.BulkMoveItem
                        {
                            cardIdOrTitle = c,
                            targetColumn = p.DefaultTargetColumn,
                            targetBoard = p.DefaultTargetBoard,
                            sourceBoard = p.DefaultSourceBoard
                        });
                    }
                }
            }

            if (items.Count == 0)
                return new { success = false, message = "No valid cards or moves provided to bulk move." };

            var (successCount, totalCount, results) = ATMNextService.BulkMoveCards(items, p.DefaultTargetColumn, p.DefaultTargetBoard, p.DefaultSourceBoard);

            return new
            {
                success = successCount > 0,
                movedCount = successCount,
                totalRequested = totalCount,
                message = $"Bulk move completed: {successCount}/{totalCount} cards moved.",
                results = results.Select(r => new
                {
                    cardIdOrTitle = r.cardIdOrTitle,
                    cardTitle = r.cardTitle,
                    targetColumn = r.targetColumn,
                    targetBoard = r.targetBoard,
                    success = r.success,
                    message = r.message
                }).ToList()
            };
        }

#if ATM_NEXT_UNITYAI_MCP_PRESENT || ATM_NEXT_MCP_PRESENT
        [McpTool("atm_clone_task", "Clones/duplicates a task card to another column or board.", Groups = new[] { "Task Manager/Card" })]
#endif
        public static object CloneTask(CloneTaskParams p)
        {
            if (p == null || string.IsNullOrWhiteSpace(p.CardIdOrTitle))
                return new { success = false, message = "CardIdOrTitle is required." };

            var clonedCard = ATMNextService.CloneCard(p.CardIdOrTitle, p.TargetColumn, p.TargetBoard, p.SourceBoard, p.NewTitle, p.TargetIndex ?? -1);
            return new
            {
                success = clonedCard != null,
                message = clonedCard != null
                    ? $"Successfully cloned task '{clonedCard.title}'."
                    : $"Failed to clone task '{p.CardIdOrTitle}'. Ensure the card and target board/column exist.",
                taskId = clonedCard?.id,
                taskTitle = clonedCard?.title
            };
        }

#if ATM_NEXT_UNITYAI_MCP_PRESENT || ATM_NEXT_MCP_PRESENT
        [McpTool("atm_bulk_clone_tasks", "Clones/duplicates multiple task cards across columns or boards in a single atomic batch operation.", Groups = new[] { "Task Manager/Card" })]
#endif
        public static object BulkCloneTasks(BulkCloneTasksParams p)
        {
            if (p == null)
                return new { success = false, message = "Parameters required for bulk clone." };

            var items = new List<ATMNextService.BulkCloneItem>();

            if (p.Clones != null && p.Clones.Length > 0)
            {
                foreach (var c in p.Clones)
                {
                    if (c != null && !string.IsNullOrWhiteSpace(c.CardIdOrTitle))
                    {
                        items.Add(new ATMNextService.BulkCloneItem
                        {
                            cardIdOrTitle = c.CardIdOrTitle,
                            targetColumn = c.TargetColumn,
                            targetBoard = c.TargetBoard,
                            sourceBoard = c.SourceBoard,
                            newTitle = c.NewTitle
                        });
                    }
                }
            }

            var cardList = p.CardIdsOrTitles ?? p.Tasks;
            if (cardList != null && cardList.Length > 0)
            {
                foreach (var c in cardList)
                {
                    if (!string.IsNullOrWhiteSpace(c))
                    {
                        items.Add(new ATMNextService.BulkCloneItem
                        {
                            cardIdOrTitle = c,
                            targetColumn = p.DefaultTargetColumn,
                            targetBoard = p.DefaultTargetBoard,
                            sourceBoard = p.DefaultSourceBoard
                        });
                    }
                }
            }

            if (items.Count == 0)
                return new { success = false, message = "No valid cards or clone specifications provided to bulk clone." };

            var (successCount, totalCount, results) = ATMNextService.BulkCloneCards(items, p.DefaultTargetColumn, p.DefaultTargetBoard, p.DefaultSourceBoard);

            return new
            {
                success = successCount > 0,
                clonedCount = successCount,
                totalRequested = totalCount,
                message = $"Bulk clone completed: {successCount}/{totalCount} cards cloned.",
                results = results.Select(r => new
                {
                    cardIdOrTitle = r.cardIdOrTitle,
                    originalCardId = r.originalCardId,
                    clonedCardId = r.clonedCardId,
                    clonedCardTitle = r.clonedCardTitle,
                    targetColumn = r.targetColumn,
                    targetBoard = r.targetBoard,
                    success = r.success,
                    message = r.message
                }).ToList()
            };
        }

#if ATM_NEXT_UNITYAI_MCP_PRESENT || ATM_NEXT_MCP_PRESENT
        [McpTool("atm_bulk_update_tasks", "Updates multiple task cards in bulk (category, color, priority, assignees, etc.) in a single atomic batch.", Groups = new[] { "Task Manager/Card" })]
#endif
        public static object BulkUpdateTasks(BulkUpdateTasksParams p)
        {
            if (p == null)
                return new { success = false, message = "Parameters required for bulk update." };

            var items = new List<ATMNextService.BulkUpdateItem>();

            if (p.Updates != null && p.Updates.Length > 0)
            {
                foreach (var u in p.Updates)
                {
                    if (u == null || string.IsNullOrWhiteSpace(u.CardIdOrTitle)) continue;
                    items.Add(new ATMNextService.BulkUpdateItem
                    {
                        cardIdOrTitle = u.CardIdOrTitle,
                        newTitle = u.NewTitle,
                        newDescription = u.NewDescription,
                        newCategory = u.NewCategory,
                        newPriority = u.NewPriority,
                        newDueDate = u.NewDueDate,
                        newColorLabel = u.NewColorLabel,
                        completed = u.Completed,
                        archived = u.Archived,
                        assignees = u.Assignees != null ? new List<string>(u.Assignees) : null
                    });
                }
            }

            var cardList = p.CardIdsOrTitles ?? p.Tasks;
            if (cardList != null && cardList.Length > 0)
            {
                foreach (var titleOrId in cardList)
                {
                    if (string.IsNullOrWhiteSpace(titleOrId)) continue;
                    if (!items.Any(x => string.Equals(x.cardIdOrTitle, titleOrId, StringComparison.OrdinalIgnoreCase)))
                    {
                        items.Add(new ATMNextService.BulkUpdateItem
                        {
                            cardIdOrTitle = titleOrId
                        });
                    }
                }
            }

            if (items.Count == 0)
                return new { success = false, message = "No valid cards or update specifications provided to bulk update." };

            var (successCount, totalCount, results) = ATMNextService.BulkUpdateCards(
                items,
                p.DefaultCategory,
                p.DefaultColorLabel,
                p.DefaultPriority,
                p.DefaultAssignees != null ? new List<string>(p.DefaultAssignees) : null
            );

            return new
            {
                success = successCount > 0,
                updatedCount = successCount,
                totalRequested = totalCount,
                message = $"Bulk update completed: {successCount}/{totalCount} cards updated.",
                results = results.Select(r => new
                {
                    cardIdOrTitle = r.cardIdOrTitle,
                    cardId = r.cardId,
                    cardTitle = r.cardTitle,
                    success = r.success,
                    message = r.message
                }).ToList()
            };
        }

#if ATM_NEXT_UNITYAI_MCP_PRESENT || ATM_NEXT_MCP_PRESENT
        [McpTool("atm_bulk_delete_tasks", "Deletes multiple task cards in bulk from Awesome Task Manager (requires confirm: true).", Groups = new[] { "Task Manager/Card" })]
#endif
        public static object BulkDeleteTasks(BulkDeleteTasksParams p)
        {
            if (p == null)
                return new { success = false, message = "Parameters required for bulk task deletion." };

            var taskList = p.CardIdsOrTitles ?? p.Tasks;
            if (taskList == null || taskList.Length == 0)
                return new { success = false, message = "No task IDs or titles specified to delete." };

            if (!p.Confirm)
                return new { success = false, message = $"Are you sure you want to delete {taskList.Length} tasks? This action cannot be undone. Please call again with 'confirm: true' to proceed." };

            var (successCount, totalCount, results) = ATMNextService.BulkDeleteCards(taskList);

            return new
            {
                success = successCount > 0,
                deletedCount = successCount,
                totalRequested = totalCount,
                message = $"Bulk task deletion completed: {successCount}/{totalCount} tasks deleted.",
                results = results.Select(r => new
                {
                    cardIdOrTitle = r.cardIdOrTitle,
                    cardTitle = r.cardTitle,
                    cardId = r.cardId,
                    success = r.success,
                    message = r.message
                }).ToList()
            };
        }

#if ATM_NEXT_UNITYAI_MCP_PRESENT || ATM_NEXT_MCP_PRESENT
        [McpTool("atm_reorder_task", "Moves a task card up or down within its column, or to a specific position index (mimics up/down arrows on card).", Groups = new[] { "Task Manager/Card" })]
#endif
        public static object ReorderTask(ReorderTaskParams p)
        {
            if (p == null || string.IsNullOrWhiteSpace(p.CardIdOrTitle))
                return new { success = false, message = "CardIdOrTitle is required." };

            var (reordered, newIdx) = ATMNextService.ReorderCard(p.CardIdOrTitle, p.Direction, p.TargetIndex, p.Board);
            if (!reordered)
                return new { success = false, message = $"Failed to reorder task '{p.CardIdOrTitle}'. Ensure card exists." };

            return new
            {
                success = true,
                cardIdOrTitle = p.CardIdOrTitle,
                newIndex = newIdx,
                message = $"Reordered task '{p.CardIdOrTitle}' to index {newIdx}."
            };
        }

#if ATM_NEXT_UNITYAI_MCP_PRESENT || ATM_NEXT_MCP_PRESENT
        [McpTool("atm_set_active_board", "Switches the currently active task board in the Unity Editor and Awesome Task Manager.", Groups = new[] { "Task Manager/Board" })]
#endif
        public static object SetActiveBoard(SetActiveBoardParams p)
        {
            if (p == null || string.IsNullOrWhiteSpace(p.BoardNameOrId))
                return new { success = false, message = "BoardNameOrId is required." };

            var board = ATMNextService.SetActiveBoard(p.BoardNameOrId);
            if (board == null)
                return new { success = false, message = $"Board '{p.BoardNameOrId}' not found." };

            return new
            {
                success = true,
                boardId = board.id,
                boardName = board.name,
                columnsCount = board.columns?.Count ?? 0,
                cardsCount = board.columns?.Sum(c => c.cards?.Count ?? 0) ?? 0,
                message = $"Switched active board to '{board.name}'."
            };
        }

#if ATM_NEXT_UNITYAI_MCP_PRESENT || ATM_NEXT_MCP_PRESENT
        [McpTool("atm_set_filter", "Sets or resets board UI filters including Category, Assignee, Priority, and Search text query.", Groups = new[] { "Task Manager/Board" })]
#endif
        public static object SetFilter(SetFilterParams p)
        {
            if (p == null) p = new SetFilterParams();

            int? pri = p.Priority;
            if (!pri.HasValue && !string.IsNullOrWhiteSpace(p.PriorityName))
            {
                string pn = p.PriorityName.Trim().ToLowerInvariant();
                switch (pn)
                {
                    case "all": pri = 0; break;
                    case "none": case "—": case "-": case "0": pri = 1; break;
                    case "low": case "1": pri = 2; break;
                    case "medium": case "med": case "2": pri = 3; break;
                    case "high": case "3": pri = 4; break;
                    case "urgent": case "critical": case "4": pri = 5; break;
                }
            }

            var filter = ATMNextService.SetFilter(p.Category, p.Assignee, pri, p.SearchQuery, p.ResetAll);
            return new
            {
                success = true,
                category = filter.category,
                assignee = filter.assignee,
                assigneeId = filter.assigneeId,
                priority = filter.priority,
                priorityName = filter.priorityName,
                searchQuery = filter.searchQuery,
                totalCardsOnBoard = filter.totalCardsOnBoard,
                visibleCardsOnBoard = filter.visibleCardsOnBoard,
                message = $"Active filter updated: Category='{filter.category}', Assignee='{filter.assignee}', Priority='{filter.priorityName}', Search='{filter.searchQuery}' ({filter.visibleCardsOnBoard}/{filter.totalCardsOnBoard} cards visible)."
            };
        }

#if ATM_NEXT_UNITYAI_MCP_PRESENT || ATM_NEXT_MCP_PRESENT
        [McpTool("atm_get_filter", "Returns the current active board filter settings and visible/total card counts.", Groups = new[] { "Task Manager/Board" })]
#endif
        public static object GetFilter(GetFilterParams p = null)
        {
            var filter = ATMNextService.GetFilter();
            return new
            {
                success = true,
                category = filter.category,
                assignee = filter.assignee,
                assigneeId = filter.assigneeId,
                priority = filter.priority,
                priorityName = filter.priorityName,
                searchQuery = filter.searchQuery,
                totalCardsOnBoard = filter.totalCardsOnBoard,
                visibleCardsOnBoard = filter.visibleCardsOnBoard
            };
        }

#if ATM_NEXT_UNITYAI_MCP_PRESENT || ATM_NEXT_MCP_PRESENT
        [McpTool("atm_create_category", "Creates a new category tag and optionally assigns its default color label index.", Groups = new[] { "Task Manager/Category" })]
#endif
        public static object CreateCategory(CreateCategoryParams p)
        {
            if (p == null || string.IsNullOrWhiteSpace(p.Name))
                return new { success = false, message = "Category Name is required." };

            string cat = ATMNextService.CreateCategory(p.Name, p.ColorIndex);
            return new
            {
                success = true,
                name = cat,
                colorIndex = p.ColorIndex ?? ATMNextService.GetData().GetCategoryColor(cat),
                message = $"Created category '{cat}'."
            };
        }

#if ATM_NEXT_UNITYAI_MCP_PRESENT || ATM_NEXT_MCP_PRESENT
        [McpTool("atm_get_categories", "Lists all task categories and their color label indices.", Groups = new[] { "Task Manager/Category" })]
#endif
        public static object GetCategories(GetCategoriesParams p = null)
        {
            var categories = ATMNextService.GetCategories();
            return new
            {
                success = true,
                categories = categories.Select(c => new { name = c.name, colorIndex = c.colorIndex }).ToList()
            };
        }

#if ATM_NEXT_UNITYAI_MCP_PRESENT || ATM_NEXT_MCP_PRESENT
        [McpTool("atm_delete_category", "Deletes a category tag globally and removes it from all cards (requires confirm: true).", Groups = new[] { "Task Manager/Category" })]
#endif
        public static object DeleteCategory(DeleteCategoryParams p)
        {
            if (p == null || string.IsNullOrWhiteSpace(p.Category))
                return new { success = false, message = "Category name is required." };

            if (!p.Confirm)
                return new { success = false, message = $"Are you sure you want to delete category '{p.Category}'? This will remove it from all cards. Please call again with 'confirm: true' to proceed." };

            bool deleted = ATMNextService.DeleteCategory(p.Category);
            return new { success = deleted, message = deleted ? $"Deleted category '{p.Category}'." : "Category not found." };
        }

#if ATM_NEXT_UNITYAI_MCP_PRESENT || ATM_NEXT_MCP_PRESENT
        [McpTool("atm_create_assignee", "Creates a new project member / assignee with custom avatar colors.", Groups = new[] { "Task Manager/Assignee" })]
#endif
        public static object CreateAssignee(CreateAssigneeParams p)
        {
            if (p == null || string.IsNullOrWhiteSpace(p.Name))
                return new { success = false, message = "Assignee Name is required." };

            var ass = ATMNextService.CreateAssignee(p.Name, p.ColorIndex ?? 1, p.BorderColorIndex ?? 0, p.ProfileImageGuid);
            return new
            {
                success = true,
                assigneeId = ass.id,
                name = ass.name,
                colorIndex = ass.colorIndex,
                borderColorIndex = ass.borderColorIndex,
                message = $"Created assignee '{ass.name}'."
            };
        }

#if ATM_NEXT_UNITYAI_MCP_PRESENT || ATM_NEXT_MCP_PRESENT
        [McpTool("atm_get_assignees", "Lists all team members / assignees with IDs and color styles.", Groups = new[] { "Task Manager/Assignee" })]
#endif
        public static object GetAssignees(GetAssigneesParams p = null)
        {
            var assignees = ATMNextService.GetAssignees();
            return new
            {
                success = true,
                assignees = assignees.Select(a => new
                {
                    id = a.id,
                    name = a.name,
                    colorIndex = a.colorIndex,
                    borderColorIndex = a.borderColorIndex,
                    profileImageGuid = a.profileImageGuid
                }).ToList()
            };
        }

#if ATM_NEXT_UNITYAI_MCP_PRESENT || ATM_NEXT_MCP_PRESENT
        [McpTool("atm_delete_assignee", "Deletes a team member / assignee and unassigns them from all cards (requires confirm: true).", Groups = new[] { "Task Manager/Assignee" })]
#endif
        public static object DeleteAssignee(DeleteAssigneeParams p)
        {
            if (p == null || string.IsNullOrWhiteSpace(p.AssigneeIdOrName))
                return new { success = false, message = "AssigneeIdOrName is required." };

            if (!p.Confirm)
                return new { success = false, message = $"Are you sure you want to delete assignee '{p.AssigneeIdOrName}'? This will unassign them from all cards. Please call again with 'confirm: true' to proceed." };

            bool deleted = ATMNextService.DeleteAssignee(p.AssigneeIdOrName);
            return new { success = deleted, message = deleted ? $"Deleted assignee '{p.AssigneeIdOrName}'." : "Assignee not found." };
        }

        #endregion
    }
}
