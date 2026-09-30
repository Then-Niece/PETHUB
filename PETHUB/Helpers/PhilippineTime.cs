namespace PETHUB.Helpers
{
    public static class PhilippineTime
    {
        public static DateTime Now
        {
            get
            {
                var philippinesTimeZone =
                    TimeZoneInfo.FindSystemTimeZoneById(
                        "Singapore Standard Time"
                    );

                return TimeZoneInfo.ConvertTimeFromUtc(
                    DateTime.UtcNow,
                    philippinesTimeZone
                );
            }
        }
    }
}