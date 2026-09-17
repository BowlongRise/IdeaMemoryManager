using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using IdeaMemoryManager.Common;
using IdeaMemoryManager.Common.Localization;
using IdeaMemoryManager.Config;
using IdeaMemoryManager.Core.Models;

namespace IdeaMemoryManager.UI.Controls
{
    /// <summary>
    /// Expandable process topology drawer providing granular process inspection,
    /// checkbox inclusion, and persistent whitelist management.
    /// </summary>
    public class ProcessDrawerControl : UserControl
    {
        private readonly ListView _listView;
        private readonly ContextMenuStrip _contextMenu;
        private List<ProcessTarget> _targets = new();

        public event Action OnWhitelistChanged;

        public ProcessDrawerControl()
        {
            Height = 180;
            BackColor = Color.FromArgb(24, 25, 28);
            Padding = new Padding(2);

            _listView = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                CheckBoxes = true,
                HeaderStyle = ColumnHeaderStyle.Nonclickable,
                BackColor = Color.FromArgb(24, 25, 28),
                ForeColor = Color.FromArgb(220, 224, 230),
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 8.0f)
            };

            _listView.Columns.Add("Process", 155);
            _listView.Columns.Add("PID", 50);
            _listView.Columns.Add("Category", 75);
            _listView.Columns.Add("RAM", 65);

            _listView.ItemCheck += ListView_ItemCheck;

            _contextMenu = new ContextMenuStrip
            {
                BackColor = Color.FromArgb(35, 37, 42),
                ForeColor = Color.White,
                ShowImageMargin = false
            };

            var mnuToggleWhitelist = new ToolStripMenuItem(I18n.T("WhitelistAdd"), null, ToggleWhitelist_Click);
            _contextMenu.Items.Add(mnuToggleWhitelist);
            _listView.ContextMenuStrip = _contextMenu;

            _contextMenu.Opening += (s, e) =>
            {
                if (_listView.SelectedItems.Count == 0)
                {
                    e.Cancel = true;
                    return;
                }
                var target = _listView.SelectedItems[0].Tag as ProcessTarget;
                if (target == null) return;

                mnuToggleWhitelist.Text = target.IsWhitelisted 
                    ? I18n.T("WhitelistRemove") 
                    : I18n.T("WhitelistAdd");
            };

            Controls.Add(_listView);
        }

        private void ListView_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            if (e.Index >= 0 && e.Index < _targets.Count)
            {
                var target = _targets[e.Index];
                if (target.IsWhitelisted)
                {
                    // Whitelisted processes cannot be selected for cleaning
                    e.NewValue = CheckState.Unchecked;
                    target.IsSelected = false;
                }
                else
                {
                    target.IsSelected = (e.NewValue == CheckState.Checked);
                }
            }
        }

        public void SetTargets(List<ProcessTarget> targets)
        {
            _targets = targets ?? new List<ProcessTarget>();
            _listView.BeginUpdate();
            _listView.Items.Clear();

            foreach (var t in _targets)
            {
                string catName = t.Category switch
                {
                    ProcessCategory.IdeaHost => "IDE Host",
                    ProcessCategory.JavaService => "Java Service",
                    ProcessCategory.NodeFrontend => "Frontend Tool",
                    _ => "Helper"
                };

                string nameDisplay = t.IsWhitelisted ? $"{t.Name} {I18n.T("WhitelistedBadge")}" : t.Name;

                var item = new ListViewItem(nameDisplay)
                {
                    Tag = t,
                    Checked = t.IsSelected && !t.IsWhitelisted,
                    ForeColor = t.IsWhitelisted ? Color.FromArgb(120, 125, 135) : Color.FromArgb(220, 224, 230)
                };

                item.SubItems.Add(t.Id.ToString());
                item.SubItems.Add(catName);
                item.SubItems.Add(ByteSizeFormatter.Format(t.WorkingSetBytes));

                _listView.Items.Add(item);
            }

            _listView.EndUpdate();
        }

        private void ToggleWhitelist_Click(object sender, EventArgs e)
        {
            if (_listView.SelectedItems.Count == 0) return;
            var target = _listView.SelectedItems[0].Tag as ProcessTarget;
            if (target == null) return;

            var cfg = ConfigManager.Current;
            if (target.IsWhitelisted)
            {
                cfg.Whitelist.Remove(target.Name);
                cfg.Whitelist.Remove($"{target.Name}:{target.Id}");
                target.IsWhitelisted = false;
                target.IsSelected = true;
            }
            else
            {
                cfg.Whitelist.Add(target.Name);
                target.IsWhitelisted = true;
                target.IsSelected = false;
            }

            ConfigManager.Save();
            OnWhitelistChanged?.Invoke();
        }
    }
}