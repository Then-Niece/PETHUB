using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using PETHUB.Data;
using PETHUB.Helpers;
using PETHUB.Models;
using PETHUB.ViewModels;
using Microsoft.AspNetCore.SignalR;
using PETHUB.Hubs;

namespace PETHUB.Services
{
    public class MessagingService
    {
        private readonly ApplicationDbContext _context;

        private readonly UserManager<ApplicationUser> _userManager;

        private readonly IHubContext<ChatHub> _hubContext;

        public MessagingService(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IHubContext<ChatHub> hubContext)
        {
            _context = context;
            _userManager = userManager;
            _hubContext = hubContext;
        }


        // =========================================================
        // Get or create a marketplace conversation between the current user and the listing owner
        // =========================================================
        public async Task<Conversation?> GetOrCreateMarketplaceConversationAsync(int listingId, string currentUserId)
        {
            // 1. Find the listing and its owner
            var listing = await _context.Listings
                .Include(l => l.Member)
                .FirstOrDefaultAsync(l => l.ListingId == listingId);

            if (listing == null || listing.Member == null || listing.MemberId == null)
            {
                return null;
            }

            // 2. Prevent users from messaging themselves
            if (listing.MemberId == currentUserId)
            {
                return null;
            }

            // 3. Find the current user
            var currentUser = await _userManager.FindByIdAsync(currentUserId);

            if (currentUser == null)
            {
                return null;
            }

            // 4. Both users must be Members
            var currentUserIsMember = await _userManager.IsInRoleAsync(currentUser, "Member");

            var listingOwnerIsMember = await _userManager.IsInRoleAsync(listing.Member, "Member");

            if (!currentUserIsMember || !listingOwnerIsMember)
            {
                return null;
            }

            // 5. Check if a conversation already exists
            var existingConversation = await _context.Conversations
                .Include(c => c.Participants)
                .FirstOrDefaultAsync(c =>
                    c.Type == ConversationType.Marketplace &&
                    c.ListingId == listingId &&
                    c.Participants.Any(p => p.UserId == currentUserId) &&
                    c.Participants.Any(p => p.UserId == listing.MemberId));

            if (existingConversation != null)
            {
                return existingConversation;
            }

            // 6. Create a new conversation
            var conversation = new Conversation
            {
                Type = ConversationType.Marketplace,
                ContextTitle = listing.Title,
                ListingId = listing.ListingId
            };

            // 7. Add both members as participants
            conversation.Participants.Add(
                new ConversationParticipant
                {
                    UserId = currentUserId
                });

            conversation.Participants.Add(
                new ConversationParticipant
                {
                    UserId = listing.MemberId
                });

            // 8. Save the conversation
            _context.Conversations.Add(conversation);

            await _context.SaveChangesAsync();

            return conversation;
        }

