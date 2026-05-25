/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using Corsinvest.ProxmoxVE.Report.Compliance.Packs;

namespace Corsinvest.ProxmoxVE.Report.Compliance;

/// <summary>
/// Single place that maps <see cref="SettingsCompliance"/> toggles to concrete pack instances.
/// Keeps <c>ReportEngine</c> agnostic of the actual pack classes — adding a new standard
/// is one flag in settings plus one line here.
/// </summary>
internal static class ComplianceRegistry
{
    public static IEnumerable<ICompliancePack> EnabledPacks(SettingsCompliance settings)
    {
        if (settings.ISO27001) { yield return new Iso27001Pack(); }
        if (settings.NIS2) { yield return new Nis2Pack(); }
        if (settings.CIS) { yield return new CisControlsV8Pack(); }
        if (settings.AgID) { yield return new AgIdPack(); }
        if (settings.PCIDSS) { yield return new PciDssV4Pack(); }
        if (settings.GDPR) { yield return new GdprPack(); }
        if (settings.DORA) { yield return new DoraPack(); }
        if (settings.NISTCSF) { yield return new NistCsfV2Pack(); }
        if (settings.ISO27017) { yield return new Iso27017Pack(); }
    }
}
