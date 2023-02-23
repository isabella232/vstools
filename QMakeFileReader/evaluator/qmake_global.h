/***************************************************************************************************
 Copyright (C) 2023 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/


#ifndef QMAKE_GLOBAL_H
#define QMAKE_GLOBAL_H

#include <qglobal.h>

#if defined(QMAKE_AS_LIBRARY)
#  if defined(QMAKE_LIBRARY)
#    define QMAKE_EXPORT Q_DECL_EXPORT
#  else
#    define QMAKE_EXPORT Q_DECL_IMPORT
#  endif
#else
#  define QMAKE_EXPORT
#endif

// Be fast even for debug builds
// MinGW GCC 4.5+ has a problem with always_inline putTok and putBlockLen
#if defined(__GNUC__) && !(defined(__MINGW32__) && __GNUC__ == 4 && __GNUC_MINOR__ >= 5)
# define ALWAYS_INLINE inline __attribute__((always_inline))
#elif defined(_MSC_VER)
# define ALWAYS_INLINE __forceinline
#else
# define ALWAYS_INLINE inline
#endif

#endif
