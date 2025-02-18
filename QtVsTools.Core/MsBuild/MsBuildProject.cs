/***************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0
***************************************************************************************************/

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TaskStatusCenter;
using Microsoft.VisualStudio.VCProjectEngine;

using Task = System.Threading.Tasks.Task;

namespace QtVsTools.Core.MsBuild
{
    using Options;
    using QtVsTools.Common;
    using VisualStudio;
    using static Common.Utils;

    /// <summary>
    /// QtProject holds the Qt specific properties for a Visual Studio project.
    /// There exists at most one QtProject perVCProject. Use QtProject.GetOrAdd
    /// to get the QtProject for a VCProject.
    /// </summary>
    public partial class MsBuildProject : Concurrent<MsBuildProject>
    {
        private static LazyFactory StaticLazy { get; } = new();

        private static ConcurrentDictionary<string, MsBuildProject> Instances => StaticLazy.Get(() =>
            Instances, () => new ConcurrentDictionary<string, MsBuildProject>());

        private static IVsTaskStatusCenterService StatusCenter => StaticLazy.Get(() =>
            StatusCenter, VsServiceProvider
            .GetService<SVsTaskStatusCenterService, IVsTaskStatusCenterService>);

        public static MsBuildProject GetOrAdd(VCProject vcProject)
        {
            if (vcProject == null)
                return null;
            lock (StaticCriticalSection) {
                if (MsBuildProjectFormat.GetVersion(vcProject) >= MsBuildProjectFormat.Version.V3) {
                    if (Instances.TryGetValue(vcProject.ProjectFile, out var project))
                        return project;
                    project = new MsBuildProject(vcProject);
                    Instances[vcProject.ProjectFile] = project;
                    if (QtOptionsPage.ProjectTracking) {
                        InitQueue.Enqueue(project);
                        _ = Task.Run(InitDispatcherLoopAsync);
                    }

                    var configs = (vcProject.Configurations as IVCCollection)
                        ?.OfType<VCConfiguration>() ?? Enumerable.Empty<VCConfiguration>();
                    foreach (var config in configs) {
                        if (config.Rules.Item("QtRule10_Settings") is not
                            IVCRulePropertyStorage props) {
                            continue;
                        }
                        var qtInstall = props.GetEvaluatedPropertyValue("QtInstall");
                        if (!QtVersionManager.VersionExists(qtInstall))
                            ShowUpdateQtInstallationMessage(project);
                    }

                    return project;
                }

                if (MsBuildProjectFormat.GetVersion(vcProject) >= MsBuildProjectFormat.Version.V1)
                    ShowUpdateFormatMessage();

                return null; // ignore old or unknown projects
            }
        }

        public static void Remove(string projectPath)
        {
            lock (StaticCriticalSection)
                Instances.TryRemove(projectPath, out _);
        }

        public static void Reset()
        {
            lock (StaticCriticalSection) {
                Instances.Clear();
                InitQueue.Clear();
            }
            CloseUpdateFormatMessage();
            CloseProjectFormatUpdated();
        }

        private MsBuildProject(VCProject vcProject)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            VcProject = vcProject;
            VcProjectPath = vcProject.ProjectFile;
            VcProjectDirectory = vcProject.ProjectDirectory;
            Initialized = new EventWaitHandle(false, EventResetMode.ManualReset);
        }

        public VCProject VcProject { get; }
        public string VcProjectPath { get; }
        public string VcProjectDirectory { get; }

        public string SolutionPath { get; set; } = "";
        public bool IsTracked =>
            QtOptionsPage.ProjectTracking && Instances.ContainsKey(VcProjectPath);

