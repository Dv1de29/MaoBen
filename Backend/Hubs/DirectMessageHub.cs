using Backend.Data;
using Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace Backend.Hubs
{
    [Authorize]
    public class DirectMessageHub : Hub
    {
        private readonly UserManager<ApplicationUser> _userManager;

        // Injectăm UserManager pentru a putea găsi ID-ul pe baza Username-ului primit din React
        public DirectMessageHub(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        /// <summary>
        /// Apelat din frontend: connection.invoke("JoinChat", "nume_utilizator_partener")
        /// Backend-ul caută ID-ul partenerului, generează ID-ul grupului și face join.
        /// </summary>
        public async Task JoinChat(string otherUsername)
        {
            var currentUserId = Context.UserIdentifier;
            if (string.IsNullOrEmpty(currentUserId)) return;

            // Căutăm userul target în baza de date
            var otherUser = await _userManager.FindByNameAsync(otherUsername);
            if (otherUser == null) return; // Sau poți arunca o eroare/log

            // Generăm numele grupului intern (ex: "conversation_GUID1_GUID2")
            var conversationId = GenerateConversationId(currentUserId, otherUser.Id);

            // Adăugăm conexiunea curentă în acest grup
            await Groups.AddToGroupAsync(Context.ConnectionId, conversationId);

            // Opțional: Putem notifica debug
            // await Clients.Caller.SendAsync("DebugInfo", $"Joined group: {conversationId}");
        }

        /// <summary>
        /// Apelat din frontend: connection.invoke("LeaveChat", "nume_utilizator_partener")
        /// </summary>
        public async Task LeaveChat(string otherUsername)
        {
            var currentUserId = Context.UserIdentifier;
            var otherUser = await _userManager.FindByNameAsync(otherUsername);

            if (string.IsNullOrEmpty(currentUserId) || otherUser == null) return;

            var conversationId = GenerateConversationId(currentUserId, otherUser.Id);

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, conversationId);
        }

        /// <summary>
        /// Frontend: connection.invoke("SendTypingIndicator", "nume_utilizator_partener", true/false)
        /// </summary>
        public async Task SendTypingIndicator(string otherUsername, bool isTyping)
        {
            var currentUserId = Context.UserIdentifier;
            var otherUser = await _userManager.FindByNameAsync(otherUsername);

            if (string.IsNullOrEmpty(currentUserId) || otherUser == null) return;

            var conversationId = GenerateConversationId(currentUserId, otherUser.Id);

            // Trimitem evenimentul către toți ceilalți din grup (exclusiv cel care scrie)
            await Clients.OthersInGroup(conversationId).SendAsync("UserTyping", new
            {
                Username = Context.User?.Identity?.Name, // Trimitem numele, e mai util in UI
                IsTyping = isTyping
            });
        }

        /// <summary>
        /// Helper: Generează ID-ul unic al conversației (alfabetic)
        /// Astfel, conversation_A_B este identic cu conversation_B_A
        /// </summary>
        private static string GenerateConversationId(string userId1, string userId2)
        {
            var sorted = new[] { userId1, userId2 }.OrderBy(x => x).ToArray();
            return $"conversation_{sorted[0]}_{sorted[1]}";
        }

        public override async Task OnConnectedAsync()
        {
            // Aici poți mapa conexiunea la un UserID în DB dacă vrei să știi cine e online
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            await base.OnDisconnectedAsync(exception);
        }
    }
}