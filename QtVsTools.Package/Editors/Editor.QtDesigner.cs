/***************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0
***************************************************************************************************/

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.VCProjectEngine;

using Task = System.Threading.Tasks.Task;

namespace QtVsTools.Editors
{
    using Core.MsBuild;
    using Core.Options;
    using VisualStudio;

    internal class QtDesignerFileSniffer : IFileTypeSniffer
    {
        private const string Pattern = @"<\s*UI\s+version\s*=\s*""*\d+\.\d+""\s*>";

        public bool IsSupportedFile(string filePath)
        {
            try {
                var line = File.ReadLines(filePath).FirstOrDefault();
                return System.Text.RegularExpressions.Regex.IsMatch(line?.Trim() ?? "", Pattern);
            } catch {
                return false;
            }
        }
    }

    [Guid(GuidString)]
    public class QtDesigner : Editor
    {
        public const string GuidString = "96FE523D-6182-49F5-8992-3BEA5F7E6FF6";
        public const string Title = "Qt Widgets Designer";
        public const string LegacyTitle = "Qt Designer";

        public QtDesigner()
            : base(new QtDesignerFileSniffer())
        {}

        private Guid? guid;
        public override Guid Guid => guid ??= new Guid(GuidString);

        public override string ExecutableName => "designer.exe";

        public override Func<string, bool> WindowFilter =>
            caption => caption.StartsWith(Title) || caption.StartsWith(LegacyTitle);

        protected override string GetTitle(Process editorProcess)
        {
            return Title;
        }

        protected override void OnStart(Process process)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            base.OnStart(process);
            var document = VsShell.GetDocument(Context, ItemId);

            if (document?.ProjectItem?.ContainingProject?.Object is not VCProject vcProject)
                return;

            if (MsBuildProject.GetOrAdd(vcProject) is not { IsTracked: true } project)
                return;

            var filePath = document.FullName;
            var lastWriteTime = File.GetLastWriteTime(filePath);

            _ = Task.Run(async () =>
            {
                while (!process.WaitForExit(1000)) {
                    var latestWriteTime = File.GetLastWriteTime(filePath);
                    if (lastWriteTime == latestWriteTime)
                        continue;
                    lastWriteTime = latestWriteTime;
                    await project.RefreshAsync();
                }
                if (lastWriteTime != File.GetLastWriteTime(filePath)) {
                    await project.RefreshAsync();
                }
            });
        }

        protected override bool Detached => QtOptionsPage.DesignerDetached;
    }
}