        public string QtVersion
        {
            get
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                return GetPropertyValue("QtInstall");
            }
        }

        public MsBuildProjectFormat.Version FormatVersion
        {
            get
            {
                return MsBuildProjectFormat.GetVersion(VcProject);
            }
        }

        public string InstallPath
        {
            get
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                return QtVersionManager.GetInstallPath(this);
            }
        }

        public VersionInformation VersionInfo
        {
            get
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                return VersionInformation.GetOrAddByName(QtVersion);
            }
        }

        public string GetPropertyValue(string propertyName)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            return VcProject.ActiveConfiguration is {} activeConfiguration
                ? activeConfiguration.GetEvaluatedPropertyValue(propertyName)
                : null;
        }

        /// <summary>
        /// Returns the files specified by the file name from a given project as list of VCFile
        /// objects.
        /// </summary>
        /// <param name="fileName">file name (relative path)</param>
        /// <returns></returns>
        public IEnumerable<VCFile> GetFilesFromProject(string fileName)
        {
            var fi = new FileInfo(HelperFunctions.NormalizeRelativeFilePath(fileName));
            foreach (VCFile f in (IVCCollection)VcProject.Files) {
                if (string.Equals(f.Name, fi.Name, IgnoreCase))
                    yield return f;
            }
        }

        public void MarkAsQtPlugin()
        {
            if (VcProject.Configurations is not IVCCollection configurations)
                return;

            foreach (VCConfiguration config in configurations) {
                if (config.Rules.Item("QtRule10_Settings") is IVCRulePropertyStorage rule)
                    rule.SetPropertyValue("QtPlugin", "true");
            }
        }

        public void EnableActiveQtBuildStep(string version, string defFile = null)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            foreach (VCConfiguration config in (IVCCollection)VcProject.Configurations) {
                if (config.Rules.Item("QtRule80_IDC") is IVCRulePropertyStorage rule) {
                    rule.SetPropertyValue("QtIDC", "true");
                    rule.SetPropertyValue("QtIDCVersion", version);
                }

                var linker = (VCLinkerTool)((IVCCollection)config.Tools).Item("VCLinkerTool");
                var librarian = (VCLibrarianTool)((IVCCollection)config.Tools).Item("VCLibrarianTool");

                if (linker != null) {
                    linker.Version = version;
                    linker.ModuleDefinitionFile = defFile ?? VcProject.Name + ".def";
                } else {
                    librarian.ModuleDefinitionFile = defFile ?? VcProject.Name + ".def";
                }
            }
        }

        public bool UsesPrecompiledHeaders()
        {
            if (VcProject.Configurations is not IVCCollection configurations)
                return false;

            const pchOption pchNone = pchOption.pchNone;
            return configurations.Cast<VCConfiguration>()
                .Select(CompilerToolWrapper.Create)
                .All(compiler => (compiler?.GetUsePrecompiledHeader() ?? pchNone) != pchNone);
        }

        public string GetPrecompiledHeaderThrough()
        {
            if (VcProject.Configurations is not IVCCollection configurations)
                return null;

            return configurations.Cast<VCConfiguration>()
                .Select(CompilerToolWrapper.Create)
                .Select(compiler => compiler?.GetPrecompiledHeaderThrough() ?? "")
                .Where(header => !string.IsNullOrEmpty(header))
                .Select(header => header.ToLower())
                .FirstOrDefault();
        }

        public static void SetPCHOption(VCFile vcFile, pchOption option)
        {
            if (vcFile.FileConfigurations is not IVCCollection fileConfigurations)
                return;

            foreach (VCFileConfiguration config in fileConfigurations)
                CompilerToolWrapper.Create(config)?.SetUsePrecompiledHeader(option);
        }

        public void RemoveGeneratedFiles(string fileName)
        {
            var fi = new FileInfo(fileName);
            var lastIndex = fileName.LastIndexOf(fi.Extension, StringComparison.Ordinal);
            var baseName = fi.Name.Remove(lastIndex, fi.Extension.Length);
            string delName = null;
            if (HelperFunctions.IsHeaderFile(fileName))
                delName = "moc_" + baseName + ".cpp";
            else if (HelperFunctions.IsSourceFile(fileName) && !fileName.StartsWith("moc_", IgnoreCase))
                delName = baseName + ".moc";
            else if (HelperFunctions.IsUicFile(fileName))
                delName = "ui_" + baseName + ".h";
            else if (HelperFunctions.IsQrcFile(fileName))
                delName = "qrc_" + baseName + ".cpp";

            if (delName != null) {
                foreach (var vcFile in GetFilesFromProject(delName))
                    vcFile.DeleteAndRemoveFromFilter(FakeFilter.GeneratedFiles());
            }
        }

        public bool SelectInSolutionExplorer()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (VsServiceProvider.Instance is not IServiceProvider provider)
                return false;
            var explorer = VsShellUtilities.GetUIHierarchyWindow(provider,
                VSConstants.StandardToolWindows.SolutionExplorer);
            var hierarchy = VsShellUtilities.GetHierarchy(provider,
                Guid.Parse(VcProject.ProjectGUID)) as IVsUIHierarchy;

            if (explorer == null || hierarchy == null)
                return false;

            explorer.ExpandItem(hierarchy, VSConstants.VSITEMID_ROOT, EXPANDFLAGS.EXPF_SelectItem);
            var ret = explorer.GetItemState(hierarchy, VSConstants.VSITEMID_ROOT,
                (uint)__VSHIERARCHYITEMSTATE.HIS_Selected, out var state);

            return !ErrorHandler.Failed(ret) && state == (uint)__VSHIERARCHYITEMSTATE.HIS_Selected;
        }
    }
}
