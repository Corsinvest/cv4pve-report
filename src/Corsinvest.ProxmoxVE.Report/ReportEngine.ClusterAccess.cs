/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using Corsinvest.ProxmoxVE.Api.Extension;
using Corsinvest.ProxmoxVE.Report.Writers;

namespace Corsinvest.ProxmoxVE.Report;

public partial class ReportEngine
{
    private async Task<int> AddClusterAccessDataAsync()
    {
        if (!settings.Cluster.Include) { return 0; }

        using var sw = _writer.AddSection("Cluster Access");

        ReportGlobal("Cluster Access: Fetching data");

        var usersTask = client.Access.Users.GetAsync(full: true).ToSafeEnum(_issues, "Cluster Access", LinkKey.ClusterAccess);
        var tfaTask = client.Access.Tfa.GetAsync().ToSafeEnum(_issues, "Cluster Access", LinkKey.ClusterAccess);
        var groupsTask = client.Access.Groups.GetAsync().ToSafeEnum(_issues, "Cluster Access", LinkKey.ClusterAccess);
        var rolesTask = client.Access.Roles.GetAsync().ToSafeEnum(_issues, "Cluster Access", LinkKey.ClusterAccess);
        var aclTask = client.Access.Acl.GetAsync().ToSafeEnum(_issues, "Cluster Access", LinkKey.ClusterAccess);
        var domainsTask = client.Access.Domains.GetAsync().ToSafeEnum(_issues, "Cluster Access", LinkKey.ClusterAccess);

        await Task.WhenAll(usersTask, tfaTask, groupsTask, rolesTask, aclTask, domainsTask);

        sw.AddTable("Users",
                    usersTask.Result.Select(a => new
                    {
                        a.Id,
                        EnableFlag = ToX(a.Enable),
                        a.Firstname,
                        a.Lastname,
                        a.Email,
                        a.Groups,
                        a.Keys,
                        TotpLockedFlag = ToX(a.TotpLocked),
                        TfaLockedUntil = FromUnixTime(a.TfaLockedUntil ?? 0),
                        Expire = FromUnixTime(a.Expire),
                        CommentWrap = a.Comment,
                    }));

        sw.AddTable("API Tokens",
                    usersTask.Result.SelectMany(a => (a.Tokens ?? []).Select(t => new
                    {
                        User = a.Id,
                        TokenId = t.Id,
                        Expire = FromUnixTime(t.Expire),
                        PrivSeparatedFlag = ToX(t.Privsep == 1),
                        CommentWrap = t.Comment
                    })));

        sw.AddTable("Two-Factor Authentication",
                    tfaTask.Result.Select(t => new
                    {
                        User = t.UserId,
                        TfaTypes = string.Join(", ", t.Entries?.Select(e => e.Type).Distinct() ?? []),
                        TfaCount = t.Entries?.Count() ?? 0
                    }));

        sw.AddTable("Groups",
                    groupsTask.Result.Select(a => new
                    {
                        a.Id,
                        a.Users,
                        a.Comment
                    }));

        sw.AddTable("Roles",
                    rolesTask.Result.Select(a => new
                    {
                        a.Id,
                        Privileges = ToNewLine(a.Privileges),
                        SpecialFlag = ToX(a.Special == 1)
                    }));

        sw.AddTable("ACL",
                    aclTask.Result.Select(a => new
                    {
                        a.Path,
                        UsersOrGroup = a.UsersGroupid,
                        a.Type,
                        Id = a.Roleid,
                        PropagateFlag = ToX(a.Propagate == 1),
                    }));

        sw.AddTable("Domains",
                    domainsTask.Result.Select(a => new
                    {
                        a.Realm,
                        a.Type,
                        a.Tfa,
                        a.Comment
                    }));

        return usersTask.Result.Count + aclTask.Result.Count + groupsTask.Result.Count + rolesTask.Result.Count + domainsTask.Result.Count;
    }
}
