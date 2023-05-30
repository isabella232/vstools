/***************************************************************************************************
 Copyright (C) 2023 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Task = System.Threading.Tasks.Task;

namespace QtVsTools.Core.CMake
{
    public partial class CMakeProject : Concurrent<CMakeProject>
    {
        private enum QtStatus { False, NullPresets, ConversionPending, True }

        private QtStatus Status { get; set; } = QtStatus.False;

        private async Task CheckQtStatusAsync()
        {
            await GetAsync("CheckQtStatus");
            if (ActiveProject != this)
                return;
            try {
                await StateMachineAsync();
            } catch (Exception ex) {
                ex.Log();
            }
            Release("CheckQtStatus");
        }

        private async Task StateMachineAsync()
        {
            var lists = ListFiles();

            switch (Status) {
            case QtStatus.False:
            case QtStatus.NullPresets:
                if (HasQtReference(lists))
                    Status = TryLoadPresets() ? QtStatus.True : QtStatus.ConversionPending;
                break;
            case QtStatus.ConversionPending:
                return;
            case QtStatus.True:
                if (!HasQtReference(lists))
                    Status = QtStatus.False;
                else if (!TryLoadPresets())
                    Status = QtStatus.ConversionPending;
                break;
            }

            switch (Status) {
            case QtStatus.False:
                return;
            case QtStatus.NullPresets:
                try {
                    if (File.ReadAllText(PresetsPath) == NullPresetsText)
                        File.Delete(PresetsPath);
                    if (File.ReadAllText(UserPresetsPath) == NullPresetsText)
                        File.Delete(UserPresetsPath);
                } catch (Exception ex) {
                    ex.Log();
                }
                Status = QtStatus.False;
                return;
            case QtStatus.ConversionPending:
                if (!IsAutoConfigurable()) {
                    await ShowConversionConfirmationAsync();
                    return;
                }
                Status = QtStatus.True;
                break;
            case QtStatus.True:
                break;
            }

            await RefreshAsync();
        }

        private async Task RefreshAsync()
        {
            VerifyChecksums();
            CheckQtPresets();
            CheckQtVersions();
            CheckVisiblePresets();
            if (SaveIfRequired() && Index != null)
                await Index.InvalidateFileScannerCache();
        }

        private bool SaveIfRequired()
        {
            var isDirty = false;
            var records = Presets.Descendants()
                .Union(UserPresets.Descendants())
                .Append(Presets)
                .Append(UserPresets)
                .Select(x => new
                {
                    Self = x as JObject,
                    Info = RecordInfo(x as JObject)
                })
                .Select(x => new
                {
                    x.Self,
                    x.Info,
                    Checksum = x.Info?.Value["checksum"]
                })
                .Where(x => x.Info != null)
                .ToList();
            foreach (var record in records) {
                var oldChecksum = record.Checksum?.Value<string>();
                var newChecksum = EvalChecksum(record.Self);
                if (oldChecksum == newChecksum)
                    continue;
                isDirty = true;
                record.Info.Value["checksum"] = newChecksum;
            }
            if (isDirty) {
                File.WriteAllText(PresetsPath, Presets.ToString(Formatting.Indented));
                File.WriteAllText(UserPresetsPath, UserPresets.ToString(Formatting.Indented));
            }
            return isDirty;
        }
    }
}
