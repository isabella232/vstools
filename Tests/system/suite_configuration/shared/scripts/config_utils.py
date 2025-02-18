####################################################################################################
# Copyright (C) 2024 The Qt Company Ltd.
# SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0
####################################################################################################

# -*- coding: utf-8 -*-

source("../../shared/utils.py")

import os
import sys

from newprojectdialog import NewProjectDialog
from testsection import TestSection
import names


def readQtDirs():
    dirString = os.getenv("SQUISH_VSTOOLS_QTDIRS")
    if not dirString:
        return []
    uniquePaths = set()
    qtDirs = []
    for current in dirString.split(";"):
        loweredPath = current.lower()
        if loweredPath in uniquePaths:
            continue
        uniquePaths.add(loweredPath)
        qtDirs.append({"path": current,
                       "name": current.rsplit(":")[-1].strip("\\").replace("\\", "_")})
    return qtDirs


def typeToEdit(editId, text):
    edit = waitForObject(editId)
    mouseClick(edit)
    type(edit, text)
    snooze(1)
    waitFor(lambda: waitForObject(editId).text == text)


def tableCell(col, row):
    return {"column": col, "container": names.dataGrid_Table, "row": row, "type": "TableCell"}


def tableCellEdit(col, row):
    return {"container": tableCell(col, row), "type": "Label"}


def configureQtVersions(qtDirs, withTests=False):
    openVsToolsMenu()
    mouseClick(waitForObject(names.pART_Popup_Qt_Versions_MenuItem))
    tableRows = waitForObjectExists(names.dataGrid_Table).rowCount
    if withTests:
        test.compare(tableRows, 0,
                     "The table should have zero lines before adding Qt versions.")
    if tableRows != 0:
        test.fatal("Unexpected table shown. Probably there is either an unexpected configuration "
                   "or the UI changed.")
        clickButton(waitForObject(names.options_Cancel_Button))
        return False
    for i, qtDir in enumerate(qtDirs):
        clickButton(waitForObject(names.add_Button))
        typeToEdit(names.location_Edit, qtDir["path"])
        typeToEdit(names.name_Edit, qtDir["name"])
        if withTests:
            dirsAdded = i + 1
            test.compare(waitForObjectExists(names.dataGrid_Table).rowCount, dirsAdded,
                         "The table should have %d lines after adding %d Qt versions."
                         % (dirsAdded, dirsAdded))
    clickButton(waitForObject(names.options_OK_Button))
    waitFor(lambda: not object.exists(names.options_Dialog))
    return True


def clearQtVersions():
    openVsToolsMenu()
    mouseClick(waitForObject(names.pART_Popup_Qt_Versions_MenuItem))
    qtVersionCount = waitForObjectExists(names.dataGrid_Table).rowCount
    if qtVersionCount > 1:
        # default kit can only be removed if other exist, select other
        mouseClick(waitForObject(tableCell(1, 1)))
    for _ in range(qtVersionCount):
        clickButton(waitForObject(names.remove_Button))
    snooze(1)
    clickButton(waitForObject(names.options_OK_Button))
    waitFor(lambda: not object.exists(names.options_Dialog))


def getExpectedName(templateName):
    if templateName == "Qt ActiveQt Server":
        return "ActiveQtServer"
    elif templateName == "Qt Designer Custom Widget":
        return "QtDesignerWidget"
    elif templateName == "Qt Empty Application":
        return "QtApplication"
    elif templateName == "Qt Test Application":
        return "QtTest"
    else:
        return templateName.replace(" ", "")


