namespace PETHUB.Models
{
    public class SavedLostFound
    {
        public int SavedLostFoundId { get; set; }


        // =========================================================
        // MEMBER
        // =========================================================

        public string MemberId { get; set; } = string.Empty;

        public ApplicationUser? Member { get; set; }


        // =========================================================
        // LOST & FOUND REPORT
        // =========================================================

        public int LostFoundId { get; set; }

        public LostFound? LostFound { get; set; }


        // =========================================================
        // DATE SAVED
        // =========================================================

        public DateTime DateSaved { get; set; } = DateTime.Now;
    }
}