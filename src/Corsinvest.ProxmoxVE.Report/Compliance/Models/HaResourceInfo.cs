/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

namespace Corsinvest.ProxmoxVE.Report.Compliance.Models;

/// <summary>
/// Cluster HA resource. <see cref="Sid"/> is "vm:&lt;id&gt;" or "ct:&lt;id&gt;";
/// <see cref="VmId"/> extracts the numeric guest id when parsable.
/// </summary>
internal sealed record HaResourceInfo(string Sid,
                                      string Type,
                                      string? Group,
                                      string State,
                                      long? VmId);
