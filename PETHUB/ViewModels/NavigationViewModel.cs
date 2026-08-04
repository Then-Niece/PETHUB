namespace PETHUB.ViewModels
{
    public class NavigationViewModel
    {
        // Logged-in user's full name.
        public string FullName { get; set; }

        // Logged-in user's profile picture.
        public string? ProfilePicturePath { get; set; }

        // Current user's role.
        public string Role { get; set; }
    }
}