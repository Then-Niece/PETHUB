using Microsoft.AspNetCore.Http;
using System.Runtime.Intrinsics.Arm;

namespace PETHUB.Helpers
{
    public static class ProfilePictureHelper
    {
        // ============================================================
        // Profile Picture Settings
        // ============================================================

        // Folder inside wwwroot where profile pictures are stored.
        private const string FolderName = "uploads/profilepictures";

        // URL prefix used when storing the path in the database.
        private const string UrlPrefix = "/uploads/profilepictures/";

        // Maximum profile picture size: 5 MB.
        private const long MaxFileSize = 5 * 1024 * 1024;

        // ============================================================
        // Validate Profile Picture
        // ============================================================

        public static string? ValidateProfilePicture(IFormFile? file)
        {
            // No file was selected.
            if (file == null)
            {
                return null;
            }


            // Make sure the file is not empty.
            if (file.Length == 0)
            {
                return "The selected profile picture is empty.";
            }


            // Allowed image extensions.
            string[] allowedExtensions =
            {
                ".jpg",
                ".jpeg",
                ".png",
                ".webp"
            };


            // Get the file extension.
            string extension =
                Path.GetExtension(file.FileName)
                    .ToLowerInvariant();


            // Check the extension.
            if (!allowedExtensions.Contains(extension))
            {
                return
                    "Only JPG, JPEG, PNG, and WEBP files are allowed.";
            }


            // Check the file size.
            if (file.Length > MaxFileSize)
            {
                return
                    "Profile picture cannot exceed 5 MB.";
            }


            // Validation passed.
            return null;
        }


        // ============================================================
        // Save Profile Picture
        // ============================================================

        public static async Task<string?> SaveProfilePictureAsync(IFormFile? file, string webRootPath)
        {
            // No file selected.
            if (file == null || file.Length == 0)
                return null;


            // Create the upload folder if it does not exist.
            string uploadFolder = Path.Combine(webRootPath,FolderName);


            // Create the folder if it does not exist.
            if (!Directory.Exists(uploadFolder))
            {
                Directory.CreateDirectory(uploadFolder);
            }


            // Generate a unique filename.
            string extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            string uniqueFileName = $"{Guid.NewGuid()}{extension}";


            // Create the complete physical file path.
            string physicalFilePath = Path.Combine(uploadFolder, uniqueFileName);


            // Save the uploaded file.
            using (var fileStream = new FileStream(physicalFilePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }


            // Return the URL that will be stored
            // in the user's ProfilePicturePath.
            return UrlPrefix + uniqueFileName;
        }


        // ============================================================
        // Delete Profile Picture
        // ============================================================

        public static void DeleteProfilePicture(string? profilePicturePath, string webRootPath)
        {
            // Nothing to delete.
            if (string.IsNullOrWhiteSpace(profilePicturePath))
                return;


            // Only delete files belonging to our profile-picture folder.
            if (!profilePicturePath.StartsWith(UrlPrefix, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }


            // Get the filename only.
            string fileName = Path.GetFileName(profilePicturePath);


            // Build the physical path.
            string uploadFolder = Path.Combine(webRootPath, FolderName);

            string physicalFilePath = Path.Combine(uploadFolder, fileName);


            // Delete the file if it exists.
            if (File.Exists(physicalFilePath))
            {
                File.Delete(physicalFilePath);
            }
        }
    }
}