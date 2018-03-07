/****************************************************************************
**
** Copyright (C) 2016 The Qt Company Ltd.
** Contact: https://www.qt.io/licensing/
**
** This file is part of the Qt VS Tools.
**
** $QT_BEGIN_LICENSE:GPL-EXCEPT$
** Commercial License Usage
** Licensees holding valid commercial Qt licenses may use this file in
** accordance with the commercial license agreement provided with the
** Software or, alternatively, in accordance with the terms contained in
** a written agreement between you and The Qt Company. For licensing terms
** and conditions see https://www.qt.io/terms-conditions. For further
** information use the contact form at https://www.qt.io/contact-us.
**
** GNU General Public License Usage
** Alternatively, this file may be used under the terms of the GNU
** General Public License version 3 as published by the Free Software
** Foundation with exceptions as appearing in the file LICENSE.GPL3-EXCEPT
** included in the packaging of this file. Please review the following
** information to ensure the GNU General Public License requirements will
** be met: https://www.gnu.org/licenses/gpl-3.0.html.
**
** $QT_END_LICENSE$
**
****************************************************************************/

using System;
using System.IO;
using System.Text.RegularExpressions;

namespace QtProjectLib
{
        enum BuildType {
            Unknown,
            Static,
            Shared
        }

    /// <summary>
    /// A very simple reader for the qconfig.pri file.
    /// </summary>
    class QtConfig
    {
        public BuildType BuildType { get; private set; }

        public string LibInfix { get; private set; }

        public QtConfig(string qtdir)
        {
            LibInfix = string.Empty;

            var fi = new FileInfo(qtdir + "\\mkspecs\\qconfig.pri");
            if (!fi.Exists)
                return;

            var variableDef = new Regex(@"^\s*(\w+)\s*([\+\-]?\=)(.*)");
            try {
                using (var reader = new StreamReader(fi.FullName)) {
                    string line;
                    while ((line = reader.ReadLine()) != null) {
                        var match = variableDef.Match(line);
                        if (!match.Success || match.Groups.Count < 4)
                            continue;

                        var name = match.Groups[1].Value;
                        var oper = match.Groups[2].Value;
                        var data = match.Groups[3].Value;
                        if (name == "CONFIG") {
                            var values = data.Split(new char[] { ' ', '\t' },
                                StringSplitOptions.RemoveEmptyEntries);
                            foreach (var value in values) {
                                if (value == "static") {
                                    BuildType = BuildType.Static;
                                    break;
                                } else if (value == "shared") {
                                    BuildType = BuildType.Shared;
                                    break;
                                }
                            }
                        } else if (name == "QT_LIBINFIX") {
                            LibInfix = data.Trim();
                        }
                    }
                }
            } catch { }
        }
    }
}
