/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

namespace Corsinvest.ProxmoxVE.Report.Compliance.Models;

internal sealed record VmInfo(long VmId,
                              string Name,
                              string Node,
                              string Type,
                              string Status,
                              bool IsTemplate);
