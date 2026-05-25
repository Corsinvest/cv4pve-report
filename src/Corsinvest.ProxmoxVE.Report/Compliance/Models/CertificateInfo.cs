/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

namespace Corsinvest.ProxmoxVE.Report.Compliance.Models;

internal sealed record CertificateInfo(string Node,
                                       string FileName,
                                       string Subject,
                                       string Issuer,
                                       long NotAfterUnix);
