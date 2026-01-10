using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;

namespace Backend.Hubs
{
    [Authorize] // Doar utilizatorii logați se pot conecta la socket
    public class ChatHub : Hub
    {
        // Frontend-ul apelează asta când intră pe pagina unui grup (ex: /groups/5)
        public async Task JoinGroup(string groupId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, groupId);
            // Opțional: Poți trimite o notificare că X a intrat, dar momentan păstrăm simplu
        }

        // Frontend-ul apelează asta când iese de pe pagină
        public async Task LeaveGroup(string groupId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupId);
        }
    }
}