/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

namespace Corsinvest.ProxmoxVE.Report.Compliance.Models;

internal sealed record ClusterLogEntryInfo(long TimeUnix,
                                           string? Node,
                                           string? User,
                                           int Pri,
                                           string? Tag,
                                           string? Message);
