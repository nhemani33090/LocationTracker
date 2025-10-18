using SQLite;
using LocationTracker.Models;

namespace LocationTracker.Services
{
    /// <summary>
    /// Service for managing SQLite database operations for location tracking
    /// Implements singleton pattern for database connection management
    /// </summary>
    public class DatabaseService
    {
        private readonly SQLiteAsyncConnection _database;
        private static DatabaseService? _instance;
        private static readonly object _lock = new object();

        /// <summary>
        /// Gets the singleton instance of the database service
        /// Thread-safe implementation using double-check locking
        /// </summary>
        public static DatabaseService Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new DatabaseService();
                        }
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// Private constructor to enforce singleton pattern
        /// Initializes database connection and creates tables if needed
        /// </summary>
        private DatabaseService()
        {
            // Get the path to the database file in the app's local data folder
            string dbPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "locations.db3"
            );

            // Initialize the SQLite connection
            _database = new SQLiteAsyncConnection(dbPath);

            // Create the locations table if it doesn't exist
            _database.CreateTableAsync<LocationPoint>().Wait();
        }

        /// <summary>
        /// Saves a new location point to the database
        /// </summary>
        /// <param name="location">The location point to save</param>
        /// <returns>The number of rows inserted (should be 1)</returns>
        public async Task<int> SaveLocationAsync(LocationPoint location)
        {
            try
            {
                return await _database.InsertAsync(location);
            }
            catch (Exception ex)
            {
                // Log the error (in production, use proper logging framework)
                System.Diagnostics.Debug.WriteLine($"Error saving location: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Retrieves all location points from the database
        /// Ordered by timestamp in descending order (newest first)
        /// </summary>
        /// <returns>List of all location points</returns>
        public async Task<List<LocationPoint>> GetAllLocationsAsync()
        {
            try
            {
                return await _database.Table<LocationPoint>()
                    .OrderByDescending(l => l.Timestamp)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error retrieving locations: {ex.Message}");
                return new List<LocationPoint>();
            }
        }

        /// <summary>
        /// Retrieves location points within a specific time range
        /// Useful for filtering heat map data by date
        /// </summary>
        /// <param name="startDate">Start of the date range</param>
        /// <param name="endDate">End of the date range</param>
        /// <returns>List of location points within the specified range</returns>
        public async Task<List<LocationPoint>> GetLocationsByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                return await _database.Table<LocationPoint>()
                    .Where(l => l.Timestamp >= startDate && l.Timestamp <= endDate)
                    .OrderByDescending(l => l.Timestamp)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error retrieving locations by date: {ex.Message}");
                return new List<LocationPoint>();
            }
        }

        /// <summary>
        /// Gets the count of all location points in the database
        /// </summary>
        /// <returns>Total number of location points</returns>
        public async Task<int> GetLocationCountAsync()
        {
            try
            {
                return await _database.Table<LocationPoint>().CountAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting location count: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Deletes a specific location point from the database
        /// </summary>
        /// <param name="location">The location point to delete</param>
        /// <returns>The number of rows deleted (should be 1)</returns>
        public async Task<int> DeleteLocationAsync(LocationPoint location)
        {
            try
            {
                return await _database.DeleteAsync(location);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error deleting location: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Deletes all location points from the database
        /// Use with caution - this action cannot be undone
        /// </summary>
        /// <returns>The number of rows deleted</returns>
        public async Task<int> DeleteAllLocationsAsync()
        {
            try
            {
                return await _database.DeleteAllAsync<LocationPoint>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error deleting all locations: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Gets the most recent location point
        /// </summary>
        /// <returns>The most recent location or null if no locations exist</returns>
        public async Task<LocationPoint?> GetLatestLocationAsync()
        {
            try
            {
                return await _database.Table<LocationPoint>()
                    .OrderByDescending(l => l.Timestamp)
                    .FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting latest location: {ex.Message}");
                return null;
            }
        }
    }
}