using System;

namespace PETHUB.Helpers
{

    //Review this Code
    public static class ImageUploadHelper
    {
       public static async Task<List<TImage>> SaveImagesAsync<TImage>(List<IFormFile> files,int entityId,Func<int, string, TImage> createImage, string folderName = "images")
        {
            var images = new List<TImage>();

            if (files == null || files.Count == 0)
                return images;

            var uploadDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", folderName);
            if (!Directory.Exists(uploadDir))
                Directory.CreateDirectory(uploadDir);

            foreach (var file in files)
            {
                var uniqueFileName = Guid.NewGuid() + Path.GetExtension(file.FileName);
                var filePath = Path.Combine(uploadDir, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                    await file.CopyToAsync(stream);

                images.Add(createImage(entityId, $"/{folderName}/{uniqueFileName}"));
            }

            return images;
        }

    }
}
