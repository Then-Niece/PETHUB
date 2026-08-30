using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using PETHUB.Data;
using PETHUB.Services;

namespace PETHUB.Hubs
{
    [Authorize(Roles = "Member")]
    public class ChatHub : Hub
    {
        private readonly ApplicationDbContext _context;
        private readonly MessagingService _messagingService;

        public ChatHub( ApplicationDbContext context, MessagingService messagingService)
        {
            _context = context;
            _messagingService = messagingService;
        }


        // When a user connects, add them to a group based on their user ID
        public override async Task OnConnectedAsync()
        {
            var userId = Context.UserIdentifier;

            if (!string.IsNullOrEmpty(userId))
            {
                await Groups.AddToGroupAsync(
                    Context.ConnectionId,
                    $"user-{userId}"
                );
            }

            await base.OnConnectedAsync();
        }

        // When a user disconnects, remove them from the group based on their user ID
        public async Task JoinConversation(int conversationId)
        {
            var userId = Context.UserIdentifier;

            if (string.IsNullOrEmpty(userId))
            {
                return;
            }


            var isParticipant =
                await _context.ConversationParticipants
                    .AnyAsync(cp =>
                        cp.ConversationId == conversationId &&
                        cp.UserId == userId);


            if (!isParticipant)
            {
                return;
            }


            await Groups.AddToGroupAsync(
                Context.ConnectionId,
                $"conversation-{conversationId}"
            );
        }

        // When a user leaves a conversation, remove them from the group based on the conversation ID
        public async Task LeaveConversation(int conversationId)
        {
            await Groups.RemoveFromGroupAsync(
                Context.ConnectionId,
                $"conversation-{conversationId}"
            );
        }


        public async Task MarkAsRead(int conversationId, int messageId)
        {
            var userId =
                Context.UserIdentifier;


            if (string.IsNullOrEmpty(userId))
            {
                return;
            }


            await _messagingService
                .MarkMessageAsReadAsync(
                    conversationId,
                    messageId,
                    userId
                );
        }


        public async Task StartTyping(int conversationId)
        {
            var userId =
                Context.UserIdentifier;

            if (string.IsNullOrEmpty(userId))
            {
                return;
            }


            // Make sure the current user belongs
            // to this conversation.
            var isParticipant =
                await _context.ConversationParticipants
                    .AnyAsync(cp =>
                        cp.ConversationId == conversationId &&
                        cp.UserId == userId);


            if (!isParticipant)
            {
                return;
            }


            // Find the OTHER participant.
            var otherParticipantId =
                await _context.ConversationParticipants
                    .Where(cp =>
                        cp.ConversationId == conversationId &&
                        cp.UserId != userId)
                    .Select(cp =>
                        cp.UserId)
                    .FirstOrDefaultAsync();


            if (string.IsNullOrEmpty(otherParticipantId))
            {
                return;
            }


            // Send typing state directly to the other user's
            // personal SignalR group.
            await Clients
                .Group($"user-{otherParticipantId}")
                .SendAsync(
                    "UserTyping",
                    new
                    {
                        conversationId,
                        userId,
                        isTyping = true
                    }
                );
        }


        public async Task StopTyping(int conversationId)
        {
            var userId =
                Context.UserIdentifier;

            if (string.IsNullOrEmpty(userId))
            {
                return;
            }


            // Make sure the current user belongs
            // to this conversation.
            var isParticipant =
                await _context.ConversationParticipants
                    .AnyAsync(cp =>
                        cp.ConversationId == conversationId &&
                        cp.UserId == userId);


            if (!isParticipant)
            {
                return;
            }


            // Find the OTHER participant.
            var otherParticipantId =
                await _context.ConversationParticipants
                    .Where(cp =>
                        cp.ConversationId == conversationId &&
                        cp.UserId != userId)
                    .Select(cp =>
                        cp.UserId)
                    .FirstOrDefaultAsync();


            if (string.IsNullOrEmpty(otherParticipantId))
            {
                return;
            }


            await Clients
                .Group($"user-{otherParticipantId}")
                .SendAsync(
                    "UserTyping",
                    new
                    {
                        conversationId,
                        userId,
                        isTyping = false
                    }
                );
        }
    }
}