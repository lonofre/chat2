using System;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Chat;

public class GroupMembershipStore
{
    private readonly ConcurrentDictionary<string, HashSet<string>> _groups = new();
    private readonly ConcurrentDictionary<string, HashSet<string>> _userGroups = new();
    private readonly object _lock = new();

    public bool AddMember(string groupName, string username)
    {
        lock (_lock)
        {
            var members = _groups.GetOrAdd(groupName, _ => []);
            var groups  = _userGroups.GetOrAdd(username, _ => []);
            return members.Add(username) | groups.Add(groupName); // both sides
        }
    }

    public bool RemoveMember(string groupName, string username)
    {
        lock (_lock)
        {
            var removedFromGroup = _groups.TryGetValue(groupName, out var members)
                                   && members.Remove(username);
            var removedFromUser  = _userGroups.TryGetValue(username, out var groups)
                                   && groups.Remove(groupName);
            return removedFromGroup || removedFromUser;
        }
    }

    public IReadOnlyCollection<string> GetGroupsForUser(string username) =>
        _userGroups.TryGetValue(username, out var groups)
            ? groups.ToHashSet()
            : [];

    public IReadOnlyCollection<string> GetMembersOfGroup(string groupName) =>
        _groups.TryGetValue(groupName, out var members)
            ? members.ToHashSet()
            : [];
}