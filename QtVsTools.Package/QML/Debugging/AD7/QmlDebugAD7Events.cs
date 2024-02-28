/***************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0
***************************************************************************************************/

using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Debugger.Interop;
using System;

namespace QtVsTools.Qml.Debug.AD7
{
    abstract class DebugEvent :

        IDebugEvent2 // "This interface is used to communicate both critical debug information,
                     //  such as stopping at a breakpoint, and non-critical information, such as a
                     //  debugging message."
    {
        private Guid InterfaceId { get; }
        private uint Attributes { get; }

        protected const uint ASYNCHRONOUS = (uint)enum_EVENTATTRIBUTES.EVENT_ASYNCHRONOUS;
        protected const uint STOPPING = (uint)enum_EVENTATTRIBUTES.EVENT_ASYNC_STOP;
        protected const uint SYNCHRONOUS = (uint)enum_EVENTATTRIBUTES.EVENT_SYNCHRONOUS;
        protected const uint SYNCHRONOUS_STOPPING =
              (uint)enum_EVENTATTRIBUTES.EVENT_STOPPING
            | (uint)enum_EVENTATTRIBUTES.EVENT_SYNCHRONOUS;

        protected QmlEngine Engine { get; }
        private IDebugProgram2 Program { get; }
        private IDebugThread2 Thread { get; }
        private IDebugEventCallback2 Callback { get; }

        protected DebugEvent(
            QmlEngine engine,
            Guid interfaceId,
            uint attributes,
            IDebugProgram2 program = null,
            IDebugThread2 thread = null,
            IDebugEventCallback2 callback = null)
        {
            InterfaceId = interfaceId;
            Attributes = attributes;
            Engine = engine;
            Program = program;
            Thread = thread;
            Callback = callback ?? engine;
        }

        protected void Send()
        {
            if (Callback != null) {
                var interfaceId = InterfaceId;
                Callback.Event(
                    Engine, null, Program, Thread,
                    this, ref interfaceId, Attributes);
            }
        }

        public int /*IDebugEvent2*/ GetAttributes(out uint eventAttributes)
        {
            eventAttributes = Attributes;
            return VSConstants.S_OK;
        }

        public static void Send(DebugEvent debugEvent)
        {
            debugEvent.Send();
        }
    }

    class EngineCreateEvent : DebugEvent, IDebugEngineCreateEvent2
    {
        public EngineCreateEvent(QmlEngine engine)
        : base(engine, typeof(IDebugEngineCreateEvent2).GUID, ASYNCHRONOUS)
        { }

        int IDebugEngineCreateEvent2.GetEngine(out IDebugEngine2 pEngine)
        {
            pEngine = Engine as IDebugEngine2;
            return VSConstants.S_OK;
        }
    }

    class ProgramCreateEvent : DebugEvent, IDebugProgramCreateEvent2
    {
        public ProgramCreateEvent(Program program)
        : base(program.Engine, typeof(IDebugProgramCreateEvent2).GUID,
              ASYNCHRONOUS, program)
        { }
    }

    class ProgramDestroyEvent : DebugEvent, IDebugProgramDestroyEvent2
    {
        readonly uint exitCode;
        public Program Program { get; }

        public ProgramDestroyEvent(Program program, uint exitCode)
        : base(program.Engine, typeof(IDebugProgramDestroyEvent2).GUID,
              SYNCHRONOUS, program)
        {
            Program = program;
            this.exitCode = exitCode;
        }

        int IDebugProgramDestroyEvent2.GetExitCode(out uint exitCode)
        {
            exitCode = this.exitCode;
            return VSConstants.S_OK;
        }
    }

    class ThreadCreateEvent : DebugEvent, IDebugThreadCreateEvent2
    {
        public ThreadCreateEvent(Program program)
        : base(program.Engine, typeof(IDebugThreadCreateEvent2).GUID,
              ASYNCHRONOUS, program, program)
        { }
    }

    class ThreadDestroyEvent : DebugEvent, IDebugThreadDestroyEvent2
    {
        readonly uint exitCode;

        public ThreadDestroyEvent(Program program, uint exitCode)
        : base(program.Engine, typeof(IDebugThreadDestroyEvent2).GUID,
              SYNCHRONOUS, program, program)
        {
            this.exitCode = exitCode;
        }

