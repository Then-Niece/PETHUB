namespace PETHUB.Helpers
{
    /// <summary>
    /// Provides reusable address information
    /// used throughout the application.
    /// This helper acts as the single source of truth
    /// for all Provinces, Cities and Barangays.
    /// </summary>
    public static class AddressHelper
    {
        /// <summary>
        /// Stores the available locations.
        /// Structure:
        /// Province
        ///     └── City
        ///             └── Barangays
        /// </summary>
        public static readonly Dictionary<string, Dictionary<string, List<string>>> Locations
            = new()
            {
                ["Cebu"] = new()
                {
                    ["Cebu City"] = new()
                    {
                        "Lahug",
                        "Apas",
                        "Guadalupe"
                    },

                    ["Mandaue City"] = new()
                    {
                        "Centro",
                        "Subangdaku",
                        "Tipolo"
                    },

                    ["Talisay City"] = new()
                    {
                        "Bulacao",
                        "Dumlog",
                        "Tabunok"
                    }
                }
            };

        /// <summary>
        /// Returns all available Provinces.
        /// </summary>
        public static List<string> GetProvinces()
        {
            return Locations.Keys.ToList();
        }

        /// <summary>
        /// Returns all Cities that belong
        /// to the selected Province.
        /// </summary>
        public static List<string> GetCities(string province)
        {
            if (Locations.TryGetValue(province, out var cities))
            {
                return cities.Keys.ToList();
            }

            return new List<string>();
        }

        /// <summary>
        /// Returns all Barangays that belong
        /// to the selected City.
        /// </summary>
        public static List<string> GetBarangays(
            string province,
            string city)
        {
            if (Locations.TryGetValue(province, out var cities))
            {
                if (cities.TryGetValue(city, out var barangays))
                {
                    return barangays;
                }
            }

            return new List<string>();
        }
    }

}