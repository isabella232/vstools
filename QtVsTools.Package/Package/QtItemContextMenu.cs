/***************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0
***************************************************************************************************/

using System;
using System.ComponentModel.Design;
using Microsoft.VisualStudio.Shell;

namespace QtVsTools
{
    using Core;
    using VisualStudio;

    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class QtItemContextMenu
    {
        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        private static QtItemContextMenu Instance
        {
            get;
            set;
        }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        public static void Initialize()
        {
            Instance = new QtItemContextMenu();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="QtMainMenu"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        private QtItemContextMenu()
        {
            var commandService = VsServiceProvider
                .GetService<IMenuCommandService, OleMenuCommandService>();
            if (commandService == null)
                return;

            var command = new OleMenuCommand(ExecHandler,
                new CommandID(QtMenus.Package.Guid, QtMenus.Package.lUpdateOnItem));
            command.BeforeQueryStatus += BeforeQueryStatus;
            commandService.AddCommand(command);

            command = new OleMenuCommand(ExecHandler,
                new CommandID(QtMenus.Package.Guid, QtMenus.Package.lReleaseOnItem));
            command.BeforeQueryStatus += BeforeQueryStatus;
            commandService.AddCommand(command);
        }

        private static void ExecHandler(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (sender is not OleMenuCommand command)
                return;

            switch (command.CommandID.ID) {
            case QtMenus.Package.lUpdateOnItem:
                Translation.RunLUpdate(HelperFunctions.GetSelectedFiles(QtVsToolsPackage.Instance.Dte));
                break;
            case QtMenus.Package.lReleaseOnItem:
                Translation.RunLRelease(HelperFunctions.GetSelectedFiles(QtVsToolsPackage.Instance.Dte));
                break;
            }
        }

        private static void BeforeQueryStatus(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (sender is not OleMenuCommand command)
                return;

            command.Visible = command.Enabled = false;

            if (QtVsToolsPackage.Instance.Dte.SelectedItems.Count <= 0)
                return;

            var dte = QtVsToolsPackage.Instance.Dte;
            if (HelperFunctions.GetSelectedQtProject(dte) is not {} qtProject)
                return;

            foreach (EnvDTE.SelectedItem si in QtVsToolsPackage.Instance.Dte.SelectedItems) {
                if (!HelperFunctions.IsTranslationFile(si.Name))
                    return; // Don't display commands if one of the selected files is not a .ts file.
            }

            command.Visible = true;
            command.Enabled = Translation.ToolsAvailable(qtProject);
        }
    }
}
