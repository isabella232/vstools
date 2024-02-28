/***************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0
***************************************************************************************************/

#pragma once

#include <QtCore/qglobal.h>

#ifndef BUILD_STATIC
# if defined(BIGPROJECTNNN_LIB)
#  define BIGPROJECTNNN_EXPORT Q_DECL_EXPORT
# else
#  define BIGPROJECTNNN_EXPORT Q_DECL_IMPORT
# endif
#else
# define BIGPROJECTNNN_EXPORT
#endif
