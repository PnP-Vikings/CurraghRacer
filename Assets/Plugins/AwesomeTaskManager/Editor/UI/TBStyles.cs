using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using AwesomeTaskManager.Data;

namespace AwesomeTaskManager.UI
{
    public static class TBStyles
    {
        public static readonly Color[] DefaultLabelColors = ThemeData.DefaultLabelColors;
        public static readonly string[] DefaultPriorityIcons = ThemeData.DefaultPriorityIcons;

        public static Color[] LabelColors = (Color[])DefaultLabelColors.Clone();
        public static string[] PriorityIcons = (string[])DefaultPriorityIcons.Clone();

        public static string BoardTabIcon = "📋";
        public static string NotesTabIcon = "📝";
        public static string StyleTabIcon = "🎨";
        public static string BoardHeaderIcon = "🎯";
        public static string NotesHeaderIcon = "📝";
        public static string CategoryIcon = "🏷";
        public static string AssigneeIcon = "👥";
        public static string PriorityFilterIcon = "🚩";
        public static string ParentLinkIcon = "🌳";
        public static string ChildLinkIcon = "🌿";
        public static string PinnedNoteIcon = "📌";
        public static string CompletedIcon = "✅";
        public static string OverdueIcon = "🔴";
        public static string DueTodayIcon = "🟠";
        public static string DueSoonIcon = "🟡";
        public static string DueDateIcon = "📅";
        public static string ArchiveIcon = "📦";
        public static string UnarchiveIcon = "🗃️";

        public static string CardDetailIcon = "📝";
        public static string NewCardIcon = "✨";
        public static string ChecklistIcon = "☑";
        public static string AttachmentIcon = "📎";
        public static string UrlIcon = "🔗";
        public static string DeleteIcon = "🗑";
        public static string SaveIcon = "💾";
        public static string CancelIcon = "✕";
        public static string MoveUpIcon = "▲";
        public static string MoveDownIcon = "▼";

        public static Color Pro_ColumnBg = new Color(0.22f, 0.22f, 0.22f);
        public static Color Pro_ColumnBgAlt = new Color(0.25f, 0.25f, 0.25f);
        public static Color Personal_ColumnBg = new Color(0.88f, 0.90f, 0.92f);
        public static Color Personal_ColumnBgAlt = new Color(0.92f, 0.93f, 0.95f);

        public static Color Pro_CardBg = new Color(0.24f, 0.24f, 0.24f);
        public static Color Personal_CardBg = new Color(0.96f, 0.96f, 0.96f);
        public static Color CardBg => EditorGUIUtility.isProSkin ? Pro_CardBg : Personal_CardBg;

        public static Color Pro_CardHighlighted = new Color(0.15f, 0.32f, 0.55f);
        public static Color Personal_CardHighlighted = new Color(0.55f, 0.72f, 0.95f);

        public static Color Pro_BoardBg = new Color(0.18f, 0.18f, 0.18f);
        public static Color Personal_BoardBg = new Color(0.80f, 0.82f, 0.84f);
        public static Color BoardBg => EditorGUIUtility.isProSkin ? Pro_BoardBg : Personal_BoardBg;

        public static Color Pro_TopBarBg = new Color(0.15f, 0.15f, 0.15f);
        public static Color Personal_TopBarBg = new Color(0.85f, 0.85f, 0.85f);
        public static Color TopBarBg => EditorGUIUtility.isProSkin ? Pro_TopBarBg : Personal_TopBarBg;

        public static Color Pro_StatusBarBg = new Color(0.15f, 0.15f, 0.15f);
        public static Color Personal_StatusBarBg = new Color(0.85f, 0.85f, 0.85f);
        public static Color StatusBarBg => EditorGUIUtility.isProSkin ? Pro_StatusBarBg : Personal_StatusBarBg;

        public static Color Pro_StatusBarText = new Color(0.75f, 0.75f, 0.78f);
        public static Color Personal_StatusBarText = new Color(0.35f, 0.35f, 0.38f);
        public static Color StatusBarTextColor => EditorGUIUtility.isProSkin ? Pro_StatusBarText : Personal_StatusBarText;

        public static Color Pro_NoteSidebarBg = new Color(0.20f, 0.20f, 0.20f);
        public static Color Personal_NoteSidebarBg = new Color(0.88f, 0.90f, 0.92f);
        public static Color NoteSidebarBg => EditorGUIUtility.isProSkin ? Pro_NoteSidebarBg : Personal_NoteSidebarBg;

        public static Color Pro_NoteEditorBg = new Color(0.18f, 0.18f, 0.18f);
        public static Color Personal_NoteEditorBg = new Color(0.84f, 0.86f, 0.88f);
        public static Color NoteEditorBg => EditorGUIUtility.isProSkin ? Pro_NoteEditorBg : Personal_NoteEditorBg;

        public static Color Pro_NotePopoutBg = new Color(0.18f, 0.18f, 0.18f);
        public static Color Personal_NotePopoutBg = new Color(0.85f, 0.85f, 0.85f);
        public static Color NotePopoutBg => EditorGUIUtility.isProSkin ? Pro_NotePopoutBg : Personal_NotePopoutBg;

        public static Color Pro_NoteInputBg = new Color(0.14f, 0.14f, 0.14f);
        public static Color Personal_NoteInputBg = new Color(0.96f, 0.96f, 0.96f);
        public static Color NoteInputBg => EditorGUIUtility.isProSkin ? Pro_NoteInputBg : Personal_NoteInputBg;

        public static Color Pro_NoteInputText = Color.white;
        public static Color Personal_NoteInputText = Color.black;
        public static Color NoteInputText => EditorGUIUtility.isProSkin ? Pro_NoteInputText : Personal_NoteInputText;

        public static Color Pro_CardDetailBg = new Color(0.18f, 0.18f, 0.18f);
        public static Color Personal_CardDetailBg = new Color(0.85f, 0.85f, 0.85f);
        public static Color CardDetailBg => EditorGUIUtility.isProSkin ? Pro_CardDetailBg : Personal_CardDetailBg;

        public static Color Pro_ButtonBg = new Color(0.26f, 0.26f, 0.26f);
        public static Color Personal_ButtonBg = new Color(0.92f, 0.92f, 0.92f);
        public static Color ButtonBg => EditorGUIUtility.isProSkin ? Pro_ButtonBg : Personal_ButtonBg;

        public static Color Pro_ButtonText = Color.white;
        public static Color Personal_ButtonText = Color.black;
        public static Color ButtonText => EditorGUIUtility.isProSkin ? Pro_ButtonText : Personal_ButtonText;

        public static Color Pro_ButtonHoverBg = new Color(0.35f, 0.35f, 0.35f);
        public static Color Personal_ButtonHoverBg = new Color(0.98f, 0.98f, 0.98f);
        public static Color ButtonHoverBg => EditorGUIUtility.isProSkin ? Pro_ButtonHoverBg : Personal_ButtonHoverBg;

        public static Color Pro_ButtonHoverText = Color.white;
        public static Color Personal_ButtonHoverText = Color.black;
        public static Color ButtonHoverText => EditorGUIUtility.isProSkin ? Pro_ButtonHoverText : Personal_ButtonHoverText;

        public static Color Pro_DropdownBg = new Color(0.22f, 0.22f, 0.22f);
        public static Color Personal_DropdownBg = new Color(0.94f, 0.94f, 0.94f);
        public static Color DropdownBg => EditorGUIUtility.isProSkin ? Pro_DropdownBg : Personal_DropdownBg;

        public static Color Pro_DropdownText = Color.white;
        public static Color Personal_DropdownText = Color.black;
        public static Color DropdownText => EditorGUIUtility.isProSkin ? Pro_DropdownText : Personal_DropdownText;

        public static Color Pro_DropdownHoverBg = new Color(0.30f, 0.30f, 0.30f);
        public static Color Personal_DropdownHoverBg = new Color(0.98f, 0.98f, 0.98f);
        public static Color DropdownHoverBg => EditorGUIUtility.isProSkin ? Pro_DropdownHoverBg : Personal_DropdownHoverBg;

        public static Color Pro_DropdownHoverText = Color.white;
        public static Color Personal_DropdownHoverText = Color.black;
        public static Color DropdownHoverText => EditorGUIUtility.isProSkin ? Pro_DropdownHoverText : Personal_DropdownHoverText;

        public static Color Pro_DropdownMenuBg = new Color(0.16f, 0.16f, 0.16f);
        public static Color Personal_DropdownMenuBg = new Color(0.93f, 0.93f, 0.93f);
        public static Color DropdownMenuBg => EditorGUIUtility.isProSkin ? Pro_DropdownMenuBg : Personal_DropdownMenuBg;

        public static Color Pro_DropdownMenuText = Color.white;
        public static Color Personal_DropdownMenuText = Color.black;
        public static Color DropdownMenuText => EditorGUIUtility.isProSkin ? Pro_DropdownMenuText : Personal_DropdownMenuText;

        public static Color Pro_DropdownMenuHoverBg = new Color(0.24f, 0.24f, 0.24f);
        public static Color Personal_DropdownMenuHoverBg = new Color(0.86f, 0.86f, 0.86f);
        public static Color DropdownMenuHoverBg => EditorGUIUtility.isProSkin ? Pro_DropdownMenuHoverBg : Personal_DropdownMenuHoverBg;

        public static Color Pro_DropdownMenuHoverText = Color.white;
        public static Color Personal_DropdownMenuHoverText = Color.black;
        public static Color DropdownMenuHoverText => EditorGUIUtility.isProSkin ? Pro_DropdownMenuHoverText : Personal_DropdownMenuHoverText;

        public static Color Pro_PopupBg = new Color(0.16f, 0.16f, 0.16f);
        public static Color Personal_PopupBg = new Color(0.92f, 0.92f, 0.92f);
        public static Color PopupBg => EditorGUIUtility.isProSkin ? Pro_PopupBg : Personal_PopupBg;

        public static Color Pro_DeleteBtnBg = new Color(0.48f, 0.16f, 0.16f);
        public static Color Personal_DeleteBtnBg = new Color(0.88f, 0.33f, 0.33f);
        public static Color DeleteBtnBg => EditorGUIUtility.isProSkin ? Pro_DeleteBtnBg : Personal_DeleteBtnBg;

        public static Color Pro_DeleteBtnText = Color.white;
        public static Color Personal_DeleteBtnText = Color.white;
        public static Color DeleteBtnText => EditorGUIUtility.isProSkin ? Pro_DeleteBtnText : Personal_DeleteBtnText;

        public static Color Pro_DeleteBtnHoverBg = new Color(0.60f, 0.20f, 0.20f);
        public static Color Personal_DeleteBtnHoverBg = new Color(0.94f, 0.40f, 0.40f);
        public static Color DeleteBtnHoverBg => EditorGUIUtility.isProSkin ? Pro_DeleteBtnHoverBg : Personal_DeleteBtnHoverBg;

        public static Color Pro_HeaderTabActiveBg = new Color(0.2f, 0.5f, 0.85f);
        public static Color Personal_HeaderTabActiveBg = new Color(0.25f, 0.55f, 0.90f);
        public static Color HeaderTabActiveBg => EditorGUIUtility.isProSkin ? Pro_HeaderTabActiveBg : Personal_HeaderTabActiveBg;

        public static Color Pro_HeaderTabActiveText = Color.white;
        public static Color Personal_HeaderTabActiveText = Color.white;
        public static Color HeaderTabActiveText => EditorGUIUtility.isProSkin ? Pro_HeaderTabActiveText : Personal_HeaderTabActiveText;

        public static Color Pro_HeaderTabInactiveBg = new Color(0.22f, 0.22f, 0.22f);
        public static Color Personal_HeaderTabInactiveBg = new Color(0.88f, 0.88f, 0.88f);
        public static Color HeaderTabInactiveBg => EditorGUIUtility.isProSkin ? Pro_HeaderTabInactiveBg : Personal_HeaderTabInactiveBg;

