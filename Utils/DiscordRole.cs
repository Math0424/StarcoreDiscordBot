using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace StarcoreDiscordBot
{
    class DiscordRole
    {

        public Action<ulong> RoleIdUpdated;
        private string RoleName;
        private ulong _roleId;
        public ulong RoleID 
        { 
            get { return _roleId; }
            private set { _roleId = value; RoleIdUpdated?.Invoke(_roleId); }
        }

        public DiscordRole(string roleName, ulong roleId) 
        {
            RoleName = roleName;
            RoleID = roleId;
        }

        private async Task<SocketRole> GetRoleAsync()
        {
            SocketRole role = Bot.Instance.MainServer.GetRole(RoleID);
            if (role == null)
            {
                foreach (var r in Bot.Instance.MainServer.Roles)
                    if (r.Name == RoleName)
                        role = r;

                if (role != null)
                {
                    RoleID = role.Id;
                }
            }

            if (role == null)
            {
                Utils.Log("Creating role for " + RoleName);
                await Bot.Instance.MainServer.CreateRoleAsync(RoleName, isMentionable: true);
                await Task.Delay(1000); //delay 1000ms because above doesnt update the guild instantly
                foreach (var r in Bot.Instance.MainServer.Roles)
                    if (r.Name == RoleName)
                        role = r;

                RoleID = role.Id;
            }
            return role;
        }

        public async Task UpdateMembers(List<ulong> members)
        {
            var role = await GetRoleAsync();
            foreach (var m in role.Members)
            {
                members.Remove(m.Id);
            }
            if (members.Count != 0)
                Utils.Log($"Adding {members.Count} to {RoleName}");
            foreach (var i in members)
            {
                var user = Bot.Instance.MainServer.GetUser(i);
                if (user != null)
                {
                    Utils.Log("Added member to " + RoleName);
                    await user.AddRoleAsync(role);
                }
                else
                {
                    Utils.Log($"User {Utils.GetUsername(i)} not found in server!");
                }
            }
        }

        public async Task RemoveMember(ulong id)
        {
            var role = await GetRoleAsync();
            (Utils.GetUser(id) as IGuildUser)?.RemoveRoleAsync(role.Id);
        }

        public async Task RemoveMembers(List<ulong> members)
        {
            var role = await GetRoleAsync();
            foreach(var i in members)
            {
                (Utils.GetUser(i) as IGuildUser)?.RemoveRoleAsync(role.Id);
            }
        }

        public async Task AddMember(ulong id)
        {
            var role = await GetRoleAsync();
            (Utils.GetUser(id) as IGuildUser)?.AddRoleAsync(role.Id);
        }

        public void DeleteRole()
        {
            foreach (var r in Bot.Instance.MainServer.Roles)
            {
                if (r.Name == RoleName)
                {
                    Utils.Log("Deleting role " + RoleName);
                    RoleID = 0;
                    r.DeleteAsync();
                    return;
                }
            }
        }

    }
}
