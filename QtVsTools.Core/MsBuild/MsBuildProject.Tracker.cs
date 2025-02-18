/***************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0
***************************************************************************************************/

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.TaskStatusCenter;
using Microsoft.VisualStudio.Threading;

using Task = System.Threading.Tasks.Task;

namespace QtVsTools.Core.MsBuild
{
    using Core;
    using Options;
    using VisualStudio;

    public partial class MsBuildProject
    {
        private static PunisherQueue<MsBuildProject> InitQueue => StaticLazy.Get(() =>
            InitQueue, () => new PunisherQueue<MsBuildProject>());

        private static ITaskHandler2 InitStatus { get; set; }

        public UnconfiguredProject UnconfiguredProject { get; private set; }
        public EventWaitHandle Initialized { get; }

        private static async Task InitDispatcherLoopAsync()
        {
            if (VsServiceProvider.Instance is not AsyncPackage package)
                return;

            while (!VsShellUtilities.ShutdownToken.IsCancellationRequested) {
                while (InitQueue.IsEmpty)
                    await Task.Delay(100, VsShellUtilities.ShutdownToken);
                if (InitQueue.TryDequeue(out var tracker)) {
                    if (InitStatus == null) {
                        await package.JoinableTaskFactory.SwitchToMainThreadAsync();
                        tracker.BeginInitStatus();
                        await TaskScheduler.Default;
                    } else {
                        await package.JoinableTaskFactory.SwitchToMainThreadAsync();
                        tracker.UpdateInitStatus(0);
                        await TaskScheduler.Default;
                    }
                    await tracker.InitializeAsync();
                }

                if (InitStatus == null)
                    continue;
                var cancellationRequested = InitStatus.UserCancellation.IsCancellationRequested;
                if (!InitQueue.IsEmpty && !cancellationRequested)
                    continue;
                if (cancellationRequested)
                    InitQueue.Clear();
                EndInitStatus();
            }
        }

        private async Task InitializeAsync()
        {
            var p = 0;
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            UpdateInitStatus(p += 10);

            UpdateInitStatus(p += 10);

            if (VcProject is not IVsBrowseObjectContext context) {
                if (VcProject.Object is not EnvDTE.Project project)
                    return;
                context = project.Object as IVsBrowseObjectContext;
            }
            if (context == null)
                return;

            UpdateInitStatus(p += 10);

            UnconfiguredProject = context.UnconfiguredProject;
            if (UnconfiguredProject?.ProjectService.Services == null)
                return;

            await TaskScheduler.Default;
            UpdateInitStatus(p += 10);

            var service = UnconfiguredProject.Services
                .ProjectConfigurationsService;
            if (service == null)
                return;

            var configs = await service.GetKnownProjectConfigurationsAsync();
            UpdateInitStatus(p += 10);

            Initialized.Set();

            var n = configs.Count;
            var d = (100 - p) / (n * 2);
            foreach (var config in configs) {
                var configProject = await UnconfiguredProject.LoadConfiguredProjectAsync(config);
                UpdateInitStatus(p += d);
                configProject.ProjectUnloading += OnProjectUnloadingAsync;
                if (QtOptionsPage.BuildDebugInformation) {
                    Messages.Print($"{DateTime.Now:HH:mm:ss.FFF} "
                        + $"QtProjectTracker({Thread.CurrentThread.ManagedThreadId}): "
                        + $"Started tracking [{config.Name}] {VcProjectPath}");
                }
                UpdateInitStatus(p += d);
            }
        }

        private async Task OnProjectUnloadingAsync(object sender, EventArgs args)
        {
            if (sender is ConfiguredProject project) {
                if (QtOptionsPage.BuildDebugInformation) {
                    Messages.Print($"{DateTime.Now:HH:mm:ss.FFF} QtProjectTracker: "
                        + $"Stopped tracking [{project.ProjectConfiguration.Name}] "
                        + $"{project.UnconfiguredProject.FullPath}");
                }

                lock (CriticalSection) {
                    project.ProjectUnloading -= OnProjectUnloadingAsync;
                    Instances.TryRemove(project.UnconfiguredProject.FullPath, out _);
                }

                await Task.Yield();
            }
        }

        private void BeginInitStatus()
        {
            lock (StaticCriticalSection) {
                if (InitStatus != null)
                    return;
                try {
                    InitStatus = StatusCenter.PreRegister(
                        new TaskHandlerOptions
                        {
                            Title = "Qt VS Tools: Setting up project tracking..."
                        },
                        new TaskProgressData
                        {
                            ProgressText = $"{VcProjectPath} ({InitQueue.Count} projects remaining)",
                            CanBeCanceled = true,
                            PercentComplete = 0
                        })
                        as ITaskHandler2;
                } catch (Exception exception) {
                    exception.Log();
                }
                InitStatus?.RegisterTask(new Task(() => throw new InvalidOperationException()));
            }
        }

        private void UpdateInitStatus(int percentComplete)
        {
            lock (StaticCriticalSection) {
                if (InitStatus == null)
                    return;
                try {
                    InitStatus.Progress.Report(new TaskProgressData
                    {
                        ProgressText = $"{Path.GetFileNameWithoutExtension(VcProjectPath)} "
                            + $"({InitQueue.Count} project(s) remaining)",
                        CanBeCanceled = true,
                        PercentComplete = percentComplete
                    });
                } catch (Exception exception) {
                    exception.Log();
                }
            }
        }

        private static void EndInitStatus()
        {
            lock (StaticCriticalSection) {
                if (InitStatus == null)
                    return;
                InitStatus.Dismiss();
                InitStatus = null;
            }
        }
    }
}
