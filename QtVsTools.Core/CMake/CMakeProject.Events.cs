/***************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0
***************************************************************************************************/

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Workspace.Debug;
using Microsoft.VisualStudio.Workspace.Indexing;

namespace QtVsTools.Core.CMake
{
    public partial class CMakeProject : Concurrent<CMakeProject>
    {
        private void SubscribeEvents()
        {
            FileWatcher.OnFileSystemChanged += OnFileSystemChangedAsync;
            Index.OnFileScannerCompleted += OnFileScannerCompletedAsync;
            Index.OnFileEntityChanged += OnFileEntityChangedAsync;
        }

        private void UnsubscribeEvents()
        {
            FileWatcher.OnFileSystemChanged -= OnFileSystemChangedAsync;
            Index.OnFileScannerCompleted -= OnFileScannerCompletedAsync;
            Index.OnFileEntityChanged -= OnFileEntityChangedAsync;
        }

        private async Task OnFileSystemChangedAsync(object sender, FileSystemEventArgs args)
        {
            if (IsProjectFile(args.FullPath))
                await CheckQtStatusAsync();
        }

        private async Task OnFileScannerCompletedAsync(object sender, FileScannerEventArgs args)
        {
            if (Status != QtStatus.True)
                return;
            if (!args.TryGetContent<IReadOnlyCollection<FileDataValue>>(out var values))
                return;
            if (!values.Any())
                return;

            RefreshVariables(values);
            if (!Variables.Any())
                return;

            if (values.FirstOrDefault(item => item.Type == DebugLaunchActionContext.ContextTypeGuid
                && string.Equals(item.Name, "IsDefaultStartupProject")) is not { } defaultStartup) {
                return;
            }

            await Task.Yield();
        }

        private async Task OnFileEntityChangedAsync(object sender, FileEntityChangedEventArgs args)
        {
            if (Status != QtStatus.True)
                return;

            await Task.Yield();
        }
    }
}
