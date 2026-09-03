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

            // The listing owner must still have an active account.
            if (listing.Member.Status != UserStatus.Active)
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

            // The current user must also have an active account.
            if (currentUser.Status != UserStatus.Active)
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
        // GET MESSAGES PAGE
        // =========================================================
        //
        // PERFORMANCE DESIGN:
        //
        // QUERY 1:
        // Build the conversation sidebar using lightweight
        // database projections.
        //
        // This does NOT load every message in every conversation.
        //
        // QUERY 2:
        // If the user opens a conversation, load the complete
        // information for that ONE conversation only.
        //
        // This prevents the Messages page from becoming slower
        // as the number of conversations/messages grows.
        // =========================================================

        public async Task<MessagesIndexViewModel> GetMessagesPageAsync(
            string currentUserId,
            int? selectedConversationId,
            bool showArchived = false)
        {
            // =========================================================
            // CREATE PAGE VIEW MODEL
            // =========================================================

            var viewModel = new MessagesIndexViewModel
            {
                CurrentUserId = currentUserId,
                IsArchiveView = showArchived
            };


            // =========================================================
            // QUERY 1 - LIGHTWEIGHT CONVERSATION SIDEBAR
            // =========================================================
            //
            // IMPORTANT:
            //
            // We intentionally DO NOT use:
            //
            // .Include(c => c.Messages)
            //
            // here.
            //
            // Instead, SQL Server retrieves only:
            // - Other Member information
            // - Post information
            // - First post image
            // - Latest message
            // - Latest message image count
            // - Unread count
            //
            // This keeps the sidebar lightweight.
            // =========================================================

            var conversationRows =
                await _context.Conversations

                    // Only conversations belonging to this user,
                    // respecting Inbox / Archive view.
                    .Where(c =>
                        c.Participants.Any(p =>
                            p.UserId == currentUserId &&
                            p.IsArchived == showArchived))

                    // Read-only query = no EF change tracking needed.
                    .AsNoTracking()

                    // =================================================
                    // PROJECT ONLY THE DATA THE SIDEBAR NEEDS
                    // =================================================

                    .Select(c => new
                    {
                        // ---------------------------------------------
                        // Conversation
                        // ---------------------------------------------

                        c.ConversationId,
                        c.Type,
                        c.ContextTitle,
                        c.ListingId,
                        c.LostFoundId,


                        // ---------------------------------------------
                        // OTHER PARTICIPANT
                        // ---------------------------------------------

                        OtherUserId =
                            c.Participants
                                .Where(p =>
                                    p.UserId != currentUserId)
                                .Select(p =>
                                    p.UserId)
                                .FirstOrDefault(),

                        OtherUserFirstName =
                            c.Participants
                                .Where(p =>
                                    p.UserId != currentUserId)
                                .Select(p =>
                                    p.User.FirstName)
                                .FirstOrDefault(),

                        OtherUserLastName =
                            c.Participants
                                .Where(p =>
                                    p.UserId != currentUserId)
                                .Select(p =>
                                    p.User.LastName)
                                .FirstOrDefault(),

                        OtherUserProfilePicture =
                            c.Participants
                                .Where(p =>
                                    p.UserId != currentUserId)
                                .Select(p =>
                                    p.User.ProfilePicturePath)
                                .FirstOrDefault(),

                        OtherUserStatus =
                            c.Participants
                                .Where(p =>
                                    p.UserId != currentUserId)
                                .Select(p =>
                                    (UserStatus?)p.User.Status)
                                .FirstOrDefault(),


                        // ---------------------------------------------
                        // CURRENT USER READ POSITION
                        // ---------------------------------------------

                        CurrentUserLastReadMessageId =
                            c.Participants
                                .Where(p =>
                                    p.UserId == currentUserId)
                                .Select(p =>
                                    p.LastReadMessageId)
                                .FirstOrDefault(),


                        // ---------------------------------------------
                        // LATEST MESSAGE
                        // ---------------------------------------------

                        LastMessageId =
                            c.Messages
                                .OrderByDescending(m =>
                                    m.MessageId)
                                .Select(m =>
                                    (int?)m.MessageId)
                                .FirstOrDefault(),

                        LastMessageContent =
                            c.Messages
                                .OrderByDescending(m =>
                                    m.MessageId)
                                .Select(m =>
                                    m.Content)
                                .FirstOrDefault(),

                        LastMessageSenderId =
                            c.Messages
                                .OrderByDescending(m =>
                                    m.MessageId)
                                .Select(m =>
                                    m.SenderId)
                                .FirstOrDefault(),

                        LastMessageAt =
                            c.Messages
                                .OrderByDescending(m =>
                                    m.MessageId)
                                .Select(m =>
                                    (DateTime?)m.CreatedAt)
                                .FirstOrDefault(),

                        LastMessageImageCount =
                            c.Messages
                                .OrderByDescending(m =>
                                    m.MessageId)
                                .Select(m =>
                                    m.Images.Count())
                                .FirstOrDefault(),


                        // ---------------------------------------------
                        // UNREAD COUNT
                        // ---------------------------------------------

                        UnreadCount =
                            c.Messages.Count(m =>
                                m.SenderId != currentUserId &&
                                (
                                    !c.Participants
                                        .Where(p =>
                                            p.UserId ==
                                            currentUserId)
                                        .Select(p =>
                                            p.LastReadMessageId)
                                        .FirstOrDefault()
                                        .HasValue

                                    ||

                                    m.MessageId >
                                    c.Participants
                                        .Where(p =>
                                            p.UserId ==
                                            currentUserId)
                                        .Select(p =>
                                            p.LastReadMessageId)
                                        .FirstOrDefault()
                                        .Value
                                )),


                        // =================================================
                        // MARKETPLACE CONTEXT
                        // =================================================

                        ListingStatus =
                            c.Listing != null
                                ? (ListingStatus?)
                                    c.Listing.ListStatus
                                : null,

                        ListingApprovalStatus =
                            c.Listing != null
                                ? (ListApprovalStatus?)
                                    c.Listing.Status
                                : null,

                        ListingOwnerStatus =
                            c.Listing != null &&
                            c.Listing.Member != null
                                ? (UserStatus?)
                                    c.Listing.Member.Status
                                : null,

                        ListingExists =
                            c.Listing != null,

                        ListingFirstImage =
                            c.Listing != null
                                ? c.Listing.Images
                                    .OrderBy(i =>
                                        i.ListingImageId)
                                    .Select(i =>
                                        i.ImagePath)
                                    .FirstOrDefault()
                                : null,


                        // =================================================
                        // LOST & FOUND CONTEXT
                        // =================================================

                        ReportApprovalStatus =
                            c.LostFound != null
                                ? (ApprovalStatus?)
                                    c.LostFound.Status
                                : null,

                        ReportStatus =
                            c.LostFound != null
                                ? (ReportStatus?)
                                    c.LostFound.RStatus
                                : null,

                        LostFoundType =
                            c.LostFound != null
                                ? (LostFoundType?)
                                    c.LostFound.Type
                                : null,

                        ReportOwnerId =
                            c.LostFound != null
                                ? c.LostFound.UserId
                                : null,

                        ReportOwnerStatus =
                            c.LostFound != null &&
                            c.LostFound.User != null
                                ? (UserStatus?)
                                    c.LostFound.User.Status
                                : null,

                        ReportExists =
                            c.LostFound != null,

                        ReportFirstImage =
                            c.LostFound != null
                                ? c.LostFound.Images
                                    .OrderBy(i =>
                                        i.LostFoundImageId)
                                    .Select(i =>
                                        i.ImagePath)
                                    .FirstOrDefault()
                                : null
                    })

                    .ToListAsync();


            // =========================================================
            // BUILD SIDEBAR VIEW MODELS
            // =========================================================

            foreach (var row in conversationRows)
            {
                // Conversation should always have another participant.
                // Skip malformed records instead of crashing.
                if (string.IsNullOrWhiteSpace(row.OtherUserId))
                {
                    continue;
                }


                // =====================================================
                // LAST MESSAGE PREVIEW
                // =====================================================

                string? lastMessagePreview = null;


                if (row.LastMessageId.HasValue)
                {
                    // Prefer text when available.
                    if (!string.IsNullOrWhiteSpace(
                        row.LastMessageContent))
                    {
                        lastMessagePreview =
                            row.LastMessageContent;
                    }

                    // Image-only message.
                    else if (row.LastMessageImageCount > 0)
                    {
                        var sentByCurrentUser =
                            row.LastMessageSenderId ==
                            currentUserId;


                        if (row.LastMessageImageCount == 1)
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

                                    ? $"You sent {row.LastMessageImageCount} photos"

                                    : $"Sent {row.LastMessageImageCount} photos";
                        }
                    }
                }


                // =====================================================
                // POST CONTEXT
                // =====================================================

                string? contextImagePath = null;
                string? contextStatus = null;


                // =====================================================
                // MARKETPLACE
                // =====================================================

                if (row.Type ==
                    ConversationType.Marketplace)
                {
                    contextImagePath =
                        row.ListingFirstImage;


                    if (!row.ListingExists)
                    {
                        contextStatus =
                            "Unavailable";
                    }
                    else if (
                        row.ListingStatus ==
                        ListingStatus.Deleted)
                    {
                        contextStatus =
                            "Deleted by Owner";
                    }
                    else if (
                        row.ListingApprovalStatus ==
                        ListApprovalStatus.Removed)
                    {
                        contextStatus =
                            "Removed";
                    }
                    else if (
                        row.ListingStatus ==
                        ListingStatus.Sold)
                    {
                        contextStatus =
                            "Sold";
                    }
                    else if (
                        row.ListingStatus ==
                        ListingStatus.Adopted)
                    {
                        contextStatus =
                            "Adopted";
                    }
                    else if (
                        row.ListingApprovalStatus ==
                        ListApprovalStatus.Rejected)
                    {
                        contextStatus =
                            "Rejected";
                    }
                    else if (
                        row.ListingApprovalStatus ==
                        ListApprovalStatus.Pending)
                    {
                        contextStatus =
                            "Pending Approval";
                    }
                    else
                    {
                        contextStatus =
                            "Available";
                    }
                }


                // =====================================================
                // LOST & FOUND
                // =====================================================

                else if (
                    row.Type ==
                    ConversationType.LostFound)
                {
                    contextImagePath =
                        row.ReportFirstImage;


                    if (!row.ReportExists)
                    {
                        contextStatus =
                            "Unavailable";
                    }
                    else if (
                        row.ReportStatus ==
                        ReportStatus.Deleted)
                    {
                        contextStatus =
                            "Deleted by Owner";
                    }
                    else if (
                        row.ReportApprovalStatus ==
                        ApprovalStatus.Removed)
                    {
                        contextStatus =
                            "Removed";
                    }
                    else if (
                        row.ReportStatus ==
                        ReportStatus.Resolved)
                    {
                        contextStatus =
                            row.LostFoundType ==
                            LostFoundType.Lost

                                ? "Found"

                                : "Resolved";
                    }
                    else if (
                        row.ReportApprovalStatus ==
                        ApprovalStatus.Rejected)
                    {
                        contextStatus =
                            "Rejected";
                    }
                    else if (
                        row.ReportApprovalStatus ==
                        ApprovalStatus.Pending)
                    {
                        contextStatus =
                            "Pending Approval";
                    }
                    else
                    {
                        contextStatus =
                            "Active";
                    }
                }


                // =====================================================
                // ADD SIDEBAR ITEM
                // =====================================================

                viewModel.Conversations.Add(
                    new ConversationListItemViewModel
                    {
                        ConversationId =
                            row.ConversationId,

                        OtherUserId =
                            row.OtherUserId,

                        OtherUserFirstName =
                            row.OtherUserFirstName,

                        OtherUserName =
                            $"{row.OtherUserFirstName} {row.OtherUserLastName}"
                                .Trim(),

                        OtherUserProfilePicture =
                            row.OtherUserProfilePicture,

                        IsOtherUserActive =
                            row.OtherUserStatus ==
                            UserStatus.Active,

                        ContextTitle =
                            row.ContextTitle,

                        ContextType =
                            row.Type.ToString(),

                        ContextImagePath =
                            contextImagePath,

                        ContextStatus =
                            contextStatus,

                        LastMessage =
                            lastMessagePreview,

                        LastMessageAt =
                            row.LastMessageAt,

                        UnreadCount =
                            row.UnreadCount
                    }
                );
            }


            // =========================================================
            // SORT SIDEBAR
            // =========================================================

            viewModel.Conversations =
                viewModel.Conversations

                    .OrderByDescending(c =>
                        c.LastMessageAt)

                    .ThenByDescending(c =>
                        c.ConversationId)

                    .ToList();


            // =========================================================
            // NO CONVERSATION SELECTED
            // =========================================================
            //
            // If the user is only looking at the inbox,
            // STOP HERE.
            //
            // This means we never load a complete message history
            // unless a conversation is actually opened.
            // =========================================================

            if (!selectedConversationId.HasValue)
            {
                return viewModel;
            }


            // =========================================================
            // QUERY 2 - LOAD ONE SELECTED CONVERSATION
            // =========================================================

            var selectedConversation =
                await _context.Conversations

                    // Security:
                    // Ensure the selected conversation actually belongs
                    // to the currently logged-in Member.
                    .Where(c =>
                        c.ConversationId ==
                            selectedConversationId.Value
                        &&
                        c.Participants.Any(p =>
                            p.UserId == currentUserId &&
                            p.IsArchived == showArchived))

                    // Participants
                    .Include(c =>
                        c.Participants)

                        .ThenInclude(p =>
                            p.User)

                    // Full message history for THIS conversation only
                    .Include(c =>
                        c.Messages)

                        .ThenInclude(m =>
                            m.Images)

                    // Marketplace
                    .Include(c =>
                        c.Listing)

                        .ThenInclude(l =>
                            l!.Images)

                    .Include(c =>
                        c.Listing)

                        .ThenInclude(l =>
                            l!.Member)

                    // Lost & Found
                    .Include(c =>
                        c.LostFound)

                        .ThenInclude(l =>
                            l!.Images)

                    .Include(c =>
                        c.LostFound)

                        .ThenInclude(l =>
                            l!.User)

                    // Multiple collection Includes exist here,
                    // so split them into separate SQL queries.
                    .AsSplitQuery()

                    .FirstOrDefaultAsync();


            // Conversation does not exist or does not belong
            // to this user's current Inbox / Archive view.
            if (selectedConversation == null)
            {
                return viewModel;
            }


            // =========================================================
            // FIND OTHER PARTICIPANT
            // =========================================================

            var selectedOtherParticipant =
                selectedConversation.Participants
                    .FirstOrDefault(p =>
                        p.UserId != currentUserId);


            if (selectedOtherParticipant == null ||
                selectedOtherParticipant.User == null)
            {
                return viewModel;
            }


            // =========================================================
            // SELECTED POST CONTEXT
            // =========================================================

            string? selectedContextImagePath = null;
            string? selectedContextStatus = null;

            var selectedContextAvailable = false;


            // =========================================================
            // MARKETPLACE SELECTED CONTEXT
            // =========================================================

            if (selectedConversation.Type ==
                ConversationType.Marketplace)
            {
                var listing =
                    selectedConversation.Listing;


                if (listing != null)
                {
                    // First Marketplace post image.
                    selectedContextImagePath =
                        listing.Images?
                            .OrderBy(i =>
                                i.ListingImageId)
                            .Select(i =>
                                i.ImagePath)
                            .FirstOrDefault();


                    // -------------------------------------------------
                    // Marketplace status
                    // -------------------------------------------------

                    if (listing.ListStatus ==
                        ListingStatus.Deleted)
                    {
                        selectedContextStatus =
                            "Deleted by Owner";
                    }
                    else if (
                        listing.Status ==
                        ListApprovalStatus.Removed)
                    {
                        selectedContextStatus =
                            "Removed";
                    }
                    else if (
                        listing.ListStatus ==
                        ListingStatus.Sold)
                    {
                        selectedContextStatus =
                            "Sold";
                    }
                    else if (
                        listing.ListStatus ==
                        ListingStatus.Adopted)
                    {
                        selectedContextStatus =
                            "Adopted";
                    }
                    else if (
                        listing.Status ==
                        ListApprovalStatus.Rejected)
                    {
                        selectedContextStatus =
                            "Rejected";
                    }
                    else if (
                        listing.Status ==
                        ListApprovalStatus.Pending)
                    {
                        selectedContextStatus =
                            "Pending Approval";
                    }
                    else
                    {
                        selectedContextStatus =
                            "Available";
                    }


                    // -------------------------------------------------
                    // Can the View Listing button appear?
                    // -------------------------------------------------

                    selectedContextAvailable =
                        listing.Status ==
                            ListApprovalStatus.Approved
                        &&
                        listing.ListStatus ==
                            ListingStatus.Pending
                        &&
                        listing.Member != null
                        &&
                        listing.Member.Status ==
                            UserStatus.Active;
                }
                else
                {
                    selectedContextStatus =
                        "Unavailable";
                }
            }


            // =========================================================
            // LOST & FOUND SELECTED CONTEXT
            // =========================================================

            else if (
                selectedConversation.Type ==
                ConversationType.LostFound)
            {
                var report =
                    selectedConversation.LostFound;


                if (report != null)
                {
                    // First report image.
                    selectedContextImagePath =
                        report.Images?
                            .OrderBy(i =>
                                i.LostFoundImageId)
                            .Select(i =>
                                i.ImagePath)
                            .FirstOrDefault();


                    // -------------------------------------------------
                    // Report status
                    // -------------------------------------------------

                    if (report.RStatus ==
                        ReportStatus.Deleted)
                    {
                        selectedContextStatus =
                            "Deleted by Owner";
                    }
                    else if (
                        report.Status ==
                        ApprovalStatus.Removed)
                    {
                        selectedContextStatus =
                            "Removed";
                    }
                    else if (
                        report.RStatus ==
                        ReportStatus.Resolved)
                    {
                        selectedContextStatus =
                            report.Type ==
                            LostFoundType.Lost

                                ? "Found"

                                : "Resolved";
                    }
                    else if (
                        report.Status ==
                        ApprovalStatus.Rejected)
                    {
                        selectedContextStatus =
                            "Rejected";
                    }
                    else if (
                        report.Status ==
                        ApprovalStatus.Pending)
                    {
                        selectedContextStatus =
                            "Pending Approval";
                    }
                    else
                    {
                        selectedContextStatus =
                            "Active";
                    }


                    // -------------------------------------------------
                    // Can View Report appear?
                    // -------------------------------------------------

                    selectedContextAvailable =
                        report.Status ==
                            ApprovalStatus.Approved
                        &&
                        report.RStatus ==
                            ReportStatus.Active
                        &&
                        (
                            report.UserId == null
                            ||
                            (
                                report.User != null
                                &&
                                report.User.Status ==
                                    UserStatus.Active
                            )
                        );
                }
                else
                {
                    selectedContextStatus =
                        "Unavailable";
                }
            }


            // =========================================================
            // BUILD SELECTED CONVERSATION VIEW MODEL
            // =========================================================

            viewModel.SelectedConversation =
                new ConversationViewModel
                {
                    ConversationId =
                        selectedConversation
                            .ConversationId,

                    OtherUserId =
                        selectedOtherParticipant
                            .UserId,

                    OtherUserName =
                        $"{selectedOtherParticipant.User.FirstName} {selectedOtherParticipant.User.LastName}"
                            .Trim(),

                    OtherUserProfilePicture =
                        selectedOtherParticipant.User
                            .ProfilePicturePath,

                    IsOtherUserActive =
                        selectedOtherParticipant.User.Status ==
                        UserStatus.Active,

                    ContextTitle =
                        selectedConversation
                            .ContextTitle,

                    ContextType =
                        selectedConversation
                            .Type
                            .ToString(),

                    ContextImagePath =
                        selectedContextImagePath,

                    ContextStatus =
                        selectedContextStatus,

                    ListingId =
                        selectedConversation
                            .ListingId,

                    LostFoundId =
                        selectedConversation
                            .LostFoundId,

                    OtherParticipantLastReadMessageId =
                        selectedOtherParticipant
                            .LastReadMessageId,

                    ContextAvailable =
                        selectedContextAvailable,

                    Messages =
                        selectedConversation.Messages

                            .OrderBy(m =>
                                m.CreatedAt)

                            .Select(m =>
                                new MessageViewModel
                                {
                                    MessageId =
                                        m.MessageId,

                                    SenderId =
                                        m.SenderId,

                                    Content =
                                        m.Content,

                                    ImagePaths =
                                        m.Images
                                            .Select(i =>
                                                i.ImagePath)
                                            .ToList(),

                                    CreatedAt =
                                        m.CreatedAt,

                                    IsMine =
                                        m.SenderId ==
                                        currentUserId
                                })

                            .ToList()
                };


            return viewModel;
        }

        public async Task<Message?> SendMessageAsync(
     int conversationId,
     string senderId,
     string? content,
     List<IFormFile>? imageFiles)
        {
            // =========================================================
            // VERIFY CONVERSATION PARTICIPANTS
            // =========================================================

            var participants =
                await _context.ConversationParticipants
                    .Where(cp =>
                        cp.ConversationId == conversationId)
                    .Include(cp => cp.User)
                    .ToListAsync();


            // ---------------------------------------------------------
            // VERIFY SENDER
            // ---------------------------------------------------------

            var senderParticipant =
                participants.FirstOrDefault(
                    cp => cp.UserId == senderId
                );


            // Sender must belong to this conversation.
            if (senderParticipant == null)
            {
                return null;
            }


            // Sender account must still be active.
            if (senderParticipant.User == null ||
                senderParticipant.User.Status != UserStatus.Active)
            {
                return null;
            }


            // ---------------------------------------------------------
            // VERIFY RECIPIENT
            // ---------------------------------------------------------

            var recipientParticipant =
                participants.FirstOrDefault(
                    cp => cp.UserId != senderId
                );


            if (recipientParticipant == null)
            {
                return null;
            }


            // Do not allow sending new messages to
            // a deactivated Member account.
            if (recipientParticipant.User == null ||
                recipientParticipant.User.Status != UserStatus.Active)
            {
                return null;
            }


            // =========================================================
            // VALIDATE MESSAGE CONTENT
            // =========================================================

            var hasText =
                !string.IsNullOrWhiteSpace(content);


            var filesToProcess =
                imageFiles?
                    .Where(file =>
                        file != null &&
                        file.Length > 0)
                    .Take(5)
                    .ToList()
                ?? new List<IFormFile>();


            // A message must contain either text
            // or at least one selected image.
            if (!hasText &&
                filesToProcess.Count == 0)
            {
                return null;
            }


            // =========================================================
            // CREATE MESSAGE
            // =========================================================

            /*
             * Create the message first so SQL Server
             * can generate the MessageId.
             */
            var message =
                new Message
                {
                    ConversationId =
                        conversationId,

                    SenderId =
                        senderId,

                    Content =
                        hasText
                            ? content!.Trim()
                            : null,

                    MessageType =
                        MessageType.Text,

                    CreatedAt =
                        DateTime.UtcNow
                };


            _context.Messages.Add(message);


            await _context.SaveChangesAsync();


            // =========================================================
            // SAVE MESSAGE IMAGES
            // =========================================================

            var messageImages =
                await ImageHelper.SaveImagesAsync(
                    filesToProcess,
                    message.MessageId,

                    (messageId, imagePath) =>
                        new MessageImage
                        {
                            MessageId =
                                messageId,

                            ImagePath =
                                imagePath
                        },

                    "messages",

                    maxFiles: 5,

                    maxFileSize:
                        5 * 1024 * 1024
                );


            var hasValidImages =
                messageImages.Any();


            // =========================================================
            // REMOVE EMPTY MESSAGE
            // =========================================================

            /*
             * If the user selected only images but all
             * uploaded images failed validation, remove
             * the temporary empty message.
             */
            if (!hasText &&
                !hasValidImages)
            {
                _context.Messages.Remove(message);


                await _context.SaveChangesAsync();


                return null;
            }


            // =========================================================
            // DETERMINE MESSAGE TYPE
            // =========================================================

            if (hasText &&
                hasValidImages)
            {
                message.MessageType =
                    MessageType.TextAndImage;
            }
            else if (hasValidImages)
            {
                message.MessageType =
                    MessageType.Image;
            }
            else
            {
                message.MessageType =
                    MessageType.Text;
            }


            // =========================================================
            // SAVE IMAGE RECORDS
            // =========================================================

            if (hasValidImages)
            {
                _context.MessageImages
                    .AddRange(messageImages);
            }


            await _context.SaveChangesAsync();


            var savedImagePaths =
                messageImages
                    .Select(image =>
                        image.ImagePath)
                    .ToList();


            // =========================================================
            // AUTO-UNARCHIVE CONVERSATION
            // =========================================================
            //
            // A conversation should return to the normal Inbox whenever
            // a new message is sent.
            //
            // This applies to BOTH:
            //
            // 1. The sender
            //    - If they are messaging from Archived, sending a new
            //      message means the conversation becomes active again.
            //
            // 2. The recipient
            //    - If they archived the conversation earlier, receiving
            //      a new message should bring it back to their Inbox.
            // =========================================================

            var recipientId =
                recipientParticipant.UserId;

            var archiveChanged = false;


            // ---------------------------------------------------------
            // UNARCHIVE SENDER
            // ---------------------------------------------------------

            if (senderParticipant.IsArchived)
            {
                senderParticipant.IsArchived = false;
                archiveChanged = true;
            }


            // ---------------------------------------------------------
            // UNARCHIVE RECIPIENT
            // ---------------------------------------------------------

            if (recipientParticipant.IsArchived)
            {
                recipientParticipant.IsArchived = false;
                archiveChanged = true;
            }


            // ---------------------------------------------------------
            // SAVE ONLY IF SOMETHING CHANGED
            // ---------------------------------------------------------

            if (archiveChanged)
            {
                await _context.SaveChangesAsync();
            }


            // =========================================================
            // SEND MESSAGE TO OPEN CHAT
            // =========================================================

            await _hubContext.Clients
                .Group(
                    $"conversation-{conversationId}"
                )
                .SendAsync(
                    "ReceiveMessage",

                    new
                    {
                        messageId =
                            message.MessageId,

                        conversationId =
                            message.ConversationId,

                        senderId =
                            message.SenderId,

                        content =
                            message.Content,

                        imagePaths =
                            savedImagePaths,

                        createdAt =
                            message.CreatedAt
                    }
                );


            // =========================================================
            // GET SENDER INFORMATION
            // =========================================================

            var sender =
                await _context.Users
                    .Where(u =>
                        u.Id == senderId)
                    .Select(u => new
                    {
                        u.FirstName,
                        u.LastName,
                        u.ProfilePicturePath
                    })
                    .FirstOrDefaultAsync();


            // =========================================================
            // GET CONVERSATION INFORMATION
            // =========================================================

            var conversationInfo =
                await _context.Conversations
                    .Where(c =>
                        c.ConversationId ==
                        conversationId)
                    .Select(c => new
                    {
                        c.ContextTitle,
                        c.Type
                    })
                    .FirstOrDefaultAsync();


            // =========================================================
            // SEND UNREAD NOTIFICATION TO RECIPIENT
            // =========================================================

            await _hubContext.Clients
                .Group(
                    $"user-{recipientId}"
                )
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

            // The Lost & Found owner must still have an active account.
            if (lostFound.User.Status != UserStatus.Active)
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

            if (currentUser.Status != UserStatus.Active)
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