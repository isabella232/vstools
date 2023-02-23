/***************************************************************************************************
 Copyright (C) 2023 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System;
using System.Reflection;

namespace QtVsTools.SyntaxAnalysis
{
    public abstract partial class RegExpr
    {
        public interface IRuleAction<T>
        {
            int NumOperands { get; }
            string SourceTokenId { get; set; }
            bool Execute(ref T productionObj, string capturedValue, params object[] operandObjs);
            MethodInfo AssertInfo { get; }
            MethodInfo ActionInfo { get; }
        }

        public class RuleAction<T, T1, T2> : IRuleAction<T>
        {
            public Delegate Assert { get; set; }
            public Delegate Action { get; set; }

            public MethodInfo AssertInfo => Assert?.Method;
            public MethodInfo ActionInfo => Action?.Method;

            public string SourceTokenId { get; set; }

            static readonly int _NumOperands
                = (typeof(T1) != typeof(Void) ? 1 : 0)
                + (typeof(T2) != typeof(Void) ? 1 : 0);

            public int NumOperands => _NumOperands;

            bool TestAssert(T prod, string value, T1 x, T2 y)
            {
                if (Assert == null)
                    return true;

                else if (Assert is CaptureCallback.Predicate)
                    return ((CaptureCallback.Predicate)Assert)(value);

                else if (Assert is UnaryCallback.Predicate<T1>)
                    return ((UnaryCallback.Predicate<T1>)Assert)(x);

                else if (Assert is UnaryCallback.Predicate<T, T1>)
                    return ((UnaryCallback.Predicate<T, T1>)Assert)(prod, x);

                else if (Assert is BinaryCallback.Predicate<T1, T2>)
                    return ((BinaryCallback.Predicate<T1, T2>)Assert)(x, y);

                else if (Assert is BinaryCallback.Predicate<T, T1, T2>)
                    return ((BinaryCallback.Predicate<T, T1, T2>)Assert)(prod, x, y);

                else
                    throw new InvalidOperationException("Incompatible assert callback.");
            }

            void RunAction(ref T prod, string value, T1 x, T2 y)
            {
                if (Action == null)
                    throw new InvalidOperationException("Missing action callback.");

                else if (Action is CaptureCallback.Create<T>)
                    prod = ((CaptureCallback.Create<T>)Action)(value);

                else if (Action is UnaryCallback.Create<T, T1>)
                    prod = ((UnaryCallback.Create<T, T1>)Action)(x);

                else if (Action is BinaryCallback.Create<T, T1, T2>)
                    prod = ((BinaryCallback.Create<T, T1, T2>)Action)(x, y);

                else if (Action is UnaryCallback.Transform<T, T1>)
                    prod = ((UnaryCallback.Transform<T, T1>)Action)(prod, x);

                else if (Action is BinaryCallback.Transform<T, T1, T2>)
                    prod = ((BinaryCallback.Transform<T, T1, T2>)Action)(prod, x, y);

                else if (Action is UnaryCallback.Update<T, T1>)
                    ((UnaryCallback.Update<T, T1>)Action)(prod, x);

                else if (Action is BinaryCallback.Update<T, T1, T2>)
                    ((BinaryCallback.Update<T, T1, T2>)Action)(prod, x, y);

                else if (Action is UnaryCallback.Error<T, T1>)
                    throw new ErrorException(((UnaryCallback.Error<T, T1>)Action)(prod, x));

                else if (Action is BinaryCallback.Error<T, T1, T2>)
                    throw new ErrorException(((BinaryCallback.Error<T, T1, T2>)Action)(prod, x, y));

                else
                    throw new InvalidOperationException("Incompatible action callback.");
            }

            bool GetOperand<TOperand>(out TOperand x, object[] operands, ref int idx)
            {
                x = default(TOperand);
                if (typeof(TOperand) == typeof(Void))
                    return true;
                if (operands.Length <= idx)
                    return false;
                object operandObj = operands[idx++];
                if (operandObj is TOperand operand) {
                    x = operand;
                    return true;
                }
                return false;
            }

            public bool Execute(ref T prod, string value, params object[] operands)
            {
                int idx = 0;
                if (!GetOperand(out T1 x, operands, ref idx))
                    return false;
                if (!GetOperand(out T2 y, operands, ref idx))
                    return false;

                if (!TestAssert(prod, value, x, y))
                    return false;

                RunAction(ref prod, value, x, y);
                return true;
            }
        }

        public class RuleAction<T, T1> : RuleAction<T, T1, Void> { }

        public class RuleAction<T> : RuleAction<T, Void, Void> { }

        ///////////////////////////////////////////////////////////////////////////////////////////
        #region Capture

        public static RuleAction<T> Capture<T>(
            CaptureCallback.Predicate p, CaptureCallback.Create<T> a)
        { return new RuleAction<T> { Assert = p, Action = a }; }

        public static RuleAction<T> Capture<T>(
            CaptureCallback.Create<T> a)
        { return new RuleAction<T> { Assert = null, Action = a }; }

        #endregion Capture

        ///////////////////////////////////////////////////////////////////////////////////////////
        #region Create

        public static RuleAction<T, T1> Create<T, T1>(
            string sid, UnaryCallback.Predicate<T1> p, UnaryCallback.Create<T, T1> a)
        { return new RuleAction<T, T1> { SourceTokenId = sid, Assert = p, Action = a }; }

        public static RuleAction<T, T1> Create<T, T1>(
            Enum sid, UnaryCallback.Predicate<T1> p, UnaryCallback.Create<T, T1> a)
        { return Create(sid.ToString(), p, a); }

        public static RuleAction<T, T1> Create<T, T1>(
            string sid, UnaryCallback.Create<T, T1> a)
        { return Create(sid, null, a); }

        public static RuleAction<T, T1> Create<T, T1>(
            Enum sid, UnaryCallback.Create<T, T1> a)
        { return Create(sid, null, a); }

        public static RuleAction<T, T1> Create<T, T1>(
            UnaryCallback.Predicate<T1> p, UnaryCallback.Create<T, T1> a)
        { return Create(string.Empty, p, a); }

        public static RuleAction<T, T1> Create<T, T1>(
            UnaryCallback.Create<T, T1> a)
        { return Create(string.Empty, null, a); }

        public static RuleAction<T, T1, T2> Create<T, T1, T2>(
            BinaryCallback.Predicate<T1, T2> p, BinaryCallback.Create<T, T1, T2> a)
        { return new RuleAction<T, T1, T2> { Assert = p, Action = a }; }

        public static RuleAction<T, T1, T2> Create<T, T1, T2>(
            BinaryCallback.Create<T, T1, T2> a)
        { return Create(null, a); }

        #endregion Create

        ///////////////////////////////////////////////////////////////////////////////////////////
        #region Transform

        public static RuleAction<T, T1> Transform<T, T1>(
            string sid, UnaryCallback.Predicate<T, T1> p, UnaryCallback.Transform<T, T1> a)
        { return new RuleAction<T, T1> { SourceTokenId = sid, Assert = p, Action = a }; }

        public static RuleAction<T, T1> Transform<T, T1>(
            Enum sid, UnaryCallback.Predicate<T, T1> p, UnaryCallback.Transform<T, T1> a)
        { return Transform(sid.ToString(), p, a); }

        public static RuleAction<T, T1> Transform<T, T1>(
            string sid, UnaryCallback.Transform<T, T1> a)
        { return Transform(sid, null, a); }

        public static RuleAction<T, T1> Transform<T, T1>(
            Enum sid, UnaryCallback.Transform<T, T1> a)
        { return Transform(sid, null, a); }

        public static RuleAction<T, T1> Transform<T, T1>(
            UnaryCallback.Transform<T, T1> a)
        { return Transform(string.Empty, null, a); }

        public static RuleAction<T, T1, T2> Transform<T, T1, T2>(
            BinaryCallback.Predicate<T, T1, T2> p, BinaryCallback.Transform<T, T1, T2> a)
        { return new RuleAction<T, T1, T2> { Assert = p, Action = a }; }

        public static RuleAction<T, T1, T2> Transform<T, T1, T2>(
            BinaryCallback.Transform<T, T1, T2> a)
        { return Transform(null, a); }

        #endregion Transform

        ///////////////////////////////////////////////////////////////////////////////////////////
        #region Update

        public static RuleAction<T, T1> Update<T, T1>(
            string sid, UnaryCallback.Predicate<T, T1> p, UnaryCallback.Update<T, T1> a)
        { return new RuleAction<T, T1> { SourceTokenId = sid, Assert = p, Action = a }; }

        public static RuleAction<T, T1> Update<T, T1>(
            Enum sid, UnaryCallback.Predicate<T, T1> p, UnaryCallback.Update<T, T1> a)
        { return Update(sid.ToString(), p, a); }

        public static RuleAction<T, T1> Update<T, T1>(
            string sid, UnaryCallback.Update<T, T1> a)
        { return Update(sid, null, a); }

        public static RuleAction<T, T1> Update<T, T1>(
            Enum sid, UnaryCallback.Update<T, T1> a)
        { return Update(sid, null, a); }

        public static RuleAction<T, T1> Update<T, T1>(
            UnaryCallback.Update<T, T1> a)
        { return Update(string.Empty, null, a); }

        public static RuleAction<T, T1, T2> Update<T, T1, T2>(
            BinaryCallback.Predicate<T, T1, T2> p, BinaryCallback.Update<T, T1, T2> a)
        { return new RuleAction<T, T1, T2> { Assert = p, Action = a }; }

        public static RuleAction<T, T1, T2> Update<T, T1, T2>(
            BinaryCallback.Update<T, T1, T2> a)
        { return Update(null, a); }

        #endregion Update

        ///////////////////////////////////////////////////////////////////////////////////////////
        #region Error

        public static RuleAction<T, T1> Error<T, T1>(
            string sid, UnaryCallback.Predicate<T, T1> p, UnaryCallback.Error<T, T1> a)
        { return new RuleAction<T, T1> { SourceTokenId = sid, Assert = p, Action = a }; }

        public static RuleAction<T, T1> Error<T, T1>(
            Enum sid, UnaryCallback.Predicate<T, T1> p, UnaryCallback.Error<T, T1> a)
        { return Error(sid.ToString(), p, a); }

        public static RuleAction<T, T1> Error<T, T1>(
            string sid, UnaryCallback.Error<T, T1> a)
        { return Error(sid, null, a); }

        public static RuleAction<T, T1> Error<T, T1>(
            Enum sid,
            UnaryCallback.Error<T, T1> a)
        { return Error(sid, null, a); }

        public static RuleAction<T, T1> Error<T, T1>(
            UnaryCallback.Predicate<T, T1> p, UnaryCallback.Error<T, T1> a)
        { return Error(string.Empty, p, a); }

        public static RuleAction<T, T1> Error<T, T1>(
            UnaryCallback.Error<T, T1> a)
        { return Error(string.Empty, null, a); }

        public static RuleAction<T, T1, T2> Error<T, T1, T2>(
            BinaryCallback.Predicate<T, T1, T2> p, BinaryCallback.Error<T, T1, T2> a)
        { return new RuleAction<T, T1, T2> { Assert = p, Action = a }; }

        public static RuleAction<T, T1, T2> Error<T, T1, T2>(
            BinaryCallback.Error<T, T1, T2> a)
        { return Error(null, a); }

        public class ErrorException : RegExprException
        { public ErrorException(string message = null) : base(message) { } }

        #endregion Error

        ///////////////////////////////////////////////////////////////////////////////////////////
        #region Callbacks

        public static class CaptureCallback
        {
            public delegate bool Predicate(string capture);
            public delegate T Create<T>(string capture);
        }

        public static class UnaryCallback
        {
            public delegate bool Predicate<T1>(T1 operand1);
            public delegate bool Predicate<T, T1>(T obj, T1 operand1);
            public delegate T Create<T, T1>(T1 operand1);
            public delegate T Transform<T, T1>(T obj, T1 operand1);
            public delegate void Update<T, T1>(T obj, T1 operand1);
            public delegate string Error<T, T1>(T obj, T1 operand1);
        }

        public static class BinaryCallback
        {
            public delegate bool Predicate<T1, T2>(T1 operand1, T2 operand2);
            public delegate bool Predicate<T, T1, T2>(T obj, T1 operand1, T2 operand2);
            public delegate T Create<T, T1, T2>(T1 operand1, T2 operand2);
            public delegate T Transform<T, T1, T2>(T obj, T1 operand1, T2 operand2);
            public delegate void Update<T, T1, T2>(T obj, T1 operand1, T2 operand2);
            public delegate string Error<T, T1, T2>(T obj, T1 operand1, T2 operand2);
        }

        #endregion Callbacks
    }
}