        public static Color Pro_HeaderTabInactiveText = new Color(0.85f, 0.85f, 0.85f);
        public static Color Personal_HeaderTabInactiveText = new Color(0.2f, 0.2f, 0.2f);
        public static Color HeaderTabInactiveText => EditorGUIUtility.isProSkin ? Pro_HeaderTabInactiveText : Personal_HeaderTabInactiveText;

        public static Color Pro_HeaderTabHoverBg = new Color(0.32f, 0.32f, 0.32f);
        public static Color Personal_HeaderTabHoverBg = new Color(0.95f, 0.95f, 0.95f);
        public static Color HeaderTabHoverBg => EditorGUIUtility.isProSkin ? Pro_HeaderTabHoverBg : Personal_HeaderTabHoverBg;

        public static Color Pro_AddCardBg = new Color(0.20f, 0.38f, 0.28f);
        public static Color Personal_AddCardBg = new Color(0.82f, 0.92f, 0.85f);
        public static Color AddCardBg => EditorGUIUtility.isProSkin ? Pro_AddCardBg : Personal_AddCardBg;

        public static Color Pro_AddCardText = new Color(0.85f, 1f, 0.9f);
        public static Color Personal_AddCardText = new Color(0.1f, 0.35f, 0.15f);
        public static Color AddCardText => EditorGUIUtility.isProSkin ? Pro_AddCardText : Personal_AddCardText;

        public static Color Pro_AddCardHoverBg = new Color(0.25f, 0.48f, 0.35f);
        public static Color Personal_AddCardHoverBg = new Color(0.88f, 0.97f, 0.90f);
        public static Color AddCardHoverBg => EditorGUIUtility.isProSkin ? Pro_AddCardHoverBg : Personal_AddCardHoverBg;

        public static Color Pro_NoteCardBg = new Color(0.22f, 0.22f, 0.22f);
        public static Color Personal_NoteCardBg = new Color(0.92f, 0.94f, 0.96f);
        public static Color NoteCardBg => EditorGUIUtility.isProSkin ? Pro_NoteCardBg : Personal_NoteCardBg;

        public static Color Pro_NoteCardSelectedBg = new Color(0.15f, 0.32f, 0.55f);
        public static Color Personal_NoteCardSelectedBg = new Color(0.55f, 0.72f, 0.95f);
        public static Color NoteCardSelectedBg => EditorGUIUtility.isProSkin ? Pro_NoteCardSelectedBg : Personal_NoteCardSelectedBg;

        public static Color Pro_NoteCardHoverBg = new Color(0.26f, 0.26f, 0.26f);
        public static Color Personal_NoteCardHoverBg = new Color(0.96f, 0.97f, 0.98f);
        public static Color NoteCardHoverBg => EditorGUIUtility.isProSkin ? Pro_NoteCardHoverBg : Personal_NoteCardHoverBg;

        public static Color Pro_NoteActionBg = new Color(0.20f, 0.40f, 0.60f);
        public static Color Personal_NoteActionBg = new Color(0.80f, 0.88f, 0.96f);
        public static Color NoteActionBg => EditorGUIUtility.isProSkin ? Pro_NoteActionBg : Personal_NoteActionBg;

        public static Color Pro_NoteActionText = Color.white;
        public static Color Personal_NoteActionText = new Color(0.12f, 0.22f, 0.35f);
        public static Color NoteActionText => EditorGUIUtility.isProSkin ? Pro_NoteActionText : Personal_NoteActionText;

        public static Color Pro_NoteActionHoverBg = new Color(0.26f, 0.48f, 0.70f);
        public static Color Personal_NoteActionHoverBg = new Color(0.86f, 0.93f, 0.99f);
        public static Color NoteActionHoverBg => EditorGUIUtility.isProSkin ? Pro_NoteActionHoverBg : Personal_NoteActionHoverBg;

        public static Color Pro_NoteActionHoverText = Color.white;
        public static Color Personal_NoteActionHoverText = new Color(0.12f, 0.22f, 0.35f);
        public static Color NoteActionHoverText => EditorGUIUtility.isProSkin ? Pro_NoteActionHoverText : Personal_NoteActionHoverText;

        public static Color Pro_AddNoteBg = new Color(0.20f, 0.40f, 0.60f);
        public static Color Personal_AddNoteBg = new Color(0.80f, 0.88f, 0.96f);
        public static Color AddNoteBg => EditorGUIUtility.isProSkin ? Pro_AddNoteBg : Personal_AddNoteBg;

        public static Color Pro_AddNoteText = Color.white;
        public static Color Personal_AddNoteText = new Color(0.12f, 0.22f, 0.35f);
        public static Color AddNoteText => EditorGUIUtility.isProSkin ? Pro_AddNoteText : Personal_AddNoteText;

        public static Color Pro_AddNoteHoverBg = new Color(0.26f, 0.48f, 0.70f);
        public static Color Personal_AddNoteHoverBg = new Color(0.86f, 0.93f, 0.99f);
        public static Color AddNoteHoverBg => EditorGUIUtility.isProSkin ? Pro_AddNoteHoverBg : Personal_AddNoteHoverBg;

        public static Color Pro_AddNoteHoverText = Color.white;
        public static Color Personal_AddNoteHoverText = new Color(0.12f, 0.22f, 0.35f);
        public static Color AddNoteHoverText => EditorGUIUtility.isProSkin ? Pro_AddNoteHoverText : Personal_AddNoteHoverText;

        public static Color Pro_ImportNoteBg = new Color(0.20f, 0.40f, 0.60f);
        public static Color Personal_ImportNoteBg = new Color(0.80f, 0.88f, 0.96f);
        public static Color ImportNoteBg => EditorGUIUtility.isProSkin ? Pro_ImportNoteBg : Personal_ImportNoteBg;

        public static Color Pro_ImportNoteText = Color.white;
        public static Color Personal_ImportNoteText = new Color(0.12f, 0.22f, 0.35f);
        public static Color ImportNoteText => EditorGUIUtility.isProSkin ? Pro_ImportNoteText : Personal_ImportNoteText;

        public static Color Pro_ImportNoteHoverBg = new Color(0.26f, 0.48f, 0.70f);
        public static Color Personal_ImportNoteHoverBg = new Color(0.86f, 0.93f, 0.99f);
        public static Color ImportNoteHoverBg => EditorGUIUtility.isProSkin ? Pro_ImportNoteHoverBg : Personal_ImportNoteHoverBg;

        public static Color Pro_ImportNoteHoverText = Color.white;
        public static Color Personal_ImportNoteHoverText = new Color(0.12f, 0.22f, 0.35f);
        public static Color ImportNoteHoverText => EditorGUIUtility.isProSkin ? Pro_ImportNoteHoverText : Personal_ImportNoteHoverText;

        public static Color Pro_NoteFolderText = new Color(0.85f, 0.85f, 0.85f);
        public static Color Personal_NoteFolderText = new Color(0.20f, 0.20f, 0.20f);
        public static Color NoteFolderTextColor => EditorGUIUtility.isProSkin ? Pro_NoteFolderText : Personal_NoteFolderText;

        public static Color Pro_CardDetailsText = new Color(0.75f, 0.75f, 0.78f);
        public static Color Personal_CardDetailsText = new Color(0.35f, 0.35f, 0.38f);
        public static Color CardDetailsTextColor => EditorGUIUtility.isProSkin ? Pro_CardDetailsText : Personal_CardDetailsText;

        public static Color Pro_CardTasksText = new Color(0.70f, 0.70f, 0.70f);
        public static Color Personal_CardTasksText = new Color(0.30f, 0.30f, 0.30f);
        public static Color CardTasksTextColor => EditorGUIUtility.isProSkin ? Pro_CardTasksText : Personal_CardTasksText;

        public static Color Pro_CardCategoryTag = new Color(0.75f, 0.75f, 0.78f);
        public static Color Personal_CardCategoryTag = new Color(0.35f, 0.35f, 0.38f);
        public static Color CardCategoryTagColor => EditorGUIUtility.isProSkin ? Pro_CardCategoryTag : Personal_CardCategoryTag;

        public static Color Pro_AssigneeAvatarBg = new Color(0.20f, 0.20f, 0.22f, 1.0f);
        public static Color Personal_AssigneeAvatarBg = new Color(0.92f, 0.92f, 0.94f, 1.0f);
        public static Color AssigneeAvatarBg => EditorGUIUtility.isProSkin ? Pro_AssigneeAvatarBg : Personal_AssigneeAvatarBg;

        public static Color Pro_ChecklistTickBg = new Color(0.18f, 0.18f, 0.20f);
        public static Color Personal_ChecklistTickBg = new Color(0.92f, 0.94f, 0.96f);
        public static Color ChecklistTickBg => EditorGUIUtility.isProSkin ? Pro_ChecklistTickBg : Personal_ChecklistTickBg;

        public static Color Pro_ChecklistTickCheckedBg = Color.clear;
        public static Color Personal_ChecklistTickCheckedBg = Color.clear;
        public static Color ChecklistTickCheckedBg => EditorGUIUtility.isProSkin ? Pro_ChecklistTickCheckedBg : Personal_ChecklistTickCheckedBg;

        public static Color Pro_ChecklistTickBorder = new Color(0.40f, 0.40f, 0.45f);
        public static Color Personal_ChecklistTickBorder = new Color(0.65f, 0.70f, 0.75f);
        public static Color ChecklistTickBorder => EditorGUIUtility.isProSkin ? Pro_ChecklistTickBorder : Personal_ChecklistTickBorder;

        public static Color Pro_ChecklistTickColor = Color.white;
        public static Color Personal_ChecklistTickColor = new Color(0.15f, 0.15f, 0.15f);
        public static Color ChecklistTickColor => EditorGUIUtility.isProSkin ? Pro_ChecklistTickColor : Personal_ChecklistTickColor;

        public static ChecklistTickStyle ChecklistTickStyle = ChecklistTickStyle.Vector;
        public static string CustomChecklistTickChar = "";

        public static Color Pro_StatusOverdue = new Color(1f, 0.35f, 0.3f);
        public static Color Personal_StatusOverdue = new Color(0.85f, 0.2f, 0.15f);
        public static Color StatusOverdueColor => EditorGUIUtility.isProSkin ? Pro_StatusOverdue : Personal_StatusOverdue;

        public static Color Pro_StatusDueToday = new Color(1f, 0.65f, 0.15f);
        public static Color Personal_StatusDueToday = new Color(0.9f, 0.5f, 0.05f);
        public static Color StatusDueTodayColor => EditorGUIUtility.isProSkin ? Pro_StatusDueToday : Personal_StatusDueToday;

        public static Color Pro_StatusDueSoon = new Color(0.95f, 0.85f, 0.2f);
        public static Color Personal_StatusDueSoon = new Color(0.8f, 0.7f, 0.1f);
        public static Color StatusDueSoonColor => EditorGUIUtility.isProSkin ? Pro_StatusDueSoon : Personal_StatusDueSoon;

        public static Color Pro_StatusCompleted = new Color(0.4f, 0.88f, 0.45f);
        public static Color Personal_StatusCompleted = new Color(0.15f, 0.65f, 0.25f);
        public static Color StatusCompletedColor => EditorGUIUtility.isProSkin ? Pro_StatusCompleted : Personal_StatusCompleted;

        public static Color Pro_TasksCompletedCount = new Color(0.4f, 0.88f, 0.45f);
        public static Color Personal_TasksCompletedCount = new Color(0.15f, 0.65f, 0.25f);
        public static Color TasksCompletedCountColor => EditorGUIUtility.isProSkin ? Pro_TasksCompletedCount : Personal_TasksCompletedCount;

        public static Color Pro_TooltipBg = new Color(0.12f, 0.12f, 0.14f, 0.96f);
        public static Color Personal_TooltipBg = new Color(0.96f, 0.96f, 0.98f, 0.96f);
        public static Color TooltipBg => EditorGUIUtility.isProSkin ? Pro_TooltipBg : Personal_TooltipBg;

        public static Color Pro_TooltipText = Color.white;
        public static Color Personal_TooltipText = new Color(0.10f, 0.10f, 0.12f);
        public static Color TooltipTextColor => EditorGUIUtility.isProSkin ? Pro_TooltipText : Personal_TooltipText;

