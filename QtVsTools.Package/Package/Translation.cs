/***************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0
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
        public static void RunLRelease(VCFile[] vcFiles)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var project = MsBuildProject.GetOrAdd(vcFiles.FirstOrDefault()?.project as VCProject);
            RunTranslationTarget(BuildAction.Release,
                project, vcFiles.Select(vcFile => vcFile?.RelativePath));
        }

        public static void RunLRelease(MsBuildProject project)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            RunTranslationTarget(BuildAction.Release, project);
        }

        public static void RunLRelease(EnvDTE.Solution solution)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (solution == null)
                return;

            foreach (var project in HelperFunctions.ProjectsInSolution(solution.DTE))
                RunLRelease(MsBuildProject.GetOrAdd(project));
        }

        public static void RunLUpdate(VCFile vcFile)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var project = MsBuildProject.GetOrAdd(vcFile.project as VCProject);
            RunTranslationTarget(BuildAction.Update,
                project, new[] { vcFile.RelativePath });
        }

        public static void RunLUpdate(VCFile[] vcFiles)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var project = MsBuildProject.GetOrAdd(vcFiles.FirstOrDefault()?.project as VCProject);
            RunTranslationTarget(BuildAction.Update,
                project, vcFiles.Select(vcFile => vcFile?.RelativePath));
        }

        public static void RunLUpdate(MsBuildProject project)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            RunTranslationTarget(BuildAction.Update, project);
        }

        private enum BuildAction { Update, Release }

        private static void RunTranslationTarget(BuildAction buildAction, MsBuildProject project,
            IEnumerable<string> selectedFiles = null)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (project == null) {
                Messages.Print("translation: Error accessing project interface");
                return;
            }

            if (project.VcProject.ActiveConfiguration is not {} activeConfiguration) {
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

            project.StartBuild(activeConfiguration.Name, properties, new[] { "QtTranslation" });
        }

        public static void RunLUpdate(EnvDTE.Solution solution)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            foreach (var project in HelperFunctions.ProjectsInSolution(solution?.DTE))
                RunLUpdate(MsBuildProject.GetOrAdd(project));
        }

        public static bool ToolsAvailable(MsBuildProject project)
        {
            if (project == null)
                return false;
            if (project.GetPropertyValue("ApplicationType") == "Linux")
                return true;

            var qtToolsPath = project.GetPropertyValue("QtToolsPath");
            if (string.IsNullOrEmpty(qtToolsPath)) {
                var qtInstallPath = QtVersionManager.GetInstallPath(project.QtVersion);
                if (string.IsNullOrEmpty(qtInstallPath))
                    return false;
                qtToolsPath = Path.Combine(qtInstallPath, "bin");
            }
            return File.Exists(Path.Combine(qtToolsPath, "lupdate.exe"))
                && File.Exists(Path.Combine(qtToolsPath, "lrelease.exe"));
        }
    }
}