        // =========================================================    
        // Get the messages page view model for the current user
        // =========================================================
        public async Task<MessagesIndexViewModel> GetMessagesPageAsync(string currentUserId, int? selectedConversationId, bool showArchived = false)
        {
            var conversations = await _context.Conversations
                .Where(c =>
                    c.Participants.Any(p =>
                        p.UserId == currentUserId &&
                        p.IsArchived == showArchived))

                .Include(c => c.Participants)
                    .ThenInclude(p => p.User)

                .Include(c => c.Messages)
                    .ThenInclude(m => m.Images)

                .Include(c => c.Listing)

                .Include(c => c.LostFound)

                .ToListAsync();

            var viewModel = new MessagesIndexViewModel
            {
                CurrentUserId = currentUserId,
                IsArchiveView = showArchived
            };

            foreach (var conversation in conversations)
            {
                var otherParticipant = conversation.Participants
                    .FirstOrDefault(p => p.UserId != currentUserId);

                if (otherParticipant == null)
                {
                    continue;
                }

                var currentParticipant = conversation.Participants
                    .FirstOrDefault(p => p.UserId == currentUserId);

                var lastMessage = conversation.Messages
                    .OrderByDescending(m => m.CreatedAt)
                    .FirstOrDefault();

                string? lastMessagePreview = null;

                if (lastMessage != null)
                {
                    // Prefer actual text if the message contains text
                    if (!string.IsNullOrWhiteSpace(lastMessage.Content))
                    {
                        lastMessagePreview = lastMessage.Content;
                    }

                    // Image-only message
                    else if (lastMessage.Images.Any())
                    {
                        var imageCount = lastMessage.Images.Count;

                        var sentByCurrentUser =
                            lastMessage.SenderId ==
                            currentUserId;


                        if (imageCount == 1)
                        {
                            lastMessagePreview =
                                sentByCurrentUser
                                    ? "You sent a photo"
                                    : "Sent a photo";
                        }
                        else
                        {
                            lastMessagePreview =
                                sentByCurrentUser
                                    ? $"You sent {imageCount} photos"
                                    : $"Sent {imageCount} photos";
                        }
                    }
                }

                var unreadCount = 0;

                if (currentParticipant != null)
                {
                    unreadCount = conversation.Messages.Count(m =>
                        m.SenderId != currentUserId &&
                        (!currentParticipant.LastReadMessageId.HasValue ||
                         m.MessageId > currentParticipant.LastReadMessageId.Value));
                }



                viewModel.Conversations.Add(new ConversationListItemViewModel
                {
                    ConversationId = conversation.ConversationId,

                    OtherUserId = otherParticipant.UserId,

                    OtherUserName = $"{otherParticipant.User.FirstName} {otherParticipant.User.LastName}".Trim(),

                    OtherUserProfilePicture = otherParticipant.User.ProfilePicturePath,

                    ContextTitle = conversation.ContextTitle,

                    ContextType = conversation.Type.ToString(),

                    LastMessage = lastMessagePreview,

                    LastMessageAt = lastMessage?.CreatedAt,

                    UnreadCount = unreadCount

                });
            }

            viewModel.Conversations = viewModel.Conversations
                .OrderByDescending(c => c.LastMessageAt)
                .ThenByDescending(c => c.ConversationId)
                .ToList();

            if (selectedConversationId.HasValue)
            {
                var selectedConversation = conversations
                    .FirstOrDefault(c =>
                        c.ConversationId == selectedConversationId.Value);

                if (selectedConversation != null)
                {
                    var otherParticipant = selectedConversation.Participants
                        .FirstOrDefault(p => p.UserId != currentUserId);

                    if (otherParticipant != null)
                    {
                        viewModel.SelectedConversation =
                            new ConversationViewModel
                            {
                                ConversationId = selectedConversation.ConversationId,

                                OtherUserId = otherParticipant.UserId,

                                OtherUserName = $"{otherParticipant.User.FirstName} {otherParticipant.User.LastName}".Trim(),

                                OtherUserProfilePicture = otherParticipant.User.ProfilePicturePath,

                                ContextTitle = selectedConversation.ContextTitle,

                                ContextType = selectedConversation.Type.ToString(),

                                ListingId = selectedConversation.ListingId,

                                LostFoundId = selectedConversation.LostFoundId,

                                OtherParticipantLastReadMessageId = otherParticipant.LastReadMessageId,

                                ContextAvailable = selectedConversation.Type == ConversationType.Marketplace
                                        ? selectedConversation.Listing != null
                                        : selectedConversation.LostFound != null,

                                Messages = selectedConversation.Messages
                                    .OrderBy(m => m.CreatedAt)
                                    .Select(m => new MessageViewModel
                                    {
                                        MessageId = m.MessageId,
                                        SenderId = m.SenderId,
                                        Content = m.Content,
                                        ImagePaths = m.Images
                                            .Select(i => i.ImagePath)
                                            .ToList(),
                                        CreatedAt = m.CreatedAt,
                                        IsMine = m.SenderId == currentUserId
                                    })
                                    .ToList()
                            };
                    }
                }
            }

            return viewModel;
        }


