/***************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0
***************************************************************************************************/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace QtVsTools.Qml.Debug
{
    using Core;
    using static Core.Common.Utils;

    internal class FileSystem : Concurrent
    {
        private Dictionary<string, string> qrcToLocalFileMap;

        public static FileSystem Create()
        {
            return new FileSystem
            {
                qrcToLocalFileMap = new Dictionary<string, string>()
            };
        }

        static readonly string[] KNOWN_EXTENSIONS = { ".qml", ".js" };

        private FileSystem()
        { }

        public void RegisterRccFile(string rccFilePath)
        {
            XDocument rccXml;
            try {
                var xmlText = File.ReadAllText(rccFilePath, Encoding.UTF8);
                var settings = new XmlReaderSettings
                {
                    DtdProcessing = DtdProcessing.Ignore
                };
                using (var reader = XmlReader.Create(new StringReader(xmlText), settings)) {
                    rccXml = XDocument.Load(reader);
                }
            } catch (Exception exception) {
                exception.Log();
                return;
            }

            var files = rccXml
                .Elements("RCC")
                .Elements("qresource")
                .SelectMany(x => x.Elements("file")
                    .Select(y => new
                    {
                        Prefix = x.Attribute("prefix"),
                        Alias = y.Attribute("alias"),
                        Path = HelperFunctions.ToNativeSeparator((string)y)
                    })
                    .Where(z => KNOWN_EXTENSIONS.Contains(Path.GetExtension(z.Path), CaseIgnorer)));

            var rccFileDir = Path.GetDirectoryName(rccFilePath);
            foreach (var file in files) {
                string qrcPath;
                if (file.Alias != null)
                    qrcPath = (string)file.Alias;
                else if (!Path.IsPathRooted(file.Path))
                    qrcPath = HelperFunctions.FromNativeSeparators(file.Path);
                else
                    continue;

                var qrcPathPrefix = file.Prefix != null ? (string)file.Prefix : "";
                if (!string.IsNullOrEmpty(qrcPathPrefix) && !qrcPathPrefix.EndsWith("/"))
                    qrcPathPrefix += Path.AltDirectorySeparatorChar;

                while (!string.IsNullOrEmpty(qrcPathPrefix) && qrcPathPrefix[0] == Path.AltDirectorySeparatorChar)
                    qrcPathPrefix = qrcPathPrefix.Substring(1);

                qrcToLocalFileMap[$"qrc:///{qrcPathPrefix}{qrcPath}"] =
                    HelperFunctions.ToNativeSeparator(Path.Combine(rccFileDir!, file.Path));
            }
        }

        private string FromQrcPath(string qrcPath)
        {
            // Normalize qrc path:
            //  - Only pre-condition is that qrcPath have a "qrc:" prefix
            //  - It might have any number of '/' after that, or none at all
            //  - A "qrc:///" prefix is required to match the mapping key
            //  - to enforce this, the "qrc:" prefix is removed, as well as any leading '/'
            //  - then the "normalized" prefix "qrc:///" is added
            if (!qrcPath.StartsWith("qrc:"))
                return default;
            qrcPath = qrcPath.Substring("qrc:".Length);

            while (!string.IsNullOrEmpty(qrcPath) && qrcPath[0] == Path.AltDirectorySeparatorChar)
                qrcPath = qrcPath.Substring(1);

            qrcPath = $"qrc:///{qrcPath}";
            return qrcToLocalFileMap.TryGetValue(qrcPath, out var filePath) ? filePath : default;
        }

        private static string FromFileUrl(string fileUrl)
        {
            var filePath = fileUrl.Substring("file://".Length);

            while (!string.IsNullOrEmpty(filePath) && filePath[0] == Path.AltDirectorySeparatorChar)
                filePath = filePath.Substring(1);

            return File.Exists(filePath) ? HelperFunctions.ToNativeSeparator(filePath) : default;
        }

        private static string FromFilePath(string filePath)
        {
            try {
                var fullPath = Path.GetFullPath(filePath);
                return File.Exists(fullPath) ? new Uri(fullPath).AbsoluteUri : default;
            } catch {
                return default;
            }
        }

        public string this[string path]
        {
            get
            {
                if (path.StartsWith("qrc:", IgnoreCase))
                    return FromQrcPath(path);
                if (path.StartsWith("file:", IgnoreCase))
                    return FromFileUrl(path);
                return FromFilePath(path);
            }
        }
    }
}
