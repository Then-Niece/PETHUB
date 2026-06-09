namespace PETHUB.ViewModels
{
    public class MemberViewModel
    {
        // Purpose: Used only in MembersController Create/Edit views.
        // Captures Member fields (extra ones) from the form, then maps to ApplicationUser.

        public string UserName { get; set; }
        public string Email { get; set; }
        public string Password { get; set; } // plain password for form
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string ContactNumber { get; set; }
        public string Status { get; set; } = "Active";

        // Member-only fields
        public string Address { get; set; }
        public string Gender { get; set; }
        public DateTime Birthdate { get; set; }
    }

}
