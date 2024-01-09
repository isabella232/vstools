/***************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace QtVsTools.Qml.Debug.V4
{
    using Json;

    [DataContract]
    class JsRef<TJsObject> : JsValue
        where TJsObject : JsRef<TJsObject>
    {
        protected JsRef()
        {
            Type = DataType.Object;
            Ref = null;
        }

        [DataMember(Name = "ref")]
        public int? Ref { get; set; }

        [DataMember(Name = "value", IsRequired = false)]
        public int? PropertyCount { get; set; }

        protected override bool? IsCompatible(JsValue obj)
        {
            System.Diagnostics.Debug.Assert(IsPrototype);

            if (base.IsCompatible(obj) == false)
                return false;

            if (obj is JsRef<TJsObject>)
                return true;
            return null;

        }
    }

    [DataContract]
    class JsObjectRef : JsRef<JsObjectRef>
    {
        protected override bool? IsCompatible(JsValue obj)
        {
            System.Diagnostics.Debug.Assert(IsPrototype);

            if (base.IsCompatible(obj) == false)
                return false;

            if (obj is JsObjectRef that)
                return ((JsRef<JsObjectRef>) that).Ref.HasValue;
            return null;
        }

        public new int Ref
        {
            get => base.Ref.HasValue ? base.Ref.Value : 0;
            set => base.Ref = value;
        }
    }

    [DataContract]
    class JsObject : JsRef<JsObject>
    {
        //  { "handle"              : <handle>,
        //    "type"                : "object",
        //    "className"           : <Class name, ECMA-262 property [[Class]]>,
        //    "constructorFunction" : {"ref":<handle>},
        //    "protoObject"         : {"ref":<handle>},
        //    "prototypeObject"     : {"ref":<handle>},
        //    "properties" : [ {"name" : <name>,
        //                      "ref"  : <handle>
        //                     },
        //                     ...
        //                   ]
        //  }
        protected override bool? IsCompatible(JsValue obj)
        {
            System.Diagnostics.Debug.Assert(IsPrototype);

            if (base.IsCompatible(obj) == false)
                return false;

            if (obj is JsObject that)
                return !that.Ref.HasValue;
            return null;

        }

        [DataMember(Name = "className")]
        public string ClassName { get; set; }

        [DataMember(Name = "constructorFunction")]
        public DeferredObject<JsValue> Constructor { get; set; }

        [DataMember(Name = "protoObject")]
        public DeferredObject<JsValue> ProtoObject { get; set; }

        [DataMember(Name = "prototypeObject")]
        public DeferredObject<JsValue> PrototypeObject { get; set; }

        [DataMember(Name = "properties")]
        public List<DeferredObject<JsValue>> Properties { get; set; }

        public IDictionary<string, JsValue> PropertiesByName =>
            Properties?.Where(x => x.Object != null && !string.IsNullOrEmpty(x.Object.Name))
                .Select(x => x.Object)
                .GroupBy(x => x.Name)
                .ToDictionary(x => x.Key, x => x.First());

        public bool IsArray =>
            !Properties.Where((x, i) => x.HasData && ((JsValue)x).Name != i.ToString()).Any();
    }

    [DataContract]
    class FunctionStruct : JsObject
    {
        //  { "handle" : <handle>,
        //    "type"                : "function",
        //    "className"           : "Function",
        //    "constructorFunction" : {"ref":<handle>},
        //    "protoObject"         : {"ref":<handle>},
        //    "prototypeObject"     : {"ref":<handle>},
        //    "name"                : <function name>,
        //    "inferredName"        : <inferred function name for anonymous functions>
        //    "source"              : <function source>,
        //    "script"              : <reference to function script>,
        //    "scriptId"            : <id of function script>,
        //    "position"            : <function begin position in script>,
        //    "line"                : <function begin source line in script>,
        //    "column"              : <function begin source column in script>,
        //    "properties" : [ {"name" : <name>,
        //                      "ref"  : <handle>
        //                     },
        //                     ...
        //                   ]
        //  }
        public FunctionStruct()
        {
            Type = DataType.Function;
        }

        [DataMember(Name = "inferredName")]
        public string InferredName { get; set; }

        [DataMember(Name = "source")]
        public string Source { get; set; }

        [DataMember(Name = "script")]
        public string Script { get; set; }

        [DataMember(Name = "scriptId")]
        public string ScriptId { get; set; }

        [DataMember(Name = "position")]
        public string Position { get; set; }

        [DataMember(Name = "line")]
        public int Line { get; set; }

        [DataMember(Name = "column")]
        public int Column { get; set; }
    }
}
