/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

namespace Corsinvest.ProxmoxVE.Report.Models;

internal enum IssueSeverity { Info, Warning, Error }

internal sealed record Issue(
    IssueSeverity Severity,
    string Section,
    string Message,
    DateTime Timestamp,
    string LinkKey);
