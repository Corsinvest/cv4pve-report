/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

namespace Corsinvest.ProxmoxVE.Report.Compliance.Models;

internal sealed record UserInfo(string Id,
                                bool Enabled,
                                long? ExpireUnix,
                                string? Email,
                                IReadOnlyList<TokenInfo> Tokens);

internal sealed record TokenInfo(string TokenId,
                                 long? ExpireUnix,
                                 bool PrivSeparated);