        int IDebugThreadDestroyEvent2.GetExitCode(out uint exitCode)
        {
            exitCode = this.exitCode;
            return VSConstants.S_OK;
        }
    }

    class LoadCompleteEvent : DebugEvent, IDebugLoadCompleteEvent2
    {
        public LoadCompleteEvent(Program program)
        : base(program.Engine, typeof(IDebugLoadCompleteEvent2).GUID,
              STOPPING, program, program)
        { }
    }

    class EntryPointEvent : DebugEvent, IDebugEntryPointEvent2
    {
        public EntryPointEvent(Program program)
        : base(program.Engine, typeof(IDebugEntryPointEvent2).GUID,
              STOPPING, program, program)
        { }
    }

    class BreakpointBoundEvent : DebugEvent, IDebugBreakpointBoundEvent2
    {
        private Breakpoint Breakpoint { get; }
        public BreakpointBoundEvent(Breakpoint breakpoint)
        : base(breakpoint.Program.Engine, typeof(IDebugBreakpointBoundEvent2).GUID,
              ASYNCHRONOUS, breakpoint.Program, breakpoint.Program)
        {
            Breakpoint = breakpoint;
        }

        int IDebugBreakpointBoundEvent2.GetPendingBreakpoint(
            out IDebugPendingBreakpoint2 ppPendingBP)
        {
            ppPendingBP = Breakpoint.Parent;
            return VSConstants.S_OK;
        }

        int IDebugBreakpointBoundEvent2.EnumBoundBreakpoints(
            out IEnumDebugBoundBreakpoints2 ppEnum)
        {
            ppEnum = BoundBreakpointsEnum.Create(Breakpoint);
            return VSConstants.S_OK;
        }
    }

    class BreakpointEvent : DebugEvent, IDebugBreakpointEvent2
    {
        readonly IEnumDebugBoundBreakpoints2 boundBreakpoints;

        public BreakpointEvent(Program program,
            IEnumDebugBoundBreakpoints2 boundBreakpoints)
        : base(program.Engine, typeof(IDebugBreakpointEvent2).GUID,
              STOPPING, program, program)
        {
            this.boundBreakpoints = boundBreakpoints;
        }

        int IDebugBreakpointEvent2.EnumBreakpoints(out IEnumDebugBoundBreakpoints2 ppEnum)
        {
            ppEnum = boundBreakpoints;
            return VSConstants.S_OK;
        }
    }

    class StepCompleteEvent : DebugEvent, IDebugStepCompleteEvent2
    {
        public StepCompleteEvent(Program program)
        : base(program.Engine, typeof(IDebugStepCompleteEvent2).GUID,
              STOPPING, program, program)
        { }
    }

    class ExpressionEvaluationCompleteEvent : DebugEvent, IDebugExpressionEvaluationCompleteEvent2
    {
        private Expression Expression { get; }
        private Property Property { get; }

        public ExpressionEvaluationCompleteEvent(
            IDebugEventCallback2 callback,
            Expression expression,
            Property property)
            : base(expression.Engine, typeof(IDebugExpressionEvaluationCompleteEvent2).GUID,
                SYNCHRONOUS, expression.Program, expression.Program, callback)
        {
            Expression = expression;
            Property = property;
        }

        int IDebugExpressionEvaluationCompleteEvent2.GetExpression(out IDebugExpression2 ppExpr)
        {
            ppExpr = Expression;
            return VSConstants.S_OK;
        }

        int IDebugExpressionEvaluationCompleteEvent2.GetResult(out IDebugProperty2 ppResult)
        {
            ppResult = Property;
            return VSConstants.S_OK;
        }
    }

    class OutputStringEvent : DebugEvent, IDebugOutputStringEvent2
    {
        readonly string outputString;

        public OutputStringEvent(QmlEngine engine, string outputString)
        : base(engine, typeof(IDebugOutputStringEvent2).GUID, ASYNCHRONOUS)
        {
            this.outputString = outputString;
        }

        int IDebugOutputStringEvent2.GetString(out string pbstrString)
        {
            pbstrString = outputString;
            return VSConstants.S_OK;
        }
    }
}