        public static Color Pro_TooltipBorder = new Color(0.32f, 0.32f, 0.38f, 0.8f);
        public static Color Personal_TooltipBorder = new Color(0.72f, 0.72f, 0.78f, 0.8f);
        public static Color TooltipBorder => EditorGUIUtility.isProSkin ? Pro_TooltipBorder : Personal_TooltipBorder;

        public static Color Pro_BoardHeader = Color.white;
        public static Color Personal_BoardHeader = Color.black;
        public static Color BoardHeaderColor => EditorGUIUtility.isProSkin ? Pro_BoardHeader : Personal_BoardHeader;

        public static Color Pro_ColumnHeader = new Color(0.90f, 0.90f, 0.90f);
        public static Color Personal_ColumnHeader = Color.black;
        public static Color ColumnHeaderColor => EditorGUIUtility.isProSkin ? Pro_ColumnHeader : Personal_ColumnHeader;

        public static Color Pro_CardTitle = Color.white;
        public static Color Personal_CardTitle = Color.black;
        public static Color CardTitleColor => EditorGUIUtility.isProSkin ? Pro_CardTitle : Personal_CardTitle;

        public static Color Pro_NoteTitle = Color.white;
        public static Color Personal_NoteTitle = Color.black;
        public static Color NoteTitleColor => EditorGUIUtility.isProSkin ? Pro_NoteTitle : Personal_NoteTitle;

        public static Color Pro_CardText = new Color(0.85f, 0.85f, 0.85f);
        public static Color Personal_CardText = new Color(0.20f, 0.20f, 0.20f);
        public static Color CardTextColor => EditorGUIUtility.isProSkin ? Pro_CardText : Personal_CardText;

        public static Color Pro_SectionLabel = Color.white;
        public static Color Personal_SectionLabel = Color.black;
        public static Color SectionLabelColor => EditorGUIUtility.isProSkin ? Pro_SectionLabel : Personal_SectionLabel;

        public static Color TabActiveColor = new Color(0.2f, 0.5f, 0.85f);
        public static Color NoteSelectedAccentColor = new Color(0.2f, 0.6f, 1f);
        public static Color LinkColor = new Color(0.2f, 0.55f, 0.95f);

        public static Color GetLabelColor(int index)
        {
            if (LabelColors == null || LabelColors.Length == 0) return DefaultLabelColors[0];
            return LabelColors[Mathf.Clamp(index, 0, LabelColors.Length - 1)];
        }

        public static string GetPriorityIcon(int priority)
        {
            if (PriorityIcons == null || PriorityIcons.Length == 0) return DefaultPriorityIcons[0];
            return PriorityIcons[Mathf.Clamp(priority, 0, PriorityIcons.Length - 1)];
        }

        public static string[] GetPriorityDisplayNames()
        {
            string[] res = new string[PriorityNames.Length];
            for (int i = 0; i < PriorityNames.Length; i++)
            {
                string icon = GetPriorityIcon(i);
                res[i] = string.IsNullOrEmpty(icon) ? PriorityNames[i] : $"{icon} {PriorityNames[i]}";
            }
            return res;
        }

        public static void ApplyTheme(ThemeData theme)
        {
            if (theme == null) return;
            theme.Normalize();

            LabelColors = theme.labelColors.ToArray();
            PriorityIcons = theme.priorityIcons.ToArray();

            BoardTabIcon = string.IsNullOrEmpty(theme.boardTabIcon) ? "📋" : theme.boardTabIcon;
            NotesTabIcon = string.IsNullOrEmpty(theme.notesTabIcon) ? "📝" : theme.notesTabIcon;
            StyleTabIcon = string.IsNullOrEmpty(theme.styleTabIcon) ? "🎨" : theme.styleTabIcon;
            BoardHeaderIcon = string.IsNullOrEmpty(theme.boardHeaderIcon) ? "🎯" : theme.boardHeaderIcon;
            NotesHeaderIcon = string.IsNullOrEmpty(theme.notesHeaderIcon) ? "📝" : theme.notesHeaderIcon;
            CategoryIcon = string.IsNullOrEmpty(theme.categoryIcon) ? "🏷" : theme.categoryIcon;
            AssigneeIcon = string.IsNullOrEmpty(theme.assigneeIcon) ? "👥" : theme.assigneeIcon;
            PriorityFilterIcon = string.IsNullOrEmpty(theme.priorityFilterIcon) ? "🚩" : theme.priorityFilterIcon;
            ParentLinkIcon = string.IsNullOrEmpty(theme.parentLinkIcon) ? "🌳" : theme.parentLinkIcon;
            ChildLinkIcon = string.IsNullOrEmpty(theme.childLinkIcon) ? "🌿" : theme.childLinkIcon;
            PinnedNoteIcon = string.IsNullOrEmpty(theme.pinnedNoteIcon) ? "📌" : theme.pinnedNoteIcon;
            CompletedIcon = string.IsNullOrEmpty(theme.completedIcon) ? "✅" : theme.completedIcon;
            OverdueIcon = string.IsNullOrEmpty(theme.overdueIcon) ? "🔴" : theme.overdueIcon;
            DueTodayIcon = string.IsNullOrEmpty(theme.dueTodayIcon) ? "🟠" : theme.dueTodayIcon;
            DueSoonIcon = string.IsNullOrEmpty(theme.dueSoonIcon) ? "🟡" : theme.dueSoonIcon;
            DueDateIcon = string.IsNullOrEmpty(theme.dueDateIcon) ? "📅" : theme.dueDateIcon;
            ArchiveIcon = string.IsNullOrEmpty(theme.archiveIcon) ? "📦" : theme.archiveIcon;
            UnarchiveIcon = string.IsNullOrEmpty(theme.unarchiveIcon) ? "🗃️" : theme.unarchiveIcon;

            CardDetailIcon = string.IsNullOrEmpty(theme.cardDetailIcon) ? "📝" : theme.cardDetailIcon;
            NewCardIcon = string.IsNullOrEmpty(theme.newCardIcon) ? "✨" : theme.newCardIcon;
            ChecklistIcon = string.IsNullOrEmpty(theme.checklistIcon) ? "☑" : theme.checklistIcon;
            AttachmentIcon = string.IsNullOrEmpty(theme.attachmentIcon) ? "📎" : theme.attachmentIcon;
            UrlIcon = string.IsNullOrEmpty(theme.urlIcon) ? "🔗" : theme.urlIcon;
            DeleteIcon = string.IsNullOrEmpty(theme.deleteIcon) ? "🗑" : theme.deleteIcon;
            SaveIcon = string.IsNullOrEmpty(theme.saveIcon) ? "💾" : theme.saveIcon;
            CancelIcon = string.IsNullOrEmpty(theme.cancelIcon) ? "✕" : theme.cancelIcon;
            MoveUpIcon = string.IsNullOrEmpty(theme.moveUpIcon) ? "▲" : theme.moveUpIcon;
            MoveDownIcon = string.IsNullOrEmpty(theme.moveDownIcon) ? "▼" : theme.moveDownIcon;

            Pro_BoardHeader = theme.pro_BoardHeader;
            Personal_BoardHeader = theme.personal_BoardHeader;

            Pro_ColumnHeader = theme.pro_ColumnHeader;
            Personal_ColumnHeader = theme.personal_ColumnHeader;

            Pro_CardTitle = theme.pro_CardTitle;
            Personal_CardTitle = theme.personal_CardTitle;

            Pro_NoteTitle = theme.pro_NoteTitle;
            Personal_NoteTitle = theme.personal_NoteTitle;

            Pro_CardText = theme.pro_CardText;
            Personal_CardText = theme.personal_CardText;

            Pro_SectionLabel = theme.pro_SectionLabel;
            Personal_SectionLabel = theme.personal_SectionLabel;

            Pro_ColumnBg = theme.pro_ColumnBg;
            Pro_ColumnBgAlt = theme.pro_ColumnBgAlt;
            Personal_ColumnBg = theme.personal_ColumnBg;
            Personal_ColumnBgAlt = theme.personal_ColumnBgAlt;

            Pro_CardBg = theme.pro_CardBg;
            Personal_CardBg = theme.personal_CardBg;

            Pro_CardHighlighted = theme.pro_CardHighlighted;
            Personal_CardHighlighted = theme.personal_CardHighlighted;

            Pro_BoardBg = theme.pro_BoardBg;
            Personal_BoardBg = theme.personal_BoardBg;

            Pro_TopBarBg = theme.pro_TopBarBg;
            Personal_TopBarBg = theme.personal_TopBarBg;

            Pro_StatusBarBg = theme.pro_StatusBarBg;
            Personal_StatusBarBg = theme.personal_StatusBarBg;
            Pro_StatusBarText = theme.pro_StatusBarText;
            Personal_StatusBarText = theme.personal_StatusBarText;

            Pro_NoteSidebarBg = theme.pro_NoteSidebarBg;
            Personal_NoteSidebarBg = theme.personal_NoteSidebarBg;

            Pro_NoteEditorBg = theme.pro_NoteEditorBg;
            Personal_NoteEditorBg = theme.personal_NoteEditorBg;

            Pro_NotePopoutBg = theme.pro_NotePopoutBg;
            Personal_NotePopoutBg = theme.personal_NotePopoutBg;

            Pro_NoteInputBg = theme.pro_NoteInputBg;
            Personal_NoteInputBg = theme.personal_NoteInputBg;
            Pro_NoteInputText = theme.pro_NoteInputText;
            Personal_NoteInputText = theme.personal_NoteInputText;

            Pro_CardDetailBg = theme.pro_CardDetailBg;
            Personal_CardDetailBg = theme.personal_CardDetailBg;

            Pro_ButtonBg = theme.pro_ButtonBg;
            Personal_ButtonBg = theme.personal_ButtonBg;
            Pro_ButtonText = theme.pro_ButtonText;
            Personal_ButtonText = theme.personal_ButtonText;
            Pro_ButtonHoverBg = theme.pro_ButtonHoverBg;
            Personal_ButtonHoverBg = theme.personal_ButtonHoverBg;
            Pro_ButtonHoverText = theme.pro_ButtonHoverText;
            Personal_ButtonHoverText = theme.personal_ButtonHoverText;

            Pro_DropdownBg = theme.pro_DropdownBg;
            Personal_DropdownBg = theme.personal_DropdownBg;
            Pro_DropdownText = theme.pro_DropdownText;
            Personal_DropdownText = theme.personal_DropdownText;
            Pro_DropdownHoverBg = theme.pro_DropdownHoverBg;
            Personal_DropdownHoverBg = theme.personal_DropdownHoverBg;
            Pro_DropdownHoverText = theme.pro_DropdownHoverText;
            Personal_DropdownHoverText = theme.personal_DropdownHoverText;

            Pro_DropdownMenuBg = theme.pro_DropdownMenuBg;
            Personal_DropdownMenuBg = theme.personal_DropdownMenuBg;
            Pro_DropdownMenuText = theme.pro_DropdownMenuText;
            Personal_DropdownMenuText = theme.personal_DropdownMenuText;
            Pro_DropdownMenuHoverBg = theme.pro_DropdownMenuHoverBg;
            Personal_DropdownMenuHoverBg = theme.personal_DropdownMenuHoverBg;
            Pro_DropdownMenuHoverText = theme.pro_DropdownMenuHoverText;
            Personal_DropdownMenuHoverText = theme.personal_DropdownMenuHoverText;

            Pro_PopupBg = theme.pro_PopupBg;
            Personal_PopupBg = theme.personal_PopupBg;

            Pro_DeleteBtnBg = theme.pro_DeleteBtnBg;
            Personal_DeleteBtnBg = theme.personal_DeleteBtnBg;
            Pro_DeleteBtnText = theme.pro_DeleteBtnText;
            Personal_DeleteBtnText = theme.personal_DeleteBtnText;
            Pro_DeleteBtnHoverBg = theme.pro_DeleteBtnHoverBg;
            Personal_DeleteBtnHoverBg = theme.personal_DeleteBtnHoverBg;

            Pro_HeaderTabActiveBg = theme.pro_HeaderTabActiveBg;
            Personal_HeaderTabActiveBg = theme.personal_HeaderTabActiveBg;
            Pro_HeaderTabActiveText = theme.pro_HeaderTabActiveText;
            Personal_HeaderTabActiveText = theme.personal_HeaderTabActiveText;
            Pro_HeaderTabInactiveBg = theme.pro_HeaderTabInactiveBg;
            Personal_HeaderTabInactiveBg = theme.personal_HeaderTabInactiveBg;
            Pro_HeaderTabInactiveText = theme.pro_HeaderTabInactiveText;
            Personal_HeaderTabInactiveText = theme.personal_HeaderTabInactiveText;
            Pro_HeaderTabHoverBg = theme.pro_HeaderTabHoverBg;
            Personal_HeaderTabHoverBg = theme.personal_HeaderTabHoverBg;

            Pro_AddCardBg = theme.pro_AddCardBg;
            Personal_AddCardBg = theme.personal_AddCardBg;
            Pro_AddCardText = theme.pro_AddCardText;
            Personal_AddCardText = theme.personal_AddCardText;
            Pro_AddCardHoverBg = theme.pro_AddCardHoverBg;
            Personal_AddCardHoverBg = theme.personal_AddCardHoverBg;

            Pro_NoteCardBg = theme.pro_NoteCardBg;
            Personal_NoteCardBg = theme.personal_NoteCardBg;
            Pro_NoteCardSelectedBg = theme.pro_NoteCardSelectedBg;
            Personal_NoteCardSelectedBg = theme.personal_NoteCardSelectedBg;
            Pro_NoteCardHoverBg = theme.pro_NoteCardHoverBg;
            Personal_NoteCardHoverBg = theme.personal_NoteCardHoverBg;

            Pro_NoteActionBg = theme.pro_NoteActionBg;
            Personal_NoteActionBg = theme.personal_NoteActionBg;
            Pro_NoteActionText = theme.pro_NoteActionText;
            Personal_NoteActionText = theme.personal_NoteActionText;
            Pro_NoteActionHoverBg = theme.pro_NoteActionHoverBg;
            Personal_NoteActionHoverBg = theme.personal_NoteActionHoverBg;
            Pro_NoteActionHoverText = theme.pro_NoteActionHoverText;
            Personal_NoteActionHoverText = theme.personal_NoteActionHoverText;

            Pro_AddNoteBg = theme.pro_AddNoteBg;
            Personal_AddNoteBg = theme.personal_AddNoteBg;
            Pro_AddNoteText = theme.pro_AddNoteText;
            Personal_AddNoteText = theme.personal_AddNoteText;
            Pro_AddNoteHoverBg = theme.pro_AddNoteHoverBg;
            Personal_AddNoteHoverBg = theme.personal_AddNoteHoverBg;
            Pro_AddNoteHoverText = theme.pro_AddNoteHoverText;
            Personal_AddNoteHoverText = theme.personal_AddNoteHoverText;

            Pro_ImportNoteBg = theme.pro_ImportNoteBg;
            Personal_ImportNoteBg = theme.personal_ImportNoteBg;
            Pro_ImportNoteText = theme.pro_ImportNoteText;
            Personal_ImportNoteText = theme.personal_ImportNoteText;
            Pro_ImportNoteHoverBg = theme.pro_ImportNoteHoverBg;
            Personal_ImportNoteHoverBg = theme.personal_ImportNoteHoverBg;
            Pro_ImportNoteHoverText = theme.pro_ImportNoteHoverText;
            Personal_ImportNoteHoverText = theme.personal_ImportNoteHoverText;

            Pro_NoteFolderText = theme.pro_NoteFolderText;
            Personal_NoteFolderText = theme.personal_NoteFolderText;

            Pro_CardDetailsText = theme.pro_CardDetailsText;
            Personal_CardDetailsText = theme.personal_CardDetailsText;

            Pro_CardTasksText = theme.pro_CardTasksText;
            Personal_CardTasksText = theme.personal_CardTasksText;

            Pro_CardCategoryTag = theme.pro_CardCategoryTag;
            Personal_CardCategoryTag = theme.personal_CardCategoryTag;

            Pro_AssigneeAvatarBg = theme.pro_AssigneeAvatarBg;
            Personal_AssigneeAvatarBg = theme.personal_AssigneeAvatarBg;

            Pro_ChecklistTickBg = theme.pro_ChecklistTickBg;
            Personal_ChecklistTickBg = theme.personal_ChecklistTickBg;
            Pro_ChecklistTickCheckedBg = theme.pro_ChecklistTickCheckedBg;
            Personal_ChecklistTickCheckedBg = theme.personal_ChecklistTickCheckedBg;
            Pro_ChecklistTickBorder = theme.pro_ChecklistTickBorder;
            Personal_ChecklistTickBorder = theme.personal_ChecklistTickBorder;
            Pro_ChecklistTickColor = theme.pro_ChecklistTickColor;
            Personal_ChecklistTickColor = theme.personal_ChecklistTickColor;
            ChecklistTickStyle = theme.checklistTickStyle;
            CustomChecklistTickChar = theme.customChecklistTickChar ?? "";

            Pro_StatusOverdue = theme.pro_StatusOverdue;
            Personal_StatusOverdue = theme.personal_StatusOverdue;

            Pro_StatusDueToday = theme.pro_StatusDueToday;
            Personal_StatusDueToday = theme.personal_StatusDueToday;

            Pro_StatusDueSoon = theme.pro_StatusDueSoon;
            Personal_StatusDueSoon = theme.personal_StatusDueSoon;

            Pro_StatusCompleted = theme.pro_StatusCompleted;
            Personal_StatusCompleted = theme.personal_StatusCompleted;

            Pro_TasksCompletedCount = theme.pro_TasksCompletedCount;
            Personal_TasksCompletedCount = theme.personal_TasksCompletedCount;

            Pro_TooltipBg = theme.pro_TooltipBg;
            Personal_TooltipBg = theme.personal_TooltipBg;
            Pro_TooltipText = theme.pro_TooltipText;
            Personal_TooltipText = theme.personal_TooltipText;
            Pro_TooltipBorder = theme.pro_TooltipBorder;
            Personal_TooltipBorder = theme.personal_TooltipBorder;

            TabActiveColor = theme.tabActive;
            NoteSelectedAccentColor = theme.noteSelectedAccent;
            LinkColor = theme.linkColor;

            InvalidateCache();
        }

