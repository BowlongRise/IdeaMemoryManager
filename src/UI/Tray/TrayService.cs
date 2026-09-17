using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using IdeaMemoryManager.Common.Localization;

namespace IdeaMemoryManager.UI.Tray
{
    /// <summary>
    /// System tray icon integration providing quick actions and background notifications.
    /// </summary>
    public class TrayService : IDisposable
    {
        private readonly NotifyIcon _notifyIcon;
        private ToolStripMenuItem _cleanItem;
        private ToolStripMenuItem _showItem;
        private ToolStripMenuItem _exitItem;

        public event Action OnShowRequested;
        public event Action OnCleanRequested;
        public event Action OnExitRequested;

        public TrayService(Icon appIcon = null)
        {
            var menu = new ContextMenuStrip();
            _cleanItem = new ToolStripMenuItem(I18n.T("TrayCleanNow"), null, (s, e) => OnCleanRequested?.Invoke());
            _showItem = new ToolStripMenuItem(I18n.T("TrayShowWindow"), null, (s, e) => OnShowRequested?.Invoke());
            _exitItem = new ToolStripMenuItem(I18n.T("TrayExit"), null, (s, e) => OnExitRequested?.Invoke());

            menu.Items.Add(_cleanItem);
            menu.Items.Add(_showItem);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(_exitItem);

            _notifyIcon = new NotifyIcon
            {
                Text = I18n.T("AppTitle"),
                Icon = appIcon ?? SystemIcons.Application,
                ContextMenuStrip = menu,
                Visible = true
            };
            _notifyIcon.DoubleClick += (s, e) => OnShowRequested?.Invoke();

            I18n.OnLanguageChanged += UpdateMenuLabels;
        }

        public void UpdateMenuLabels()
        {
            _cleanItem.Text = I18n.T("TrayCleanNow");
            _showItem.Text = I18n.T("TrayShowWindow");
            _exitItem.Text = I18n.T("TrayExit");
            _notifyIcon.Text = I18n.T("AppTitle");
        }

        public void ShowNotification(string title, string text)
        {
            _notifyIcon.ShowBalloonTip(2000, title, text, ToolTipIcon.Info);
        }

        public void Dispose()
        {
            I18n.OnLanguageChanged -= UpdateMenuLabels;
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
        }
    }
}