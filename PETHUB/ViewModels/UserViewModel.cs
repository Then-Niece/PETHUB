namespace PETHUB.ViewModels
{
    public class UserViewModel
    {
        // Purpose: Used only in UsersController Create/Edit views.
        // Captures Admin fields from the form, then maps to ApplicationUser.

        public string UserName { get; set; }
        public string Email { get; set; }
        public string Password { get; set; } // plain password for form
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string ContactNumber { get; set; }
        public string Status { get; set; } = "Active";
    }


}
