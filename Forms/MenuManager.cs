using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using RecoveryCommander.UI;
using static RecoveryCommander.UI.Theme;

namespace RecoveryCommander.Forms
{
    public static class MenuManager
    {
        private const string README_PATH = @"Resources\README.md";
        private const string CHANGELOG_PATH = @"Resources\CHANGELOG.md";
        private const string PROJECT_MANIFEST_PATH = @"Resources\PROJECT_MANIFEST.md";

        public static IEnumerable<ToolStripItem> BuildMenuItems(Form parent)
        {
            var host = parent;

            var fileMenu = new ToolStripMenuItem("File");
            var exitItem = new ToolStripMenuItem("Exit");
            exitItem.Click += (s, e) => parent.Close();
            fileMenu.DropDownItems.Add(exitItem);

            var toolsMenu = new ToolStripMenuItem("Tools");
            toolsMenu.DropDownItems.Add("Restore Point Manager", null, (s, e) => DialogFactory.ShowRestorePointManager(host));
            toolsMenu.DropDownItems.Add("Startup Manager", null, (s, e) => DialogFactory.ShowStartupManager(host));
            toolsMenu.DropDownItems.Add("Network Repair & Optimization", null, (s, e) => DialogFactory.ShowNetworkOptimizer(host));
            toolsMenu.DropDownItems.Add("Media Tools", null, (s, e) => DialogFactory.ShowMediaTools(host));

            var helpMenu = new ToolStripMenuItem("Help");
            helpMenu.DropDownItems.Add("About", null, (s, e) => DialogFactory.ShowAboutDialog(parent));
            helpMenu.DropDownItems.Add(new ToolStripSeparator());
            helpMenu.DropDownItems.Add("View README", null, (s, e) => DialogFactory.ShowHelpWindow(host, README_PATH, "README"));
            helpMenu.DropDownItems.Add("View Project Manifest", null, (s, e) => DialogFactory.ShowHelpWindow(host, PROJECT_MANIFEST_PATH, "Project Manifest"));
            helpMenu.DropDownItems.Add("View Changelog", null, (s, e) => DialogFactory.ShowHelpWindow(host, CHANGELOG_PATH, "Changelog"));

            ApplyMenuColors(fileMenu, toolsMenu, helpMenu);

            yield return fileMenu;
            yield return toolsMenu;
            yield return helpMenu;
        }

        private static void ApplyMenuColors(params ToolStripMenuItem[] menus)
        {
            foreach (var menu in menus)
            {
                menu.ForeColor = Theme.Text;
                foreach (ToolStripItem item in menu.DropDownItems)
                {
                    if (item is ToolStripMenuItem subMenu)
                    {
                        ApplyMenuColors(subMenu);
                    }
                    item.ForeColor = Theme.Text;
                }
            }
        }
    }
}
