using System;
using System.Collections.Generic;
using System.Linq;
using AwesomeTaskManager.Data;
using AwesomeTaskManager.Editor;
using UnityEditor;
using UnityEngine;

namespace AwesomeTaskManager.Next
{
    /// <summary>
    /// Core programmatic API for Awesome Task Manager Next.
    /// Provides full CRUD, search, and linking operations for boards, columns, cards, checklists, and notes.
    /// </summary>
    public static class ATMNextService
    {
        private static readonly object LockObj = new object();

        #region Data Management

        /// <summary>
        /// Loads the active SaveData, ensuring it is normalized and ready for use.
        /// </summary>
        public static SaveData GetData()
        {
            lock (LockObj)
            {
                var data = Persistence.Load() ?? new SaveData();
                data.Normalize();
                return data;
            }
        }

        /// <summary>
        /// Saves changes to disk and notifies all open Unity Editor windows to reload.
        /// </summary>
        public static void Save(SaveData data)
        {
            if (data == null) return;
            lock (LockObj)
            {
                data.Normalize();
                Persistence.Save(data);

                if (Persistence.SuppressReloadEvents || Persistence.InMemoryDataOverride != null || !string.IsNullOrEmpty(Persistence.SavePathOverride))
                {
                    return;
                }
                
                try
                {
                    TaskBoardWindow.ReloadAllOpenWindows();
                }
                catch { }

                // Ensure UI reload runs on main thread
                if (EditorApplication.isCompiling) return;
                
                EditorApplication.delayCall += () =>
                {
                    try
                    {
                        TaskBoardWindow.ReloadAllOpenWindows();
                    }
                    catch { }
                };
            }
        }

        #endregion

        #region Boards

        /// <summary>
        /// Gets the active board based on data.lastBoardIndex.
        /// </summary>
        public static TaskBoard GetActiveBoard(SaveData data)
        {
            if (data?.boards == null || data.boards.Count == 0) return null;
            int idx = Mathf.Clamp(data.lastBoardIndex, 0, data.boards.Count - 1);
            return data.boards[idx];
        }

        /// <summary>
        /// Finds a board by ID, exact name, or partial name, falling back to active board if query is null/empty.
        /// </summary>
        public static TaskBoard FindBoard(SaveData data, string boardNameOrId)
        {
            if (data?.boards == null || data.boards.Count == 0) return null;
            if (string.IsNullOrWhiteSpace(boardNameOrId)) return GetActiveBoard(data);

            string query = boardNameOrId.Trim();
            // 1. Exact ID
            var board = data.boards.FirstOrDefault(b => b != null && b.id == query);
            if (board != null) return board;

            // 2. Exact Name
            board = data.boards.FirstOrDefault(b => b != null && string.Equals(b.name, query, StringComparison.OrdinalIgnoreCase));
            if (board != null) return board;

            // 3. Name contains
            board = data.boards.FirstOrDefault(b => b != null && b.name != null && b.name.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0);
            return board;
        }

        /// <summary>
        /// Finds a column in a board by ID, exact title, partial title, or normalized alphanumeric match.
        /// </summary>
        public static TaskColumn FindColumn(TaskBoard board, string columnNameOrId)
        {
            if (board?.columns == null || board.columns.Count == 0) return null;
            if (string.IsNullOrWhiteSpace(columnNameOrId))
            {
                // Default to a "To Do" / "Backlog" column if found, or first column
                return board.columns.FirstOrDefault(c => c != null && c.title != null && 
                    (c.title.IndexOf("to do", StringComparison.OrdinalIgnoreCase) >= 0 || 
                     c.title.IndexOf("todo", StringComparison.OrdinalIgnoreCase) >= 0 || 
                     c.title.IndexOf("backlog", StringComparison.OrdinalIgnoreCase) >= 0)) ?? board.columns[0];
            }

            string q = columnNameOrId.Trim();

            // 1. Exact ID
            var col = board.columns.FirstOrDefault(c => c != null && (c.id == q || string.Equals(c.title, q, StringComparison.OrdinalIgnoreCase)));
            if (col != null) return col;

            // 2. Title contains
            col = board.columns.FirstOrDefault(c => c != null && c.title != null && c.title.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0);
            if (col != null) return col;

            // 3. Normalized match (ignoring emojis/symbols)
            string qNorm = NormalizeAlphaNumeric(q);
            if (!string.IsNullOrEmpty(qNorm))
            {
                col = board.columns.FirstOrDefault(c => c != null && c.title != null && NormalizeAlphaNumeric(c.title).Equals(qNorm, StringComparison.OrdinalIgnoreCase));
                if (col != null) return col;

                col = board.columns.FirstOrDefault(c => c != null && c.title != null && NormalizeAlphaNumeric(c.title).IndexOf(qNorm, StringComparison.OrdinalIgnoreCase) >= 0);
                if (col != null) return col;
            }

            return board.columns[0];
        }

        private static string NormalizeAlphaNumeric(string input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;
            var sb = new System.Text.StringBuilder();
            foreach (char ch in input)
            {
                if (char.IsLetterOrDigit(ch)) sb.Append(char.ToLowerInvariant(ch));
            }
            return sb.ToString();
        }

        /// <summary>
        /// Retrieves all task boards in the project.
        /// </summary>
        public static List<TaskBoard> GetBoards()
        {
            var data = GetData();
            return data.boards ?? new List<TaskBoard>();
        }

        /// <summary>
        /// Finds a board by ID or name (case-insensitive).
        /// </summary>
        public static TaskBoard GetBoard(string nameOrId)
        {
            if (string.IsNullOrWhiteSpace(nameOrId)) return null;
            var data = GetData();
            return FindBoard(data, nameOrId);
        }

        /// <summary>
        /// Creates a new board with default columns.
        /// </summary>
        public static TaskBoard CreateBoard(string boardName)
        {
            if (string.IsNullOrWhiteSpace(boardName)) return null;
            var data = GetData();
            string trimmed = boardName.Trim();

            // Check if already exists
            var existing = data.boards.FirstOrDefault(b => b != null && string.Equals(b.name, trimmed, StringComparison.OrdinalIgnoreCase));
            if (existing != null) return existing;

            var newBoard = new TaskBoard(trimmed);
            data.boards.Add(newBoard);
            data.lastBoardIndex = data.boards.IndexOf(newBoard);
            Save(data);
            return newBoard;
        }

        /// <summary>
        /// Deletes a board by ID or name.
        /// </summary>
        public static bool DeleteBoard(string nameOrId)
        {
            if (string.IsNullOrWhiteSpace(nameOrId)) return false;
            var data = GetData();
            string query = nameOrId.Trim();
            var board = data.boards.FirstOrDefault(b => b != null && (b.id == query || string.Equals(b.name, query, StringComparison.OrdinalIgnoreCase)));
            if (board == null) return false;

            // Keep at least one board
            if (data.boards.Count <= 1) return false;

            data.boards.Remove(board);
            if (data.lastBoardIndex >= data.boards.Count)
                data.lastBoardIndex = Math.Max(0, data.boards.Count - 1);

            Save(data);
            return true;
        }

        /// <summary>
        /// Sets the active board by ID or name, updating lastBoardIndex and open windows.
        /// </summary>
        public static TaskBoard SetActiveBoard(string boardNameOrId)
        {
            if (string.IsNullOrWhiteSpace(boardNameOrId)) return null;
            var data = GetData();
            var board = FindBoard(data, boardNameOrId);
            if (board == null) return null;

            int idx = data.boards.IndexOf(board);
            if (idx >= 0)
            {
                data.lastBoardIndex = idx;
                Save(data);

                try
                {
                    if (!Persistence.SuppressReloadEvents && Persistence.InMemoryDataOverride == null && string.IsNullOrEmpty(Persistence.SavePathOverride) && TaskBoardWindow.Instance != null)
                    {
                        TaskBoardWindow.Instance.SetActiveBoardIndex(idx);
                    }
                }
                catch { }
            }
            return board;
        }

        /// <summary>
        /// Gets the active TaskBoard from data.
        /// </summary>
        public static TaskBoard GetActiveBoard()
        {
            var data = GetData();
            return GetActiveBoard(data);
        }

        #endregion

        #region Columns