        public async Task<Message?> SendMessageAsync(int conversationId, string senderId, string? content, List<IFormFile>? imageFiles)
        {
            var isParticipant = await _context.ConversationParticipants
                .AnyAsync(cp =>
                    cp.ConversationId == conversationId &&
                    cp.UserId == senderId);

            if (!isParticipant)
            {
                return null;
            }


            var hasText =
                !string.IsNullOrWhiteSpace(content);

            var filesToProcess = imageFiles?
                .Where(file => file != null && file.Length > 0)
                .Take(5)
                .ToList()
                ?? new List<IFormFile>();


            // No text and no selected files
            if (!hasText && filesToProcess.Count == 0)
            {
                return null;
            }


            /*
             * Temporarily create the message first
             * so SQL can generate MessageId.
             */
            var message = new Message
            {
                ConversationId = conversationId,
                SenderId = senderId,

                Content = hasText
                    ? content!.Trim()
                    : null,

                MessageType = MessageType.Text,

                CreatedAt = DateTime.UtcNow
            };


            _context.Messages.Add(message);

            await _context.SaveChangesAsync();


            /*
             * Save valid images.
             */
            var messageImages =
                await ImageHelper.SaveImagesAsync(
                    filesToProcess,
                    message.MessageId,

                    (messageId, imagePath) =>
                        new MessageImage
                        {
                            MessageId = messageId,
                            ImagePath = imagePath
                        },

                    "messages",
                    maxFiles: 5,
                    maxFileSize: 5 * 1024 * 1024
                );


            var hasValidImages = messageImages.Any();


            /*
             * If there is no text and all selected
             * images were rejected, remove the empty message.
             */
            if (!hasText && !hasValidImages)
            {
                _context.Messages.Remove(message);

                await _context.SaveChangesAsync();

                return null;
            }


            /*
             * Determine the final message type
             * based on what actually survived validation.
             */
            if (hasText && hasValidImages)
            {
                message.MessageType = MessageType.TextAndImage;
            }
            else if (hasValidImages)
            {
                message.MessageType = MessageType.Image;
            }
            else
            {
                message.MessageType = MessageType.Text;
            }


            if (hasValidImages)
            {
                _context.MessageImages.AddRange(messageImages);
            }


            await _context.SaveChangesAsync();

            var savedImagePaths = messageImages
                .Select(image => image.ImagePath)
                .ToList();


            // =========================================================
            // FIND THE OTHER PARTICIPANT
            // Auto-unarchive the conversation for them if needed.
            // =========================================================

            var recipientParticipant =
                await _context.ConversationParticipants
                    .FirstOrDefaultAsync(cp =>
                        cp.ConversationId == conversationId &&
                        cp.UserId != senderId);


            string? recipientId = null;


            if (recipientParticipant != null)
            {
                recipientId = recipientParticipant.UserId;

                if (recipientParticipant.IsArchived)
                {
                    recipientParticipant.IsArchived = false;

                    await _context.SaveChangesAsync();
                }
            }



            // =========================================================
            // 1. SEND MESSAGE TO OPEN CHAT
            // =========================================================

            await _hubContext.Clients
                .Group($"conversation-{conversationId}")
                .SendAsync(
                    "ReceiveMessage",
                    new
                    {
                        messageId = message.MessageId,
                        conversationId = message.ConversationId,
                        senderId = message.SenderId,
                        content = message.Content,
                        imagePaths = savedImagePaths,
                        createdAt = message.CreatedAt
                    }
                );

            var sender =
                await _context.Users
                    .Where(u => u.Id == senderId)
                    .Select(u => new
                    {
                        u.FirstName,
                        u.LastName,
                        u.ProfilePicturePath
                    })
                    .FirstOrDefaultAsync();


            var conversationInfo =
                await _context.Conversations
                    .Where(c =>
                        c.ConversationId == conversationId)
                    .Select(c => new
                    {
                        c.ContextTitle,
                        c.Type
                    })
                    .FirstOrDefaultAsync();


            // =========================================================
            // SEND UNREAD NOTIFICATION TO RECEIVER
            // =========================================================

            if (!string.IsNullOrEmpty(recipientId))
            {
                await _hubContext.Clients
                    .Group($"user-{recipientId}")
                    .SendAsync(
                        "MessageNotification",
                        new
                        {
                            messageId =
                                message.MessageId,

                            conversationId =
                                message.ConversationId,

                            senderId =
                                message.SenderId,

                            senderName =
                                sender == null
                                    ? "Member"
                                    : $"{sender.FirstName} {sender.LastName}",

                            senderProfilePicture =
                                sender?.ProfilePicturePath,

                            contextTitle =
                                conversationInfo?.ContextTitle,

                            contextType =
                                conversationInfo?.Type.ToString(),

                            content =
                                message.Content,

                            imagePaths =
                                savedImagePaths,

                            createdAt =
                                message.CreatedAt
                        }
                    );
            }


            return message;

        }

        public async Task<Conversation?> GetOrCreateLostFoundConversationAsync(int lostFoundId, string currentUserId)
        {
            // 1. Find the Lost & Found report and its registered owner
            var lostFound = await _context.LostFounds
                .Include(l => l.User)
                .FirstOrDefaultAsync(l => l.LostFoundId == lostFoundId);

            if (lostFound == null)
            {
                return null;
            }

            // Guest-created reports cannot use in-app messaging
            if (lostFound.UserId == null || lostFound.User == null)
            {
                return null;
            }

            // 2. Prevent users from messaging themselves
            if (lostFound.UserId == currentUserId)
            {
                return null;
            }

            // 3. Find the current logged-in user
            var currentUser = await _userManager.FindByIdAsync(currentUserId);

            if (currentUser == null)
            {
                return null;
            }

            // 4. Both users must be Members
            var currentUserIsMember =
                await _userManager.IsInRoleAsync(currentUser, "Member");

            var reportOwnerIsMember =
                await _userManager.IsInRoleAsync(lostFound.User, "Member");

            if (!currentUserIsMember || !reportOwnerIsMember)
            {
                return null;
            }

            // 5. Check if this exact conversation already exists
            var existingConversation = await _context.Conversations
                .Include(c => c.Participants)
                .FirstOrDefaultAsync(c =>
                    c.Type == ConversationType.LostFound &&
                    c.LostFoundId == lostFoundId &&
                    c.Participants.Any(p => p.UserId == currentUserId) &&
                    c.Participants.Any(p => p.UserId == lostFound.UserId));

            if (existingConversation != null)
            {
                return existingConversation;
            }

            // 6. Create a new conversation
            var conversation = new Conversation
            {
                Type = ConversationType.LostFound,
                ContextTitle = lostFound.Title,
                LostFoundId = lostFound.LostFoundId
            };

            // 7. Add both members
            conversation.Participants.Add(
                new ConversationParticipant
                {
                    UserId = currentUserId
                });

            conversation.Participants.Add(
                new ConversationParticipant
                {
                    UserId = lostFound.UserId
                });

            // 8. Save
            _context.Conversations.Add(conversation);

            await _context.SaveChangesAsync();

            return conversation;
        }


