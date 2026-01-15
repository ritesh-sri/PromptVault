using System;
using System.Windows;
using System.Windows.Forms;
using System.Drawing;

namespace PromptVault.Services
{
    public class SystemTrayManager : IDisposable
    {
        private NotifyIcon notifyIcon;
        private Window mainWindow;
        private bool isMinimizeToTrayEnabled;

        public event EventHandler OpenRequested;
        public event EventHandler ExitRequested;

        public SystemTrayManager(Window window)
        {
            mainWindow = window;
            InitializeTrayIcon();
        }

        private void InitializeTrayIcon()
        {
            notifyIcon = new NotifyIcon
            {
                Icon = GetApplicationIcon(),
                Text = "PromptVault - AI Prompt Manager",
                Visible = false
            };

            // Create context menu
            var contextMenu = new ContextMenuStrip();

            var openItem = new ToolStripMenuItem("Open PromptVault", null, (s, e) =>
            {
                OpenRequested?.Invoke(this, EventArgs.Empty);
            });
            openItem.Font = new Font(openItem.Font, System.Drawing.FontStyle.Bold);
            contextMenu.Items.Add(openItem);

            contextMenu.Items.Add(new ToolStripSeparator());

            contextMenu.Items.Add(new ToolStripMenuItem("New Prompt", null, (s, e) =>
            {
                OpenRequested?.Invoke(this, EventArgs.Empty);
            }));

            contextMenu.Items.Add(new ToolStripMenuItem("Quick Capture (Clipboard)", null, (s, e) =>
            {
                OpenRequested?.Invoke(this, EventArgs.Empty);
            }));

            contextMenu.Items.Add(new ToolStripSeparator());

            contextMenu.Items.Add(new ToolStripMenuItem("Settings", null, (s, e) =>
            {
                OpenRequested?.Invoke(this, EventArgs.Empty);
            }));

            contextMenu.Items.Add(new ToolStripSeparator());

            contextMenu.Items.Add(new ToolStripMenuItem("Exit", null, (s, e) =>
            {
                ExitRequested?.Invoke(this, EventArgs.Empty);
            }));

            notifyIcon.ContextMenuStrip = contextMenu;

            // Double-click to open
            notifyIcon.DoubleClick += (s, e) =>
            {
                OpenRequested?.Invoke(this, EventArgs.Empty);
            };

            // Show balloon tip on first minimize
            notifyIcon.BalloonTipTitle = "PromptVault";
            notifyIcon.BalloonTipText = "PromptVault is running in the background. Double-click to open.";
        }

        public void SetMinimizeToTray(bool enabled)
        {
            isMinimizeToTrayEnabled = enabled;
        }

        public bool IsMinimizeToTrayEnabled => isMinimizeToTrayEnabled;

        public void ShowInTray()
        {
            notifyIcon.Visible = true;
        }

        public void HideFromTray()
        {
            notifyIcon.Visible = false;
        }

        public void ShowBalloonTip(string title, string text, int timeout = 3000)
        {
            notifyIcon.BalloonTipTitle = title;
            notifyIcon.BalloonTipText = text;
            notifyIcon.ShowBalloonTip(timeout);
        }

        public void HandleWindowStateChange(WindowState state)
        {
            if (isMinimizeToTrayEnabled && state == WindowState.Minimized)
            {
                mainWindow.Hide();
                ShowInTray();

                // Show balloon tip only on first minimize
                if (!notifyIcon.Visible)
                {
                    ShowBalloonTip("PromptVault", "Running in background. Use your hotkey or double-click the tray icon to open.");
                }
            }
        }

        public void RestoreWindow()
        {
            mainWindow.Show();
            mainWindow.WindowState = WindowState.Normal;
            mainWindow.Activate();
            mainWindow.Focus();
        }

        private Icon GetApplicationIcon()
        {
            try
            {
                // Try to load icon from application resources
                var iconStream = System.Windows.Application.GetResourceStream(new Uri("pack://application:,,,/PromptVault;component/icon.ico"));
                if (iconStream != null)
                {
                    return new Icon(iconStream.Stream);
                }
            }
            catch { }

            // Fallback: Create a simple icon programmatically
            return CreateDefaultIcon();
        }

        private Icon CreateDefaultIcon()
        {
            // Create a 16x16 bitmap
            var bitmap = new Bitmap(16, 16);
            using (var graphics = Graphics.FromImage(bitmap))
            {
                graphics.Clear(Color.Transparent);

                // Draw a simple box icon (📦 emoji representation)
                using (var brush = new SolidBrush(Color.FromArgb(33, 150, 243))) // Blue color
                {
                    graphics.FillRectangle(brush, 2, 2, 12, 12);
                }

                using (var pen = new Pen(Color.White, 1))
                {
                    graphics.DrawRectangle(pen, 3, 3, 10, 10);
                    graphics.DrawLine(pen, 3, 8, 13, 8);
                }
            }

            IntPtr hIcon = bitmap.GetHicon();
            Icon icon = Icon.FromHandle(hIcon);
            return icon;
        }

        public void Dispose()
        {
            if (notifyIcon != null)
            {
                notifyIcon.Visible = false;
                notifyIcon.Dispose();
                notifyIcon = null;
            }
        }
    }
}