        /// <summary>
        /// Locates a column and its containing board.
        /// Searches preferredBoard first if specified, active board, then all boards.
        /// </summary>
        public static bool FindBoardAndColumn(SaveData data, string columnNameOrId, string preferredBoardNameOrId, out TaskBoard outBoard, out TaskColumn outColumn)
        {
            outBoard = null;
            outColumn = null;
            if (data?.boards == null || string.IsNullOrWhiteSpace(columnNameOrId)) return false;

            // 1. If preferred board is provided, search that board
            if (!string.IsNullOrWhiteSpace(preferredBoardNameOrId))
            {
                var b = FindBoard(data, preferredBoardNameOrId);
                if (b?.columns != null)
                {
                    var col = FindColumn(b, columnNameOrId);
                    if (col != null)
                    {
                        outBoard = b;
                        outColumn = col;
                        return true;
                    }
                }
            }

            string q = columnNameOrId.Trim();

            // 2. Search active board
            var activeBoard = GetActiveBoard(data);
            if (activeBoard?.columns != null)
            {
                var col = activeBoard.columns.FirstOrDefault(c => c != null && (c.id == q || string.Equals(c.title, q, StringComparison.OrdinalIgnoreCase)));
                if (col != null)
                {
                    outBoard = activeBoard;
                    outColumn = col;
                    return true;
                }
            }

            // 3. Search all boards for exact match
            foreach (var b in data.boards)
            {
                if (b?.columns == null) continue;
                var col = b.columns.FirstOrDefault(c => c != null && (c.id == q || string.Equals(c.title, q, StringComparison.OrdinalIgnoreCase)));
                if (col != null)
                {
                    outBoard = b;
                    outColumn = col;
                    return true;
                }
            }

            // 4. Search all boards for partial/normalized match
            foreach (var b in data.boards)
            {
                if (b?.columns == null) continue;
                var col = FindColumn(b, q);
                if (col != null)
                {
                    outBoard = b;
                    outColumn = col;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Gets all columns for a given board.
        /// </summary>
        public static List<TaskColumn> GetColumns(string boardNameOrId = null)
        {
            var data = GetData();
            var board = FindBoard(data, boardNameOrId);
            return board?.columns ?? new List<TaskColumn>();
        }

        /// <summary>
        /// Creates a new column in the specified board.
        /// </summary>
        public static TaskColumn CreateColumn(string boardNameOrId, string columnTitle)
        {
            if (string.IsNullOrWhiteSpace(columnTitle)) return null;
            var data = GetData();
            var board = FindBoard(data, boardNameOrId);

            if (board == null) return null;

            board.columns ??= new List<TaskColumn>();
            var newCol = new TaskColumn(columnTitle.Trim());
            board.columns.Add(newCol);
            Save(data);
            return newCol;
        }

        /// <summary>
        /// Moves a column to a target board (or reorders it within the same board).
        /// </summary>
        public static bool MoveColumn(string columnNameOrId, string targetBoardNameOrId, string sourceBoardNameOrId = null, int targetIndex = -1)
        {
            if (string.IsNullOrWhiteSpace(columnNameOrId)) return false;

            var data = GetData();
            if (!FindBoardAndColumn(data, columnNameOrId, sourceBoardNameOrId, out var srcBoard, out var col))
                return false;

            var destBoard = string.IsNullOrWhiteSpace(targetBoardNameOrId)
                ? srcBoard
                : FindBoard(data, targetBoardNameOrId);

            if (destBoard == null) return false;

            if (srcBoard == destBoard)
            {
                // Reordering inside the same board
                int currentIndex = srcBoard.columns.IndexOf(col);
                if (currentIndex < 0) return false;
                srcBoard.columns.RemoveAt(currentIndex);

                int insertIdx = (targetIndex < 0 || targetIndex >= srcBoard.columns.Count) ? srcBoard.columns.Count : Math.Max(0, targetIndex);
                srcBoard.columns.Insert(insertIdx, col);
            }
            else
            {
                // Moving to different board
                srcBoard.columns.Remove(col);
                destBoard.columns ??= new List<TaskColumn>();

                int insertIdx = (targetIndex < 0 || targetIndex >= destBoard.columns.Count) ? destBoard.columns.Count : Math.Max(0, targetIndex);
                destBoard.columns.Insert(insertIdx, col);
            }

            Save(data);
            return true;
        }

        /// <summary>
        /// Clones/duplicates a column (including cards and checklist items) to another board or within the same board.
        /// </summary>
        public static TaskColumn CloneColumn(
            string columnNameOrId,
            string targetBoardNameOrId,
            string sourceBoardNameOrId = null,
            string newTitle = null,
            bool includeCards = true,
            int targetIndex = -1)
        {
            if (string.IsNullOrWhiteSpace(columnNameOrId)) return null;

            var data = GetData();
            if (!FindBoardAndColumn(data, columnNameOrId, sourceBoardNameOrId, out var srcBoard, out var srcCol))
                return null;

            var destBoard = string.IsNullOrWhiteSpace(targetBoardNameOrId)
                ? srcBoard
                : FindBoard(data, targetBoardNameOrId);

            if (destBoard == null) return null;

            var clonedCol = srcCol.Clone(resetIds: true, includeCards: includeCards);

            if (!string.IsNullOrWhiteSpace(newTitle))
            {
                clonedCol.title = newTitle.Trim();
            }
            else if (destBoard == srcBoard)
            {
                clonedCol.title = $"{srcCol.title} (Copy)";
            }
            else
            {
                clonedCol.title = srcCol.title;
            }

            destBoard.columns ??= new List<TaskColumn>();
            int insertIdx = (targetIndex < 0 || targetIndex >= destBoard.columns.Count) ? destBoard.columns.Count : Math.Max(0, targetIndex);
            destBoard.columns.Insert(insertIdx, clonedCol);

            Save(data);
            return clonedCol;
        }

        /// <summary>
        /// Reorders a column within its board (e.g., "left", "right", "first", "last", or explicit target index).
        /// </summary>
        public static bool ReorderColumn(string columnNameOrId, string direction = null, int? targetIndex = null, string boardNameOrId = null)
        {
            if (string.IsNullOrWhiteSpace(columnNameOrId)) return false;

            var data = GetData();
            if (!FindBoardAndColumn(data, columnNameOrId, boardNameOrId, out var board, out var col))
                return false;

            if (board.columns == null || board.columns.Count <= 1) return true;

            int currentIndex = board.columns.IndexOf(col);
            if (currentIndex < 0) return false;

            int newIndex = currentIndex;
            if (targetIndex.HasValue)
            {
                newIndex = targetIndex.Value;
            }
            else if (!string.IsNullOrWhiteSpace(direction))
            {
                string d = direction.Trim().ToLowerInvariant();
                switch (d)
                {
                    case "left":
                    case "up":
                    case "prev":
                    case "previous":
                    case "back":
                        newIndex = currentIndex - 1;
                        break;
                    case "right":
                    case "down":
                    case "next":
                    case "forward":
                        newIndex = currentIndex + 1;
                        break;
                    case "first":
                    case "start":
                    case "top":
                    case "beginning":
                        newIndex = 0;
                        break;
                    case "last":
                    case "end":
                    case "bottom":
                        newIndex = board.columns.Count - 1;
                        break;
                }
            }

            newIndex = Mathf.Clamp(newIndex, 0, board.columns.Count - 1);
            if (newIndex == currentIndex) return true;

            board.columns.RemoveAt(currentIndex);
            board.columns.Insert(newIndex, col);

            Save(data);
            return true;
        }

        /// <summary>
        /// Moves a column one position to the left.
        /// </summary>
        public static bool MoveColumnLeft(string columnNameOrId, string boardNameOrId = null) =>
            ReorderColumn(columnNameOrId, "left", null, boardNameOrId);

        /// <summary>
        /// Moves a column one position to the right.
        /// </summary>
        public static bool MoveColumnRight(string columnNameOrId, string boardNameOrId = null) =>
            ReorderColumn(columnNameOrId, "right", null, boardNameOrId);

        /// <summary>
        /// Deletes a column and all its task cards.
        /// </summary>
        public static bool DeleteColumn(string boardNameOrId, string columnNameOrId)
        {
            if (string.IsNullOrWhiteSpace(columnNameOrId)) return false;

            var data = GetData();
            if (!FindBoardAndColumn(data, columnNameOrId, boardNameOrId, out var board, out var col))
                return false;

            board.columns.Remove(col);
            Save(data);
            return true;
        }

        #endregion

        #region Cards (Tasks)

        /// <summary>
        /// Locates a card and its containing board and column.
        /// Searches preferredBoard first if specified, active board, then all boards.
        /// </summary>
        public static bool FindBoardAndCard(SaveData data, string cardIdOrTitle, string preferredBoardNameOrId, out TaskBoard outBoard, out TaskColumn outColumn, out TaskCard outCard)
        {
            outBoard = null;
            outColumn = null;
            outCard = null;
            if (data?.boards == null || string.IsNullOrWhiteSpace(cardIdOrTitle)) return false;

            string q = cardIdOrTitle.Trim();

            // 1. If preferred board is provided, search that board
            if (!string.IsNullOrWhiteSpace(preferredBoardNameOrId))
            {
                var b = FindBoard(data, preferredBoardNameOrId);
                if (b?.columns != null)
                {
                    foreach (var col in b.columns)
                    {
                        if (col?.cards == null) continue;
                        var c = col.cards.FirstOrDefault(x => x != null && (x.id == q || string.Equals(x.title, q, StringComparison.OrdinalIgnoreCase) || string.Equals(x.taskCode, q, StringComparison.OrdinalIgnoreCase)));
                        if (c != null)
                        {
                            outBoard = b;
                            outColumn = col;
                            outCard = c;
                            return true;
                        }
                    }
                }
            }

            // 2. Search active board
            var activeBoard = GetActiveBoard(data);
            if (activeBoard?.columns != null)
            {
                foreach (var col in activeBoard.columns)
                {
                    if (col?.cards == null) continue;
                    var c = col.cards.FirstOrDefault(x => x != null && (x.id == q || string.Equals(x.title, q, StringComparison.OrdinalIgnoreCase) || string.Equals(x.taskCode, q, StringComparison.OrdinalIgnoreCase)));
                    if (c != null)
                    {
                        outBoard = activeBoard;
                        outColumn = col;
                        outCard = c;
                        return true;
                    }
                }
            }

            // 3. Search all boards
            foreach (var b in data.boards)
            {
                if (b?.columns == null) continue;
                foreach (var col in b.columns)
                {
                    if (col?.cards == null) continue;
                    var c = col.cards.FirstOrDefault(x => x != null && (x.id == q || string.Equals(x.title, q, StringComparison.OrdinalIgnoreCase) || string.Equals(x.taskCode, q, StringComparison.OrdinalIgnoreCase)));
                    if (c != null)
                    {
                        outBoard = b;
                        outColumn = col;
                        outCard = c;
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Gets all task cards across the board, or within a specific board / column.
        /// </summary>
        public static List<TaskCard> GetCards(string boardNameOrId = null, string columnNameOrId = null)
        {
            var data = GetData();
            var result = new List<TaskCard>();

            var boards = string.IsNullOrWhiteSpace(boardNameOrId)
                ? data.boards
                : data.boards.Where(b => b.id == boardNameOrId || string.Equals(b.name, boardNameOrId, StringComparison.OrdinalIgnoreCase)).ToList();

            foreach (var b in boards)
            {
                if (b?.columns == null) continue;
                var cols = string.IsNullOrWhiteSpace(columnNameOrId)
                    ? b.columns
                    : b.columns.Where(c => c.id == columnNameOrId || string.Equals(c.title, columnNameOrId, StringComparison.OrdinalIgnoreCase)).ToList();

                foreach (var c in cols)
                {
                    if (c?.cards != null)
                        result.AddRange(c.cards.Where(card => card != null));
                }
            }

            return result;
        }

        /// <summary>
        /// Finds a card by its ID or title across all boards.
        /// </summary>
        public static TaskCard GetCard(string cardIdOrTitle)
        {
            if (string.IsNullOrWhiteSpace(cardIdOrTitle)) return null;
            var data = GetData();
            string q = cardIdOrTitle.Trim();

            foreach (var b in data.boards)
            {
                if (b?.columns == null) continue;
                foreach (var col in b.columns)
                {
                    if (col?.cards == null) continue;
                    var card = col.cards.FirstOrDefault(c => c != null && (c.id == q || string.Equals(c.title, q, StringComparison.OrdinalIgnoreCase) || string.Equals(c.taskCode, q, StringComparison.OrdinalIgnoreCase)));
                    if (card != null) return card;
                }
            }
            return null;
        }

        /// <summary>
        /// Creates a new card in the specified board and column.
        /// </summary>
        public static TaskCard CreateCard(
            string boardNameOrId,
            string columnNameOrId,
            string title,
            string description = "",
            string category = "",
            int priority = 0,
            string dueDate = "",
            int colorLabel = 0,
            List<string> checklist = null,
            List<string> assignees = null)
        {
            if (string.IsNullOrWhiteSpace(title)) return null;

            var data = GetData();
            var board = FindBoard(data, boardNameOrId);

            if (board == null)
            {
                // Auto create board if missing
                board = new TaskBoard(!string.IsNullOrWhiteSpace(boardNameOrId) ? boardNameOrId.Trim() : "My First Board");
                data.boards.Add(board);
            }

            board.columns ??= new List<TaskColumn>();
            if (board.columns.Count == 0)
            {
                board.columns.Add(new TaskColumn("📋 To Do"));
                board.columns.Add(new TaskColumn("🔨 In Progress"));
                board.columns.Add(new TaskColumn("✅ Done"));
            }

            var column = FindColumn(board, columnNameOrId);
            column.cards ??= new List<TaskCard>();

            var card = new TaskCard(title.Trim())
            {
                description = description ?? "",
                category = category != null ? category.Trim() : "",
                priority = Mathf.Clamp(priority, 0, 4),
                dueDate = dueDate ?? "",
                colorLabel = Mathf.Clamp(colorLabel, 0, 16)
            };

            // Auto-assign category default color if not specified
            if (card.colorLabel == 0 && !string.IsNullOrEmpty(card.category))
            {
                int defaultCatColor = data.GetCategoryColor(card.category);
                if (defaultCatColor > 0) card.colorLabel = defaultCatColor;
            }

            // Ensure category is registered globally
            if (!string.IsNullOrEmpty(card.category))
            {
                data.categories ??= new List<string>();
                if (!data.categories.Any(c => string.Equals(c, card.category, StringComparison.OrdinalIgnoreCase)))
                    data.categories.Add(card.category);
            }

            // Add checklist items if provided
            if (checklist != null && checklist.Count > 0)
            {
                foreach (var item in checklist)
                {
                    if (!string.IsNullOrWhiteSpace(item))
                    {
                        card.checklistItems.Add(item.Trim());
                        card.checklistStates.Add(false);
                        card.checklistLinkedCardIds.Add(string.Empty);
                    }
                }
            }

            // Assign members if provided
            if (assignees != null && assignees.Count > 0)
            {
                foreach (var a in assignees)
                {
                    if (string.IsNullOrWhiteSpace(a)) continue;
                    var match = data.assignees?.FirstOrDefault(x => x.id == a || string.Equals(x.name, a, StringComparison.OrdinalIgnoreCase));
                    if (match != null && !card.assigneeIds.Contains(match.id))
                        card.assigneeIds.Add(match.id);
                }
            }

            column.cards.Add(card);
            Save(data);
            return card;
        }

        /// <summary>
        /// Updates an existing card's properties.
        /// </summary>
        public static TaskCard UpdateCard(
            string cardIdOrTitle,
            string newTitle = null,
            string newDescription = null,
            string newCategory = null,
            int? newPriority = null,
            string newDueDate = null,
            int? newColorLabel = null,
            bool? completed = null,
            bool? archived = null,
            List<string> assignees = null)
        {
            var data = GetData();
            TaskCard card = null;

            foreach (var b in data.boards)
            {
                if (b?.columns == null) continue;
                foreach (var col in b.columns)
                {
                    if (col?.cards == null) continue;
                    card = col.cards.FirstOrDefault(c => c != null && (c.id == cardIdOrTitle || string.Equals(c.title, cardIdOrTitle, StringComparison.OrdinalIgnoreCase) || string.Equals(c.taskCode, cardIdOrTitle, StringComparison.OrdinalIgnoreCase)));
                    if (card != null) break;
                }
                if (card != null) break;
            }

            if (card == null) return null;

            if (newTitle != null) card.title = newTitle.Trim();
            if (newDescription != null) card.description = newDescription;
            if (newCategory != null)
            {
                card.category = newCategory.Trim();
                if (!string.IsNullOrEmpty(card.category) && !data.categories.Any(c => string.Equals(c, card.category, StringComparison.OrdinalIgnoreCase)))
                    data.categories.Add(card.category);
            }
            if (newPriority.HasValue) card.priority = Mathf.Clamp(newPriority.Value, 0, 4);
            if (newDueDate != null) card.dueDate = newDueDate;
            if (newColorLabel.HasValue) card.colorLabel = Mathf.Clamp(newColorLabel.Value, 0, 16);
            else if (newCategory != null && card.colorLabel == 0)
            {
                int defaultCatColor = data.GetCategoryColor(card.category);
                if (defaultCatColor > 0) card.colorLabel = defaultCatColor;
            }
            if (completed.HasValue)
            {
                card.completed = completed.Value;
                data.SyncLinkedChecklistItems(card.id, card.completed);
            }
            if (archived.HasValue) card.archived = archived.Value;
            if (assignees != null)
            {
                card.assigneeIds = new List<string>();
                foreach (var a in assignees)
                {
                    if (string.IsNullOrWhiteSpace(a)) continue;
                    var match = data.assignees.FirstOrDefault(x => x.id == a || string.Equals(x.name, a, StringComparison.OrdinalIgnoreCase));
                    if (match != null && !card.assigneeIds.Contains(match.id))
                        card.assigneeIds.Add(match.id);
                }
            }

            Save(data);
            return card;
        }

        /// <summary>
        /// Updates multiple cards' properties (category, color label, priority, assignees, etc.) in a single batch operation.
        /// </summary>
        public static (int successCount, int totalCount, List<BulkUpdateResult> results) BulkUpdateCards(
            IEnumerable<BulkUpdateItem> updates,
            string defaultCategory = null,
            int? defaultColorLabel = null,
            int? defaultPriority = null,
            List<string> defaultAssignees = null)
        {
            var results = new List<BulkUpdateResult>();
            if (updates == null) return (0, 0, results);

            var updateList = updates.ToList();
            if (updateList.Count == 0) return (0, 0, results);

            var data = GetData();
            int successCount = 0;
            bool modified = false;

            foreach (var item in updateList)
            {
                if (item == null || string.IsNullOrWhiteSpace(item.cardIdOrTitle))
                {
                    results.Add(new BulkUpdateResult
                    {
                        cardIdOrTitle = item?.cardIdOrTitle ?? "",
                        success = false,
                        message = "CardIdOrTitle is required."
                    });
                    continue;
                }

                TaskCard card = null;
                foreach (var b in data.boards)
                {
                    if (b?.columns == null) continue;
                    foreach (var col in b.columns)
                    {
                        if (col?.cards == null) continue;
                        card = col.cards.FirstOrDefault(c => c != null && (c.id == item.cardIdOrTitle || string.Equals(c.title, item.cardIdOrTitle, StringComparison.OrdinalIgnoreCase) || string.Equals(c.taskCode, item.cardIdOrTitle, StringComparison.OrdinalIgnoreCase)));
                        if (card != null) break;
                    }
                    if (card != null) break;
                }

                if (card == null)
                {
                    results.Add(new BulkUpdateResult
                    {
                        cardIdOrTitle = item.cardIdOrTitle,
                        success = false,
                        message = $"Task '{item.cardIdOrTitle}' not found."
                    });
                    continue;
                }

                string cat = item.newCategory ?? defaultCategory;
                int? cl = item.newColorLabel ?? defaultColorLabel;
                int? prio = item.newPriority ?? defaultPriority;
                var ass = item.assignees ?? defaultAssignees;

                if (item.newTitle != null) card.title = item.newTitle.Trim();
                if (item.newDescription != null) card.description = item.newDescription;
                if (cat != null)
                {
                    card.category = cat.Trim();
                    if (!string.IsNullOrEmpty(card.category) && !data.categories.Any(c => string.Equals(c, card.category, StringComparison.OrdinalIgnoreCase)))
                        data.categories.Add(card.category);
                }
                if (prio.HasValue) card.priority = Mathf.Clamp(prio.Value, 0, 4);
                if (item.newDueDate != null) card.dueDate = item.newDueDate;
                if (cl.HasValue) card.colorLabel = Mathf.Clamp(cl.Value, 0, 16);
                else if (cat != null && card.colorLabel == 0)
                {
                    int defaultCatColor = data.GetCategoryColor(card.category);
                    if (defaultCatColor > 0) card.colorLabel = defaultCatColor;
                }
                if (item.completed.HasValue)
                {
                    card.completed = item.completed.Value;
                    data.SyncLinkedChecklistItems(card.id, card.completed);
                }
                if (item.archived.HasValue) card.archived = item.archived.Value;
                if (ass != null)
                {
                    card.assigneeIds = new List<string>();
                    foreach (var a in ass)
                    {
                        if (string.IsNullOrWhiteSpace(a)) continue;
                        var match = data.assignees.FirstOrDefault(x => x.id == a || string.Equals(x.name, a, StringComparison.OrdinalIgnoreCase));
                        if (match != null && !card.assigneeIds.Contains(match.id))
                            card.assigneeIds.Add(match.id);
                    }
                }

                modified = true;
                successCount++;
                results.Add(new BulkUpdateResult
                {
                    cardIdOrTitle = item.cardIdOrTitle,
                    cardTitle = card.title,
                    cardId = card.id,
                    success = true,
                    message = $"Updated task '{card.title}'."
                });
            }

            if (modified)
            {
                Save(data);
            }

            return (successCount, updateList.Count, results);
        }

        /// <summary>
        /// Moves a card to a different column (and optionally a different board).
        /// </summary>
        public static bool MoveCard(string cardIdOrTitle, string targetColumnNameOrId, string targetBoardNameOrId = null, string sourceBoardNameOrId = null, int targetIndex = -1)
        {
            if (string.IsNullOrWhiteSpace(cardIdOrTitle) || string.IsNullOrWhiteSpace(targetColumnNameOrId))
                return false;

            var data = GetData();
            if (!FindBoardAndCard(data, cardIdOrTitle, sourceBoardNameOrId, out var sourceBoard, out var sourceCol, out var cardToMove))
                return false;

            var destBoard = string.IsNullOrWhiteSpace(targetBoardNameOrId)
                ? sourceBoard
                : (FindBoard(data, targetBoardNameOrId) ?? sourceBoard);

            if (destBoard?.columns == null || destBoard.columns.Count == 0) return false;

            var destCol = FindColumn(destBoard, targetColumnNameOrId);
            if (destCol == null) return false;

            // Remove from source and add to dest
            sourceCol.cards.Remove(cardToMove);
            destCol.cards ??= new List<TaskCard>();
            int insertIdx = (targetIndex < 0 || targetIndex >= destCol.cards.Count) ? destCol.cards.Count : Math.Max(0, targetIndex);
            destCol.cards.Insert(insertIdx, cardToMove);

            // Auto-update completed status if moving into a 'Done' column
            if (destCol.title.IndexOf("Done", StringComparison.OrdinalIgnoreCase) >= 0 || destCol.title.IndexOf("Completed", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                cardToMove.completed = true;
                data.SyncLinkedChecklistItems(cardToMove.id, true);
            }
            else if (sourceCol.title.IndexOf("Done", StringComparison.OrdinalIgnoreCase) >= 0 && destCol.title.IndexOf("Done", StringComparison.OrdinalIgnoreCase) < 0)
            {
                cardToMove.completed = false;
                data.SyncLinkedChecklistItems(cardToMove.id, false);
            }

            Save(data);
            return true;
        }

        /// <summary>
        /// Moves multiple cards to their target columns/boards in a single batch operation.
        /// </summary>
        public static (int successCount, int totalCount, List<BulkMoveResult> results) BulkMoveCards(
            IEnumerable<BulkMoveItem> moves,
            string defaultTargetColumn = null,
            string defaultTargetBoard = null,
            string defaultSourceBoard = null)
        {
            var results = new List<BulkMoveResult>();
            if (moves == null) return (0, 0, results);

            var moveList = moves.ToList();
            if (moveList.Count == 0) return (0, 0, results);

            var data = GetData();
            int successCount = 0;
            bool modified = false;

            foreach (var item in moveList)
            {
                if (item == null || string.IsNullOrWhiteSpace(item.cardIdOrTitle))
                {
                    results.Add(new BulkMoveResult
                    {
                        cardIdOrTitle = item?.cardIdOrTitle ?? "",
                        success = false,
                        message = "CardIdOrTitle is required."
                    });
                    continue;
                }

                string srcBrdName = !string.IsNullOrWhiteSpace(item.sourceBoard) ? item.sourceBoard : defaultSourceBoard;
                string targetColName = !string.IsNullOrWhiteSpace(item.targetColumn) ? item.targetColumn : defaultTargetColumn;
                string targetBrdName = !string.IsNullOrWhiteSpace(item.targetBoard) ? item.targetBoard : defaultTargetBoard;

                if (string.IsNullOrWhiteSpace(targetColName))
                {
                    results.Add(new BulkMoveResult
                    {
                        cardIdOrTitle = item.cardIdOrTitle,
                        success = false,
                        message = "Target column is required."
                    });
                    continue;
                }

                if (!FindBoardAndCard(data, item.cardIdOrTitle, srcBrdName, out var sourceBoard, out var sourceCol, out var cardToMove))
                {
                    results.Add(new BulkMoveResult
                    {
                        cardIdOrTitle = item.cardIdOrTitle,
                        success = false,
                        message = $"Card '{item.cardIdOrTitle}' not found."
                    });
                    continue;
                }

                var destBoard = string.IsNullOrWhiteSpace(targetBrdName)
                    ? sourceBoard
                    : (FindBoard(data, targetBrdName) ?? sourceBoard);

                if (destBoard?.columns == null || destBoard.columns.Count == 0)
                {
                    results.Add(new BulkMoveResult
                    {
                        cardIdOrTitle = item.cardIdOrTitle,
                        cardTitle = cardToMove.title,
                        cardId = cardToMove.id,
                        success = false,
                        message = "Destination board not found or has no columns."
                    });
                    continue;
                }

                var destCol = FindColumn(destBoard, targetColName);
                if (destCol == null)
                {
                    results.Add(new BulkMoveResult
                    {
                        cardIdOrTitle = item.cardIdOrTitle,
                        cardTitle = cardToMove.title,
                        cardId = cardToMove.id,
                        targetBoard = destBoard.name,
                        success = false,
                        message = $"Target column '{targetColName}' not found on board '{destBoard.name}'."
                    });
                    continue;
                }

                // If already in that column on that board
                if (sourceBoard == destBoard && sourceCol == destCol)
                {
                    results.Add(new BulkMoveResult
                    {
                        cardIdOrTitle = item.cardIdOrTitle,
                        cardTitle = cardToMove.title,
                        cardId = cardToMove.id,
                        targetColumn = destCol.title,
                        targetBoard = destBoard.name,
                        success = true,
                        message = $"Card is already in column '{destCol.title}'."
                    });
                    successCount++;
                    continue;
                }

                sourceCol.cards.Remove(cardToMove);
                destCol.cards ??= new List<TaskCard>();
                destCol.cards.Add(cardToMove);
                modified = true;
                successCount++;

                // Auto-update completed status
                if (destCol.title.IndexOf("Done", StringComparison.OrdinalIgnoreCase) >= 0 || destCol.title.IndexOf("Completed", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    cardToMove.completed = true;
                    data.SyncLinkedChecklistItems(cardToMove.id, true);
                }
                else if (sourceCol.title.IndexOf("Done", StringComparison.OrdinalIgnoreCase) >= 0 && destCol.title.IndexOf("Done", StringComparison.OrdinalIgnoreCase) < 0)
                {
                    cardToMove.completed = false;
                    data.SyncLinkedChecklistItems(cardToMove.id, false);
                }

                results.Add(new BulkMoveResult
                {
                    cardIdOrTitle = item.cardIdOrTitle,
                    cardTitle = cardToMove.title,
                    cardId = cardToMove.id,
                    targetColumn = destCol.title,
                    targetBoard = destBoard.name,
                    success = true,
                    message = $"Moved '{cardToMove.title}' to '{destCol.title}' on board '{destBoard.name}'."
                });
            }

            if (modified)
            {
                Save(data);
            }

            return (successCount, moveList.Count, results);
        }

        /// <summary>
        /// Clones/duplicates a task card to a target column (and optionally a target board).
        /// </summary>
        public static TaskCard CloneCard(
            string cardIdOrTitle,
            string targetColumnNameOrId = null,
            string targetBoardNameOrId = null,
            string sourceBoardNameOrId = null,
            string newTitle = null,
            int targetIndex = -1)
        {
            if (string.IsNullOrWhiteSpace(cardIdOrTitle)) return null;

            var data = GetData();
            if (!FindBoardAndCard(data, cardIdOrTitle, sourceBoardNameOrId, out var srcBoard, out var srcCol, out var srcCard))
                return null;

            var destBoard = string.IsNullOrWhiteSpace(targetBoardNameOrId)
                ? srcBoard
                : (FindBoard(data, targetBoardNameOrId) ?? srcBoard);

            if (destBoard?.columns == null || destBoard.columns.Count == 0) return null;

            var destCol = string.IsNullOrWhiteSpace(targetColumnNameOrId)
                ? (destBoard == srcBoard ? srcCol : destBoard.columns[0])
                : FindColumn(destBoard, targetColumnNameOrId);

            if (destCol == null) return null;

            var clonedCard = srcCard.Clone(resetId: true);

            if (!string.IsNullOrWhiteSpace(newTitle))
            {
                clonedCard.title = newTitle.Trim();
            }
            else if (destBoard == srcBoard && destCol == srcCol)
            {
                clonedCard.title = $"{srcCard.title} (Copy)";
            }
            else
            {
                clonedCard.title = srcCard.title;
            }

            destCol.cards ??= new List<TaskCard>();
            int insertIdx = (targetIndex < 0 || targetIndex >= destCol.cards.Count) ? destCol.cards.Count : Math.Max(0, targetIndex);
            destCol.cards.Insert(insertIdx, clonedCard);

            // Auto-update completed status if cloning into a 'Done' column
            if (destCol.title.IndexOf("Done", StringComparison.OrdinalIgnoreCase) >= 0 || destCol.title.IndexOf("Completed", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                clonedCard.completed = true;
            }
            else if (srcCol.title.IndexOf("Done", StringComparison.OrdinalIgnoreCase) >= 0 && destCol.title.IndexOf("Done", StringComparison.OrdinalIgnoreCase) < 0)
            {
                clonedCard.completed = false;
            }

            Save(data);
            return clonedCard;
        }

        /// <summary>
        /// Clones multiple cards to target columns/boards in a single atomic batch operation.
        /// </summary>
        public static (int successCount, int totalCount, List<BulkCloneResult> results) BulkCloneCards(
            IEnumerable<BulkCloneItem> clones,
            string defaultTargetColumn = null,
            string defaultTargetBoard = null,
            string defaultSourceBoard = null)
        {
            var results = new List<BulkCloneResult>();
            if (clones == null) return (0, 0, results);

            var cloneList = clones.ToList();
            if (cloneList.Count == 0) return (0, 0, results);

            var data = GetData();
            int successCount = 0;
            bool modified = false;

            foreach (var item in cloneList)
            {
                if (item == null || string.IsNullOrWhiteSpace(item.cardIdOrTitle))
                {
                    results.Add(new BulkCloneResult
                    {
                        cardIdOrTitle = item?.cardIdOrTitle ?? "",
                        success = false,
                        message = "CardIdOrTitle is required."
                    });
                    continue;
                }

                string srcBrdName = !string.IsNullOrWhiteSpace(item.sourceBoard) ? item.sourceBoard : defaultSourceBoard;
                string targetColName = !string.IsNullOrWhiteSpace(item.targetColumn) ? item.targetColumn : defaultTargetColumn;
                string targetBrdName = !string.IsNullOrWhiteSpace(item.targetBoard) ? item.targetBoard : defaultTargetBoard;

                if (!FindBoardAndCard(data, item.cardIdOrTitle, srcBrdName, out var srcBoard, out var srcCol, out var srcCard))
                {
                    results.Add(new BulkCloneResult
                    {
                        cardIdOrTitle = item.cardIdOrTitle,
                        success = false,
                        message = $"Card '{item.cardIdOrTitle}' not found."
                    });
                    continue;
                }

                var destBoard = string.IsNullOrWhiteSpace(targetBrdName)
                    ? srcBoard
                    : (FindBoard(data, targetBrdName) ?? srcBoard);

                if (destBoard?.columns == null || destBoard.columns.Count == 0)
                {
                    results.Add(new BulkCloneResult
                    {
                        cardIdOrTitle = item.cardIdOrTitle,
                        originalCardId = srcCard.id,
                        success = false,
                        message = "Destination board not found or has no columns."
                    });
                    continue;
                }

                var destCol = string.IsNullOrWhiteSpace(targetColName)
                    ? (destBoard == srcBoard ? srcCol : destBoard.columns[0])
                    : FindColumn(destBoard, targetColName);

                if (destCol == null)
                {
                    results.Add(new BulkCloneResult
                    {
                        cardIdOrTitle = item.cardIdOrTitle,
                        originalCardId = srcCard.id,
                        targetBoard = destBoard.name,
                        success = false,
                        message = $"Target column '{targetColName}' not found on board '{destBoard.name}'."
                    });
                    continue;
                }

                var clonedCard = srcCard.Clone(resetId: true);
                if (!string.IsNullOrWhiteSpace(item.newTitle))
                {
                    clonedCard.title = item.newTitle.Trim();
                }
                else if (destBoard == srcBoard && destCol == srcCol)
                {
                    clonedCard.title = $"{srcCard.title} (Copy)";
                }
                else
                {
                    clonedCard.title = srcCard.title;
                }

                destCol.cards ??= new List<TaskCard>();
                destCol.cards.Add(clonedCard);
                modified = true;
                successCount++;

                // Auto-update completed status
                if (destCol.title.IndexOf("Done", StringComparison.OrdinalIgnoreCase) >= 0 || destCol.title.IndexOf("Completed", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    clonedCard.completed = true;
                }
                else if (srcCol.title.IndexOf("Done", StringComparison.OrdinalIgnoreCase) >= 0 && destCol.title.IndexOf("Done", StringComparison.OrdinalIgnoreCase) < 0)
                {
                    clonedCard.completed = false;
                }

                results.Add(new BulkCloneResult
                {
                    cardIdOrTitle = item.cardIdOrTitle,
                    originalCardId = srcCard.id,
                    clonedCardId = clonedCard.id,
                    clonedCardTitle = clonedCard.title,
                    targetColumn = destCol.title,
                    targetBoard = destBoard.name,
                    success = true,
                    message = $"Cloned '{srcCard.title}' to '{destCol.title}' on board '{destBoard.name}'."
                });
            }

            if (modified)
            {
                Save(data);
            }

            return (successCount, cloneList.Count, results);
        }

        /// <summary>
        /// Reorders a card within its column up, down, to top/bottom, or to a specific target index.
        /// </summary>
        public static (bool success, int newIndex) ReorderCard(string cardIdOrTitle, string direction = null, int? targetIndex = null, string boardNameOrId = null)
        {
            if (string.IsNullOrWhiteSpace(cardIdOrTitle)) return (false, -1);

            var data = GetData();
            TaskCard card = null;
            TaskColumn col = null;

            var searchBoards = !string.IsNullOrWhiteSpace(boardNameOrId)
                ? new List<TaskBoard> { FindBoard(data, boardNameOrId) }.Where(b => b != null).ToList()
                : data.boards;

            foreach (var b in searchBoards)
            {
                if (b?.columns == null) continue;
                foreach (var c in b.columns)
                {
                    if (c?.cards == null) continue;
                    var match = c.cards.FirstOrDefault(x => x != null && (x.id == cardIdOrTitle || string.Equals(x.title, cardIdOrTitle, StringComparison.OrdinalIgnoreCase)));
                    if (match != null)
                    {
                        card = match;
                        col = c;
                        break;
                    }
                }
                if (card != null) break;
            }

            if (card == null || col == null || col.cards.Count == 0) return (false, -1);

            int oldIdx = col.cards.IndexOf(card);
            if (oldIdx < 0) return (false, -1);

            int targetIdx = oldIdx;

            if (targetIndex.HasValue)
            {
                targetIdx = Mathf.Clamp(targetIndex.Value, 0, col.cards.Count - 1);
            }
            else if (!string.IsNullOrWhiteSpace(direction))
            {
                string dir = direction.Trim().ToLowerInvariant();
                switch (dir)
                {
                    case "up":
                    case "move_up":
                    case "previous":
                    case "prev":
                        targetIdx = Mathf.Max(0, oldIdx - 1);
                        break;
                    case "down":
                    case "move_down":
                    case "next":
                        targetIdx = Mathf.Min(col.cards.Count - 1, oldIdx + 1);
                        break;
                    case "top":
                    case "first":
                    case "start":
                        targetIdx = 0;
                        break;
                    case "bottom":
                    case "last":
                    case "end":
                        targetIdx = col.cards.Count - 1;
                        break;
                    default:
                        if (int.TryParse(direction, out int parsedIdx))
                            targetIdx = Mathf.Clamp(parsedIdx, 0, col.cards.Count - 1);
                        break;
                }
            }

            if (targetIdx != oldIdx)
            {
                col.cards.RemoveAt(oldIdx);
                col.cards.Insert(targetIdx, card);
                Save(data);
            }

            return (true, targetIdx);
        }

        public static bool MoveCardUp(string cardIdOrTitle, string boardNameOrId = null)
        {
            return ReorderCard(cardIdOrTitle, "up", null, boardNameOrId).success;
        }

        public static bool MoveCardDown(string cardIdOrTitle, string boardNameOrId = null)
        {
            return ReorderCard(cardIdOrTitle, "down", null, boardNameOrId).success;
        }

        /// <summary>
        /// Deletes a card by ID or title.
        /// </summary>
        public static bool DeleteCard(string cardIdOrTitle)
        {
            if (string.IsNullOrWhiteSpace(cardIdOrTitle)) return false;
            var data = GetData();
            bool removed = false;

            foreach (var b in data.boards)
            {
                if (b?.columns == null) continue;
                foreach (var col in b.columns)
                {
                    if (col?.cards == null) continue;
                    int countBefore = col.cards.Count;
                    col.cards.RemoveAll(c => c != null && (c.id == cardIdOrTitle || string.Equals(c.title, cardIdOrTitle, StringComparison.OrdinalIgnoreCase) || string.Equals(c.taskCode, cardIdOrTitle, StringComparison.OrdinalIgnoreCase)));
                    if (col.cards.Count < countBefore)
                    {
                        removed = true;
                        break;
                    }
                }
                if (removed) break;
            }

            if (removed)
            {
                data.CleanupReferencesToCard(cardIdOrTitle);
                Save(data);
            }
            return removed;
        }

        /// <summary>
        /// Deletes multiple task cards in a single batch operation.
        /// </summary>
        public static (int successCount, int totalCount, List<BulkDeleteCardResult> results) BulkDeleteCards(
            IEnumerable<string> cardIdsOrTitles)
        {
            var results = new List<BulkDeleteCardResult>();
            if (cardIdsOrTitles == null) return (0, 0, results);

            var list = cardIdsOrTitles.ToList();
            if (list.Count == 0) return (0, 0, results);

            var data = GetData();
            int successCount = 0;
            bool modified = false;

            foreach (var idOrTitle in list)
            {
                if (string.IsNullOrWhiteSpace(idOrTitle))
                {
                    results.Add(new BulkDeleteCardResult
                    {
                        cardIdOrTitle = idOrTitle ?? "",
                        success = false,
                        message = "Card ID or title is required."
                    });
                    continue;
                }

                string q = idOrTitle.Trim();
                TaskCard targetCard = null;
                TaskColumn targetCol = null;

                if (data.boards != null)
                {
                    foreach (var b in data.boards)
                    {
                        if (b?.columns == null) continue;
                        foreach (var col in b.columns)
                        {
                            if (col?.cards == null) continue;
                            var card = col.cards.FirstOrDefault(c => c != null && (c.id == q || string.Equals(c.title, q, StringComparison.OrdinalIgnoreCase) || string.Equals(c.taskCode, q, StringComparison.OrdinalIgnoreCase)));
                            if (card != null)
                            {
                                targetCard = card;
                                targetCol = col;
                                break;
                            }
                        }
                        if (targetCard != null) break;
                    }
                }

                if (targetCard == null)
                {
                    results.Add(new BulkDeleteCardResult
                    {
                        cardIdOrTitle = idOrTitle,
                        success = false,
                        message = $"Task '{idOrTitle}' not found."
                    });
                    continue;
                }

                string title = targetCard.title;
                string id = targetCard.id;
                targetCol.cards.Remove(targetCard);
                data.CleanupReferencesToCard(id);
                modified = true;
                successCount++;

                results.Add(new BulkDeleteCardResult
                {
                    cardIdOrTitle = idOrTitle,
                    cardTitle = title,
                    cardId = id,
                    success = true,
                    message = $"Deleted task '{title}'."
                });
            }

            if (modified)
            {
                Save(data);
            }

            return (successCount, list.Count, results);
        }

        /// <summary>
        /// Alias for BulkDeleteCards. Deletes multiple task cards in a single batch operation.
        /// </summary>
        public static (int successCount, int totalCount, List<BulkDeleteCardResult> results) BulkDeleteTasks(
            IEnumerable<string> cardIdsOrTitles) => BulkDeleteCards(cardIdsOrTitles);

        /// <summary>
        /// Deletes all task cards within a specific column.
        /// </summary>
        public static bool DeleteAllCardsInColumn(string boardNameOrId, string columnNameOrId)
        {
            var data = GetData();
            var board = FindBoard(data, boardNameOrId);
            if (board == null) return false;

            var column = FindColumn(board, columnNameOrId);
            if (column == null) return false;

            if (column.cards == null || column.cards.Count == 0) return true;

            foreach (var card in column.cards)
            {
                if (card != null) data.CleanupReferencesToCard(card.id);
            }

            column.cards.Clear();
            Save(data);
            return true;
        }

        /// <summary>
        /// Moves all cards from one board to another.
        /// Matching column names will be preserved, otherwise cards go to the first column.
        /// </summary>
        public static bool MoveAllCardsBetweenBoards(string sourceBoardNameOrId, string targetBoardNameOrId)
        {
            var data = GetData();
            var sourceBoard = FindBoard(data, sourceBoardNameOrId);
            var targetBoard = FindBoard(data, targetBoardNameOrId);

            if (sourceBoard == null || targetBoard == null || sourceBoard == targetBoard) return false;
            if (sourceBoard.columns == null) return true;

            if (targetBoard.columns == null || targetBoard.columns.Count == 0)
            {
                targetBoard.columns ??= new List<TaskColumn>();
                targetBoard.columns.Add(new TaskColumn("To Do"));
            }

            foreach (var sourceCol in sourceBoard.columns)
            {
                if (sourceCol.cards == null || sourceCol.cards.Count == 0) continue;

                var targetCol = targetBoard.columns.FirstOrDefault(c => string.Equals(c.title, sourceCol.title, StringComparison.OrdinalIgnoreCase))
                                ?? targetBoard.columns[0];

                targetCol.cards ??= new List<TaskCard>();
                targetCol.cards.AddRange(sourceCol.cards);
                sourceCol.cards.Clear();
            }

            Save(data);
            return true;
        }

        #endregion

        #region Checklists

        /// <summary>
        /// Adds a checklist item to a card.
        /// </summary>
        public static bool AddChecklistItem(string cardIdOrTitle, string itemText)
        {
            if (string.IsNullOrWhiteSpace(itemText)) return false;
            var data = GetData();
            var card = GetCardInternal(data, cardIdOrTitle);
            if (card == null) return false;

            card.checklistItems ??= new List<string>();
            card.checklistStates ??= new List<bool>();
            card.checklistLinkedCardIds ??= new List<string>();

            card.checklistItems.Add(itemText.Trim());
            card.checklistStates.Add(false);
            card.checklistLinkedCardIds.Add(string.Empty);

            Save(data);
            return true;
        }

        /// <summary>
        /// Sets the checked state of a checklist item.
        /// </summary>
        public static bool SetChecklistItemState(string cardIdOrTitle, int itemIndex, bool isChecked)
        {
            var data = GetData();
            var card = GetCardInternal(data, cardIdOrTitle);
            if (card == null || card.checklistItems == null || itemIndex < 0 || itemIndex >= card.checklistItems.Count)
                return false;

            while (card.checklistStates.Count <= itemIndex) card.checklistStates.Add(false);
            card.checklistStates[itemIndex] = isChecked;

            // Reverse sync: if this checklist item is linked to a child subtask card, update that card's completed status
            if (card.checklistLinkedCardIds != null && itemIndex < card.checklistLinkedCardIds.Count && !string.IsNullOrEmpty(card.checklistLinkedCardIds[itemIndex]))
            {
                string subId = card.checklistLinkedCardIds[itemIndex];
                var subCard = GetCardInternal(data, subId);
                if (subCard != null && subCard.completed != isChecked)
                {
                    subCard.completed = isChecked;
                    data.SyncLinkedChecklistItems(subId, isChecked);
                }
            }

            // Check if all items are completed
            if (card.checklistStates.Count > 0 && card.checklistStates.All(s => s))
            {
                card.completed = true;
                data.SyncLinkedChecklistItems(card.id, true);
            }

            Save(data);
            return true;
        }

        /// <summary>
        /// Removes a checklist item by index.
        /// </summary>
        public static bool RemoveChecklistItem(string cardIdOrTitle, int itemIndex)
        {
            var data = GetData();
            var card = GetCardInternal(data, cardIdOrTitle);
            if (card == null || card.checklistItems == null || itemIndex < 0 || itemIndex >= card.checklistItems.Count)
                return false;

            card.checklistItems.RemoveAt(itemIndex);
            if (itemIndex < card.checklistStates.Count) card.checklistStates.RemoveAt(itemIndex);
            if (itemIndex < card.checklistLinkedCardIds.Count) card.checklistLinkedCardIds.RemoveAt(itemIndex);

            Save(data);
            return true;
        }

        #endregion

        #region Parent & Child Subtasks

        /// <summary>
        /// Definition structure for creating batch subtasks.
        /// </summary>
        public class SubtaskDefinition
        {
            public string title;
            public string description;
            public string category;
            public int priority;
            public string dueDate;
            public string column;
            public List<string> checklist;
            public List<string> assignees;

            public SubtaskDefinition() { }
            public SubtaskDefinition(string title, string description = "", string category = "", int priority = 0)
            {
                this.title = title;
                this.description = description;
                this.category = category;
                this.priority = priority;
            }
        }

        /// <summary>
        /// Hierarchy details for a task card including parent cards and child subtask cards.
        /// </summary>
        public class TaskHierarchyNode
        {
            public TaskCard card;
            public List<TaskCard> parents = new List<TaskCard>();
            public List<TaskCard> children = new List<TaskCard>();
            public int totalSubtasks => children.Count;
            public int completedSubtasks => children.Count(c => c != null && c.completed);
            public float completionPercent => totalSubtasks > 0 ? ((float)completedSubtasks / totalSubtasks) * 100f : (card != null && card.completed ? 100f : 0f);
        }

        /// <summary>
        /// Creates a child task card and links it as a subtask in the parent card's checklist.
        /// If targetColumn is not specified, uses the parent card's column.
        /// </summary>
        public static TaskCard CreateSubtask(
            string parentCardIdOrTitle,
            string subtaskTitle,
            string subtaskDescription = "",
            string columnNameOrId = null,
            string category = null,
            int? priority = null,
            string dueDate = null,
            int? colorLabel = null,
            List<string> checklist = null,
            List<string> assignees = null)
        {
            if (string.IsNullOrWhiteSpace(parentCardIdOrTitle) || string.IsNullOrWhiteSpace(subtaskTitle))
                return null;

            var data = GetData();
            TaskCard parentCard = null;
            TaskBoard parentBoard = null;
            TaskColumn parentCol = null;

            foreach (var b in data.boards)
            {
                if (b?.columns == null) continue;
                foreach (var col in b.columns)
                {
                    if (col?.cards == null) continue;
                    var match = col.cards.FirstOrDefault(c => c != null && (c.id == parentCardIdOrTitle || string.Equals(c.title, parentCardIdOrTitle, StringComparison.OrdinalIgnoreCase) || string.Equals(c.taskCode, parentCardIdOrTitle, StringComparison.OrdinalIgnoreCase)));
                    if (match != null)
                    {
                        parentCard = match;
                        parentBoard = b;
                        parentCol = col;
                        break;
                    }
                }
                if (parentCard != null) break;
            }

            if (parentCard == null || parentBoard == null) return null;

            var targetCol = string.IsNullOrWhiteSpace(columnNameOrId)
                ? parentCol
                : FindColumn(parentBoard, columnNameOrId);

            if (targetCol == null) targetCol = parentCol;
            targetCol.cards ??= new List<TaskCard>();

            string effectiveCategory = !string.IsNullOrEmpty(category) ? category.Trim() : parentCard.category;
            int effectivePriority = priority.HasValue ? Mathf.Clamp(priority.Value, 0, 4) : parentCard.priority;
            int effectiveColor = colorLabel.HasValue ? Mathf.Clamp(colorLabel.Value, 0, 16) : parentCard.colorLabel;

            var childCard = new TaskCard(subtaskTitle.Trim())
            {
                description = subtaskDescription ?? "",
                category = effectiveCategory ?? "",
                priority = effectivePriority,
                dueDate = dueDate ?? "",
                colorLabel = effectiveColor
            };

            if (checklist != null && checklist.Count > 0)
            {
                foreach (var item in checklist)
                {
                    if (!string.IsNullOrWhiteSpace(item))
                    {
                        childCard.checklistItems.Add(item.Trim());
                        childCard.checklistStates.Add(false);
                        childCard.checklistLinkedCardIds.Add(string.Empty);
                    }
                }
            }

            if (assignees != null && assignees.Count > 0)
            {
                foreach (var a in assignees)
                {
                    if (string.IsNullOrWhiteSpace(a)) continue;
                    var match = data.assignees?.FirstOrDefault(x => x.id == a || string.Equals(x.name, a, StringComparison.OrdinalIgnoreCase));
                    if (match != null && !childCard.assigneeIds.Contains(match.id))
                        childCard.assigneeIds.Add(match.id);
                }
            }

            targetCol.cards.Add(childCard);

            // Link in parent checklist
            parentCard.checklistItems ??= new List<string>();
            parentCard.checklistStates ??= new List<bool>();
            parentCard.checklistLinkedCardIds ??= new List<string>();

            while (parentCard.checklistStates.Count < parentCard.checklistItems.Count) parentCard.checklistStates.Add(false);
            while (parentCard.checklistLinkedCardIds.Count < parentCard.checklistItems.Count) parentCard.checklistLinkedCardIds.Add(string.Empty);

            parentCard.checklistItems.Add(childCard.title);
            parentCard.checklistStates.Add(childCard.completed);
            parentCard.checklistLinkedCardIds.Add(childCard.id);

            Save(data);
            return childCard;
        }

        /// <summary>
        /// Links an existing card as a child subtask under a parent card.
        /// Prevents self-linking and circular hierarchies.
        /// </summary>
        public static bool LinkChildTask(string parentCardIdOrTitle, string childCardIdOrTitle, string checklistItemTitle = null)
        {
            if (string.IsNullOrWhiteSpace(parentCardIdOrTitle) || string.IsNullOrWhiteSpace(childCardIdOrTitle))
                return false;

            var data = GetData();
            var parentCard = GetCardInternal(data, parentCardIdOrTitle);
            var childCard = GetCardInternal(data, childCardIdOrTitle);

            if (parentCard == null || childCard == null) return false;
            if (parentCard.id == childCard.id) return false;

            // Prevent circular dependencies
            if (IsDescendantOf(data, parentCard.id, childCard.id))
                return false;

            parentCard.checklistItems ??= new List<string>();
            parentCard.checklistStates ??= new List<bool>();
            parentCard.checklistLinkedCardIds ??= new List<string>();

            while (parentCard.checklistStates.Count < parentCard.checklistItems.Count) parentCard.checklistStates.Add(false);
            while (parentCard.checklistLinkedCardIds.Count < parentCard.checklistItems.Count) parentCard.checklistLinkedCardIds.Add(string.Empty);

            int existingIndex = parentCard.checklistLinkedCardIds.IndexOf(childCard.id);
            if (existingIndex >= 0)
            {
                if (!string.IsNullOrWhiteSpace(checklistItemTitle))
                    parentCard.checklistItems[existingIndex] = checklistItemTitle.Trim();
                parentCard.checklistStates[existingIndex] = childCard.completed;
            }
            else
            {
                // Check if there is an unlinked checklist item matching the title
                int titleMatchIdx = -1;
                for (int i = 0; i < parentCard.checklistItems.Count; i++)
                {
                    if (string.IsNullOrEmpty(parentCard.checklistLinkedCardIds[i]) && 
                        string.Equals(parentCard.checklistItems[i], childCard.title, StringComparison.OrdinalIgnoreCase))
                    {
                        titleMatchIdx = i;
                        break;
                    }
                }

                if (titleMatchIdx >= 0)
                {
                    parentCard.checklistLinkedCardIds[titleMatchIdx] = childCard.id;
                    parentCard.checklistStates[titleMatchIdx] = childCard.completed;
                    if (!string.IsNullOrWhiteSpace(checklistItemTitle))
                        parentCard.checklistItems[titleMatchIdx] = checklistItemTitle.Trim();
                }
                else
                {
                    string label = !string.IsNullOrWhiteSpace(checklistItemTitle) ? checklistItemTitle.Trim() : childCard.title;
                    parentCard.checklistItems.Add(label);
                    parentCard.checklistStates.Add(childCard.completed);
                    parentCard.checklistLinkedCardIds.Add(childCard.id);
                }
            }

            Save(data);
            return true;
        }

        /// <summary>
        /// Unlinks a child subtask from a parent card without deleting either card.
        /// </summary>
        public static bool UnlinkChildTask(string parentCardIdOrTitle, string childCardIdOrTitle, bool removeChecklistItem = false)
        {
            if (string.IsNullOrWhiteSpace(parentCardIdOrTitle) || string.IsNullOrWhiteSpace(childCardIdOrTitle))
                return false;

            var data = GetData();
            var parentCard = GetCardInternal(data, parentCardIdOrTitle);
            var childCard = GetCardInternal(data, childCardIdOrTitle);

            if (parentCard == null || childCard == null || parentCard.checklistLinkedCardIds == null)
                return false;

            int idx = parentCard.checklistLinkedCardIds.IndexOf(childCard.id);
            if (idx < 0) return false;

            if (removeChecklistItem)
            {
                parentCard.checklistItems.RemoveAt(idx);
                if (idx < parentCard.checklistStates.Count) parentCard.checklistStates.RemoveAt(idx);
                parentCard.checklistLinkedCardIds.RemoveAt(idx);
            }
            else
            {
                parentCard.checklistLinkedCardIds[idx] = string.Empty;
            }

            Save(data);
            return true;
        }

        /// <summary>
        /// Returns all child subtask cards linked to the specified parent card.
        /// </summary>
        public static List<TaskCard> GetSubtasks(string parentCardIdOrTitle)
        {
            var data = GetData();
            var parentCard = GetCardInternal(data, parentCardIdOrTitle);
            if (parentCard?.checklistLinkedCardIds == null || parentCard.checklistLinkedCardIds.Count == 0)
                return new List<TaskCard>();

            var result = new List<TaskCard>();
            foreach (var childId in parentCard.checklistLinkedCardIds)
            {
                if (string.IsNullOrEmpty(childId)) continue;
                var child = GetCardInternal(data, childId);
                if (child != null && !result.Contains(child))
                    result.Add(child);
            }
            return result;
        }

        /// <summary>
        /// Returns the parent task card(s) that link this card as a child subtask.
        /// </summary>
        public static List<TaskCard> GetParentTasks(string childCardIdOrTitle)
        {
            var data = GetData();
            var childCard = GetCardInternal(data, childCardIdOrTitle);
            if (childCard == null) return new List<TaskCard>();

            var result = new List<TaskCard>();
            foreach (var card in GetCards())
            {
                if (card?.checklistLinkedCardIds != null && card.checklistLinkedCardIds.Contains(childCard.id))
                {
                    result.Add(card);
                }
            }
            return result;
        }

        /// <summary>
        /// Returns a hierarchy node containing the card, its parent cards, and child subtasks with metrics.
        /// </summary>
        public static TaskHierarchyNode GetTaskHierarchy(string cardIdOrTitle)
        {
            var data = GetData();
            var card = GetCardInternal(data, cardIdOrTitle);
            if (card == null) return null;

            var node = new TaskHierarchyNode { card = card };
            node.parents = GetParentTasks(card.id);
            node.children = GetSubtasks(card.id);
            return node;
        }

        /// <summary>
        /// Creates an Epic / Parent task along with multiple linked child subtasks in one atomic operation.
        /// </summary>
        public static (TaskCard parentCard, List<TaskCard> childCards) CreateHierarchicalTask(
            string boardNameOrId,
            string columnNameOrId,
            string parentTitle,
            string parentDescription,
            string parentCategory,
            int parentPriority,
            List<SubtaskDefinition> subtasks,
            string dueDate = null,
            int colorLabel = 0)
        {
            if (string.IsNullOrWhiteSpace(parentTitle)) return (null, new List<TaskCard>());

            var data = GetData();
            var board = FindBoard(data, boardNameOrId);
            if (board == null)
            {
                board = new TaskBoard(!string.IsNullOrWhiteSpace(boardNameOrId) ? boardNameOrId.Trim() : "My First Board");
                data.boards.Add(board);
            }

            board.columns ??= new List<TaskColumn>();
            if (board.columns.Count == 0)
            {
                board.columns.Add(new TaskColumn("📋 To Do"));
                board.columns.Add(new TaskColumn("🔨 In Progress"));
                board.columns.Add(new TaskColumn("✅ Done"));
            }

            var column = FindColumn(board, columnNameOrId);
            column.cards ??= new List<TaskCard>();

            var parentCard = new TaskCard(parentTitle.Trim())
            {
                description = parentDescription ?? "",
                category = parentCategory != null ? parentCategory.Trim() : "",
                priority = Mathf.Clamp(parentPriority, 0, 4),
                dueDate = dueDate ?? "",
                colorLabel = Mathf.Clamp(colorLabel, 0, 16)
            };

            if (parentCard.colorLabel == 0 && !string.IsNullOrEmpty(parentCard.category))
            {
                int defaultCatColor = data.GetCategoryColor(parentCard.category);
                if (defaultCatColor > 0) parentCard.colorLabel = defaultCatColor;
            }

            if (!string.IsNullOrEmpty(parentCard.category))
            {
                data.categories ??= new List<string>();
                if (!data.categories.Any(c => string.Equals(c, parentCard.category, StringComparison.OrdinalIgnoreCase)))
                    data.categories.Add(parentCard.category);
            }

            column.cards.Add(parentCard);

            var createdChildren = new List<TaskCard>();

            if (subtasks != null && subtasks.Count > 0)
            {
                foreach (var def in subtasks)
                {
                    if (def == null || string.IsNullOrWhiteSpace(def.title)) continue;

                    var targetCol = string.IsNullOrWhiteSpace(def.column) ? column : (FindColumn(board, def.column) ?? column);
                    targetCol.cards ??= new List<TaskCard>();

                    string cat = !string.IsNullOrWhiteSpace(def.category) ? def.category.Trim() : parentCard.category;
                    int pri = def.priority > 0 ? Mathf.Clamp(def.priority, 0, 4) : parentCard.priority;

                    var childCard = new TaskCard(def.title.Trim())
                    {
                        description = def.description ?? "",
                        category = cat ?? "",
                        priority = pri,
                        dueDate = def.dueDate ?? "",
                        colorLabel = parentCard.colorLabel
                    };

                    if (def.checklist != null)
                    {
                        foreach (var cl in def.checklist)
                        {
                            if (!string.IsNullOrWhiteSpace(cl))
                            {
                                childCard.checklistItems.Add(cl.Trim());
                                childCard.checklistStates.Add(false);
                                childCard.checklistLinkedCardIds.Add(string.Empty);
                            }
                        }
                    }

                    targetCol.cards.Add(childCard);
                    createdChildren.Add(childCard);

                    parentCard.checklistItems.Add(childCard.title);
                    parentCard.checklistStates.Add(false);
                    parentCard.checklistLinkedCardIds.Add(childCard.id);
                }
            }

            Save(data);
            return (parentCard, createdChildren);
        }

        /// <summary>
        /// Helper to detect circular parent-child relationships.
        /// </summary>
        public static bool IsDescendantOf(SaveData data, string childIdToFind, string currentParentId, HashSet<string> visited = null)
        {
            if (string.IsNullOrEmpty(childIdToFind) || string.IsNullOrEmpty(currentParentId)) return false;
            visited ??= new HashSet<string>();
            if (!visited.Add(currentParentId)) return false;

            var parent = GetCardInternal(data, currentParentId);
            if (parent?.checklistLinkedCardIds == null) return false;

            foreach (var cid in parent.checklistLinkedCardIds)
            {
                if (string.IsNullOrEmpty(cid)) continue;
                if (cid == childIdToFind) return true;
                if (IsDescendantOf(data, childIdToFind, cid, visited)) return true;
            }
            return false;
        }

        #endregion

        #region Quick Notes & Folders

        /// <summary>
        /// Retrieves all notes, optionally filtered by folder.
        /// </summary>
        public static List<QuickNote> GetNotes(string folderNameOrId = null)
        {
            var data = GetData();
            if (data.notes == null) return new List<QuickNote>();

            if (string.IsNullOrWhiteSpace(folderNameOrId))
                return data.notes.Where(n => n != null).ToList();

            string q = folderNameOrId.Trim();
            var folder = data.noteFolders?.FirstOrDefault(f => f != null && (f.id == q || string.Equals(f.name, q, StringComparison.OrdinalIgnoreCase)));
            string targetFolderId = folder?.id ?? q;

            return data.notes.Where(n => n != null && (n.folderId == targetFolderId || string.Equals(n.folderId, targetFolderId, StringComparison.OrdinalIgnoreCase))).ToList();
        }

        /// <summary>
        /// Gets a note by ID or title.
        /// </summary>
        public static QuickNote GetNote(string noteIdOrTitle)
        {
            if (string.IsNullOrWhiteSpace(noteIdOrTitle)) return null;
            var data = GetData();
            string q = noteIdOrTitle.Trim();
            return data.notes?.FirstOrDefault(n => n != null && (n.id == q || string.Equals(n.title, q, StringComparison.OrdinalIgnoreCase)));
        }

        /// <summary>
        /// Creates a new Markdown Quick Note.
        /// </summary>
        public static QuickNote CreateNote(
            string title,
            string markdownContent,
            string folderName = "",
            List<string> tags = null,
            bool pinned = false,
            int colorIndex = 0)
        {
            if (string.IsNullOrWhiteSpace(title)) return null;
            var data = GetData();

            string folderId = "";
            if (!string.IsNullOrWhiteSpace(folderName))
            {
                data.noteFolders ??= new List<NoteFolder>();
                var existingFolder = data.noteFolders.FirstOrDefault(f => f != null && string.Equals(f.name, folderName.Trim(), StringComparison.OrdinalIgnoreCase));
                if (existingFolder != null)
                {
                    folderId = existingFolder.id;
                }
                else
                {
                    var newFolder = new NoteFolder(folderName.Trim());
                    data.noteFolders.Add(newFolder);
                    folderId = newFolder.id;
                }
            }

            var note = new QuickNote
            {
                id = Guid.NewGuid().ToString(),
                title = title.Trim(),
                content = markdownContent ?? "",
                folderId = folderId,
                pinned = pinned,
                colorIndex = Mathf.Clamp(colorIndex, 0, 16),
                tags = tags ?? new List<string>()
            };

            data.notes ??= new List<QuickNote>();
            data.notes.Insert(0, note); // Prepend to top
            Save(data);
            return note;
        }

        /// <summary>
        /// Updates an existing Quick Note.
        /// </summary>
        public static QuickNote UpdateNote(
            string noteIdOrTitle,
            string newTitle = null,
            string newContent = null,
            string newFolderName = null,
            List<string> newTags = null,
            bool? pinned = null,
            int? colorIndex = null)
        {
            if (string.IsNullOrWhiteSpace(noteIdOrTitle)) return null;
            var data = GetData();
            string q = noteIdOrTitle.Trim();
            var note = data.notes?.FirstOrDefault(n => n != null && (n.id == q || string.Equals(n.title, q, StringComparison.OrdinalIgnoreCase)));
            if (note == null) return null;

            if (newTitle != null) note.title = newTitle.Trim();
            if (newContent != null) note.content = newContent;
            if (newFolderName != null)
            {
                if (string.IsNullOrWhiteSpace(newFolderName))
                {
                    note.folderId = "";
                }
                else
                {
                    var folder = data.noteFolders.FirstOrDefault(f => string.Equals(f.name, newFolderName.Trim(), StringComparison.OrdinalIgnoreCase));
                    if (folder == null)
                    {
                        folder = new NoteFolder(newFolderName.Trim());
                        data.noteFolders.Add(folder);
                    }
                    note.folderId = folder.id;
                }
            }
            if (newTags != null) note.tags = newTags;
            if (pinned.HasValue) note.pinned = pinned.Value;
            if (colorIndex.HasValue) note.colorIndex = Mathf.Clamp(colorIndex.Value, 0, 16);

            note.modifiedDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
            Save(data);
            return note;
        }

        /// <summary>
        /// Deletes a note by ID or title.
        /// </summary>
        public static bool DeleteNote(string noteIdOrTitle)
        {
            if (string.IsNullOrWhiteSpace(noteIdOrTitle)) return false;
            var data = GetData();
            if (data.notes == null) return false;

            string q = noteIdOrTitle.Trim();
            int countBefore = data.notes.Count;
            data.notes.RemoveAll(n => n != null && (n.id == q || string.Equals(n.title, q, StringComparison.OrdinalIgnoreCase)));
            if (data.notes.Count < countBefore)
            {
                Save(data);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Retrieves all note folders.
        /// </summary>
        public static List<NoteFolder> GetFolders()
        {
            var data = GetData();
            return data.noteFolders ?? new List<NoteFolder>();
        }

        /// <summary>
        /// Creates a new note folder.
        /// </summary>
        public static NoteFolder CreateFolder(string folderName)
        {
            if (string.IsNullOrWhiteSpace(folderName)) return null;
            var data = GetData();
            data.noteFolders ??= new List<NoteFolder>();

            var existing = data.noteFolders.FirstOrDefault(f => string.Equals(f.name, folderName.Trim(), StringComparison.OrdinalIgnoreCase));
            if (existing != null) return existing;

            var newFolder = new NoteFolder(folderName.Trim());
            data.noteFolders.Add(newFolder);
            Save(data);
            return newFolder;
        }

        /// <summary>
        /// Creates multiple Markdown Quick Notes in a single atomic batch operation.
        /// </summary>
        public static (int successCount, int totalCount, List<BulkCreateNoteResult> results) BulkCreateNotes(
            IEnumerable<BulkCreateNoteItem> noteItems,
            string defaultFolderName = "AI Notes",
            List<string> defaultTags = null)
        {
            var results = new List<BulkCreateNoteResult>();
            if (noteItems == null) return (0, 0, results);

            var itemList = noteItems.ToList();
            if (itemList.Count == 0) return (0, 0, results);

            var data = GetData();
            data.notes ??= new List<QuickNote>();
            data.noteFolders ??= new List<NoteFolder>();

            int successCount = 0;
            bool modified = false;

            foreach (var item in itemList)
            {
                if (item == null || string.IsNullOrWhiteSpace(item.title))
                {
                    results.Add(new BulkCreateNoteResult
                    {
                        title = item?.title ?? "",
                        success = false,
                        message = "Note title is required."
                    });
                    continue;
                }

                string fName = !string.IsNullOrWhiteSpace(item.folderName) ? item.folderName.Trim() : defaultFolderName?.Trim();
                string folderId = "";
                if (!string.IsNullOrEmpty(fName) && !string.Equals(fName, "Root", StringComparison.OrdinalIgnoreCase) && !string.Equals(fName, "Unfiled", StringComparison.OrdinalIgnoreCase))
                {
                    var folder = data.noteFolders.FirstOrDefault(f => f != null && (f.id == fName || string.Equals(f.name, fName, StringComparison.OrdinalIgnoreCase)));
                    if (folder == null)
                    {
                        folder = new NoteFolder(fName);
                        data.noteFolders.Add(folder);
                    }
                    folderId = folder.id;
                }

                var tags = item.tags ?? defaultTags ?? new List<string>();

                var note = new QuickNote
                {
                    id = Guid.NewGuid().ToString(),
                    title = item.title.Trim(),
                    content = item.content ?? "",
                    folderId = folderId,
                    pinned = item.pinned,
                    colorIndex = Mathf.Clamp(item.colorIndex, 0, 16),
                    tags = new List<string>(tags)
                };

                data.notes.Insert(0, note);
                modified = true;
                successCount++;
                results.Add(new BulkCreateNoteResult
                {
                    title = note.title,
                    noteId = note.id,
                    success = true,
                    message = $"Created note '{note.title}'."
                });
            }

            if (modified)
            {
                Save(data);
            }

            return (successCount, itemList.Count, results);
        }

        /// <summary>
        /// Updates multiple Quick Notes (title, content, folder, tags, color, pinned status) in a single atomic batch operation.
        /// </summary>
        public static (int successCount, int totalCount, List<BulkUpdateNoteResult> results) BulkUpdateNotes(
            IEnumerable<BulkUpdateNoteItem> updates,
            string defaultFolderName = null,
            List<string> defaultTags = null,
            List<string> addTags = null,
            List<string> removeTags = null,
            bool? defaultPinned = null,
            int? defaultColorIndex = null,
            string appendContent = null)
        {
            var results = new List<BulkUpdateNoteResult>();
            if (updates == null) return (0, 0, results);

            var updateList = updates.ToList();
            if (updateList.Count == 0) return (0, 0, results);

            var data = GetData();
            if (data.notes == null) data.notes = new List<QuickNote>();
            data.noteFolders ??= new List<NoteFolder>();

            int successCount = 0;
            bool modified = false;

            foreach (var item in updateList)
            {
                if (item == null || string.IsNullOrWhiteSpace(item.noteIdOrTitle))
                {
                    results.Add(new BulkUpdateNoteResult
                    {
                        noteIdOrTitle = item?.noteIdOrTitle ?? "",
                        success = false,
                        message = "NoteIdOrTitle is required."
                    });
                    continue;
                }

                string q = item.noteIdOrTitle.Trim();
                var note = data.notes.FirstOrDefault(n => n != null && (n.id == q || string.Equals(n.title, q, StringComparison.OrdinalIgnoreCase)));
                if (note == null)
                {
                    results.Add(new BulkUpdateNoteResult
                    {
                        noteIdOrTitle = item.noteIdOrTitle,
                        success = false,
                        message = $"Note '{item.noteIdOrTitle}' not found."
                    });
                    continue;
                }

                if (item.newTitle != null) note.title = item.newTitle.Trim();
                if (item.newContent != null) note.content = item.newContent;

                string toAppend = item.appendContent ?? appendContent;
                if (!string.IsNullOrEmpty(toAppend))
                {
                    note.content = string.IsNullOrEmpty(note.content) ? toAppend : (note.content + "\n\n" + toAppend);
                }

                string fName = item.newFolderName ?? defaultFolderName;
                if (fName != null)
                {
                    string fTrim = fName.Trim();
                    if (string.IsNullOrEmpty(fTrim) || string.Equals(fTrim, "Root", StringComparison.OrdinalIgnoreCase) || string.Equals(fTrim, "Unfiled", StringComparison.OrdinalIgnoreCase))
                    {
                        note.folderId = "";
                    }
                    else
                    {
                        var folder = data.noteFolders.FirstOrDefault(f => f != null && (f.id == fTrim || string.Equals(f.name, fTrim, StringComparison.OrdinalIgnoreCase)));
                        if (folder == null)
                        {
                            folder = new NoteFolder(fTrim);
                            data.noteFolders.Add(folder);
                        }
                        note.folderId = folder.id;
                    }
                }

                var tagsToSet = item.newTags ?? defaultTags;
                if (tagsToSet != null)
                {
                    note.tags = new List<string>(tagsToSet);
                }

                var tagsToAdd = item.addTags ?? addTags;
                if (tagsToAdd != null)
                {
                    note.tags ??= new List<string>();
                    foreach (var t in tagsToAdd)
                    {
                        if (string.IsNullOrWhiteSpace(t)) continue;
                        string trimmed = t.Trim();
                        if (!note.tags.Any(existing => string.Equals(existing, trimmed, StringComparison.OrdinalIgnoreCase)))
                            note.tags.Add(trimmed);
                    }
                }

                var tagsToRemove = item.removeTags ?? removeTags;
                if (tagsToRemove != null && note.tags != null)
                {
                    foreach (var t in tagsToRemove)
                    {
                        if (string.IsNullOrWhiteSpace(t)) continue;
                        string trimmed = t.Trim();
                        note.tags.RemoveAll(existing => string.Equals(existing, trimmed, StringComparison.OrdinalIgnoreCase));
                    }
                }

                bool? pin = item.pinned ?? defaultPinned;
                if (pin.HasValue) note.pinned = pin.Value;

                int? color = item.colorIndex ?? defaultColorIndex;
                if (color.HasValue) note.colorIndex = Mathf.Clamp(color.Value, 0, 16);

                note.modifiedDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
                modified = true;
                successCount++;

                results.Add(new BulkUpdateNoteResult
                {
                    noteIdOrTitle = item.noteIdOrTitle,
                    noteTitle = note.title,
                    noteId = note.id,
                    success = true,
                    message = $"Updated note '{note.title}'."
                });
            }

            if (modified)
            {
                Save(data);
            }

            return (successCount, updateList.Count, results);
        }

        /// <summary>
        /// Moves multiple Quick Notes to a target folder in a single atomic batch operation.
        /// </summary>
        public static (int successCount, int totalCount, List<BulkMoveNoteResult> results) BulkMoveNotes(
            IEnumerable<BulkMoveNoteItem> moves,
            string defaultTargetFolderName = "")
        {
            var results = new List<BulkMoveNoteResult>();
            if (moves == null) return (0, 0, results);

            var moveList = moves.ToList();
            if (moveList.Count == 0) return (0, 0, results);

            var data = GetData();
            if (data.notes == null) data.notes = new List<QuickNote>();
            data.noteFolders ??= new List<NoteFolder>();

            int successCount = 0;
            bool modified = false;

            foreach (var item in moveList)
            {
                if (item == null || string.IsNullOrWhiteSpace(item.noteIdOrTitle))
                {
                    results.Add(new BulkMoveNoteResult
                    {
                        noteIdOrTitle = item?.noteIdOrTitle ?? "",
                        success = false,
                        message = "NoteIdOrTitle is required."
                    });
                    continue;
                }

                string q = item.noteIdOrTitle.Trim();
                var note = data.notes.FirstOrDefault(n => n != null && (n.id == q || string.Equals(n.title, q, StringComparison.OrdinalIgnoreCase)));
                if (note == null)
                {
                    results.Add(new BulkMoveNoteResult
                    {
                        noteIdOrTitle = item.noteIdOrTitle,
                        success = false,
                        message = $"Note '{item.noteIdOrTitle}' not found."
                    });
                    continue;
                }

                string targetFolder = item.targetFolderName ?? defaultTargetFolderName ?? "";
                string targetFolderTrim = targetFolder.Trim();
                string targetFolderId = "";

                if (!string.IsNullOrEmpty(targetFolderTrim) && !string.Equals(targetFolderTrim, "Root", StringComparison.OrdinalIgnoreCase) && !string.Equals(targetFolderTrim, "Unfiled", StringComparison.OrdinalIgnoreCase))
                {
                    var folder = data.noteFolders.FirstOrDefault(f => f != null && (f.id == targetFolderTrim || string.Equals(f.name, targetFolderTrim, StringComparison.OrdinalIgnoreCase)));
                    if (folder == null)
                    {
                        folder = new NoteFolder(targetFolderTrim);
                        data.noteFolders.Add(folder);
                    }
                    targetFolderId = folder.id;
                }

                note.folderId = targetFolderId;
                note.modifiedDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
                modified = true;
                successCount++;

                results.Add(new BulkMoveNoteResult
                {
                    noteIdOrTitle = item.noteIdOrTitle,
                    noteTitle = note.title,
                    noteId = note.id,
                    targetFolderName = string.IsNullOrEmpty(targetFolderTrim) ? "Unfiled (Root)" : targetFolderTrim,
                    success = true,
                    message = $"Moved note '{note.title}' to folder '{(string.IsNullOrEmpty(targetFolderTrim) ? "Unfiled (Root)" : targetFolderTrim)}'."
                });
            }

            if (modified)
            {
                Save(data);
            }

            return (successCount, moveList.Count, results);
        }

        /// <summary>
        /// Deletes multiple Quick Notes in a single batch operation.
        /// </summary>
        public static (int successCount, int totalCount, List<BulkDeleteNoteResult> results) BulkDeleteNotes(
            IEnumerable<string> noteIdsOrTitles)
        {
            var results = new List<BulkDeleteNoteResult>();
            if (noteIdsOrTitles == null) return (0, 0, results);

            var list = noteIdsOrTitles.ToList();
            if (list.Count == 0) return (0, 0, results);

            var data = GetData();
            if (data.notes == null || data.notes.Count == 0)
            {
                foreach (var idOrTitle in list)
                {
                    results.Add(new BulkDeleteNoteResult
                    {
                        noteIdOrTitle = idOrTitle,
                        success = false,
                        message = "No notes exist in project."
                    });
                }
                return (0, list.Count, results);
            }

            int successCount = 0;
            bool modified = false;

            foreach (var idOrTitle in list)
            {
                if (string.IsNullOrWhiteSpace(idOrTitle))
                {
                    results.Add(new BulkDeleteNoteResult
                    {
                        noteIdOrTitle = idOrTitle ?? "",
                        success = false,
                        message = "Note ID or title is required."
                    });
                    continue;
                }

                string q = idOrTitle.Trim();
                var note = data.notes.FirstOrDefault(n => n != null && (n.id == q || string.Equals(n.title, q, StringComparison.OrdinalIgnoreCase)));
                if (note == null)
                {
                    results.Add(new BulkDeleteNoteResult
                    {
                        noteIdOrTitle = idOrTitle,
                        success = false,
                        message = $"Note '{idOrTitle}' not found."
                    });
                    continue;
                }

                string title = note.title;
                string id = note.id;
                data.notes.Remove(note);
                modified = true;
                successCount++;

                results.Add(new BulkDeleteNoteResult
                {
                    noteIdOrTitle = idOrTitle,
                    noteTitle = title,
                    noteId = id,
                    success = true,
                    message = $"Deleted note '{title}'."
                });
            }

            if (modified)
            {
                Save(data);
            }

            return (successCount, list.Count, results);
        }

        #endregion

        #region Linked Items

        /// <summary>
        /// Links an existing note to a task card.
        /// </summary>
        public static bool LinkNoteToCard(string cardIdOrTitle, string noteIdOrTitle)
        {
            var data = GetData();
            var card = GetCardInternal(data, cardIdOrTitle);
            var note = data.notes?.FirstOrDefault(n => n != null && (n.id == noteIdOrTitle || string.Equals(n.title, noteIdOrTitle, StringComparison.OrdinalIgnoreCase)));
            if (card == null || note == null) return false;

            card.linkedItems ??= new List<LinkedItem>();
            if (card.linkedItems.Any(li => li.isNote && li.guid == note.id)) return true;

            var item = LinkedItem.CreateNote(note.id);
            item.displayName = note.title;
            card.linkedItems.Add(item);
            Save(data);
            return true;
        }

        /// <summary>
        /// Links an external URL to a task card.
        /// </summary>
        public static bool LinkUrlToCard(string cardIdOrTitle, string url, string displayName = "")
        {
            if (string.IsNullOrWhiteSpace(url)) return false;
            var data = GetData();
            var card = GetCardInternal(data, cardIdOrTitle);
            if (card == null) return false;

            card.linkedItems ??= new List<LinkedItem>();
            string formattedUrl = url.Trim();
            if (!formattedUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) && !formattedUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                formattedUrl = "https://" + formattedUrl;

            card.linkedItems.Add(LinkedItem.CreateUrl(formattedUrl, displayName));
            Save(data);
            return true;
        }

        /// <summary>
        /// Links a Unity project asset GUID or asset path to a task card.
        /// </summary>
        public static bool LinkAssetToCard(string cardIdOrTitle, string assetGuidOrPath)
        {
            if (string.IsNullOrWhiteSpace(assetGuidOrPath)) return false;
            var data = GetData();
            var card = GetCardInternal(data, cardIdOrTitle);
            if (card == null) return false;

            string guid = assetGuidOrPath.Trim();
            if (guid.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase) || guid.StartsWith("Packages/", StringComparison.OrdinalIgnoreCase))
            {
                guid = AssetDatabase.AssetPathToGUID(guid);
            }

            if (string.IsNullOrEmpty(guid)) return false;

            card.linkedItems ??= new List<LinkedItem>();
            if (!card.linkedItems.Any(li => !li.isSceneObject && !li.isNote && !li.isUrl && li.guid == guid))
            {
                card.linkedItems.Add(new LinkedItem(guid));
                Save(data);
                return true;
            }
            return true;
        }

        /// <summary>
        /// Links multiple Quick Notes, external URLs, or Unity project assets to task cards in a single atomic batch.
        /// </summary>
        public static (int successCount, int totalCount, List<BulkLinkResult> results) BulkLinkItems(
            IEnumerable<BulkLinkItemEntry> entries,
            string defaultCardIdOrTitle = null,
            string defaultLinkType = "note")
        {
            var results = new List<BulkLinkResult>();
            if (entries == null) return (0, 0, results);

            var entryList = entries.ToList();
            if (entryList.Count == 0) return (0, 0, results);

            var data = GetData();
            int successCount = 0;
            bool modified = false;

            foreach (var entry in entryList)
            {
                if (entry == null) continue;

                string cardTarget = !string.IsNullOrWhiteSpace(entry.cardIdOrTitle) ? entry.cardIdOrTitle.Trim() : defaultCardIdOrTitle?.Trim();
                string targetVal = entry.target?.Trim();
                string lType = (!string.IsNullOrWhiteSpace(entry.linkType) ? entry.linkType.Trim() : defaultLinkType ?? "note").ToLowerInvariant();

                if (string.IsNullOrWhiteSpace(cardTarget) || string.IsNullOrWhiteSpace(targetVal))
                {
                    results.Add(new BulkLinkResult
                    {
                        cardIdOrTitle = cardTarget ?? "",
                        target = targetVal ?? "",
                        linkType = lType,
                        success = false,
                        message = "Both card and link target are required."
                    });
                    continue;
                }

                var card = GetCardInternal(data, cardTarget);
                if (card == null)
                {
                    results.Add(new BulkLinkResult
                    {
                        cardIdOrTitle = cardTarget,
                        target = targetVal,
                        linkType = lType,
                        success = false,
                        message = $"Card '{cardTarget}' not found."
                    });
                    continue;
                }

                card.linkedItems ??= new List<LinkedItem>();

                if (lType == "note")
                {
                    var note = data.notes?.FirstOrDefault(n => n != null && (n.id == targetVal || string.Equals(n.title, targetVal, StringComparison.OrdinalIgnoreCase)));
                    if (note == null)
                    {
                        results.Add(new BulkLinkResult
                        {
                            cardIdOrTitle = cardTarget,
                            cardId = card.id,
                            cardTitle = card.title,
                            target = targetVal,
                            linkType = lType,
                            success = false,
                            message = $"Note '{targetVal}' not found."
                        });
                        continue;
                    }

                    if (card.linkedItems.Any(li => li.isNote && li.guid == note.id))
                    {
                        results.Add(new BulkLinkResult
                        {
                            cardIdOrTitle = cardTarget,
                            cardId = card.id,
                            cardTitle = card.title,
                            target = note.title,
                            linkType = lType,
                            success = true,
                            message = $"Note '{note.title}' already linked to '{card.title}'."
                        });
                        successCount++;
                        continue;
                    }

                    var item = LinkedItem.CreateNote(note.id);
                    item.displayName = note.title;
                    card.linkedItems.Add(item);
                    modified = true;
                    successCount++;

                    results.Add(new BulkLinkResult
                    {
                        cardIdOrTitle = cardTarget,
                        cardId = card.id,
                        cardTitle = card.title,
                        target = note.title,
                        linkType = lType,
                        success = true,
                        message = $"Linked note '{note.title}' to card '{card.title}'."
                    });
                }
                else if (lType == "url")
                {
                    string formattedUrl = targetVal;
                    if (!formattedUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) && !formattedUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                        formattedUrl = "https://" + formattedUrl;

                    if (card.linkedItems.Any(li => li.isUrl && string.Equals(li.url, formattedUrl, StringComparison.OrdinalIgnoreCase)))
                    {
                        results.Add(new BulkLinkResult
                        {
                            cardIdOrTitle = cardTarget,
                            cardId = card.id,
                            cardTitle = card.title,
                            target = formattedUrl,
                            linkType = lType,
                            success = true,
                            message = $"URL '{formattedUrl}' already linked to '{card.title}'."
                        });
                        successCount++;
                        continue;
                    }

                    card.linkedItems.Add(LinkedItem.CreateUrl(formattedUrl, entry.displayName ?? ""));
                    modified = true;
                    successCount++;

                    results.Add(new BulkLinkResult
                    {
                        cardIdOrTitle = cardTarget,
                        cardId = card.id,
                        cardTitle = card.title,
                        target = formattedUrl,
                        linkType = lType,
                        success = true,
                        message = $"Linked URL '{formattedUrl}' to card '{card.title}'."
                    });
                }
                else if (lType == "asset")
                {
                    string guid = targetVal;
                    if (guid.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase) || guid.StartsWith("Packages/", StringComparison.OrdinalIgnoreCase))
                    {
                        guid = AssetDatabase.AssetPathToGUID(guid);
                    }

                    if (string.IsNullOrEmpty(guid))
                    {
                        results.Add(new BulkLinkResult
                        {
                            cardIdOrTitle = cardTarget,
                            cardId = card.id,
                            cardTitle = card.title,
                            target = targetVal,
                            linkType = lType,
                            success = false,
                            message = $"Asset '{targetVal}' not found or invalid path."
                        });
                        continue;
                    }

                    if (card.linkedItems.Any(li => !li.isSceneObject && !li.isNote && !li.isUrl && li.guid == guid))
                    {
                        results.Add(new BulkLinkResult
                        {
                            cardIdOrTitle = cardTarget,
                            cardId = card.id,
                            cardTitle = card.title,
                            target = targetVal,
                            linkType = lType,
                            success = true,
                            message = $"Asset '{targetVal}' already linked to '{card.title}'."
                        });
                        successCount++;
                        continue;
                    }

                    card.linkedItems.Add(new LinkedItem(guid));
                    modified = true;
                    successCount++;

                    results.Add(new BulkLinkResult
                    {
                        cardIdOrTitle = cardTarget,
                        cardId = card.id,
                        cardTitle = card.title,
                        target = targetVal,
                        linkType = lType,
                        success = true,
                        message = $"Linked asset '{targetVal}' to card '{card.title}'."
                    });
                }
                else
                {
                    results.Add(new BulkLinkResult
                    {
                        cardIdOrTitle = cardTarget,
                        cardId = card.id,
                        cardTitle = card.title,
                        target = targetVal,
                        linkType = lType,
                        success = false,
                        message = $"Unsupported link type '{lType}'. Supported: note, url, asset."
                    });
                }
            }

            if (modified)
            {
                Save(data);
            }

            return (successCount, entryList.Count, results);
        }

        #endregion

        #region Search & Project Analytics

        [Serializable]
        public class MatchedCardDetail
        {
            public string id;
            public string title;
            public string board;
            public string column;
            public string category;
            public int priority;
            public string dueDate;
            public int colorLabel;
            public bool completed;
            public bool archived;
            public List<string> assignees = new List<string>();
            public List<string> checklist = new List<string>();
            public List<string> matchedChecklistItems = new List<string>();
            public string description;
            public int linksCount;
        }

        [Serializable]
        public class MatchedNoteDetail
        {
            public string id;
            public string title;
            public string folder;
            public List<string> tags = new List<string>();
            public string content;
            public bool pinned;
            public int colorIndex;
            public string modifiedDate;
        }

        [Serializable]
        public class MatchedBoardDetail
        {
            public string id;
            public string name;
            public int columnCount;
            public int cardCount;
        }

        [Serializable]
        public class MatchedColumnDetail
        {
            public string id;
            public string title;
            public string board;
            public int cardCount;
        }

        [Serializable]
        public class MatchedCategoryDetail
        {
            public string name;
            public int colorIndex;
        }

        [Serializable]
        public class MatchedAssigneeDetail
        {
            public string id;
            public string name;
            public int colorIndex;
        }

        [Serializable]
        public class MatchedToolDetail
        {
            public string name;
            public string description;
            public string category;
        }

        [Serializable]
        public class DetailedSearchResult
        {
            public bool success = true;
            public string query;
            public string message;
            public int totalMatches;
            public List<MatchedCardDetail> matchedCards = new List<MatchedCardDetail>();
            public List<MatchedNoteDetail> matchedNotes = new List<MatchedNoteDetail>();
            public List<MatchedBoardDetail> matchedBoards = new List<MatchedBoardDetail>();
            public List<MatchedColumnDetail> matchedColumns = new List<MatchedColumnDetail>();
            public List<MatchedCategoryDetail> matchedCategories = new List<MatchedCategoryDetail>();
            public List<MatchedAssigneeDetail> matchedAssignees = new List<MatchedAssigneeDetail>();
            public List<MatchedToolDetail> matchedTools = new List<MatchedToolDetail>();
        }

        /// <summary>
        /// Returns the catalog of all available ATM Next MCP tools and Command Bridge endpoints.
        /// </summary>
        public static List<MatchedToolDetail> GetToolCatalog()
        {
            return new List<MatchedToolDetail>
            {
                // Boards & Columns
                new MatchedToolDetail { name = "atm_get_boards", category = "Boards & Columns", description = "Inspect all boards, columns, and cards with completion stats" },
                new MatchedToolDetail { name = "atm_create_board", category = "Boards & Columns", description = "Create new task boards" },
                new MatchedToolDetail { name = "atm_delete_board", category = "Boards & Columns", description = "Delete boards (requires confirm: true)" },
                new MatchedToolDetail { name = "atm_create_column", category = "Boards & Columns", description = "Create new columns in boards" },
                new MatchedToolDetail { name = "atm_move_column", category = "Boards & Columns", description = "Move columns across boards or reorder" },
                new MatchedToolDetail { name = "atm_clone_column", category = "Boards & Columns", description = "Clone/duplicate columns to boards" },
                new MatchedToolDetail { name = "atm_reorder_column", category = "Boards & Columns", description = "Reorder columns left/right/first/last" },
                new MatchedToolDetail { name = "atm_delete_column", category = "Boards & Columns", description = "Delete columns (requires confirm: true)" },
                new MatchedToolDetail { name = "atm_set_active_board", category = "Boards & Columns", description = "Switch the active board in the editor" },
                new MatchedToolDetail { name = "atm_set_filter", category = "Boards & Columns", description = "Filter tasks by category/assignee/priority/search" },
                new MatchedToolDetail { name = "atm_get_filter", category = "Boards & Columns", description = "Inspect active filter settings on the board" },
                new MatchedToolDetail { name = "atm_delete_all_tasks_in_column", category = "Boards & Columns", description = "Bulk clear columns (requires confirm: true)" },
                new MatchedToolDetail { name = "atm_move_all_tasks_between_boards", category = "Boards & Columns", description = "Bulk move tasks (requires confirm: true)" },

                // Tasks & Cards
                new MatchedToolDetail { name = "atm_create_task", category = "Tasks & Cards", description = "Create new task cards with checklists" },
                new MatchedToolDetail { name = "atm_bulk_create_tasks", category = "Tasks & Cards", description = "Import dozens of cards at once" },
                new MatchedToolDetail { name = "atm_update_task", category = "Tasks & Cards", description = "Update task properties or mark complete" },
                new MatchedToolDetail { name = "atm_bulk_update_tasks", category = "Tasks & Cards", description = "Bulk update multiple task properties/assignees" },
                new MatchedToolDetail { name = "atm_move_task", category = "Tasks & Cards", description = "Move cards across columns/boards" },
                new MatchedToolDetail { name = "atm_bulk_move_tasks", category = "Tasks & Cards", description = "Bulk move multiple cards at once" },
                new MatchedToolDetail { name = "atm_clone_task", category = "Tasks & Cards", description = "Clone/duplicate a card to column/board" },
                new MatchedToolDetail { name = "atm_bulk_clone_tasks", category = "Tasks & Cards", description = "Bulk duplicate cards across columns/boards" },
                new MatchedToolDetail { name = "atm_reorder_task", category = "Tasks & Cards", description = "Reorder cards up/down in column" },
                new MatchedToolDetail { name = "atm_delete_task", category = "Tasks & Cards", description = "Remove task cards (requires confirm: true)" },
                new MatchedToolDetail { name = "atm_bulk_delete_tasks", category = "Tasks & Cards", description = "Bulk delete task cards (requires confirm: true)" },
                new MatchedToolDetail { name = "atm_manage_checklist", category = "Tasks & Cards", description = "Add, toggle, or remove checklist items" },
                new MatchedToolDetail { name = "atm_search", category = "Tasks & Cards", description = "Search tasks, checklists, notes, categories, assignees, boards, columns, and ATM tools" },

                // Categories & Assignees
                new MatchedToolDetail { name = "atm_create_category", category = "Categories & Assignees", description = "Create new category tags with colors" },
                new MatchedToolDetail { name = "atm_get_categories", category = "Categories & Assignees", description = "List all categories and color indices" },
                new MatchedToolDetail { name = "atm_delete_category", category = "Categories & Assignees", description = "Delete a category (requires confirm: true)" },
                new MatchedToolDetail { name = "atm_create_assignee", category = "Categories & Assignees", description = "Create team members / assignees" },
                new MatchedToolDetail { name = "atm_get_assignees", category = "Categories & Assignees", description = "List all assignees with IDs and styles" },
                new MatchedToolDetail { name = "atm_delete_assignee", category = "Categories & Assignees", description = "Delete an assignee (requires confirm: true)" },

                // Hierarchical Subtasks & Epics
                new MatchedToolDetail { name = "atm_create_subtask", category = "Hierarchical Subtasks & Epics", description = "Create child tasks linked to a parent" },
                new MatchedToolDetail { name = "atm_link_subtask", category = "Hierarchical Subtasks & Epics", description = "Link existing card as child to parent" },
                new MatchedToolDetail { name = "atm_unlink_subtask", category = "Hierarchical Subtasks & Epics", description = "Remove parent-child link between cards" },
                new MatchedToolDetail { name = "atm_convert_checklist_to_subtasks", category = "Hierarchical Subtasks & Epics", description = "Convert checklist items to cards" },
                new MatchedToolDetail { name = "atm_get_subtasks", category = "Hierarchical Subtasks & Epics", description = "Query parent/child relationships" },
                new MatchedToolDetail { name = "atm_create_epic_hierarchy", category = "Hierarchical Subtasks & Epics", description = "Create parent + multiple children at once" },

                // Notes & Devlogs
                new MatchedToolDetail { name = "atm_get_notes", category = "Notes & Devlogs", description = "List all Quick Notes and folders" },
                new MatchedToolDetail { name = "atm_create_note", category = "Notes & Devlogs", description = "Write devlogs and architecture notes" },
                new MatchedToolDetail { name = "atm_bulk_create_notes", category = "Notes & Devlogs", description = "Batch create notes across folders" },
                new MatchedToolDetail { name = "atm_update_note", category = "Notes & Devlogs", description = "Edit existing Quick Notes" },
                new MatchedToolDetail { name = "atm_bulk_update_notes", category = "Notes & Devlogs", description = "Bulk update content, folders, tags, colors" },
                new MatchedToolDetail { name = "atm_bulk_move_notes", category = "Notes & Devlogs", description = "Bulk move notes to target folders" },
                new MatchedToolDetail { name = "atm_delete_note", category = "Notes & Devlogs", description = "Delete notes (requires confirm: true)" },
                new MatchedToolDetail { name = "atm_bulk_delete_notes", category = "Notes & Devlogs", description = "Bulk delete notes (requires confirm: true)" },
                new MatchedToolDetail { name = "atm_generate_devlog", category = "Notes & Devlogs", description = "Auto-compile 'Done' tasks into a note" },
                new MatchedToolDetail { name = "atm_link_item", category = "Notes & Devlogs", description = "Link notes, URLs, or assets to cards" },
                new MatchedToolDetail { name = "atm_bulk_link_items", category = "Notes & Devlogs", description = "Bulk link multiple notes/URLs/assets to cards" },

                // Sprint Analytics & Health
                new MatchedToolDetail { name = "atm_get_project_summary", category = "Sprint Analytics & Health", description = "Get sprint completion analytics" }
            };
        }

        /// <summary>
        /// Performs rich, multi-criteria search across cards, notes, checklists, categories, assignees, boards, columns, and ATM tools.
        /// </summary>
        public static DetailedSearchResult SearchDetailed(
            string query = null,
            string boardNameOrId = null,
            string columnNameOrId = null,
            string category = null,
            string assignee = null,
            string scope = "all",
            bool includeCompleted = true,
            bool includeArchived = false,
            int? limit = null)
        {
            var result = new DetailedSearchResult
            {
                query = query ?? "",
                success = true
            };

            var data = GetData();
            string q = (query ?? "").Trim();
            bool hasQuery = !string.IsNullOrWhiteSpace(q);

            string scopeNorm = (scope ?? "all").Trim().ToLowerInvariant();
            bool searchAll = string.IsNullOrEmpty(scopeNorm) || scopeNorm == "all" || scopeNorm == "*";
            bool searchCards = searchAll || scopeNorm.Contains("card") || scopeNorm.Contains("task");
            bool searchNotes = searchAll || scopeNorm.Contains("note");
            bool searchChecklists = searchAll || scopeNorm.Contains("checklist");
            bool searchBoards = searchAll || scopeNorm.Contains("board");
            bool searchColumns = searchAll || scopeNorm.Contains("column");
            bool searchCategories = searchAll || scopeNorm.Contains("categor");
            bool searchAssignees = searchAll || scopeNorm.Contains("assignee") || scopeNorm.Contains("member") || scopeNorm.Contains("user");
            bool searchTools = searchAll || scopeNorm.Contains("tool") || scopeNorm.Contains("command") || scopeNorm.Contains("endpoint") || scopeNorm.Contains("api");

            // Build assignee ID -> Name lookup
            var assigneeMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (data?.assignees != null)
            {
                foreach (var a in data.assignees)
                {
                    if (a != null && !string.IsNullOrEmpty(a.id))
                        assigneeMap[a.id] = a.name ?? "";
                }
            }

            // Build note folder ID -> Name lookup
            var folderMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (data?.noteFolders != null)
            {
                foreach (var f in data.noteFolders)
                {
                    if (f != null && !string.IsNullOrEmpty(f.id))
                        folderMap[f.id] = f.name ?? "";
                }
            }

            // 1. Search Cards
            if ((searchCards || searchChecklists) && data?.boards != null)
            {
                foreach (var b in data.boards)
                {
                    if (b == null) continue;
                    if (!string.IsNullOrWhiteSpace(boardNameOrId) &&
                        b.id != boardNameOrId &&
                        !string.Equals(b.name, boardNameOrId, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    if (b.columns == null) continue;
                    foreach (var col in b.columns)
                    {
                        if (col == null) continue;
                        if (!string.IsNullOrWhiteSpace(columnNameOrId) &&
                            col.id != columnNameOrId &&
                            !string.Equals(col.title, columnNameOrId, StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        if (col.cards == null) continue;
                        foreach (var card in col.cards)
                        {
                            if (card == null) continue;
                            if (!includeCompleted && card.completed) continue;
                            if (!includeArchived && card.archived) continue;

                            if (!string.IsNullOrWhiteSpace(category) &&
                                (card.category == null || card.category.IndexOf(category.Trim(), StringComparison.OrdinalIgnoreCase) < 0))
                            {
                                continue;
                            }

                            if (!string.IsNullOrWhiteSpace(assignee))
                            {
                                bool matchesAssignee = false;
                                if (card.assigneeIds != null)
                                {
                                    foreach (var aid in card.assigneeIds)
                                    {
                                        if (string.Equals(aid, assignee, StringComparison.OrdinalIgnoreCase) ||
                                            (assigneeMap.TryGetValue(aid, out var aName) && aName.IndexOf(assignee, StringComparison.OrdinalIgnoreCase) >= 0))
                                        {
                                            matchesAssignee = true;
                                            break;
                                        }
                                    }
                                }
                                if (!matchesAssignee) continue;
                            }

                            // Match query
                            bool isMatch = !hasQuery;
                            var matchedChecklistItems = new List<string>();

                            if (hasQuery)
                            {
                                bool titleMatch = card.title != null && card.title.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0;
                                bool descMatch = card.description != null && card.description.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0;
                                bool catMatch = card.category != null && card.category.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0;
                                bool idMatch = card.id != null && card.id.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0;

                                bool checklistMatch = false;
                                if (card.checklistItems != null)
                                {
                                    foreach (var item in card.checklistItems)
                                    {
                                        if (item != null && item.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0)
                                        {
                                            checklistMatch = true;
                                            matchedChecklistItems.Add(item);
                                        }
                                    }
                                }

                                bool assigneeMatch = false;
                                if (card.assigneeIds != null)
                                {
                                    foreach (var aid in card.assigneeIds)
                                    {
                                        if (aid.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0 ||
                                            (assigneeMap.TryGetValue(aid, out var aName) && aName.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0))
                                        {
                                            assigneeMatch = true;
                                            break;
                                        }
                                    }
                                }

                                bool linkMatch = false;
                                if (card.linkedItems != null)
                                {
                                    foreach (var li in card.linkedItems)
                                    {
                                        if (li == null) continue;
                                        if ((li.displayName != null && li.displayName.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0) ||
                                            (li.url != null && li.url.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0))
                                        {
                                            linkMatch = true;
                                            break;
                                        }
                                    }
                                }

                                if (scopeNorm.Contains("checklist") && !searchAll)
                                {
                                    isMatch = checklistMatch;
                                }
                                else
                                {
                                    isMatch = titleMatch || descMatch || catMatch || idMatch || checklistMatch || assigneeMatch || linkMatch;
                                }
                            }
                            else if (card.checklistItems != null)
                            {
                                matchedChecklistItems.AddRange(card.checklistItems);
                            }

                            if (isMatch)
                            {
                                var assigneeNames = card.assigneeIds?.Select(id => assigneeMap.TryGetValue(id, out var name) && !string.IsNullOrEmpty(name) ? name : id).ToList() ?? new List<string>();
                                int linksCount = (card.linkedItems?.Count ?? 0) + (card.linkedAssetGuids?.Count ?? 0) + (card.linkedSceneObjects?.Count ?? 0);

                                result.matchedCards.Add(new MatchedCardDetail
                                {
                                    id = card.id,
                                    title = card.title,
                                    board = b.name,
                                    column = col.title,
                                    category = card.category,
                                    priority = card.priority,
                                    dueDate = card.dueDate,
                                    colorLabel = card.colorLabel,
                                    completed = card.completed,
                                    archived = card.archived,
                                    assignees = assigneeNames,
                                    checklist = card.checklistItems != null ? new List<string>(card.checklistItems) : new List<string>(),
                                    matchedChecklistItems = matchedChecklistItems,
                                    description = card.description,
                                    linksCount = linksCount
                                });
                            }
                        }
                    }
                }
            }

            // 2. Search Notes
            if (searchNotes && data?.notes != null)
            {
                foreach (var note in data.notes)
                {
                    if (note == null) continue;
                    string folderName = (note.folderId != null && folderMap.TryGetValue(note.folderId, out var fn)) ? fn : (note.folderId ?? "");

                    bool isMatch = !hasQuery;
                    if (hasQuery)
                    {
                        bool titleMatch = note.title != null && note.title.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0;
                        bool contentMatch = note.content != null && note.content.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0;
                        bool tagsMatch = note.tags != null && note.tags.Any(t => t != null && t.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0);
                        bool idMatch = note.id != null && note.id.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0;
                        bool folderMatch = folderName.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0;

                        isMatch = titleMatch || contentMatch || tagsMatch || idMatch || folderMatch;
                    }

                    if (isMatch)
                    {
                        result.matchedNotes.Add(new MatchedNoteDetail
                        {
                            id = note.id,
                            title = note.title,
                            folder = folderName,
                            tags = note.tags != null ? new List<string>(note.tags) : new List<string>(),
                            content = note.content,
                            pinned = note.pinned,
                            colorIndex = note.colorIndex,
                            modifiedDate = note.modifiedDate
                        });
                    }
                }
            }

            // 3. Search Boards
            if (searchBoards && data?.boards != null)
            {
                foreach (var b in data.boards)
                {
                    if (b == null) continue;
                    bool isMatch = !hasQuery || (b.name != null && b.name.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0) || (b.id != null && b.id.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0);
                    if (isMatch)
                    {
                        result.matchedBoards.Add(new MatchedBoardDetail
                        {
                            id = b.id,
                            name = b.name,
                            columnCount = b.columns?.Count ?? 0,
                            cardCount = b.columns?.Sum(c => c?.cards?.Count ?? 0) ?? 0
                        });
                    }
                }
            }

            // 4. Search Columns
            if (searchColumns && data?.boards != null)
            {
                foreach (var b in data.boards)
                {
                    if (b?.columns == null) continue;
                    foreach (var col in b.columns)
                    {
                        if (col == null) continue;
                        bool isMatch = !hasQuery || (col.title != null && col.title.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0) || (col.id != null && col.id.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0) || (b.name != null && b.name.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0);
                        if (isMatch)
                        {
                            result.matchedColumns.Add(new MatchedColumnDetail
                            {
                                id = col.id,
                                title = col.title,
                                board = b.name,
                                cardCount = col.cards?.Count ?? 0
                            });
                        }
                    }
                }
            }

            // 5. Search Categories
            if (searchCategories && data?.categories != null)
            {
                foreach (var cat in data.categories)
                {
                    if (string.IsNullOrWhiteSpace(cat)) continue;
                    bool isMatch = !hasQuery || cat.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0;
                    if (isMatch)
                    {
                        result.matchedCategories.Add(new MatchedCategoryDetail
                        {
                            name = cat,
                            colorIndex = data.GetCategoryColor(cat)
                        });
                    }
                }
            }

            // 6. Search Assignees
            if (searchAssignees && data?.assignees != null)
            {
                foreach (var a in data.assignees)
                {
                    if (a == null) continue;
                    bool isMatch = !hasQuery || (a.name != null && a.name.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0) || (a.id != null && a.id.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0);
                    if (isMatch)
                    {
                        result.matchedAssignees.Add(new MatchedAssigneeDetail
                        {
                            id = a.id,
                            name = a.name,
                            colorIndex = a.colorIndex
                        });
                    }
                }
            }

            // 7. Search Tools Catalog
            if (searchTools)
            {
                var toolCatalog = GetToolCatalog();
                foreach (var tool in toolCatalog)
                {
                    if (tool == null) continue;
                    bool isMatch = !hasQuery || (tool.name != null && tool.name.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0) ||
                                  (tool.description != null && tool.description.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0) ||
                                  (tool.category != null && tool.category.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0);
                    if (isMatch)
                    {
                        result.matchedTools.Add(tool);
                    }
                }
            }

            // Apply limits if requested
            if (limit.HasValue && limit.Value > 0)
            {
                int max = limit.Value;
                if (result.matchedCards.Count > max) result.matchedCards = result.matchedCards.Take(max).ToList();
                if (result.matchedNotes.Count > max) result.matchedNotes = result.matchedNotes.Take(max).ToList();
                if (result.matchedBoards.Count > max) result.matchedBoards = result.matchedBoards.Take(max).ToList();
                if (result.matchedColumns.Count > max) result.matchedColumns = result.matchedColumns.Take(max).ToList();
                if (result.matchedCategories.Count > max) result.matchedCategories = result.matchedCategories.Take(max).ToList();
                if (result.matchedAssignees.Count > max) result.matchedAssignees = result.matchedAssignees.Take(max).ToList();
                if (result.matchedTools.Count > max) result.matchedTools = result.matchedTools.Take(max).ToList();
            }

            result.totalMatches = result.matchedCards.Count +
                                  result.matchedNotes.Count +
                                  result.matchedBoards.Count +
                                  result.matchedColumns.Count +
                                  result.matchedCategories.Count +
                                  result.matchedAssignees.Count +
                                  result.matchedTools.Count;

            var parts = new List<string>();
            if (result.matchedCards.Count > 0) parts.Add($"{result.matchedCards.Count} card(s)");
            if (result.matchedNotes.Count > 0) parts.Add($"{result.matchedNotes.Count} note(s)");
            if (result.matchedBoards.Count > 0) parts.Add($"{result.matchedBoards.Count} board(s)");
            if (result.matchedColumns.Count > 0) parts.Add($"{result.matchedColumns.Count} column(s)");
            if (result.matchedCategories.Count > 0) parts.Add($"{result.matchedCategories.Count} category(s)");
            if (result.matchedAssignees.Count > 0) parts.Add($"{result.matchedAssignees.Count} assignee(s)");
            if (result.matchedTools.Count > 0) parts.Add($"{result.matchedTools.Count} tool(s)");

            if (result.totalMatches == 0)
            {
                result.message = hasQuery ? $"No items found matching '{q}'." : "No items found matching the specified search criteria.";
            }
            else
            {
                string breakdown = string.Join(", ", parts);
                result.message = hasQuery ? $"Found {result.totalMatches} item(s) matching '{q}' ({breakdown})." : $"Found {result.totalMatches} item(s) ({breakdown}).";
            }

            return result;
        }

        /// <summary>
        /// Searches across all cards (title, description, category, checklists, assignees, ID) and notes (title, content, tags, folder, ID).
        /// </summary>
        public static (List<TaskCard> matchedCards, List<QuickNote> matchedNotes) Search(string query)
        {
            var matchedCards = new List<TaskCard>();
            var matchedNotes = new List<QuickNote>();

            if (string.IsNullOrWhiteSpace(query))
                return (matchedCards, matchedNotes);

            var detailed = SearchDetailed(query: query, scope: "cards,notes");

            var allCards = GetCards();
            var cardMap = new Dictionary<string, TaskCard>();
            foreach (var c in allCards)
            {
                if (c != null && !string.IsNullOrEmpty(c.id))
                    cardMap[c.id] = c;
            }

            foreach (var mc in detailed.matchedCards)
            {
                if (mc != null && cardMap.TryGetValue(mc.id, out var card))
                {
                    matchedCards.Add(card);
                }
            }

            var data = GetData();
            var noteMap = new Dictionary<string, QuickNote>();
            if (data?.notes != null)
            {
                foreach (var n in data.notes)
                {
                    if (n != null && !string.IsNullOrEmpty(n.id))
                        noteMap[n.id] = n;
                }
            }

            foreach (var mn in detailed.matchedNotes)
            {
                if (mn != null && noteMap.TryGetValue(mn.id, out var note))
                {
                    matchedNotes.Add(note);
                }
            }

            return (matchedCards, matchedNotes);
        }

        /// <summary>
        /// Generates a high-level summary of tasks, progress, categories, and notes.
        /// </summary>
        public static object GetProjectSummary()
        {
            var data = GetData();
            var allCards = GetCards();

            int totalCards = allCards.Count;
            int completedCards = allCards.Count(c => c != null && c.completed);
            int archivedCards = allCards.Count(c => c != null && c.archived);
            int overdueCards = 0;

            var today = DateTime.Today;
            foreach (var c in allCards)
            {
                if (c != null && !c.completed && !string.IsNullOrEmpty(c.dueDate) && DateTime.TryParse(c.dueDate, out var due))
                {
                    if (due.Date < today) overdueCards++;
                }
            }

            var boardSummaries = data.boards.Select(b => new
            {
                boardName = b.name,
                columns = (b.columns ?? new List<TaskColumn>()).Select(col => new
                {
                    columnTitle = col.title,
                    cardCount = col.cards != null ? col.cards.Count : 0
                }).ToList()
            }).ToList();

            return new
            {
                totalBoards = data.boards.Count,
                totalCards,
                completedCards,
                archivedCards,
                overdueCards,
                totalNotes = data.notes?.Count ?? 0,
                totalFolders = data.noteFolders?.Count ?? 0,
                categories = data.categories,
                boards = boardSummaries
            };
        }

        #endregion

        #region Categories

        /// <summary>
        /// Creates a category if it doesn't already exist and optionally sets its default color.
        /// </summary>
        public static string CreateCategory(string name, int? colorIndex = null)
        {
            if (string.IsNullOrWhiteSpace(name)) return null;
            name = name.Trim();
            var data = GetData();
            data.categories ??= new List<string>();
            if (!data.categories.Any(c => string.Equals(c, name, StringComparison.OrdinalIgnoreCase)))
            {
                data.categories.Add(name);
            }
            if (colorIndex.HasValue)
            {
                data.SetCategoryColor(name, colorIndex.Value);
            }
            Save(data);
            return name;
        }

        /// <summary>
        /// Returns all categories and their assigned color indices.
        /// </summary>
        public static List<CategoryInfo> GetCategories()
        {
            var data = GetData();
            data.categories ??= new List<string>();
            return data.categories.Select(c => new CategoryInfo
            {
                name = c,
                colorIndex = data.GetCategoryColor(c)
            }).ToList();
        }

        /// <summary>
        /// Deletes a category globally from settings and removes it from all cards.
        /// </summary>
        public static bool DeleteCategory(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;
            var data = GetData();
            data.DeleteCategory(name);
            Save(data);
            return true;
        }

        #endregion

        #region Assignees

        /// <summary>
        /// Creates or updates an Assignee team member.
        /// </summary>
        public static Assignee CreateAssignee(string name, int colorIndex = 1, int borderColorIndex = 0, string profileImageGuid = null)
        {
            if (string.IsNullOrWhiteSpace(name)) return null;
            name = name.Trim();
            var data = GetData();
            data.assignees ??= new List<Assignee>();

            var existing = data.assignees.FirstOrDefault(a => string.Equals(a.name, name, StringComparison.OrdinalIgnoreCase));
            if (existing != null)
            {
                existing.colorIndex = colorIndex;
                existing.borderColorIndex = borderColorIndex;
                if (!string.IsNullOrEmpty(profileImageGuid)) existing.profileImageGuid = profileImageGuid;
                Save(data);
                return existing;
            }

            var newAssignee = new Assignee
            {
                id = Guid.NewGuid().ToString(),
                name = name,
                colorIndex = colorIndex,
                borderColorIndex = borderColorIndex,
                profileImageGuid = profileImageGuid
            };
            data.assignees.Add(newAssignee);
            Save(data);
            return newAssignee;
        }

        /// <summary>
        /// Returns all project assignees.
        /// </summary>
        public static List<Assignee> GetAssignees()
        {
            var data = GetData();
            return data.assignees ?? new List<Assignee>();
        }

        /// <summary>
        /// Deletes an assignee by ID or name and unassigns them from all cards.
        /// </summary>
        public static bool DeleteAssignee(string idOrName)
        {
            if (string.IsNullOrWhiteSpace(idOrName)) return false;
            idOrName = idOrName.Trim();
            var data = GetData();
            if (data.assignees == null) return false;

            var match = data.assignees.FirstOrDefault(a => a.id == idOrName || string.Equals(a.name, idOrName, StringComparison.OrdinalIgnoreCase));
            if (match == null) return false;

            data.assignees.Remove(match);
            foreach (var b in data.boards)
            {
                if (b?.columns == null) continue;
                foreach (var col in b.columns)
                {
                    if (col?.cards == null) continue;
                    foreach (var card in col.cards)
                    {
                        if (card?.assigneeIds != null)
                            card.assigneeIds.Remove(match.id);
                    }
                }
            }
            Save(data);
            return true;
        }

        #endregion

        #region UI Filters

        private static string _savedFilterCategory = "";
        private static string _savedFilterAssigneeId = "";
        private static int _savedFilterPriority = 0;
        private static string _savedFilterSearch = "";

        /// <summary>
        /// Configures active UI board filters (category, assignee, priority, search text).
        /// </summary>
        public static BoardFilterInfo SetFilter(
            string category = null,
            string assignee = null,
            int? priority = null,
            string searchQuery = null,
            bool resetAll = false)
        {
            var data = GetData();

            if (resetAll)
            {
                _savedFilterCategory = "";
                _savedFilterAssigneeId = "";
                _savedFilterPriority = 0;
                _savedFilterSearch = "";
            }

            if (category != null)
            {
                string catTrim = category.Trim();
                if (string.Equals(catTrim, "All", StringComparison.OrdinalIgnoreCase) || string.IsNullOrEmpty(catTrim))
                {
                    _savedFilterCategory = "";
                }
                else
                {
                    var matchCat = data.categories?.FirstOrDefault(c => string.Equals(c, catTrim, StringComparison.OrdinalIgnoreCase));
                    _savedFilterCategory = matchCat ?? catTrim;
                }
            }

            if (assignee != null)
            {
                string assTrim = assignee.Trim();
                if (string.Equals(assTrim, "All", StringComparison.OrdinalIgnoreCase) || string.IsNullOrEmpty(assTrim))
                {
                    _savedFilterAssigneeId = "";
                }
                else
                {
                    var matchAss = data.assignees?.FirstOrDefault(a => a.id == assTrim || string.Equals(a.name, assTrim, StringComparison.OrdinalIgnoreCase));
                    _savedFilterAssigneeId = matchAss != null ? matchAss.id : assTrim;
                }
            }

            if (priority.HasValue)
            {
                _savedFilterPriority = Mathf.Clamp(priority.Value, 0, 5);
            }

            if (searchQuery != null)
            {
                _savedFilterSearch = searchQuery.Trim();
            }

            try
            {
                if (!Persistence.SuppressReloadEvents && Persistence.InMemoryDataOverride == null && string.IsNullOrEmpty(Persistence.SavePathOverride))
                {
                    TaskBoardWindow.ApplyFiltersToOpenWindows(_savedFilterCategory, _savedFilterAssigneeId, _savedFilterPriority, _savedFilterSearch);
                }
            }
            catch { }

            return GetFilter();
        }

        /// <summary>
        /// Retrieves active UI filter settings and matching card statistics for the active board.
        /// </summary>
        public static BoardFilterInfo GetFilter()
        {
            var data = GetData();
            var activeBoard = GetActiveBoard(data);

            string assigneeName = "";
            if (!string.IsNullOrEmpty(_savedFilterAssigneeId))
            {
                var ass = data.assignees?.FirstOrDefault(a => a.id == _savedFilterAssigneeId);
                assigneeName = ass != null ? ass.name : _savedFilterAssigneeId;
            }

            string[] priorityLabels = { "All", "None", "Low", "Medium", "High", "Urgent" };
            string priLabel = (_savedFilterPriority >= 0 && _savedFilterPriority < priorityLabels.Length) 
                ? priorityLabels[_savedFilterPriority] 
                : "All";

            int totalCards = 0;
            int visibleCards = 0;

            if (activeBoard?.columns != null)
            {
                foreach (var col in activeBoard.columns)
                {
                    if (col?.cards == null) continue;
                    foreach (var card in col.cards)
                    {
                        if (card == null || card.archived) continue;
                        totalCards++;

                        bool matchCat = string.IsNullOrEmpty(_savedFilterCategory) || string.Equals(card.category, _savedFilterCategory, StringComparison.OrdinalIgnoreCase);
                        bool matchAss = string.IsNullOrEmpty(_savedFilterAssigneeId) || (card.assigneeIds != null && card.assigneeIds.Contains(_savedFilterAssigneeId));
                        bool matchPri = _savedFilterPriority == 0 || card.priority == _savedFilterPriority - 1;
                        bool matchSearch = string.IsNullOrWhiteSpace(_savedFilterSearch) || 
                            (card.title != null && card.title.IndexOf(_savedFilterSearch, StringComparison.OrdinalIgnoreCase) >= 0) ||
                            (card.description != null && card.description.IndexOf(_savedFilterSearch, StringComparison.OrdinalIgnoreCase) >= 0) ||
                            (card.category != null && card.category.IndexOf(_savedFilterSearch, StringComparison.OrdinalIgnoreCase) >= 0);

                        if (matchCat && matchAss && matchPri && matchSearch)
                        {
                            visibleCards++;
                        }
                    }
                }
            }

            return new BoardFilterInfo
            {
                category = _savedFilterCategory,
                assignee = assigneeName,
                assigneeId = _savedFilterAssigneeId,
                priority = _savedFilterPriority,
                priorityName = priLabel,
                searchQuery = _savedFilterSearch,
                totalCardsOnBoard = totalCards,
                visibleCardsOnBoard = visibleCards
            };
        }

        #endregion

        #region Data Transfer Types

        public class BulkUpdateItem
        {
            public string cardIdOrTitle;
            public string newTitle;
            public string newDescription;
            public string newCategory;
            public int? newPriority;
            public string newDueDate;
            public int? newColorLabel;
            public bool? completed;
            public bool? archived;
            public List<string> assignees;
        }

        public class BulkUpdateResult
        {
            public string cardIdOrTitle;
            public string cardTitle;
            public string cardId;
            public bool success;
            public string message;
        }

        public class BulkMoveItem
        {
            public string cardIdOrTitle;
            public string targetColumn;
            public string targetBoard;
            public string sourceBoard;
        }

        public class BulkMoveResult
        {
            public string cardIdOrTitle;
            public string cardTitle;
            public string cardId;
            public string targetColumn;
            public string targetBoard;
            public bool success;
            public string message;
        }

        public class BulkCloneItem
        {
            public string cardIdOrTitle;
            public string targetColumn;
            public string targetBoard;
            public string sourceBoard;
            public string newTitle;
        }

        public class BulkCloneResult
        {
            public string cardIdOrTitle;
            public string originalCardId;
            public string clonedCardId;
            public string clonedCardTitle;
            public string targetColumn;
            public string targetBoard;
            public bool success;
            public string message;
        }

        public class BulkDeleteCardResult
        {
            public string cardIdOrTitle;
            public string cardTitle;
            public string cardId;
            public bool success;
            public string message;
        }

        public class CategoryInfo
        {
            public string name;
            public int colorIndex;
        }

        public class BulkCreateNoteItem
        {
            public string title;
            public string content;
            public string folderName;
            public List<string> tags;
            public bool pinned;
            public int colorIndex;
        }

        public class BulkCreateNoteResult
        {
            public string title;
            public string noteId;
            public bool success;
            public string message;
        }

        public class BulkUpdateNoteItem
        {
            public string noteIdOrTitle;
            public string newTitle;
            public string newContent;
            public string appendContent;
            public string newFolderName;
            public List<string> newTags;
            public List<string> addTags;
            public List<string> removeTags;
            public bool? pinned;
            public int? colorIndex;
        }

        public class BulkUpdateNoteResult
        {
            public string noteIdOrTitle;
            public string noteTitle;
            public string noteId;
            public bool success;
            public string message;
        }

        public class BulkMoveNoteItem
        {
            public string noteIdOrTitle;
            public string targetFolderName;
        }

        public class BulkMoveNoteResult
        {
            public string noteIdOrTitle;
            public string noteTitle;
            public string noteId;
            public string targetFolderName;
            public bool success;
            public string message;
        }

        public class BulkDeleteNoteResult
        {
            public string noteIdOrTitle;
            public string noteTitle;
            public string noteId;
            public bool success;
            public string message;
        }

        public class BulkLinkItemEntry
        {
            public string cardIdOrTitle;
            public string target;
            public string linkType;
            public string displayName;
        }

        public class BulkLinkResult
        {
            public string cardIdOrTitle;
            public string cardTitle;
            public string cardId;
            public string target;
            public string linkType;
            public bool success;
            public string message;
        }

        public class BoardFilterInfo
        {
            public string category = "";
            public string assignee = "";
            public string assigneeId = "";
            public int priority = 0;
            public string priorityName = "All";
            public string searchQuery = "";
            public int totalCardsOnBoard = 0;
            public int visibleCardsOnBoard = 0;
        }

        #endregion

        #region Internal Helpers

        private static TaskCard GetCardInternal(SaveData data, string cardIdOrTitle)
        {
            if (data?.boards == null || string.IsNullOrWhiteSpace(cardIdOrTitle)) return null;
            string q = cardIdOrTitle.Trim();

            foreach (var b in data.boards)
            {
                if (b?.columns == null) continue;
                foreach (var col in b.columns)
                {
                    if (col?.cards == null) continue;
                    var match = col.cards.FirstOrDefault(c => c != null && (c.id == q || string.Equals(c.title, q, StringComparison.OrdinalIgnoreCase) || string.Equals(c.taskCode, q, StringComparison.OrdinalIgnoreCase)));
                    if (match != null) return match;
                }
            }
            return null;
        }

        #endregion
    }
}