        public static readonly string[] LabelNames = {
            "None", "Green", "Blue", "Yellow", "Orange", "Red", "Purple", "Teal",
            "Pink", "Lime", "Indigo", "Cyan", "Amber", "Deep Orange", "Deep Purple", "Blue Grey", "Brown"
        };
        public static readonly string[] PriorityNames = { "—", "Low", "Medium", "High", "Urgent" };

        // Cached textures
        private static readonly Dictionary<Color, Texture2D> _texCache = new Dictionary<Color, Texture2D>();
        private static readonly Dictionary<string, Texture2D> _profileTexCache = new Dictionary<string, Texture2D>();
        private static readonly Dictionary<string, Texture2D> _circularProfileTexCache = new Dictionary<string, Texture2D>();
        private static Texture2D _circleTex, _circleBorderTex;

        public static Texture2D GetColorTex(Color c)
        {
            if (_texCache.TryGetValue(c, out var t) && t != null) return t;
            t = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            t.SetPixel(0, 0, c);
            t.Apply();
            t.hideFlags = HideFlags.DontSave;
            _texCache[c] = t;
            return t;
        }

        public static Texture2D GetProfileTexture(string guid)
        {
            if (string.IsNullOrEmpty(guid)) return null;
            if (_profileTexCache.TryGetValue(guid, out var tex) && tex != null) return tex;

            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!string.IsNullOrEmpty(path))
            {
                tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                _profileTexCache[guid] = tex;
                return tex;
            }
            return null;
        }

