using SQLite;

namespace LocationTracker.Models
{
    /// <summary>
    /// Represents a geographic location point with timestamp
    /// Stores latitude, longitude, and when the location was recorded
    /// </summary>
    [Table("locations")]
    public class LocationPoint
    {
        /// <summary>
        /// Primary key for database records
        /// Auto-increments with each new location entry
        /// </summary>
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        /// <summary>
        /// Latitude coordinate in decimal degrees
        /// Range: -90 to 90 degrees
        /// </summary>
        [NotNull]
        public double Latitude { get; set; }

        /// <summary>
        /// Longitude coordinate in decimal degrees
        /// Range: -180 to 180 degrees
        /// </summary>
        [NotNull]
        public double Longitude { get; set; }

        /// <summary>
        /// Timestamp when the location was captured
        /// Stored as UTC time
        /// </summary>
        [NotNull]
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Optional accuracy of the location reading in meters
        /// Lower values indicate higher precision
        /// </summary>
        public double? Accuracy { get; set; }

        /// <summary>
        /// Creates a new location point with current timestamp
        /// </summary>
        public LocationPoint()
        {
            Timestamp = DateTime.UtcNow;
        }

        /// <summary>
        /// Creates a new location point with specified coordinates
        /// </summary>
        /// <param name="latitude">Latitude in decimal degrees</param>
        /// <param name="longitude">Longitude in decimal degrees</param>
        public LocationPoint(double latitude, double longitude)
        {
            Latitude = latitude;
            Longitude = longitude;
            Timestamp = DateTime.UtcNow;
        }

        /// <summary>
        /// Creates a new location point with coordinates and accuracy
        /// </summary>
        /// <param name="latitude">Latitude in decimal degrees</param>
        /// <param name="longitude">Longitude in decimal degrees</param>
        /// <param name="accuracy">Accuracy in meters</param>
        public LocationPoint(double latitude, double longitude, double accuracy)
        {
            Latitude = latitude;
            Longitude = longitude;
            Accuracy = accuracy;
            Timestamp = DateTime.UtcNow;
        }

        /// <summary>
        /// Returns a string representation of the location point
        /// </summary>
        public override string ToString()
        {
            return $"Location: ({Latitude:F6}, {Longitude:F6}) at {Timestamp:g}";
        }
    }
}