using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace PETHUB.Helpers
{
    public static class ImageHelper
    {
        public static async Task<List<TImage>> SaveImagesAsync<TImage>(
            List<IFormFile> files,
            int entityId,
            Func<int, string, TImage> createImage,
            string folderName,
            int? maxFiles = null,
            long maxFileSize = 5 * 1024 * 1024)
        {
            var images = new List<TImage>();

            if (files == null || files.Count == 0)
                return images;

            var allowedExtensions = new[]
            {
                ".jpg",
                ".jpeg",
                ".png",
                ".webp"
            };

            var allowedContentTypes = new[]
            {
                "image/jpeg",
                "image/png",
                "image/webp"
            };

            var validFiles = files
                .Where(file => file != null && file.Length > 0);

            if (maxFiles.HasValue)
            {
                validFiles = validFiles.Take(maxFiles.Value);
            }

            var filesToSave = validFiles.ToList();

            var uploadDir = Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot",
                "uploads",
                folderName
            );

            if (!Directory.Exists(uploadDir))
            {
                Directory.CreateDirectory(uploadDir);
            }

            foreach (var file in validFiles)
            {
                // File size validation
                if (file.Length > maxFileSize)
                {
                    continue;
                }

                var extension = Path
                    .GetExtension(file.FileName)
                    .ToLowerInvariant();

                // Extension validation
                if (!allowedExtensions.Contains(extension))
                {
                    continue;
                }

                // Content type validation
                if (!allowedContentTypes.Contains(
                        file.ContentType.ToLowerInvariant()))
                {
                    continue;
                }

                var uniqueFileName =
                    $"{Guid.NewGuid()}{extension}";

                var filePath = Path.Combine(
                    uploadDir,
                    uniqueFileName
                );

                using (var stream =
                    new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                var relativePath =
                    $"/uploads/{folderName}/{uniqueFileName}";

                images.Add(
                    createImage(
                        entityId,
                        relativePath
                    )
                );
            }

            return images;
        }


        // =========================================================
        // DELETE IMAGE
        // =========================================================
        public static async Task<int?> RemoveImageAsync<TImage>(
            DbContext context,
            DbSet<TImage> dbSet,
            int imageId,
            Func<TImage, string> getImagePath,
            Func<TImage, int> getParentId,
            Func<TImage, string>? getUserId = null,
            string? currentUserId = null)
            where TImage : class
        {
            var image = await dbSet.FindAsync(imageId);

            if (image == null)
            {
                return null;
            }


            // Optional ownership check
            if (getUserId != null &&
                currentUserId != null &&
                getUserId(image) != currentUserId)
            {
                return null;
            }


            var imagePath = getImagePath(image);


            // -------------------------------------------------
            // DELETE PHYSICAL FILE
            // -------------------------------------------------
            var filePath = Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot",
                imagePath.TrimStart('/')
            );


            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }


            // -------------------------------------------------
            // REMOVE DATABASE RECORD
            // -------------------------------------------------
            dbSet.Remove(image);

            await context.SaveChangesAsync();


            return getParentId(image);
        }
    }
}