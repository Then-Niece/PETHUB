using Microsoft.AspNetCore.Http;

namespace PETHUB.Helpers
{
    public static class ClientIdUploadHelper
    {
        public static async Task<string> SaveClientIdAsync(IFormFile file)
        {
            var folder = Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot",
                "uploads",
                "clientids");

            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            var fileName = Guid.NewGuid() + Path.GetExtension(file.FileName);

            var filePath = Path.Combine(folder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return "/uploads/clientids/" + fileName;
        }
    }
}