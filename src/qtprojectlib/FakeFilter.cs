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

namespace QtProjectLib
{
    public class FakeFilter
    {
        private string uniqueIdentifier = string.Empty;
        private string name = string.Empty;
        private string filter = string.Empty;
        private bool parseFiles = true;
        private bool sccFiles = true;

        public string UniqueIdentifier
        {
            get { return uniqueIdentifier; }
            set { uniqueIdentifier = value; }
        }

        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        public string Filter
        {
            get { return filter; }
            set { filter = value; }
        }

        public bool ParseFiles
        {
            get { return parseFiles; }
            set { parseFiles = value; }
        }

        public bool SCCFiles
        {
            get { return sccFiles; }
            set { sccFiles = value; }
        }
    }
}
