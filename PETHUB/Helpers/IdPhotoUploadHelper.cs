namespace PETHUB.Helpers
{
    public class IdPhotoUploadHelper
    {
        public static async Task<string?> SaveIdPhotoAsync(IFormFile? file)
        {
            // No file selected
            if (file == null)
                return null;

            // Create a unique filename
            var fileName = Guid.NewGuid().ToString() +
                           Path.GetExtension(file.FileName);

            // Folder where the photo will be stored
            var folder = Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot",
                "uploads",
                "memberids");

            // Create the folder if it doesn't exist.
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }


            // Full physical path
            var fullPath = Path.Combine(folder, fileName);

            // Save the uploaded file
            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Return the path that will be stored in the database
            return "/uploads/memberids/" + fileName;
        }
    }
}
