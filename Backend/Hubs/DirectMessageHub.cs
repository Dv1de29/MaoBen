using Backend.Data;
using Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace Backend.Hubs
{
    /// <summary>
    /// SignalR Hub pentru mesaje directe în timp real
    /// Frontend: 
    /// const connection = new HubConnectionBuilder()
    ///     .withUrl("http://localhost:5000/directMessageHub", {
    ///         accessTokenFactory: () => localStorage.getItem("token")
    ///     })
    ///     .withAutomaticReconnect()
    ///     .build();
    /// 
    /// await connection.start();
    /// connection.on("ReceiveDirectMessage", (message) => { /* update UI */ });
    /// </summary>
    [Authorize]
    public class DirectMessageHub : Hub
    {
        private readonly AppDbContext _context;

        public DirectMessageHub(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Apelat din frontend când utilizatorul deschide o conversație
        /// Frontend: connection.invoke("JoinConversation", recipientId)
        /// 
        /// Purpose: Adaugă utilizatorul în grup SignalR corespunzător conversației
        /// Group name format: "conversation_{userId1}_{userId2}" (lexicografic sortate pentru consistență)
        /// </summary>
        public async Task JoinConversation(string otherUserId)
        {
            var currentUserId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(currentUserId) || string.IsNullOrEmpty(otherUserId))
                return;

            // Grupul este sortat lexicografic pentru a asigura că ambii utilizatori sunt în același grup
            var conversationId = GenerateConversationId(currentUserId, otherUserId);
            await Groups.AddToGroupAsync(Context.ConnectionId, conversationId);
        }

        /// <summary>
        /// Apelat din frontend când utilizatorul pleacă dintr-o conversație
        /// Frontend: connection.invoke("LeaveConversation", recipientId)
        /// 
        /// Purpose: Îl elimină din grup SignalR
        /// </summary>
        public async Task LeaveConversation(string otherUserId)
        {
            var currentUserId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(currentUserId) || string.IsNullOrEmpty(otherUserId))
                return;

            var conversationId = GenerateConversationId(currentUserId, otherUserId);
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, conversationId);
        }

        /// <summary>
        /// Apelat din frontend pentru a indica faptul că utilizatorul scrie
        /// Frontend: connection.invoke("SendTypingIndicator", recipientId, true) // true = typing, false = stop
        /// 
        /// Purpose: Notifică cealaltă persoană că utilizatorul scrie în prezent
        /// </summary>
        public async Task SendTypingIndicator(string otherUserId, bool isTyping)
        {
            var currentUserId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(currentUserId) || string.IsNullOrEmpty(otherUserId))
                return;

            var conversationId = GenerateConversationId(currentUserId, otherUserId);
            
            // Notifică toți din grup despre typing indicator (exceptând expeditorul)
            await Clients.Group(conversationId).SendAsync("UserTyping", new
            {
                UserId = currentUserId,
                IsTyping = isTyping
            });
        }

        /// <summary>
        /// Apelat de backend controller după ce mesajul e salvat în DB
        /// Nu se apelează din frontend direct - controller-ul apelează asta
        /// </summary>
        public async Task NotifyNewMessage(string conversationId, object messageDto)
        {
            // Trimite mesajul în timp real grupului corespunzător conversației
            await Clients.Group(conversationId).SendAsync("ReceiveDirectMessage", messageDto);
        }

        /// <summary>
        /// Genereaza ID-ul conversației în mod consistent
        /// Sortează ID-urile pentru a asigura că ambii utilizatori sunt în același grup
        /// </summary>
        private static string GenerateConversationId(string userId1, string userId2)
        {
            var sorted = new[] { userId1, userId2 }.OrderBy(x => x).ToArray();
            return $"conversation_{sorted[0]}_{sorted[1]}";
        }

        public override async Task OnConnectedAsync()
        {
            await base.OnConnectedAsync();
            // Optional: log connection
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            await base.OnDisconnectedAsync(exception);
            // Optional: clean up
        }
    }
}