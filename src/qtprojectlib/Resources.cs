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
    /// <summary>
    /// Summary description for Resources.
    /// </summary>
    public static class Resources
    {
        // export things
        public const string exportProHeader =
"# ----------------------------------------------------\r\n" +
"# This file is generated by the Qt Visual Studio Tools.\r\n" +
"# ------------------------------------------------------\r\n" +
"\r\n" +
"# This is a reminder that you are using a generated .pro file.\r\n" +
"# Remove it when you are finished editing this file.\r\n" +
"message(\"You are running qmake on a generated .pro file. This may not work!\")\r\n" +
"\r\n";

        public const string exportSolutionHeader =
"# ----------------------------------------------------\r\n" +
"# This file is generated by the Qt Visual Studio Tools.\r\n" +
"# ------------------------------------------------------\r\n" +
"\r\n" +
"# This is a reminder that you are using a generated .pro file.\r\n" +
"# Remove it when you are finished editing this file.\r\n" +
"message(\"You are running qmake on a generated .pro file. This may not work!\")\r\n" +
"\r\n";

        public const string exportPriHeader =
"# ----------------------------------------------------\r\n" +
"# This file is generated by the Qt Visual Studio Tools.\r\n" +
"# ------------------------------------------------------\r\n";

        public const string ec_Template = "(TEMPLATE) Template.";
        public const string ec_Translations = "(TRANSLATIONS) Translation files.";
        public const string ec_rcFile = "(win32:RC_FILE) .rc file on windows.";
        public const string ec_Target = "(TARGET) Target name.";
        public const string ec_DestDir = "(DESTDIR) Destination directory.";
        public const string ec_Qt = "(QT) Additional QT options.";
        public const string ec_Config = "(CONFIG) Additional CONFIG options.";
        public const string ec_IncludePath = "(INCLUDEPATH) Additional include paths.";
        public const string ec_Libs = "(LIBS) Additional library dependencies.";
        public const string ec_PrecompiledHeader = "(PRECOMPILED_HEADER) Using precompiled headers.";
        public const string ec_DependPath = "(DEPENDPATH) Additional paths the project depends on.";
        public const string ec_Include = "Included .pri files.";
        public const string ec_Resources = "(RESOURCES) Resource files.";
        public const string ec_ObjDir = "(OBJECTS_DIR) Location where obj files are placed.";
        public const string ec_MocDir = "(MOC_DIR) Location where moc files are placed.";
        public const string ec_UiDir = "(UI_DIR) Location where ui files are compiled to.";
        public const string ec_RccDir = "(RCC_DIR) Location where qrc files are compiled to.";
        public const string ec_Defines = "(DEFINES) Additional project defines.";

        public const string qtProjectKeyword = "Qt4VS";

        public const string uic4Command = "$(QTDIR)\\bin\\uic.exe";
        public const string moc4Command = "$(QTDIR)\\bin\\moc.exe";
        public const string rcc4Command = "$(QTDIR)\\bin\\rcc.exe";
        public const string lupdateCommand = "\\bin\\lupdate.exe";
        public const string lreleaseCommand = "\\bin\\lrelease.exe";

        // All defined paths have to be relative to the project directory!!!

        public const string resourceDir = "Resources";

        // If those directories do not equal to the project directory
        // they have to be added to the include directories for
        // compiling!
        public const string generatedFilesDir = "GeneratedFiles";

        public const string mocDirKeyword = "MocDir";
        public const string mocOptionsKeyword = "MocOptions";
        public const string uicDirKeyword = "UicDir";
        public const string rccDirKeyword = "RccDir";
        public const string lupdateKeyword = "lupdateOnBuild";
        public const string lupdateOptionsKeyword = "lupdateOptions";
        public const string lreleaseOptionsKeyword = "lreleaseOptions";
        public const string askBeforeCheckoutFileKeyword = "askBeforeCheckoutFile";
        public const string disableCheckoutFilesKeyword = "disableCheckoutFiles";
        public const string disableAutoMocStepsUpdateKeyword = "disableAutoMocStepsUpdate";

        public const string registryRootPath = "Digia";

#if VS2013
        public const string registryPackagePath = registryRootPath + "\\Qt5VS2013";
#elif VS2015
        public const string registryPackagePath = registryRootPath + "\\Qt5VS2015";
#elif (VS2017 || VS2019)
        public const string registryPackagePath = registryRootPath + "\\Qt5VS2017";
#else
#error Unknown Visual Studio version!
#endif
        public const string registryVersionPath = registryRootPath + "\\Versions";
    }
}
