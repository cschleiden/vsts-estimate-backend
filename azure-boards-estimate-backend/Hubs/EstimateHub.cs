﻿using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Estimate.Models;
using Microsoft.AspNetCore.SignalR;

namespace Estimate.Hubs
{
    public class EstimateHub : Hub
    {
        private static readonly ConcurrentDictionary<string, ConnectionInfo> ConnectionMapping = new ConcurrentDictionary<string, ConnectionInfo>();

        public override async Task OnConnectedAsync()
        {
            var httpContext = this.Context.GetHttpContext();
            var query = httpContext.Request.Query;

            var sessionId = query["sessionId"];
            var tfId = query["tfId"];

            await this.Groups.AddToGroupAsync(this.Context.ConnectionId, sessionId);

            var connectionInfo = new ConnectionInfo(sessionId, new Guid(tfId[0]));
            ConnectionMapping.AddOrUpdate(this.Context.ConnectionId, connectionInfo, (key, value) => connectionInfo);

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            if (EstimateHub.ConnectionMapping.TryRemove(this.Context.ConnectionId, out ConnectionInfo connectionInfo))
            {
                if (!EstimateHub.ConnectionMapping.Values.Any(x => x.TfId == connectionInfo.TfId))
                {
                    // No connection for this user open, notify others
                    await this.Broadcast(connectionInfo.SessionId, "left", connectionInfo.TfId);
                }
            }

            await base.OnDisconnectedAsync(exception);
        }

        public async Task Join(UserInfo userInfo, Guid sessionId)
        {
            await this.Groups.AddToGroupAsync(this.Context.ConnectionId, sessionId.ToString());
        }

        public async Task Leave(Guid sessionId)
        {
            await this.Groups.RemoveFromGroupAsync(this.Context.ConnectionId, sessionId.ToString());
        }        

        public async Task Broadcast(string sessionId, string action, object payload)
        {
            await this.Clients.Group(sessionId).SendAsync(action, payload);
        }
    }
}
