using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using LocationTracker.Services;
using LocationTracker.Models;

namespace LocationTracker
{
    /// <summary>
    /// Main page of the Location Tracker application
    /// Manages UI interactions and coordinates between services
    /// </summary>
    public partial class MainPage : ContentPage
    {
        private readonly LocationService _locationService;
        private readonly DatabaseService _databaseService;
        private bool _isInitialized = false;
        private System.Timers.Timer? _heatMapRefreshTimer = null;

        /// <summary>
        /// Constructor initializes services and UI components
        /// </summary>
        public MainPage()
        {
            InitializeComponent();
            
            // Initialize services
            _locationService = new LocationService();
            _databaseService = DatabaseService.Instance;

            // Subscribe to location updates
            _locationService.LocationUpdated += OnLocationUpdated;

            // Set up the heat map overlay
            HeatMapOverlay.ParentMap = MainMap;

            // Subscribe to map changes to update heat map when panning/zooming
            MainMap.PropertyChanged += OnMapPropertyChanged;

            // Initialize the map
            _ = InitializeAsync();
        }

        /// <summary>
        /// Handles map property changes to redraw heat map
        /// </summary>
        private void OnMapPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            // Redraw heat map when map region changes
            if (e.PropertyName == "VisibleRegion" || e.PropertyName == "MapElements")
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    HeatMapOverlay.Invalidate();
                });
            }
        }

        /// <summary>
        /// Initializes the application state asynchronously
        /// Requests permissions and loads existing data
        /// </summary>
        private async Task InitializeAsync()
        {
            try
            {
                // Request location permissions
                var hasPermission = await _locationService.RequestPermissionsAsync();
                
                if (!hasPermission)
                {
                    await DisplayAlert("Permission Required", 
                        "Location permission is required to track your location.", 
                        "OK");
                    StatusLabel.Text = "Permission denied";
                    return;
                }

                // Get current location and center map
                var currentLocation = await _locationService.GetCurrentLocationAsync();
                if (currentLocation != null)
                {
                    CenterMapOnLocation(currentLocation);
                }

                // Load existing locations from database
                await LoadLocationsAsync();

                _isInitialized = true;
                StatusLabel.Text = "Ready to track";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Initialization error: {ex.Message}");
                await DisplayAlert("Error", "Failed to initialize the app.", "OK");
            }
        }

        /// <summary>
        /// Handles the Start Tracking button click event
        /// Begins continuous location tracking
        /// </summary>
        private async void OnStartTrackingClicked(object sender, EventArgs e)
        {
            try
            {
                if (!_isInitialized)
                {
                    await DisplayAlert("Not Ready", "App is still initializing. Please wait.", "OK");
                    return;
                }

                // Start tracking
                await _locationService.StartTrackingAsync();

                // Update UI
                StartButton.IsEnabled = false;
                StopButton.IsEnabled = true;
                StatusLabel.Text = "Tracking active...";

                // Start periodic heat map refresh timer
                StartHeatMapRefreshTimer();

                await DisplayAlert("Tracking Started", 
                    "Your location is being tracked every 5 seconds.", 
                    "OK");
            }
            catch (UnauthorizedAccessException)
            {
                await DisplayAlert("Permission Denied", 
                    "Location permission is required to track your location.", 
                    "OK");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error starting tracking: {ex.Message}");
                await DisplayAlert("Error", "Failed to start tracking.", "OK");
            }
        }

        /// <summary>
        /// Handles the Stop Tracking button click event
        /// Stops continuous location tracking
        /// </summary>
        private void OnStopTrackingClicked(object sender, EventArgs e)
        {
            try
            {
                // Stop tracking
                _locationService.StopTracking();

                // Stop heat map refresh timer
                StopHeatMapRefreshTimer();

                // Update UI
                StartButton.IsEnabled = true;
                StopButton.IsEnabled = false;
                StatusLabel.Text = "Tracking stopped";

                DisplayAlert("Tracking Stopped", "Location tracking has been stopped.", "OK");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error stopping tracking: {ex.Message}");
                DisplayAlert("Error", "Failed to stop tracking.", "OK");
            }
        }

        /// <summary>
        /// Handles the Refresh Map button click event
        /// Reloads location data and updates heat map
        /// </summary>
        private async void OnRefreshMapClicked(object sender, EventArgs e)
        {
            try
            {
                StatusLabel.Text = "Refreshing map...";
                await LoadLocationsAsync();
                StatusLabel.Text = _locationService.IsTracking ? "Tracking active..." : "Ready to track";
                
                await DisplayAlert("Refreshed", "Map has been updated with latest data.", "OK");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error refreshing map: {ex.Message}");
                await DisplayAlert("Error", "Failed to refresh map.", "OK");
            }
        }

        /// <summary>
        /// Handles the Clear Data button click event
        /// Removes all location data from database after confirmation
        /// </summary>
        private async void OnClearDataClicked(object sender, EventArgs e)
        {
            try
            {
                bool confirm = await DisplayAlert("Clear All Data?", 
                    "This will delete all tracked locations. This action cannot be undone.", 
                    "Delete", 
                    "Cancel");

                if (confirm)
                {
                    // Stop tracking if active
                    if (_locationService.IsTracking)
                    {
                        _locationService.StopTracking();
                        StartButton.IsEnabled = true;
                        StopButton.IsEnabled = false;
                    }

                    // Delete all locations
                    await _databaseService.DeleteAllLocationsAsync();

                    // Clear the heat map
                    HeatMapOverlay.UpdateHeatMap(new List<LocationPoint>());

                    // Update UI
                    LocationCountLabel.Text = "0 locations";
                    LastUpdateLabel.Text = "No updates yet";
                    StatusLabel.Text = "Data cleared";

                    await DisplayAlert("Data Cleared", "All location data has been deleted.", "OK");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error clearing data: {ex.Message}");
                await DisplayAlert("Error", "Failed to clear data.", "OK");
            }
        }

        /// <summary>
        /// Event handler for location updates
        /// Called when a new location is tracked
        /// </summary>
        private void OnLocationUpdated(object? sender, LocationPoint location)
        {
            // Run on UI thread
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                try
                {
                    // Update status labels
                    var count = await _databaseService.GetLocationCountAsync();
                    LocationCountLabel.Text = $"{count} location{(count != 1 ? "s" : "")}";
                    LastUpdateLabel.Text = $"Updated {DateTime.Now:HH:mm:ss}";

                    // Reload heat map
                    await LoadLocationsAsync();

                    System.Diagnostics.Debug.WriteLine($"UI updated with new location: {location}");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error updating UI: {ex.Message}");
                }
            });
        }

        /// <summary>
        /// Loads all locations from database and updates the heat map
        /// </summary>
        private async Task LoadLocationsAsync()
        {
            try
            {
                // Get all locations from database
                var locations = await _databaseService.GetAllLocationsAsync();

                // Update location count
                LocationCountLabel.Text = $"{locations.Count} location{(locations.Count != 1 ? "s" : "")}";

                if (locations.Count > 0)
                {
                    // Update last update time
                    var latest = locations.First();
                    LastUpdateLabel.Text = $"Last: {latest.Timestamp.ToLocalTime():HH:mm:ss}";

                    // Update heat map
                    HeatMapOverlay.UpdateHeatMap(locations);

                    System.Diagnostics.Debug.WriteLine($"Loaded {locations.Count} locations");
                }
                else
                {
                    LastUpdateLabel.Text = "No updates yet";
                    HeatMapOverlay.UpdateHeatMap(new List<LocationPoint>());
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading locations: {ex.Message}");
            }
        }

        /// <summary>
        /// Centers the map on a specific location
        /// </summary>
        /// <param name="location">Location to center on</param>
        private void CenterMapOnLocation(LocationPoint location)
        {
            try
            {
                var mapLocation = new Location(location.Latitude, location.Longitude);
                var mapSpan = new MapSpan(mapLocation, 0.01, 0.01); // Approximately 1km radius
                MainMap.MoveToRegion(mapSpan);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error centering map: {ex.Message}");
            }
        }

        /// <summary>
        /// Cleanup when page is removed from navigation
        /// Stops tracking and unsubscribes from events
        /// </summary>
        protected override void OnDisappearing()
        {
            base.OnDisappearing();

            // Stop tracking when leaving the page
            if (_locationService.IsTracking)
            {
                _locationService.StopTracking();
            }

            // Stop refresh timer
            StopHeatMapRefreshTimer();

            // Unsubscribe from events
            _locationService.LocationUpdated -= OnLocationUpdated;
            MainMap.PropertyChanged -= OnMapPropertyChanged;
        }

        /// <summary>
        /// Starts a timer to periodically refresh the heat map
        /// </summary>
        private void StartHeatMapRefreshTimer()
        {
            if (_heatMapRefreshTimer != null)
            {
                _heatMapRefreshTimer.Stop();
                _heatMapRefreshTimer.Dispose();
            }

            _heatMapRefreshTimer = new System.Timers.Timer(2000); // Refresh every 2 seconds
            _heatMapRefreshTimer.Elapsed += (s, e) =>
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    HeatMapOverlay.Invalidate();
                });
            };
            _heatMapRefreshTimer.Start();
        }

        /// <summary>
        /// Stops the heat map refresh timer
        /// </summary>
        private void StopHeatMapRefreshTimer()
        {
            if (_heatMapRefreshTimer != null)
            {
                _heatMapRefreshTimer.Stop();
                _heatMapRefreshTimer.Dispose();
                _heatMapRefreshTimer = null;
            }
        }
    }
}