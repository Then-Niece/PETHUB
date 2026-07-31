using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace PETHUB.Helpers
{
    public static class ImageHelper
    {
        // Save uploaded images
        // Review this code
        public static async Task<List<TImage>> SaveImagesAsync<TImage>(
            List<IFormFile> files,
            int entityId,
            Func<int, string, TImage> createImage,
            string folderName)
        {
            var images = new List<TImage>();

            if (files == null || files.Count == 0)
                return images;

            var uploadDir = Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot",
                "uploads",
                folderName
            );

            if (!Directory.Exists(uploadDir))
                Directory.CreateDirectory(uploadDir);

            foreach (var file in files)
            {
                var uniqueFileName = Guid.NewGuid() + Path.GetExtension(file.FileName);
                var filePath = Path.Combine(uploadDir, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                    await file.CopyToAsync(stream);

                images.Add(createImage(entityId, $"/uploads/{folderName}/{uniqueFileName}"));
            }

            return images;
        }

        // Delete the image file in the folder and database
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
            if (image == null) return null;

            // Optional ownership check
            if (getUserId != null && currentUserId != null && getUserId(image) != currentUserId)
                return null;

            // Delete file
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", getImagePath(image).TrimStart('/'));
            if (File.Exists(filePath)) File.Delete(filePath);

            // Remove DB record
            dbSet.Remove(image);
            await context.SaveChangesAsync();

            return getParentId(image);
        }

    }
}