        public async Task MarkConversationAsReadAsync(
     int conversationId,
     string userId)
        {
            // Get the newest message in the conversation.
            var latestMessageId =
                await _context.Messages
                    .Where(m =>
                        m.ConversationId == conversationId)
                    .OrderByDescending(m =>
                        m.MessageId)
                    .Select(m =>
                        (int?)m.MessageId)
                    .FirstOrDefaultAsync();


            // If there are no messages,
            // there is nothing to mark as read.
            if (!latestMessageId.HasValue)
            {
                return;
            }


            // Use the main read method.
            await MarkMessageAsReadAsync(
                conversationId,
                latestMessageId.Value,
                userId
            );
        }

        // Main Read/Seen Method (reused by (conversationasreadasync and in the chathub)
        public async Task MarkMessageAsReadAsync(
    int conversationId,
    int messageId,
    string userId)
        {
            // =====================================================
            // FIND PARTICIPANT
            // =====================================================

            var participant =
                await _context.ConversationParticipants
                    .FirstOrDefaultAsync(cp =>
                        cp.ConversationId == conversationId &&
                        cp.UserId == userId);


            if (participant == null)
            {
                return;
            }


            // =====================================================
            // VERIFY MESSAGE BELONGS TO CONVERSATION
            // =====================================================

            var messageExists =
                await _context.Messages
                    .AnyAsync(m =>
                        m.MessageId == messageId &&
                        m.ConversationId == conversationId);


            if (!messageExists)
            {
                return;
            }


            // =====================================================
            // DON'T MOVE READ POSITION BACKWARDS
            // =====================================================

            if (
                participant.LastReadMessageId.HasValue &&
                participant.LastReadMessageId.Value >= messageId
            )
            {
                return;
            }


            // =====================================================
            // UPDATE READ POSITION
            // =====================================================

            participant.LastReadMessageId =
                messageId;


            await _context.SaveChangesAsync();


            // =====================================================
            // FIND OTHER PARTICIPANT
            // =====================================================

            var otherParticipantId =
                await _context.ConversationParticipants
                    .Where(cp =>
                        cp.ConversationId == conversationId &&
                        cp.UserId != userId)
                    .Select(cp => cp.UserId)
                    .FirstOrDefaultAsync();


            if (string.IsNullOrEmpty(otherParticipantId))
            {
                return;
            }


            // =====================================================
            // TELL OTHER USER THEIR MESSAGE WAS SEEN
            // =====================================================

            await _hubContext.Clients
                .Group($"user-{otherParticipantId}")
                .SendAsync(
                    "MessageSeen",
                    new
                    {
                        conversationId,
                        messageId
                    }
                );
        }


        public async Task<int> GetTotalUnreadCountAsync(string userId)
        {
            var participantRows = await _context.ConversationParticipants
                .Where(cp => cp.UserId == userId)
                .Select(cp => new
                {
                    cp.ConversationId,
                    cp.LastReadMessageId
                })
                .ToListAsync();

            var totalUnread = 0;

            foreach (var participant in participantRows)
            {
                var unreadCount = await _context.Messages
                    .CountAsync(m =>
                        m.ConversationId == participant.ConversationId &&
                        m.SenderId != userId &&
                        (!participant.LastReadMessageId.HasValue ||
                         m.MessageId > participant.LastReadMessageId.Value));

                totalUnread += unreadCount;
            }

            return totalUnread;
        }

        public async Task<bool> ArchiveConversationAsync(int conversationId, string userId)
        {
            var participant =
                await _context.ConversationParticipants
                    .FirstOrDefaultAsync(cp =>
                        cp.ConversationId == conversationId &&
                        cp.UserId == userId);

            if (participant == null)
            {
                return false;
            }

            participant.IsArchived = true;

            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> UnarchiveConversationAsync(int conversationId, string userId)
        {
            var participant =
                await _context.ConversationParticipants
                    .FirstOrDefaultAsync(cp =>
                        cp.ConversationId == conversationId &&
                        cp.UserId == userId);

            if (participant == null)
            {
                return false;
            }

            participant.IsArchived = false;

            await _context.SaveChangesAsync();

            return true;
        }

    }
}