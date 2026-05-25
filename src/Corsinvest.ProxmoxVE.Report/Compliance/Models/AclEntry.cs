/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

namespace Corsinvest.ProxmoxVE.Report.Compliance.Models;

internal sealed record AclEntry(string Path,
                                string UserOrGroup,
                                string Type,
                                string RoleId,
                                bool Propagate);
