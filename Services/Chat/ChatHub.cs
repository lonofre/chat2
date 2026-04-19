using System;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace Chat;

public class ChatHub : Hub
{

    private static readonly ConcurrentDictionary<string, string> _connectionUsers = new();

    private readonly GroupMembershipStore _membership;

    public ChatHub(GroupMembershipStore membership)
    {
        _membership = membership;
    }

    public override async Task OnConnectedAsync()
    {

        var username = Context.GetHttpContext()?.Request.Query["username"].ToString();

        if (!string.IsNullOrWhiteSpace(username))
        {
            _connectionUsers[Context.ConnectionId] = username;

            var groups = _membership.GetGroupsForUser(username);
            foreach (var group in groups)
                await Groups.AddToGroupAsync(Context.ConnectionId, group);

            await Clients.Caller.SendAsync("SessionRestored", username, groups);
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _connectionUsers.TryRemove(Context.ConnectionId, out _);
        await base.OnDisconnectedAsync(exception);
    }

    public async Task SendMessage(string username, string message)
    {
        await Clients.All.SendAsync("ReceiveMessage", username, message);
    }

    public async Task JoinGroup(string username, string groupName)
    {
        if (!CanJoinGroup(username, groupName))
        {
            await Clients.Caller.SendAsync("JoinGroupDenied", groupName, "You are not allowed to join this group.");
            return;
        }

        _membership.AddMember(groupName, username);
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        await Clients.Group(groupName).SendAsync("UserJoinedGroup", groupName, username);
    }

    public async Task LeaveGroup(string username, string groupName)
    {
        _membership.RemoveMember(groupName, username);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        await Clients.Group(groupName).SendAsync("UserLeftGroup", groupName, username);
    }

    public async Task SendGroupMessage(string username, string groupName, string message)
    {
        await Clients.Group(groupName).SendAsync("ReceiveGroupMessage", groupName, username, message);
    }

    public async Task SendGroupMessageExcludeSender(string username, string groupName, string message)
    {
        await Clients.OthersInGroup(groupName).SendAsync("ReceiveGroupMessage", groupName, username, message);
    }

    private static bool CanJoinGroup(string username, string groupName)
    {
        return true;
    }
}