        public static Texture2D GetCircularProfileTexture(string guid, int targetSize = 128)
        {
            if (string.IsNullOrEmpty(guid)) return null;
            if (_circularProfileTexCache.TryGetValue(guid, out var cached) && cached != null) return cached;

            Texture2D src = GetProfileTexture(guid);
            if (src == null) return null;

            try
            {
                RenderTexture rt = RenderTexture.GetTemporary(targetSize, targetSize, 0, RenderTextureFormat.ARGB32);
                RenderTexture prev = RenderTexture.active;

                float scaleX = Mathf.Min(1f, (float)src.height / src.width);
                float scaleY = Mathf.Min(1f, (float)src.width / src.height);
                float offsetX = (1f - scaleX) * 0.5f;
                float offsetY = (1f - scaleY) * 0.5f;

                Graphics.Blit(src, rt, new Vector2(scaleX, scaleY), new Vector2(offsetX, offsetY));
                RenderTexture.active = rt;

                Texture2D cropped = new Texture2D(targetSize, targetSize, TextureFormat.RGBA32, false);
                cropped.ReadPixels(new Rect(0, 0, targetSize, targetSize), 0, 0);
                cropped.Apply();

                RenderTexture.active = prev;
                RenderTexture.ReleaseTemporary(rt);

                Color32[] pixels = cropped.GetPixels32();
                Vector2 center = new Vector2(targetSize * 0.5f, targetSize * 0.5f);
                float radius = targetSize * 0.5f;

                for (int y = 0; y < targetSize; y++)
                {
                    for (int x = 0; x < targetSize; x++)
                    {
                        int index = y * targetSize + x;
                        float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);
                        float alphaFactor = Mathf.Clamp01(radius + 0.5f - dist);

                        Color32 c = pixels[index];
                        c.a = (byte)Mathf.RoundToInt(c.a * alphaFactor);
                        pixels[index] = c;
                    }
                }

                cropped.SetPixels32(pixels);
                cropped.Apply();
                cropped.hideFlags = HideFlags.DontSave;

                _circularProfileTexCache[guid] = cropped;
                return cropped;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[AwesomeTaskManager] Failed to create circular avatar texture: {ex.Message}");
                return src;
            }
        }

        public static void DrawAssigneeIcon(Rect rect, Assignee assignee, string initials, GUIStyle style, Color? maskColor = null)
        {
            var borderColor = LabelColors[Mathf.Clamp(assignee.borderColorIndex, 0, LabelColors.Length - 1)];
            var bgColor = LabelColors[Mathf.Clamp(assignee.colorIndex, 0, LabelColors.Length - 1)];

            _circleTex ??= CreateCircleTexture(128);
            _circleBorderTex ??= CreateCircleBorderTexture(128, 8);

            Texture2D circularProfileTex = GetCircularProfileTexture(assignee.profileImageGuid);
            if (circularProfileTex != null)
            {
                // Draw themed avatar background (useful if profile image is transparent or circular)
                var oldBgColor = GUI.color;
                GUI.color = AssigneeAvatarBg;
                GUI.DrawTexture(rect, _circleTex);
                GUI.color = oldBgColor;

                // Draw circular profile image with alpha-masked transparent corners
                GUI.DrawTexture(rect, circularProfileTex, ScaleMode.ScaleAndCrop);
            }
            else
            {
                // Draw initials on a circular background
                var oldColor = GUI.color;
                GUI.color = bgColor;
                GUI.DrawTexture(rect, _circleTex);
                GUI.color = oldColor;

                GUI.Label(rect, initials, style);
            }

            // Draw border (circular)
            if (assignee.borderColorIndex > 0)
            {
                var oldColor = GUI.color;
                GUI.color = borderColor;
                GUI.DrawTexture(rect, _circleBorderTex);
                GUI.color = oldColor;
            }
        }

        private static Texture2D CreateCircleTexture(int size)
        {
            var tex = new Texture2D(size, size);
            var center = new Vector2(size * 0.5f, size * 0.5f);
            var radius = size * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    var dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);
                    // Use a small antialiasing factor
                    float alpha = Mathf.Clamp01(radius + 0.5f - dist);
                    tex.SetPixel(x, y, new Color(1, 1, 1, alpha));
                }
            }
            tex.Apply();
            tex.hideFlags = HideFlags.DontSave;
            return tex;
        }

        private static Texture2D CreateCircleBorderTexture(int size, int thickness)
        {
            var tex = new Texture2D(size, size);
            var center = new Vector2(size * 0.5f, size * 0.5f);
            var radius = size * 0.5f;
            var innerRadius = radius - thickness;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    var dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);
                    float alpha = Mathf.Clamp01(radius + 0.5f - dist) * Mathf.Clamp01(dist - (innerRadius - 0.5f));
                    tex.SetPixel(x, y, new Color(1, 1, 1, alpha));
                }
            }
            tex.Apply();
            tex.hideFlags = HideFlags.DontSave;
            return tex;
        }

        public static void DrawBorderRect(Rect rect, Color color, float thickness = 1f)
        {
            if (Event.current.type != EventType.Repaint) return;
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, thickness), color);
            EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), color);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, thickness, rect.height), color);
            EditorGUI.DrawRect(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), color);
        }

        private static Texture2D _proBackdropTex;
        private static Texture2D _personalBackdropTex;

        public static Texture2D GetCanvasBackdropTexture(bool isPro)
        {
            if (isPro)
            {
                if (_proBackdropTex == null)
                {
                    int width = 128;
                    int height = 128;
                    _proBackdropTex = new Texture2D(width, height, TextureFormat.RGBA32, false);
                    _proBackdropTex.wrapMode = TextureWrapMode.Clamp;
                    _proBackdropTex.filterMode = FilterMode.Bilinear;

                    Color topGlow = new Color(0.12f, 0.16f, 0.24f, 1.0f);
                    Color bottomDeep = new Color(0.04f, 0.05f, 0.07f, 1.0f);

                    for (int y = 0; y < height; y++)
                    {
                        float v = (float)y / (height - 1);
                        for (int x = 0; x < width; x++)
                        {
                            float u = (float)x / (width - 1);
                            float distFromTopCenter = Vector2.Distance(new Vector2(u, v), new Vector2(0.4f, 0.95f));
                            float radialGlow = Mathf.Clamp01(1f - distFromTopCenter * 0.75f);

                            Color verticalGrad = Color.Lerp(bottomDeep, topGlow, v * 0.85f);
                            Color c = Color.Lerp(verticalGrad, topGlow, radialGlow * 0.5f);
                            _proBackdropTex.SetPixel(x, y, c);
                        }
                    }
                    _proBackdropTex.Apply();
                    _proBackdropTex.hideFlags = HideFlags.DontSave;
                }
                return _proBackdropTex;
            }
            else
            {
                if (_personalBackdropTex == null)
                {
                    int width = 128;
                    int height = 128;
                    _personalBackdropTex = new Texture2D(width, height, TextureFormat.RGBA32, false);
                    _personalBackdropTex.wrapMode = TextureWrapMode.Clamp;
                    _personalBackdropTex.filterMode = FilterMode.Bilinear;

                    Color topGlow = new Color(0.98f, 0.99f, 1.00f, 1.0f);
                    Color bottomDeep = new Color(0.88f, 0.90f, 0.94f, 1.0f);

                    for (int y = 0; y < height; y++)
                    {
                        float v = (float)y / (height - 1);
                        for (int x = 0; x < width; x++)
                        {
                            float u = (float)x / (width - 1);
                            float distFromTopCenter = Vector2.Distance(new Vector2(u, v), new Vector2(0.4f, 0.95f));
                            float radialGlow = Mathf.Clamp01(1f - distFromTopCenter * 0.75f);

                            Color verticalGrad = Color.Lerp(bottomDeep, topGlow, v * 0.9f);
                            Color c = Color.Lerp(verticalGrad, topGlow, radialGlow * 0.4f);
                            _personalBackdropTex.SetPixel(x, y, c);
                        }
                    }
                    _personalBackdropTex.Apply();
                    _personalBackdropTex.hideFlags = HideFlags.DontSave;
                }
                return _personalBackdropTex;
            }
        }

        public static void DrawCanvasBackground(Rect rect, Color baseColor, bool isModal = false)
        {
            if (Event.current.type != EventType.Repaint) return;
            var backdrop = GetCanvasBackdropTexture(EditorGUIUtility.isProSkin);
            if (backdrop != null)
            {
                GUI.DrawTexture(rect, backdrop, ScaleMode.StretchToFill);
            }
            if (baseColor.a > 0.001f)
            {
                EditorGUI.DrawRect(rect, baseColor);
            }
        }

        public static void DrawGlassPanel(Rect rect, Color bg, Color? borderColor = null, bool drawTopHighlight = true)
        {
            if (Event.current.type != EventType.Repaint) return;
            if (bg.a > 0.001f)
            {
                EditorGUI.DrawRect(rect, bg);
            }
            Color border = borderColor ?? (EditorGUIUtility.isProSkin
                ? new Color(1f, 1f, 1f, 0.12f)
                : new Color(1f, 1f, 1f, 0.45f));
            if (border.a > 0.001f)
            {
                DrawBorderRect(rect, border, 1f);
            }
            if (drawTopHighlight && rect.width > 2 && rect.height > 2)
            {
                Color highlight = EditorGUIUtility.isProSkin
                    ? new Color(1f, 1f, 1f, 0.22f)
                    : new Color(1f, 1f, 1f, 0.65f);
                EditorGUI.DrawRect(new Rect(rect.x + 1, rect.y + 1, rect.width - 2, 1), highlight);
            }
        }

        public static void SetAllStateTextures(GUIStyle style, Texture2D normalTex, Texture2D hoverTex = null, Texture2D activeTex = null)
        {
            if (style == null) return;
            hoverTex ??= normalTex;
            activeTex ??= hoverTex;

            style.normal.background = normalTex;
            style.hover.background = hoverTex;
            style.active.background = activeTex;
            style.focused.background = normalTex;
            style.onNormal.background = normalTex;
            style.onHover.background = hoverTex;
            style.onActive.background = activeTex;
            style.onFocused.background = normalTex;
        }

        public static void SetAllStateTextColors(GUIStyle style, Color normalColor, Color? hoverColor = null, Color? activeColor = null)
        {
            if (style == null) return;
            Color hColor = hoverColor ?? normalColor;
            Color aColor = activeColor ?? hColor;

            style.normal.textColor = normalColor;
            style.hover.textColor = hColor;
            style.active.textColor = aColor;
            style.focused.textColor = normalColor;
            style.onNormal.textColor = normalColor;
            style.onHover.textColor = hColor;
            style.onActive.textColor = aColor;
            style.onFocused.textColor = normalColor;
        }

        // ── Reusable styles (lazy init) ──

        private static GUIStyle _boardHeader, _columnHeader, _cardBox, _cardBoxHighlighted, _cardTitle,
                                _addButton, _addCardButton, _tabActive, _tabInactive, _noteBox, _noteBoxSelected, _noteTextArea,
                                _noteTitle, _sectionLabel, _iconButton, _linkStyle, _assigneeCircle,
                                _toolbarButton, _toolbarButtonActive, _toolbarPopup, _toolbarDeleteButton, _standardButton, _standardDropdown,
                                _deleteButton, _deleteIconButton, _dropdownMenuItem, _themedTextField, _themedSearchField;

        public static GUIStyle AssigneeCircle => _assigneeCircle ??= new GUIStyle(EditorStyles.label)
        {
            fontSize = 11, fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            fixedWidth = 24, fixedHeight = 24,
            padding = new RectOffset(0,0,0,0),
            normal = { textColor = Color.white }
        };

        public static GUIStyle LinkStyle => _linkStyle ??= new GUIStyle(EditorStyles.label)
        {
            normal = { textColor = LinkColor },
            hover  = { textColor = new Color(Mathf.Min(1f, LinkColor.r * 1.2f), Mathf.Min(1f, LinkColor.g * 1.2f), Mathf.Min(1f, LinkColor.b * 1.2f), LinkColor.a) },
            fontStyle = FontStyle.Bold,
            wordWrap = true
        };

        public static GUIStyle BoardHeader => _boardHeader ??= new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 18, alignment = TextAnchor.MiddleLeft,
            padding = new RectOffset(8, 8, 6, 6),
            normal = { textColor = EditorGUIUtility.isProSkin ? Pro_BoardHeader : Personal_BoardHeader }
        };

        public static GUIStyle ColumnHeader => _columnHeader ??= new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 13, alignment = TextAnchor.MiddleLeft,
            padding = new RectOffset(6, 6, 4, 4),
            normal = { textColor = ColumnHeaderColor }
        };

        private static GUIStyle _columnBox;
        public static GUIStyle ColumnBox => _columnBox ??= new GUIStyle
        {
            padding = new RectOffset(6, 6, 6, 6),
            margin = new RectOffset(4, 4, 4, 4)
        };

        public static GUIStyle CardBox
        {
            get
            {
                if (_cardBox == null)
                {
                    _cardBox = new GUIStyle(GUIStyle.none)
                    {
                        padding = new RectOffset(8, 8, 6, 6),
                        margin = new RectOffset(4, 4, 2, 2),
                        fontSize = 11,
                        wordWrap = true,
                        border = new RectOffset(0, 0, 0, 0)
                    };
                    SetAllStateTextures(_cardBox, GetColorTex(CardBg));
                }
                return _cardBox;
            }
        }

        public static GUIStyle CardBoxHighlighted
        {
            get
            {
                if (_cardBoxHighlighted == null)
                {
                    _cardBoxHighlighted = new GUIStyle(CardBox);
                    var hiColor = EditorGUIUtility.isProSkin ? Pro_CardHighlighted : Personal_CardHighlighted;
                    SetAllStateTextures(_cardBoxHighlighted, GetColorTex(hiColor));
                }
                return _cardBoxHighlighted;
            }
        }

        public static GUIStyle CardTitle => _cardTitle ??= new GUIStyle(EditorStyles.label)
        {
            fontSize = 12, fontStyle = FontStyle.Bold, wordWrap = true,
            normal = { textColor = CardTitleColor }
        };

        public static GUIStyle AddCardButton
        {
            get
            {
                if (_addCardButton == null)
                {
                    _addCardButton = new GUIStyle(GUIStyle.none)
                    {
                        fontSize = 13,
                        fontStyle = FontStyle.Bold,
                        fixedHeight = 26,
                        alignment = TextAnchor.MiddleCenter,
                        border = new RectOffset(0, 0, 0, 0),
                        margin = new RectOffset(2, 2, 2, 2),
                        padding = new RectOffset(6, 6, 2, 2)
                    };
                    var activeBg = new Color(AddCardBg.r * 0.85f, AddCardBg.g * 0.85f, AddCardBg.b * 0.85f, AddCardBg.a);
                    SetAllStateTextures(_addCardButton, GetColorTex(AddCardBg), GetColorTex(AddCardHoverBg), GetColorTex(activeBg));
                    SetAllStateTextColors(_addCardButton, AddCardText, AddCardText, AddCardText);
                }
                return _addCardButton;
            }
        }

        public static GUIStyle AddButton
        {
            get
            {
                if (_addButton == null)
                {
                    _addButton = new GUIStyle(GUIStyle.none)
                    {
                        fontSize = 18,
                        fixedHeight = 28,
                        alignment = TextAnchor.MiddleCenter,
                        border = new RectOffset(0, 0, 0, 0),
                        margin = new RectOffset(2, 2, 2, 2),
                        padding = new RectOffset(6, 6, 2, 2)
                    };
                    var activeBg = new Color(AddCardBg.r * 0.85f, AddCardBg.g * 0.85f, AddCardBg.b * 0.85f, AddCardBg.a);
                    SetAllStateTextures(_addButton, GetColorTex(AddCardBg), GetColorTex(AddCardHoverBg), GetColorTex(activeBg));
                    SetAllStateTextColors(_addButton, AddCardText, AddCardText, AddCardText);
                }
                return _addButton;
            }
        }

        public static GUIStyle TabActive
        {
            get
            {
                if (_tabActive == null)
                {
                    _tabActive = new GUIStyle(GUIStyle.none)
                    {
                        fontSize = 13,
                        fontStyle = FontStyle.Bold,
                        alignment = TextAnchor.MiddleCenter,
                        border = new RectOffset(0, 0, 0, 0),
                        margin = new RectOffset(2, 2, 2, 2),
                        padding = new RectOffset(8, 8, 4, 4)
                    };
                    SetAllStateTextures(_tabActive, GetColorTex(HeaderTabActiveBg), GetColorTex(HeaderTabHoverBg), GetColorTex(HeaderTabActiveBg));
                    SetAllStateTextColors(_tabActive, HeaderTabActiveText, HeaderTabActiveText, HeaderTabActiveText);
                }
                return _tabActive;
            }
        }

        public static GUIStyle TabInactive
        {
            get
            {
                if (_tabInactive == null)
                {
                    _tabInactive = new GUIStyle(GUIStyle.none)
                    {
                        fontSize = 13,
                        alignment = TextAnchor.MiddleCenter,
                        border = new RectOffset(0, 0, 0, 0),
                        margin = new RectOffset(2, 2, 2, 2),
                        padding = new RectOffset(8, 8, 4, 4)
                    };
                    SetAllStateTextures(_tabInactive, GetColorTex(HeaderTabInactiveBg), GetColorTex(HeaderTabHoverBg), GetColorTex(HeaderTabActiveBg));
                    SetAllStateTextColors(_tabInactive, HeaderTabInactiveText, HeaderTabInactiveText, HeaderTabActiveText);
                }
                return _tabInactive;
            }
        }

        public static GUIStyle ToolbarButton
        {
            get
            {
                if (_toolbarButton == null)
                {
                    _toolbarButton = new GUIStyle(GUIStyle.none)
                    {
                        alignment = TextAnchor.MiddleCenter,
                        fontSize = 11,
                        border = new RectOffset(0, 0, 0, 0),
                        margin = new RectOffset(1, 1, 1, 1),
                        padding = new RectOffset(6, 6, 2, 2)
                    };
                    var activeBg = new Color(ButtonBg.r * 0.85f, ButtonBg.g * 0.85f, ButtonBg.b * 0.85f, ButtonBg.a);
                    SetAllStateTextures(_toolbarButton, GetColorTex(ButtonBg), GetColorTex(ButtonHoverBg), GetColorTex(activeBg));
                    SetAllStateTextColors(_toolbarButton, ButtonText, ButtonHoverText, ButtonHoverText);
                }
                return _toolbarButton;
            }
        }

        public static GUIStyle ToolbarButtonActive
        {
            get
            {
                if (_toolbarButtonActive == null)
                {
                    _toolbarButtonActive = new GUIStyle(GUIStyle.none)
                    {
                        alignment = TextAnchor.MiddleCenter,
                        fontSize = 11,
                        fontStyle = FontStyle.Bold,
                        border = new RectOffset(0, 0, 0, 0),
                        margin = new RectOffset(1, 1, 1, 1),
                        padding = new RectOffset(6, 6, 2, 2)
                    };
                    SetAllStateTextures(_toolbarButtonActive, GetColorTex(HeaderTabActiveBg), GetColorTex(HeaderTabHoverBg), GetColorTex(HeaderTabActiveBg));
                    SetAllStateTextColors(_toolbarButtonActive, HeaderTabActiveText, HeaderTabActiveText, HeaderTabActiveText);
                }
                return _toolbarButtonActive;
            }
        }

        public static GUIStyle ToolbarPopup
        {
            get
            {
                if (_toolbarPopup == null)
                {
                    _toolbarPopup = new GUIStyle(GUIStyle.none)
                    {
                        alignment = TextAnchor.MiddleLeft,
                        fontSize = 11,
                        border = new RectOffset(0, 0, 0, 0),
                        margin = new RectOffset(1, 1, 1, 1),
                        padding = new RectOffset(8, 18, 2, 2)
                    };
                    var activeBg = new Color(DropdownBg.r * 0.85f, DropdownBg.g * 0.85f, DropdownBg.b * 0.85f, DropdownBg.a);
                    SetAllStateTextures(_toolbarPopup, GetColorTex(DropdownBg), GetColorTex(DropdownHoverBg), GetColorTex(activeBg));
                    SetAllStateTextColors(_toolbarPopup, DropdownText, DropdownHoverText, DropdownHoverText);
                }
                return _toolbarPopup;
            }
        }

        public static GUIStyle StandardButton
        {
            get
            {
                if (_standardButton == null)
                {
                    _standardButton = new GUIStyle(GUIStyle.none)
                    {
                        alignment = TextAnchor.MiddleCenter,
                        fontSize = 12,
                        border = new RectOffset(0, 0, 0, 0),
                        margin = new RectOffset(1, 1, 1, 1),
                        padding = new RectOffset(6, 6, 2, 2)
                    };
                    var activeBg = new Color(ButtonBg.r * 0.85f, ButtonBg.g * 0.85f, ButtonBg.b * 0.85f, ButtonBg.a);
                    SetAllStateTextures(_standardButton, GetColorTex(ButtonBg), GetColorTex(ButtonHoverBg), GetColorTex(activeBg));
                    SetAllStateTextColors(_standardButton, ButtonText, ButtonHoverText, ButtonHoverText);
                }
                return _standardButton;
            }
        }

        public static GUIStyle StandardDropdown
        {
            get
            {
                if (_standardDropdown == null)
                {
                    _standardDropdown = new GUIStyle(GUIStyle.none)
                    {
                        alignment = TextAnchor.MiddleLeft,
                        fontSize = 12,
                        border = new RectOffset(0, 0, 0, 0),
                        margin = new RectOffset(2, 2, 2, 2),
                        padding = new RectOffset(8, 18, 3, 3)
                    };
                    var activeBg = new Color(DropdownBg.r * 0.85f, DropdownBg.g * 0.85f, DropdownBg.b * 0.85f, DropdownBg.a);
                    SetAllStateTextures(_standardDropdown, GetColorTex(DropdownBg), GetColorTex(DropdownHoverBg), GetColorTex(activeBg));
                    SetAllStateTextColors(_standardDropdown, DropdownText, DropdownHoverText, DropdownHoverText);
                }
                return _standardDropdown;
            }
        }

        public static GUIStyle NoteBox
        {
            get
            {
                if (_noteBox == null)
                {
                    _noteBox = new GUIStyle(GUIStyle.none)
                    {
                        padding = new RectOffset(12, 8, 6, 6),
                        margin  = new RectOffset(4, 4, 2, 2),
                        fontSize = 11,
                        wordWrap = true,
                        border = new RectOffset(0, 0, 0, 0)
                    };
                    SetAllStateTextures(_noteBox, GetColorTex(NoteCardBg), GetColorTex(NoteCardHoverBg));
                }
                return _noteBox;
            }
        }

        public static GUIStyle NoteBoxSelected
        {
            get
            {
                if (_noteBoxSelected == null)
                {
                    _noteBoxSelected = new GUIStyle(GUIStyle.none)
                    {
                        padding = new RectOffset(12, 8, 6, 6),
                        margin  = new RectOffset(4, 4, 2, 2),
                        fontSize = 11,
                        wordWrap = true,
                        border = new RectOffset(0, 0, 0, 0)
                    };
                    SetAllStateTextures(_noteBoxSelected, GetColorTex(NoteCardSelectedBg), GetColorTex(NoteCardSelectedBg));
                }
                return _noteBoxSelected;
            }
        }

        public static GUIStyle NoteTextArea
        {
            get
            {
                if (_noteTextArea == null)
                {
                    _noteTextArea = new GUIStyle(EditorStyles.textArea)
                    {
                        wordWrap = true,
                        fontSize = 13,
                        padding = new RectOffset(10, 10, 10, 10),
                        border = new RectOffset(0, 0, 0, 0)
                    };
                    SetAllStateTextures(_noteTextArea, GetColorTex(NoteInputBg));
                    SetAllStateTextColors(_noteTextArea, NoteInputText);
                    var consolas = Font.CreateDynamicFontFromOSFont("Consolas", 13);
                    if (consolas != null) _noteTextArea.font = consolas;
                }
                return _noteTextArea;
            }
        }

        // Strong left-accent color for selected note
        public static Color NoteSelectedAccent => NoteSelectedAccentColor;

        // Drag-over folder highlight (hovered target)
        public static Color FolderDropHighlight => new Color(NoteSelectedAccentColor.r, NoteSelectedAccentColor.g, NoteSelectedAccentColor.b, 0.35f);

        // Source folder highlight during drag (green tint)
        public static Color FolderDragSourceHighlight => new Color(0.3f, 0.85f, 0.4f, 0.25f);

        // Other (non-hovered, non-source) folder hint during drag (dim grey)
        public static Color FolderDragOtherHighlight => new Color(0.5f, 0.5f, 0.5f, 0.12f);

        // Card drag: column being hovered
        public static Color ColumnDropHovered => new Color(NoteSelectedAccentColor.r, NoteSelectedAccentColor.g, NoteSelectedAccentColor.b, 0.18f);

        // Card drag: column NOT hovered (dim hint)
        public static Color ColumnDropOther => new Color(0.5f, 0.5f, 0.5f, 0.08f);

        // Card drag: source column highlight
        public static Color ColumnDragSource => new Color(0.3f, 0.85f, 0.4f, 0.15f);

        public static string TruncateString(string text, int maxLength)
        {
            if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
                return text;
            return text.Substring(0, maxLength - 3) + "...";
        }

        public static void InvalidateCache()
        {
            _boardHeader = null;
            _columnHeader = null;
            _columnBox = null;
            _cardBox = null;
            _cardBoxHighlighted = null;
            _cardTitle = null;
            _addButton = null;
            _addCardButton = null;
            _tabActive = null;
            _tabInactive = null;
            _noteBox = null;
            _noteBoxSelected = null;
            _noteTextArea = null;
            _noteTitle = null;
            _sectionLabel = null;
            _iconButton = null;
            _linkStyle = null;
            _assigneeCircle = null;
            _toolbarButton = null;
            _toolbarButtonActive = null;
            _toolbarPopup = null;
            _toolbarDeleteButton = null;
            _standardButton = null;
            _standardDropdown = null;
            _deleteButton = null;
            _deleteIconButton = null;
            _noteActionButton = null;
            _addNoteButton = null;
            _importNoteButton = null;
            _cardTextArea = null;
            _dropdownMenuItem = null;
            _glassItemBox = null;
            _glassTextField = null;
            _dateInputField = null;
            _tooltipStyle = null;
            _statusBar = null;
            _themedTextField = null;
            _themedSearchField = null;
            _checkmarkStyle = null;

            _texCache.Clear();
            _profileTexCache.Clear();
            _circularProfileTexCache.Clear();
            _circleTex = null;
            _circleBorderTex = null;
            _proBackdropTex = null;
            _personalBackdropTex = null;
        }

        private static GUIStyle _glassItemBox;
        public static GUIStyle GlassItemBox => _glassItemBox ??= new GUIStyle(GUIStyle.none)
        {
            padding = new RectOffset(6, 6, 2, 2),
            margin = new RectOffset(0, 0, 2, 2)
        };

        private static GUIStyle _glassTextField;
        public static GUIStyle GlassTextField
        {
            get
            {
                if (_glassTextField == null)
                {
                    _glassTextField = new GUIStyle(GUIStyle.none)
                    {
                        fontSize = 12,
                        alignment = TextAnchor.MiddleLeft,
                        padding = new RectOffset(4, 4, 2, 2),
                        margin = new RectOffset(1, 1, 1, 1)
                    };
                    SetAllStateTextColors(_glassTextField, EditorGUIUtility.isProSkin ? Color.white : new Color(0.1f, 0.1f, 0.12f));
                }
                return _glassTextField;
            }
        }

        private static GUIStyle _dateInputField;
        public static GUIStyle DateInputField
        {
            get
            {
                if (_dateInputField == null)
                {
                    _dateInputField = new GUIStyle(EditorStyles.numberField)
                    {
                        alignment = TextAnchor.MiddleCenter,
                        fontSize = 11,
                        border = new RectOffset(0, 0, 0, 0),
                        margin = new RectOffset(1, 1, 1, 1),
                        padding = new RectOffset(3, 3, 2, 2)
                    };
                    SetAllStateTextures(_dateInputField, GetColorTex(NoteInputBg));
                    SetAllStateTextColors(_dateInputField, NoteInputText);
                }
                return _dateInputField;
            }
        }

        private static GUIStyle _tooltipStyle;
        public static GUIStyle TooltipStyle
        {
            get
            {
                if (_tooltipStyle == null)
                {
                    _tooltipStyle = new GUIStyle(EditorStyles.miniLabel)
                    {
                        fontSize = 11,
                        alignment = TextAnchor.MiddleLeft,
                        wordWrap = true,
                        richText = true,
                        padding = new RectOffset(0, 0, 0, 0),
                        margin = new RectOffset(0, 0, 0, 0)
                    };
                    SetAllStateTextColors(_tooltipStyle, TooltipTextColor);
                }
                return _tooltipStyle;
            }
        }

        public static GUIStyle NoteTitle
        {
            get
            {
                if (_noteTitle == null)
                {
                    _noteTitle = new GUIStyle(EditorStyles.textField)
                    {
                        fontSize = 13,
                        fontStyle = FontStyle.Bold,
                        alignment = TextAnchor.MiddleLeft,
                        border = new RectOffset(0, 0, 0, 0),
                        margin = new RectOffset(1, 1, 1, 1),
                        padding = new RectOffset(6, 6, 2, 2)
                    };
                    SetAllStateTextures(_noteTitle, GetColorTex(NoteInputBg));
                    SetAllStateTextColors(_noteTitle, NoteTitleColor);
                }
                return _noteTitle;
            }
        }

        public static GUIStyle ThemedTextField
        {
            get
            {
                if (_themedTextField == null)
                {
                    _themedTextField = new GUIStyle(EditorStyles.textField)
                    {
                        fontSize = 12,
                        alignment = TextAnchor.MiddleLeft,
                        border = new RectOffset(0, 0, 0, 0),
                        margin = new RectOffset(1, 1, 1, 1),
                        padding = new RectOffset(6, 6, 2, 2)
                    };
                    SetAllStateTextures(_themedTextField, GetColorTex(NoteInputBg));
                    SetAllStateTextColors(_themedTextField, NoteInputText);
                }
                return _themedTextField;
            }
        }

        public static GUIStyle ThemedSearchField
        {
            get
            {
                if (_themedSearchField == null)
                {
                    _themedSearchField = new GUIStyle(EditorStyles.textField)
                    {
                        fontSize = 11,
                        alignment = TextAnchor.MiddleLeft,
                        border = new RectOffset(0, 0, 0, 0),
                        margin = new RectOffset(1, 1, 1, 1),
                        padding = new RectOffset(6, 6, 2, 2)
                    };
                    SetAllStateTextures(_themedSearchField, GetColorTex(NoteInputBg));
                    SetAllStateTextColors(_themedSearchField, NoteInputText);
                }
                return _themedSearchField;
            }
        }

        public static GUIStyle SectionLabel => _sectionLabel ??= new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 14, padding = new RectOffset(4, 4, 8, 4),
            normal = { textColor = SectionLabelColor }
        };

        public static GUIStyle IconButton
        {
            get
            {
                if (_iconButton == null)
                {
                    _iconButton = new GUIStyle(GUIStyle.none)
                    {
                        fontSize = 12,
                        alignment = TextAnchor.MiddleCenter,
                        border = new RectOffset(0, 0, 0, 0),
                        margin = new RectOffset(1, 1, 1, 1),
                        padding = new RectOffset(2, 2, 2, 2)
                    };
                    var activeBg = new Color(ButtonBg.r * 0.85f, ButtonBg.g * 0.85f, ButtonBg.b * 0.85f, ButtonBg.a);
                    SetAllStateTextures(_iconButton, GetColorTex(ButtonBg), GetColorTex(ButtonHoverBg), GetColorTex(activeBg));
                    SetAllStateTextColors(_iconButton, ButtonText, ButtonHoverText, ButtonHoverText);
                }
                return _iconButton;
            }
        }

        public static GUIStyle DeleteButton
        {
            get
            {
                if (_deleteButton == null)
                {
                    _deleteButton = new GUIStyle(GUIStyle.none)
                    {
                        alignment = TextAnchor.MiddleCenter,
                        fontSize = 11,
                        fontStyle = FontStyle.Bold,
                        border = new RectOffset(0, 0, 0, 0),
                        margin = new RectOffset(1, 1, 1, 1),
                        padding = new RectOffset(6, 6, 2, 2)
                    };
                    var activeBg = new Color(DeleteBtnBg.r * 0.85f, DeleteBtnBg.g * 0.85f, DeleteBtnBg.b * 0.85f, DeleteBtnBg.a);
                    SetAllStateTextures(_deleteButton, GetColorTex(DeleteBtnBg), GetColorTex(DeleteBtnHoverBg), GetColorTex(activeBg));
                    SetAllStateTextColors(_deleteButton, DeleteBtnText, DeleteBtnText, DeleteBtnText);
                }
                return _deleteButton;
            }
        }

        public static GUIStyle DeleteIconButton
        {
            get
            {
                if (_deleteIconButton == null)
                {
                    _deleteIconButton = new GUIStyle(IconButton);
                    var activeBg = new Color(DeleteBtnBg.r * 0.85f, DeleteBtnBg.g * 0.85f, DeleteBtnBg.b * 0.85f, DeleteBtnBg.a);
                    SetAllStateTextures(_deleteIconButton, GetColorTex(DeleteBtnBg), GetColorTex(DeleteBtnHoverBg), GetColorTex(activeBg));
                    SetAllStateTextColors(_deleteIconButton, DeleteBtnText, DeleteBtnText, DeleteBtnText);
                }
                return _deleteIconButton;
            }
        }

        public static GUIStyle ToolbarDeleteButton
        {
            get
            {
                if (_toolbarDeleteButton == null)
                {
                    _toolbarDeleteButton = new GUIStyle(GUIStyle.none)
                    {
                        alignment = TextAnchor.MiddleCenter,
                        fontSize = 12,
                        fontStyle = FontStyle.Bold,
                        border = new RectOffset(0, 0, 0, 0),
                        margin = new RectOffset(1, 1, 1, 1),
                        padding = new RectOffset(6, 6, 2, 2)
                    };
                    var activeBg = new Color(DeleteBtnBg.r * 0.85f, DeleteBtnBg.g * 0.85f, DeleteBtnBg.b * 0.85f, DeleteBtnBg.a);
                    SetAllStateTextures(_toolbarDeleteButton, GetColorTex(DeleteBtnBg), GetColorTex(DeleteBtnHoverBg), GetColorTex(activeBg));
                    SetAllStateTextColors(_toolbarDeleteButton, DeleteBtnText, DeleteBtnText, DeleteBtnText);
                }
                return _toolbarDeleteButton;
            }
        }

        private static GUIStyle _noteActionButton;
        public static GUIStyle NoteActionButton
        {
            get
            {
                if (_noteActionButton == null)
                {
                    _noteActionButton = new GUIStyle(GUIStyle.none)
                    {
                        alignment = TextAnchor.MiddleCenter,
                        fontSize = 11,
                        fontStyle = FontStyle.Bold,
                        border = new RectOffset(0, 0, 0, 0),
                        margin = new RectOffset(2, 2, 2, 2),
                        padding = new RectOffset(6, 6, 3, 3)
                    };
                    var activeBg = new Color(NoteActionBg.r * 0.85f, NoteActionBg.g * 0.85f, NoteActionBg.b * 0.85f, NoteActionBg.a);
                    SetAllStateTextures(_noteActionButton, GetColorTex(NoteActionBg), GetColorTex(NoteActionHoverBg), GetColorTex(activeBg));
                    SetAllStateTextColors(_noteActionButton, NoteActionText, NoteActionHoverText, NoteActionHoverText);
                }
                return _noteActionButton;
            }
        }

        private static GUIStyle _addNoteButton;
        public static GUIStyle AddNoteButton
        {
            get
            {
                if (_addNoteButton == null)
                {
                    _addNoteButton = new GUIStyle(GUIStyle.none)
                    {
                        alignment = TextAnchor.MiddleCenter,
                        fontSize = 11,
                        fontStyle = FontStyle.Bold,
                        border = new RectOffset(0, 0, 0, 0),
                        margin = new RectOffset(2, 2, 2, 2),
                        padding = new RectOffset(6, 6, 3, 3)
                    };
                    var activeBg = new Color(AddNoteBg.r * 0.85f, AddNoteBg.g * 0.85f, AddNoteBg.b * 0.85f, AddNoteBg.a);
                    SetAllStateTextures(_addNoteButton, GetColorTex(AddNoteBg), GetColorTex(AddNoteHoverBg), GetColorTex(activeBg));
                    SetAllStateTextColors(_addNoteButton, AddNoteText, AddNoteHoverText, AddNoteHoverText);
                }
                return _addNoteButton;
            }
        }

        private static GUIStyle _importNoteButton;
        public static GUIStyle ImportNoteButton
        {
            get
            {
                if (_importNoteButton == null)
                {
                    _importNoteButton = new GUIStyle(GUIStyle.none)
                    {
                        alignment = TextAnchor.MiddleCenter,
                        fontSize = 12,
                        fontStyle = FontStyle.Bold,
                        border = new RectOffset(0, 0, 0, 0),
                        margin = new RectOffset(2, 2, 2, 2),
                        padding = new RectOffset(4, 4, 2, 2)
                    };
                    var activeBg = new Color(ImportNoteBg.r * 0.85f, ImportNoteBg.g * 0.85f, ImportNoteBg.b * 0.85f, ImportNoteBg.a);
                    SetAllStateTextures(_importNoteButton, GetColorTex(ImportNoteBg), GetColorTex(ImportNoteHoverBg), GetColorTex(activeBg));
                    SetAllStateTextColors(_importNoteButton, ImportNoteText, ImportNoteHoverText, ImportNoteHoverText);
                }
                return _importNoteButton;
            }
        }

        private static GUIStyle _cardTextArea;
        public static GUIStyle CardTextArea
        {
            get
            {
                if (_cardTextArea == null)
                {
                    _cardTextArea = new GUIStyle(EditorStyles.textArea)
                    {
                        wordWrap = true,
                        fontSize = 12,
                        padding = new RectOffset(8, 8, 8, 8),
                        border = new RectOffset(0, 0, 0, 0)
                    };
                    SetAllStateTextures(_cardTextArea, GetColorTex(NoteInputBg));
                    SetAllStateTextColors(_cardTextArea, NoteInputText);
                }
                return _cardTextArea;
            }
        }

        public static GUIStyle DropdownMenuItem => _dropdownMenuItem ??= new GUIStyle(EditorStyles.label)
        {
            alignment = TextAnchor.MiddleLeft,
            fontSize = 11,
            padding = new RectOffset(6, 6, 2, 2),
            normal = { textColor = DropdownMenuText },
            hover = { textColor = DropdownMenuHoverText, background = GetColorTex(DropdownMenuHoverBg) }
        };

        private static GUIStyle _statusBar;
        public static GUIStyle StatusBar => _statusBar ??= new GUIStyle(EditorStyles.miniLabel)
        {
            fontSize = 11,
            alignment = TextAnchor.MiddleLeft,
            richText = true,
            padding = new RectOffset(6, 6, 0, 0),
            normal = { textColor = StatusBarTextColor }
        };

        public static Color[] GetLabelColorsArray()
        {
            return LabelColors != null ? (Color[])LabelColors.Clone() : (Color[])DefaultLabelColors.Clone();
        }

        public static void DrawThemedDropdown(int selectedIndex, string[] options, Action<int> onSelect, GUIStyle style, params GUILayoutOption[] layoutOptions)
        {
            DrawThemedDropdown(selectedIndex, options, onSelect, style, null, null, null, layoutOptions);
        }

        public static void DrawThemedDropdown(int selectedIndex, string[] options, Action<int> onSelect, GUIStyle style, string tooltip, params GUILayoutOption[] layoutOptions)
        {
            DrawThemedDropdown(selectedIndex, options, onSelect, style, null, null, tooltip, layoutOptions);
        }

        public static void DrawThemedDropdown(int selectedIndex, string[] options, Action<int> onSelect, GUIStyle style, Color[] colors, params GUILayoutOption[] layoutOptions)
        {
            DrawThemedDropdown(selectedIndex, options, onSelect, style, colors, null, null, layoutOptions);
        }

        public static void DrawThemedDropdown(int selectedIndex, string[] options, Action<int> onSelect, GUIStyle style, Color[] colors, string tooltip, params GUILayoutOption[] layoutOptions)
        {
            DrawThemedDropdown(selectedIndex, options, onSelect, style, colors, null, tooltip, layoutOptions);
        }

        public static void DrawThemedDropdown(int selectedIndex, string[] options, Action<int> onSelect, GUIStyle style, Color[] colors, string[] icons, params GUILayoutOption[] layoutOptions)
        {
            DrawThemedDropdown(selectedIndex, options, onSelect, style, colors, icons, null, layoutOptions);
        }

        public static void DrawThemedDropdown(int selectedIndex, string[] options, Action<int> onSelect, GUIStyle style, Color[] colors, string[] icons, string tooltip, params GUILayoutOption[] layoutOptions)
        {
            string selectedText = (options != null && selectedIndex >= 0 && selectedIndex < options.Length) ? options[selectedIndex] : "Select...";
            GUIContent content = new GUIContent(selectedText);
            GUIStyle effectiveStyle = style ?? StandardDropdown;
            Rect rect = GUILayoutUtility.GetRect(content, effectiveStyle, layoutOptions);
            
            if (!string.IsNullOrEmpty(tooltip))
            {
                ThemedTooltip.SetTooltip(rect, tooltip);
            }

            if (EditorGUI.DropdownButton(rect, content, FocusType.Keyboard, effectiveStyle))
            {
                ThemedDropdownPopup.Show(rect, options, selectedIndex, onSelect, colors, icons);
            }

            if (Event.current.type == EventType.Repaint)
            {
                Color borderColor = EditorGUIUtility.isProSkin ? new Color(1f, 1f, 1f, 0.12f) : new Color(0f, 0f, 0f, 0.15f);
                DrawBorderRect(rect, borderColor, 1f);

                var arrowStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 9,
                    normal = { textColor = new Color(DropdownText.r, DropdownText.g, DropdownText.b, DropdownText.a * 0.8f) }
                };
                Rect arrowRect = new Rect(rect.xMax - 15, rect.y, 12, rect.height);
                GUI.Label(arrowRect, "▾", arrowStyle);
            }
        }

        public static string DrawThemedTextField(string text, params GUILayoutOption[] layoutOptions)
        {
            return DrawThemedTextField(text, ThemedTextField, layoutOptions);
        }

        public static string DrawThemedTextField(string text, GUIStyle style, params GUILayoutOption[] layoutOptions)
        {
            text ??= "";
            GUIStyle effectiveStyle = style ?? ThemedTextField;
            string newText = EditorGUILayout.TextField(text, effectiveStyle, layoutOptions);
            if (Event.current.type == EventType.Repaint)
            {
                Rect lastRect = GUILayoutUtility.GetLastRect();
                Color borderColor = EditorGUIUtility.isProSkin ? new Color(1f, 1f, 1f, 0.12f) : new Color(0f, 0f, 0f, 0.15f);
                DrawBorderRect(lastRect, borderColor, 1f);
            }
            return newText;
        }

        public static string DrawThemedSearchField(string text, params GUILayoutOption[] layoutOptions)
        {
            return DrawThemedTextField(text, ThemedSearchField, layoutOptions);
        }

        private static GUIStyle _checkmarkStyle;
        public static GUIStyle CheckmarkStyle
        {
            get
            {
                if (_checkmarkStyle == null)
                {
                    _checkmarkStyle = new GUIStyle(EditorStyles.miniLabel)
                    {
                        alignment = TextAnchor.MiddleCenter,
                        fontSize = 10,
                        fontStyle = FontStyle.Bold,
                        padding = new RectOffset(0, 0, 0, 0),
                        margin = new RectOffset(0, 0, 0, 0)
                    };
                }
                return _checkmarkStyle;
            }
        }

        public static void DrawCheckmarkIcon(Rect boxRect, Color tickColor, ChecklistTickStyle style = ChecklistTickStyle.Vector, string customChar = null)
        {
            switch (style)
            {
                case ChecklistTickStyle.Vector:
                    Color prevHandlesColor = Handles.color;
                    Handles.color = tickColor;
                    float w = boxRect.width;
                    float h = boxRect.height;
                    Vector3 p1 = new Vector3(boxRect.x + w * 0.24f, boxRect.y + h * 0.52f, 0f);
                    Vector3 p2 = new Vector3(boxRect.x + w * 0.44f, boxRect.y + h * 0.72f, 0f);
                    Vector3 p3 = new Vector3(boxRect.x + w * 0.76f, boxRect.y + h * 0.28f, 0f);
                    float lineWidth = Mathf.Max(1.8f, w * 0.14f);
                    Handles.DrawAAPolyLine(lineWidth, p1, p2, p3);
                    Handles.color = prevHandlesColor;
                    break;

                case ChecklistTickStyle.Classic:
                    var classicStyle = CheckmarkStyle;
                    classicStyle.normal.textColor = tickColor;
                    classicStyle.fontSize = Mathf.RoundToInt(boxRect.height * 0.80f);
                    GUI.Label(new Rect(boxRect.x, boxRect.y - 1f, boxRect.width, boxRect.height), "✓", classicStyle);
                    break;

                case ChecklistTickStyle.Heavy:
                    var heavyStyle = CheckmarkStyle;
                    heavyStyle.normal.textColor = tickColor;
                    heavyStyle.fontSize = Mathf.RoundToInt(boxRect.height * 0.75f);
                    GUI.Label(new Rect(boxRect.x, boxRect.y - 1f, boxRect.width, boxRect.height), "✔", heavyStyle);
                    break;

                case ChecklistTickStyle.Square:
                    float squareMargin = Mathf.Max(2.5f, boxRect.width * 0.22f);
                    Rect squareRect = new Rect(
                        boxRect.x + squareMargin,
                        boxRect.y + squareMargin,
                        boxRect.width - squareMargin * 2f,
                        boxRect.height - squareMargin * 2f
                    );
                    EditorGUI.DrawRect(squareRect, tickColor);
                    break;

                case ChecklistTickStyle.Dot:
                    var dotStyle = CheckmarkStyle;
                    dotStyle.normal.textColor = tickColor;
                    dotStyle.fontSize = Mathf.RoundToInt(boxRect.height * 0.70f);
                    GUI.Label(new Rect(boxRect.x, boxRect.y - 1f, boxRect.width, boxRect.height), "●", dotStyle);
                    break;

                case ChecklistTickStyle.Cross:
                    var crossStyle = CheckmarkStyle;
                    crossStyle.normal.textColor = tickColor;
                    crossStyle.fontSize = Mathf.RoundToInt(boxRect.height * 0.72f);
                    GUI.Label(new Rect(boxRect.x, boxRect.y - 1f, boxRect.width, boxRect.height), "✕", crossStyle);
                    break;

                case ChecklistTickStyle.UnityNative:
                    GUIContent nativeIcon = EditorGUIUtility.IconContent("Checkmark");
                    if (nativeIcon != null && nativeIcon.image != null)
                    {
                        float iconMargin = Mathf.Max(1f, boxRect.width * 0.10f);
                        Rect iconRect = new Rect(
                            boxRect.x + iconMargin,
                            boxRect.y + iconMargin,
                            boxRect.width - iconMargin * 2f,
                            boxRect.height - iconMargin * 2f
                        );
                        Color prevGuiColor = GUI.color;
                        GUI.color = tickColor;
                        GUI.DrawTexture(iconRect, nativeIcon.image, ScaleMode.ScaleToFit, true);
                        GUI.color = prevGuiColor;
                    }
                    else
                    {
                        var fallbackStyle = CheckmarkStyle;
                        fallbackStyle.normal.textColor = tickColor;
                        fallbackStyle.fontSize = Mathf.RoundToInt(boxRect.height * 0.80f);
                        GUI.Label(new Rect(boxRect.x, boxRect.y - 1f, boxRect.width, boxRect.height), "✓", fallbackStyle);
                    }
                    break;

                case ChecklistTickStyle.Custom:
                    string glyph = !string.IsNullOrEmpty(customChar) ? customChar : "✓";
                    var customStyle = CheckmarkStyle;
                    customStyle.normal.textColor = tickColor;
                    customStyle.fontSize = Mathf.RoundToInt(boxRect.height * 0.75f);
                    GUI.Label(new Rect(boxRect.x, boxRect.y - 1f, boxRect.width, boxRect.height), glyph, customStyle);
                    break;
            }
        }

        public static bool DrawThemedCheckbox(bool isChecked, params GUILayoutOption[] options)
        {
            return DrawThemedCheckbox(isChecked, null, options);
        }

        public static bool DrawThemedCheckbox(bool isChecked, string tooltip, params GUILayoutOption[] options)
        {
            Rect rect = GUILayoutUtility.GetRect(16, 16, options);
            return DrawThemedCheckbox(rect, isChecked, tooltip);
        }

        public static bool DrawThemedCheckbox(Rect rect, bool isChecked, string tooltip = null)
        {
            int controlID = GUIUtility.GetControlID(FocusType.Passive);
            Event current = Event.current;

            if (!string.IsNullOrEmpty(tooltip))
            {
                ThemedTooltip.SetTooltip(rect, tooltip);
            }

            bool isHovered = rect.Contains(current.mousePosition);

            switch (current.type)
            {
                case EventType.MouseDown:
                    if (current.button == 0 && isHovered)
                    {
                        GUIUtility.hotControl = controlID;
                        current.Use();
                    }
                    break;

                case EventType.MouseUp:
                    if (GUIUtility.hotControl == controlID && current.button == 0)
                    {
                        GUIUtility.hotControl = 0;
                        current.Use();
                        if (isHovered)
                        {
                            GUI.changed = true;
                            return !isChecked;
                        }
                    }
                    break;

                case EventType.Repaint:
                    float boxSize = Mathf.Min(14f, Mathf.Min(rect.width, rect.height));
                    if (boxSize < 10f) boxSize = 14f;
                    Rect boxRect = new Rect(
                        rect.x + (rect.width - boxSize) * 0.5f,
                        rect.y + (rect.height - boxSize) * 0.5f,
                        boxSize,
                        boxSize
                    );

                    // Draw checkbox background
                    Color bgColor = isChecked ? ChecklistTickCheckedBg : ChecklistTickBg;
                    if (isHovered && !isChecked)
                    {
                        bgColor = Color.Lerp(bgColor, Color.white, EditorGUIUtility.isProSkin ? 0.12f : 0.25f);
                    }
                    else if (isHovered && isChecked)
                    {
                        bgColor = Color.Lerp(bgColor, Color.white, 0.15f);
                    }

                    EditorGUI.DrawRect(boxRect, bgColor);

                    // Draw checkbox border
                    Color borderColor = ChecklistTickBorder;
                    if (isHovered)
                    {
                        borderColor = Color.Lerp(borderColor, Color.white, 0.25f);
                    }
                    DrawBorderRect(boxRect, borderColor, 1f);

                    // If checked, draw checkmark
                    if (isChecked)
                    {
                        DrawCheckmarkIcon(boxRect, ChecklistTickColor, ChecklistTickStyle, CustomChecklistTickChar);
                    }
                    break;
            }

            return isChecked;
        }

        // Column background tint
        public static Color ColumnBg => EditorGUIUtility.isProSkin
            ? Pro_ColumnBg
            : Personal_ColumnBg;

        public static Color ColumnBgAlt => EditorGUIUtility.isProSkin
            ? Pro_ColumnBgAlt
            : Personal_ColumnBgAlt;
    }
}

