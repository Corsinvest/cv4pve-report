/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

namespace Corsinvest.ProxmoxVE.Report.Compliance.Models;

internal sealed record ReplicationJobInfo(string Id,
                                          long? GuestVmId,
                                          string? Target,
                                          string? Schedule,
                                          bool Disabled);
