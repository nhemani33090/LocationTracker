using LocationTracker.Models;

namespace LocationTracker.Services
{
    /// <summary>
    /// Service for tracking user location using device GPS
    /// Manages permission requests and continuous location monitoring
    /// </summary>
    public class LocationService
    {
        private CancellationTokenSource? _cancelTokenSource;
        private bool _isTracking = false;
        private const int TrackingIntervalSeconds = 5; // Update location every 5 seconds

        /// <summary>
        /// Event fired when a new location is captured
        /// Subscribers can react to location updates in real-time
        /// </summary>
        public event EventHandler<LocationPoint>? LocationUpdated;

        /// <summary>
        /// Checks if location tracking is currently active
        /// </summary>
        public bool IsTracking => _isTracking;

        /// <summary>
        /// Requests location permissions from the user
        /// Must be called before attempting to track location
        /// </summary>
        /// <returns>True if permission granted, false otherwise</returns>
        public async Task<bool> RequestPermissionsAsync()
        {
            try
            {
                var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();

                if (status != PermissionStatus.Granted)
                {
                    status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
                }

                return status == PermissionStatus.Granted;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error requesting permissions: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Gets the current device location as a one-time request
        /// </summary>
        /// <returns>Current location or null if unable to retrieve</returns>
        public async Task<LocationPoint?> GetCurrentLocationAsync()
        {
            try
            {
                // Check permissions first
                var hasPermission = await RequestPermissionsAsync();
                if (!hasPermission)
                {
                    System.Diagnostics.Debug.WriteLine("Location permission not granted");
                    return null;
                }

                // Get the current location
                var request = new GeolocationRequest(GeolocationAccuracy.Best, TimeSpan.FromSeconds(10));
                var location = await Geolocation.Default.GetLocationAsync(request);

                if (location != null)
                {
                    return new LocationPoint(
                        location.Latitude,
                        location.Longitude,
                        location.Accuracy ?? 0
                    );
                }

                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting current location: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Starts continuous location tracking
        /// Captures location at regular intervals and saves to database
        /// </summary>
        public async Task StartTrackingAsync()
        {
            if (_isTracking)
            {
                System.Diagnostics.Debug.WriteLine("Location tracking is already active");
                return;
            }

            // Check permissions
            var hasPermission = await RequestPermissionsAsync();
            if (!hasPermission)
            {
                throw new UnauthorizedAccessException("Location permission not granted");
            }

            _isTracking = true;
            _cancelTokenSource = new CancellationTokenSource();

            // Start tracking loop in background
            _ = Task.Run(async () => await TrackingLoopAsync(_cancelTokenSource.Token));
        }

        /// <summary>
        /// Stops continuous location tracking
        /// </summary>
        public void StopTracking()
        {
            if (_cancelTokenSource != null && !_cancelTokenSource.IsCancellationRequested)
            {
                _cancelTokenSource.Cancel();
            }
            _isTracking = false;
            System.Diagnostics.Debug.WriteLine("Location tracking stopped");
        }

        /// <summary>
        /// Internal loop that continuously tracks location
        /// Runs until tracking is stopped
        /// </summary>
        /// <param name="cancellationToken">Token to signal tracking cancellation</param>
        private async Task TrackingLoopAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    // Get current location
                    var locationPoint = await GetCurrentLocationAsync();

                    if (locationPoint != null)
                    {
                        // Save to database
                        await DatabaseService.Instance.SaveLocationAsync(locationPoint);

                        // Notify subscribers
                        LocationUpdated?.Invoke(this, locationPoint);

                        System.Diagnostics.Debug.WriteLine($"Location tracked: {locationPoint}");
                    }

                    // Wait before next update
                    await Task.Delay(TimeSpan.FromSeconds(TrackingIntervalSeconds), cancellationToken);
                }
                catch (TaskCanceledException)
                {
                    // Expected when tracking is stopped
                    break;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error in tracking loop: {ex.Message}");
                    // Continue tracking despite errors
                    await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
                }
            }

            _isTracking = false;
        }

        /// <summary>
        /// Calculates the distance between two location points in meters
        /// Uses the Haversine formula for great-circle distance
        /// </summary>
        /// <param name="location1">First location point</param>
        /// <param name="location2">Second location point</param>
        /// <returns>Distance in meters</returns>
        public static double CalculateDistance(LocationPoint location1, LocationPoint location2)
        {
            return Location.CalculateDistance(
                location1.Latitude,
                location1.Longitude,
                location2.Latitude,
                location2.Longitude,
                DistanceUnits.Kilometers
            ) * 1000; // Convert to meters
        }
    }
}