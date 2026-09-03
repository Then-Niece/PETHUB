using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PETHUB.Models;
using PETHUB.Services;

namespace PETHUB.Controllers
{
    [Authorize(Roles = "Member")]
    public class MessagesController : Controller
    {
        private readonly MessagingService _messagingService;
        private readonly UserManager<ApplicationUser> _userManager;

        public MessagesController(MessagingService messagingService, UserManager<ApplicationUser> userManager)
        {
            _messagingService = messagingService;
            _userManager = userManager;
        }


        // =========================================================
        // START / OPEN MARKETPLACE CONVERSATION
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> StartMarketplaceConversation(int listingId)
        {
            var currentUserId = _userManager.GetUserId(User);

            if (currentUserId == null)
            {
                return Unauthorized();
            }

            var conversation =
                await _messagingService.GetOrCreateMarketplaceConversationAsync(
                    listingId,
                    currentUserId);

            if (conversation == null)
            {
                TempData["WarningMessage"] =
                    "This member is currently unavailable for messaging.";

                return RedirectToAction(
                    "Marketplace",
                    "Listings"
                );
            }
            return RedirectToAction(
                nameof(Index),
                new
                {
                    conversationId = conversation.ConversationId
                });
        }


        // =========================================================
        // MESSAGES PAGE
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Index(int? conversationId, string? view)
        {
            var currentUserId = _userManager.GetUserId(User);

            if (currentUserId == null)
            {
                return Unauthorized();
            }

            if (conversationId.HasValue)
            {
                await _messagingService.MarkConversationAsReadAsync(
                    conversationId.Value,
                    currentUserId);
            }

            var showArchived =
                string.Equals(
                    view,
                    "archived",
                    StringComparison.OrdinalIgnoreCase);

            var model =
                await _messagingService.GetMessagesPageAsync(
                    currentUserId,
                    conversationId,
                    showArchived);

            return View(model);
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendMessage(int conversationId, string? content, List<IFormFile>? imageFiles)
        {
            var currentUserId = _userManager.GetUserId(User);

            if (currentUserId == null)
            {
                return Unauthorized();
            }


            var message =
                await _messagingService.SendMessageAsync(
                    conversationId,
                    currentUserId,
                    content,
                    imageFiles
                );


            // =========================================================
            // MESSAGE COULD NOT BE SENT
            // =========================================================

            if (message == null)
            {
                // AJAX / JavaScript submission
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return BadRequest(new
                    {
                        success = false,
                        message =
                            "This message could not be sent. The member may no longer be available."
                    });
                }


                // Non-JavaScript fallback
                TempData["WarningMessage"] =
                    "This message could not be sent. The member may no longer be available.";

                return RedirectToAction(
                    nameof(Index),
                    new
                    {
                        conversationId
                    }
                );
            }


            // =========================================================
            // AJAX SUCCESS
            // =========================================================

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Ok(new
                {
                    success = true
                });
            }


            // =========================================================
            // NON-JAVASCRIPT FALLBACK
            // =========================================================

            return RedirectToAction(
                nameof(Index),
                new
                {
                    conversationId
                }
            );
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> StartLostFoundConversation(int lostFoundId)
        {
            var currentUserId = _userManager.GetUserId(User);

            if (currentUserId == null)
            {
                return Unauthorized();
            }

            var conversation =
                await _messagingService.GetOrCreateLostFoundConversationAsync(
                    lostFoundId,
                    currentUserId);

            if (conversation == null)
            {
                TempData["WarningMessage"] =
                    "This member is currently unavailable for messaging.";

                return RedirectToAction(
                    "Browse",
                    "LostFounds"
                );
            }

            return RedirectToAction(
                nameof(Index),
                new
                {
                    conversationId = conversation.ConversationId
                });
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ArchiveConversation(int conversationId)
        {
            var userId = _userManager.GetUserId(User);

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var success =
                await _messagingService
                    .ArchiveConversationAsync(
                        conversationId,
                        userId);

            if (!success)
            {
                return NotFound();
            }

            TempData["SuccessMessage"] =
                "Conversation archived successfully.";

            return RedirectToAction(nameof(Index));
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UnarchiveConversation(int conversationId)
        {
            var userId = _userManager.GetUserId(User);

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var success =
                await _messagingService
                    .UnarchiveConversationAsync(
                        conversationId,
                        userId);

            if (!success)
            {
                return NotFound();
            }

            TempData["SuccessMessage"] =
                "Conversation restored successfully.";

            return RedirectToAction(nameof(Index));
        }


    }
}