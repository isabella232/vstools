/***************************************************************************************************
 Copyright (C) 2023 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.VCProjectEngine;

namespace QtVsTools
{
    using Core;
    using Core.MsBuild;

    /// <summary>
    /// Run Qt translation tools by invoking the corresponding Qt/MSBuild targets
    /// </summary>
    public static class Translation
    {
        public static void RunlRelease(VCFile[] vcFiles)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var qtProject = QtProject.GetOrAdd(vcFiles.FirstOrDefault()?.project as VCProject);
            RunTranslationTarget(BuildAction.Release,
                qtProject, vcFiles.Select(vcFile => vcFile?.RelativePath));
        }

        public static void RunlRelease(QtProject qtProject)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            RunTranslationTarget(BuildAction.Release, qtProject);
        }

        public static void RunlRelease(EnvDTE.Solution solution)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (solution == null)
                return;

            foreach (var project in HelperFunctions.ProjectsInSolution(solution.DTE))
                RunlRelease(QtProject.GetOrAdd(project));
        }

        public static void RunlUpdate(VCFile vcFile)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var qtProject = QtProject.GetOrAdd(vcFile.project as VCProject);
            RunTranslationTarget(BuildAction.Update,
                qtProject, new[] { vcFile.RelativePath });
        }

        public static void RunlUpdate(VCFile[] vcFiles)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var qtProject = QtProject.GetOrAdd(vcFiles.FirstOrDefault()?.project as VCProject);
            RunTranslationTarget(BuildAction.Update,
                qtProject, vcFiles.Select(vcFile => vcFile?.RelativePath));
        }

        public static void RunlUpdate(QtProject qtProject)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            RunTranslationTarget(BuildAction.Update, qtProject);
        }

        private enum BuildAction { Update, Release }

        private static void RunTranslationTarget(BuildAction buildAction, QtProject qtProject,
            IEnumerable<string> selectedFiles = null)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (qtProject == null) {
                Messages.Print("translation: Error accessing project interface");
                return;
            }

            if (qtProject.VcProject.ActiveConfiguration is not {} activeConfig) {
                Messages.Print("translation: Error accessing build interface");
                return;
            }

            using var _ = WaitDialog.Start("Qt Visual Studio Tools", "Running translation tool...");

            var properties = new Dictionary<string, string>();
            switch (buildAction) {
            case BuildAction.Update:
                properties["QtTranslationForceUpdate"] = "true";
                break;
            case BuildAction.Release:
                properties["QtTranslationForceRelease"] = "true";
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(buildAction), buildAction, null);
            }
            if (selectedFiles != null)
                properties["SelectedFiles"] = string.Join(";", selectedFiles);

            var activeConfigId = $"{activeConfig.ConfigurationName}|{activeConfig.Platform}";
            qtProject.StartBuild(activeConfigId, properties, new[] { "QtTranslation" });
        }

        public static void RunlUpdate(EnvDTE.Solution solution)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            foreach (var project in HelperFunctions.ProjectsInSolution(solution?.DTE))
                RunlUpdate(QtProject.GetOrAdd(project));
        }

        public static bool ToolsAvailable(QtProject qtProject)
        {
            if (qtProject == null)
                return false;
            if (qtProject.GetPropertyValue("ApplicationType") == "Linux")
                return true;

            var qtToolsPath = qtProject.GetPropertyValue("QtToolsPath");
            if (string.IsNullOrEmpty(qtToolsPath)) {
                var qtInstallPath = QtVersionManager.The().GetInstallPath(qtProject.QtVersion);
                if (string.IsNullOrEmpty(qtInstallPath))
                    return false;
                qtToolsPath = Path.Combine(qtInstallPath, "bin");
            }
            return File.Exists(Path.Combine(qtToolsPath, "lupdate.exe"))
                && File.Exists(Path.Combine(qtToolsPath, "lrelease.exe"));
        }
    }
}
