/***************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0
***************************************************************************************************/

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Microsoft.Build.Framework;
using Microsoft.Build.Evaluation;

namespace QtVsTools.Test.QtMsBuild.Build
{
    public partial class Logger : ILogger
    {
        public readonly object CriticalSection = new();

        private ProjectCollection MsBuild { get; set; }

        public Logger(ProjectCollection msbuild)
        {
            MsBuild = msbuild;
            MsBuild.OnlyLogCriticalEvents = false;
            MsBuild.RegisterLogger(this);
        }

        public void Reset()
        {
            lock (CriticalSection) {
                SeenEvents.Clear();
                EventArgs = new();
            }
        }

        public LoggerVerbosity Verbosity
        {
            set { }
            get => LoggerVerbosity.Diagnostic;
        }

        public string Parameters
        {
            set { }
            get => string.Empty;
        }

        private IEventSource EventSource { get; set; }

        private HashSet<EventArgs> SeenEvents { get; set; } = new();
        private ConcurrentQueue<EventArgs> EventArgs { get; set; } = new();

        public void Initialize(IEventSource eventSource)
        {
            EventSource = eventSource
                ?? throw new ArgumentNullException(nameof(eventSource));

            EventSource.AnyEventRaised += EventSource_AnyEventRaised;
            EventSource.BuildFinished += EventSource_BuildFinished;
            EventSource.BuildStarted += EventSource_BuildStarted;
            EventSource.CustomEventRaised += EventSource_CustomEventRaised;
            EventSource.ErrorRaised += EventSource_ErrorRaised;
            EventSource.MessageRaised += EventSource_MessageRaised;
            EventSource.ProjectFinished += EventSource_ProjectFinished;
            EventSource.ProjectStarted += EventSource_ProjectStarted;
            EventSource.StatusEventRaised += EventSource_StatusEventRaised;
            EventSource.TargetFinished += EventSource_TargetFinished;
            EventSource.TargetStarted += EventSource_TargetStarted;
            EventSource.TaskFinished += EventSource_TaskFinished;
            EventSource.TaskStarted += EventSource_TaskStarted;
            EventSource.WarningRaised += EventSource_WarningRaised;
            if (EventSource is IEventSource2 eventSource2)
                eventSource2.TelemetryLogged += EventSource2_TelemetryLogged;
            if (EventSource is IEventSource3 eventSource3) {
                eventSource3.IncludeEvaluationMetaprojects();
                eventSource3.IncludeEvaluationProfiles();
                eventSource3.IncludeTaskInputs();
            }
            if (EventSource is IEventSource4 eventSource4)
                eventSource4.IncludeEvaluationPropertiesAndItems();

            MsBuild.ProjectAdded += GlobalProjectCollection_ProjectAdded;
            MsBuild.ProjectChanged += GlobalProjectCollection_ProjectChanged;
            MsBuild.ProjectCollectionChanged += GlobalProjectCollection_ProjectCollectionChanged;
            MsBuild.ProjectXmlChanged += GlobalProjectCollection_ProjectXmlChanged;
        }

        public void Shutdown()
        {
            if (EventSource == null)
                throw new InvalidOperationException();

            EventSource.AnyEventRaised -= EventSource_AnyEventRaised;
            EventSource.BuildFinished -= EventSource_BuildFinished;
            EventSource.BuildStarted -= EventSource_BuildStarted;
            EventSource.CustomEventRaised -= EventSource_CustomEventRaised;
            EventSource.ErrorRaised -= EventSource_ErrorRaised;
            EventSource.MessageRaised -= EventSource_MessageRaised;
            EventSource.ProjectFinished -= EventSource_ProjectFinished;
            EventSource.ProjectStarted -= EventSource_ProjectStarted;
            EventSource.StatusEventRaised -= EventSource_StatusEventRaised;
            EventSource.TargetFinished -= EventSource_TargetFinished;
            EventSource.TargetStarted -= EventSource_TargetStarted;
            EventSource.TaskFinished -= EventSource_TaskFinished;
            EventSource.TaskStarted -= EventSource_TaskStarted;
            EventSource.WarningRaised -= EventSource_WarningRaised;
            if (EventSource is IEventSource2 eventSource2)
                eventSource2.TelemetryLogged -= EventSource2_TelemetryLogged;

            MsBuild.ProjectAdded -= GlobalProjectCollection_ProjectAdded;
            MsBuild.ProjectChanged -= GlobalProjectCollection_ProjectChanged;
            MsBuild.ProjectCollectionChanged -= GlobalProjectCollection_ProjectCollectionChanged;
            MsBuild.ProjectXmlChanged -= GlobalProjectCollection_ProjectXmlChanged;
        }

        public delegate void EventAddedHandler(object sender, EventArgs e);
        public event EventAddedHandler EventAdded;

        private void OnEvent(object sender, EventArgs e)
        {
            bool eventAdded = false;
            lock (CriticalSection) {
                if (!SeenEvents.Contains(e)) {
                    SeenEvents.Add(e);
                    EventArgs.Enqueue(e);
                    eventAdded = true;
                }
            }
            if (eventAdded)
                EventAdded?.Invoke(this, e);
        }

        private void GlobalProjectCollection_ProjectXmlChanged(object sender, ProjectXmlChangedEventArgs e)
            => OnEvent(sender, e);

        private void GlobalProjectCollection_ProjectCollectionChanged(object sender, ProjectCollectionChangedEventArgs e)
            => OnEvent(sender, e);

        private void GlobalProjectCollection_ProjectChanged(object sender, ProjectChangedEventArgs e)
            => OnEvent(sender, e);

        private void GlobalProjectCollection_ProjectAdded(object sender, ProjectCollection.ProjectAddedToProjectCollectionEventArgs e)
            => OnEvent(sender, e);

        private void EventSource2_TelemetryLogged(object sender, TelemetryEventArgs e)
            => OnEvent(sender, e);

        private void EventSource_WarningRaised(object sender, BuildWarningEventArgs e)
            => OnEvent(sender, e);

        private void EventSource_TaskStarted(object sender, TaskStartedEventArgs e)
            => OnEvent(sender, e);

        private void EventSource_TaskFinished(object sender, TaskFinishedEventArgs e)
            => OnEvent(sender, e);

        private void EventSource_TargetStarted(object sender, TargetStartedEventArgs e)
            => OnEvent(sender, e);

        private void EventSource_TargetFinished(object sender, TargetFinishedEventArgs e)
            => OnEvent(sender, e);

        private void EventSource_StatusEventRaised(object sender, BuildStatusEventArgs e)
            => OnEvent(sender, e);

        private void EventSource_ProjectStarted(object sender, ProjectStartedEventArgs e)
            => OnEvent(sender, e);

        private void EventSource_ProjectFinished(object sender, ProjectFinishedEventArgs e)
            => OnEvent(sender, e);

        private void EventSource_MessageRaised(object sender, BuildMessageEventArgs e)
            => OnEvent(sender, e);

        private void EventSource_ErrorRaised(object sender, BuildErrorEventArgs e)
            => OnEvent(sender, e);

        private void EventSource_CustomEventRaised(object sender, CustomBuildEventArgs e)
            => OnEvent(sender, e);

        private void EventSource_BuildStarted(object sender, BuildStartedEventArgs e)
            => OnEvent(sender, e);

        private void EventSource_BuildFinished(object sender, BuildFinishedEventArgs e)
            => OnEvent(sender, e);

        private void EventSource_AnyEventRaised(object sender, BuildEventArgs e)
            => OnEvent(sender, e);
    }
}
