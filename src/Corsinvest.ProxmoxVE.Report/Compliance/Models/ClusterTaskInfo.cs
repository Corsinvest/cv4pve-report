/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

namespace Corsinvest.ProxmoxVE.Report.Compliance.Models;

internal sealed record ClusterTaskInfo(string Type,
                                       string Node,
                                       string? User,
                                       string? Status,
                                       bool StatusOk,
                                       long StartTimeUnix,
                                       long EndTimeUnix);