# Function which opens all "New Project" wizards from Qt VS Tools, one after the other. It doesn't
# run any tests itself, but it runs a callback function for each page of all the wizards. These
# functions should contain tests for the respective page.
# funcNewProjectDialog: Function run on MSVS' own "New Project" dialog. Parameters are strings:
#                       - the project template, e.g. "Qt Empty Application"
#                       - the expected name of the new project, without index, e.g. "QtApplication"
# funcPage1: Function run on the first page of the Qt VS Tools' wizard. Parameters are strings:
#            - the expected greeting text on top of the page
#            - the project template, e.g. "Qt Empty Application"
# funcPage2: Function run on the second page of the Qt VS Tools' wizard. Parameters are:
#            - a string containing the expected greeting text on top of the page
#            - a list of the configured Qt versions. Each Qt version is a dict containing two
#              values: "path" is the file system path to the Qt version, "name" is the name
#              configured for it
# funcPage3: Function run on the third page of the Qt VS Tools' wizard, in case it has one.
#            Parameters are strings:
#            - the project's template name
#            - the expected greeting text on top of the page
#            - the name of the project
# setupQtVersions: Optional boolean value whether the function shall configure Qt versions read
#                  from the environment in the VS Tools' settings before opening any wizard and
#                  clear them after the tests (True, default) or not change Qt version settings
#                  at all (False)


def testAllQtWizards(funcNewProjectDialog=None, funcPage1=None, funcPage2=None, funcPage3=None,
                     setupQtVersions=True):
    qtDirs = readQtDirs()
    if not qtDirs:
        test.fatal("No Qt versions known", "Did you set SQUISH_VSTOOLS_QTDIRS correctly?")
        return
    startApp()
    if setupQtVersions and not configureQtVersions(qtDirs):
        closeMainWindow()
        return

    with NewProjectDialog() as dialog:
        dialog.filterForQtProjects()
        for listItem, templateName in dialog.getListedTemplates():
            with TestSection(templateName):
                mouseClick(waitForObject(listItem))
                expectedName = getExpectedName(templateName)
                clickButton(waitForObject(names.microsoft_Visual_Studio_Next_Button))
                if funcNewProjectDialog:
                    funcNewProjectDialog(templateName, expectedName)
                projectName = waitForObjectExists(names.msvs_Project_name_Edit).text
                devEnvContext = currentApplicationContext()
                clickButton(waitForObject(names.microsoft_Visual_Studio_Create_Button))
                if not waitFor(lambda: object.exists(names.qt_Wizard_Window), 10000):
                    # Sometimes, a "Creating project..." dialog appears and creates
                    # a second app context. Explicitly set the wanted context.
                    setApplicationContext(devEnvContext)
                try:
                    expectedText = "Welcome to the %s Wizard" % templateName
                    if funcPage1:
                        funcPage1(expectedText, templateName)
                    clickButton(waitForObject(names.qt_Wizard_Next_Button))
                    if funcPage2:
                        funcPage2(expectedText, qtDirs)
                    if setupQtVersions:
                        if templateName in ["Qt ActiveQt Server",
                                            "Qt Class Library",
                                            "Qt Designer Custom Widget",
                                            "Qt Test Application",
                                            "Qt Widgets Application"]:
                            clickButton(waitForObject(names.qt_Wizard_Next_Button))
                            if funcPage3:
                                funcPage3(templateName, expectedText, projectName)
                        test.verify(findObject(names.qt_Wizard_Finish_Button).enabled)
                    else:
                        test.verify(not findObject(names.qt_Wizard_Finish_Button).enabled)
                    test.verify(not findObject(names.qt_Wizard_Next_Button).enabled)
                except:
                    eInfo = sys.exc_info()
                    test.fatal("Exception caught", "%s: %s" % (eInfo[0].__name__, eInfo[1]))
                finally:
                    # Cannot finish because of SQUISH-15876
                    try:
                        clickButton(waitForObject(names.qt_Wizard_Cancel_Button, 2000))
                    except:
                        test.warning("Could not click wizard's 'Cancel' button. "
                                     "Falling back to using Escape key.")
                        nativeType("<Escape>")
            dialog.goBack()

    if setupQtVersions:
        clearQtVersions()
    closeMainWindow()
