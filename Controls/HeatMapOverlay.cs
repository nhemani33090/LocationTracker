using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using LocationTracker.Models;
using LocationTracker.Services;
using Map = Microsoft.Maui.Controls.Maps.Map;

namespace LocationTracker.Controls
{
    /// <summary>
    /// Custom map overlay that renders location points as a heat map
    /// Uses color gradients to show density of location data
    /// </summary>
    public class HeatMapOverlay : GraphicsView, IDrawable
    {
        private List<LocationPoint> _locations = new();
        private const double BaseRadiusMeters = 50;
        private const double MaxIntensity = 1.0;
        private double _currentZoomLevel = 1.0;

        /// <summary>
        /// Gets or sets the list of location points to display
        /// </summary>
        public List<LocationPoint> Locations
        {
            get => _locations;
            set
            {
                _locations = value;
                Invalidate(); // Trigger redraw
            }
        }

        /// <summary>
        /// Reference to the parent map control
        /// Used for coordinate transformations
        /// </summary>
        public Map? ParentMap { get; set; }

        /// <summary>
        /// Constructor initializes the graphics view
        /// </summary>
        public HeatMapOverlay()
        {
            Drawable = this;
            BackgroundColor = Colors.Transparent;
        }

        /// <summary>
        /// Draws the heat map on the canvas
        /// Called automatically when the view needs to be rendered
        /// </summary>
        /// <param name="canvas">Drawing canvas</param>
        /// <param name="dirtyRect">Area that needs to be redrawn</param>
        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            if (_locations == null || _locations.Count == 0 || ParentMap == null)
                return;

            // Get visible region of the map
            var visibleRegion = ParentMap.VisibleRegion;
            if (visibleRegion == null)
                return;

            // Calculate zoom level based on visible region span
            _currentZoomLevel = CalculateZoomLevel(visibleRegion);

            // Calculate the bounds of the visible area
            var mapSpan = visibleRegion.LatitudeDegrees;
            var center = visibleRegion.Center;

            // Draw heat map circles for each location point
            foreach (var location in _locations)
            {
                // Check if location is within visible bounds
                if (!IsLocationVisible(location, visibleRegion))
                    continue;

                // Convert lat/long to screen coordinates
                var point = ConvertToScreenCoordinates(location, center, mapSpan, dirtyRect);

                // Calculate intensity based on proximity to other points
                var intensity = CalculateIntensity(location, visibleRegion);

                // Draw the heat map circle with gradient
                DrawHeatPoint(canvas, point, intensity);
            }
        }

        /// <summary>
        /// Calculates zoom level from visible region span
        /// Used to scale circle sizes appropriately
        /// </summary>
        private double CalculateZoomLevel(MapSpan visibleRegion)
        {
            // Larger span = zoomed out = smaller zoom level
            // Smaller span = zoomed in = larger zoom level
            var spanDegrees = visibleRegion.LatitudeDegrees;
            
            if (spanDegrees > 0.1) return 0.5; // Very zoomed out
            if (spanDegrees > 0.05) return 1.0; // Zoomed out
            if (spanDegrees > 0.01) return 2.0; // Normal
            if (spanDegrees > 0.005) return 3.0; // Zoomed in
            return 4.0; // Very zoomed in
        }

        /// <summary>
        /// Checks if a location point is within the visible map region
        /// </summary>
        private bool IsLocationVisible(LocationPoint location, MapSpan visibleRegion)
        {
            var latDelta = visibleRegion.LatitudeDegrees / 2;
            var lonDelta = visibleRegion.LongitudeDegrees / 2;
            var center = visibleRegion.Center;

            return location.Latitude >= center.Latitude - latDelta &&
                   location.Latitude <= center.Latitude + latDelta &&
                   location.Longitude >= center.Longitude - lonDelta &&
                   location.Longitude <= center.Longitude + lonDelta;
        }

        /// <summary>
        /// Converts geographic coordinates to screen pixel coordinates
        /// </summary>
        private PointF ConvertToScreenCoordinates(LocationPoint location, Location center, 
            double mapSpan, RectF dirtyRect)
        {
            // Calculate relative position within the visible region
            var latRange = mapSpan;
            var lonRange = mapSpan;

            var relativeX = (location.Longitude - center.Longitude + lonRange / 2) / lonRange;
            var relativeY = (center.Latitude - location.Latitude + latRange / 2) / latRange;

            // Convert to screen coordinates
            var x = (float)(relativeX * dirtyRect.Width);
            var y = (float)(relativeY * dirtyRect.Height);

            return new PointF(x, y);
        }

        /// <summary>
        /// Calculates the heat intensity for a location based on nearby points
        /// Higher intensity = more location points in the vicinity
        /// UPDATED: More stringent criteria for red (high heat)
        /// </summary>
        private double CalculateIntensity(LocationPoint location, MapSpan visibleRegion)
        {
            int nearbyCount = 0;
            const double influenceRadiusMeters = 100; // Points within 100m contribute to intensity

            foreach (var other in _locations)
            {
                if (other.Id == location.Id)
                    continue;

                var distance = LocationService.CalculateDistance(location, other);
                if (distance <= influenceRadiusMeters)
                {
                    nearbyCount++;
                }
            }

            // More stringent heat calculation:
            // 0-2 nearby points = Low (Blue) - intensity 0.3
            // 3-5 nearby points = Medium (Yellow/Orange) - intensity 0.5-0.7
            // 6+ nearby points = High (Red) - intensity 0.8-1.0
            
            if (nearbyCount == 0) return 0.3; // Single point - blue
            if (nearbyCount <= 2) return 0.4; // Few points - light blue
            if (nearbyCount <= 5) return 0.6; // Some clustering - yellow/orange
            if (nearbyCount <= 10) return 0.8; // Good clustering - orange/red
            
            // Only very dense clusters get max red
            return Math.Min(0.3 + (nearbyCount / 20.0), MaxIntensity);
        }

        /// <summary>
        /// Draws a single heat point with radial gradient
        /// Color varies from red (hot) to blue (cool) based on intensity
        /// Size scales with zoom level
        /// </summary>
        private void DrawHeatPoint(ICanvas canvas, PointF center, double intensity)
        {
            // Base radius scales with zoom level - smaller base size
            const float basePixelRadius = 15f; // Reduced from 50f
            var radius = basePixelRadius * (float)_currentZoomLevel;

            // Create color based on intensity
            // High intensity = Red, Medium = Yellow/Orange, Low = Blue
            Color heatColor;
            if (intensity >= 0.8)
            {
                heatColor = Colors.Red.WithAlpha(0.7f);
            }
            else if (intensity >= 0.6)
            {
                heatColor = Colors.Orange.WithAlpha(0.6f);
            }
            else if (intensity >= 0.4)
            {
                heatColor = Colors.Yellow.WithAlpha(0.5f);
            }
            else
            {
                heatColor = Colors.Blue.WithAlpha(0.4f);
            }

            // Draw multiple concentric circles for gradient effect
            for (int i = 3; i >= 1; i--)
            {
                var currentRadius = radius * (i / 3f);
                var alpha = 0.15f * (i / 3f);
                
                canvas.FillColor = heatColor.WithAlpha(alpha);
                canvas.FillCircle(center.X, center.Y, currentRadius);
            }

            // Draw center point - smaller and more subtle
            canvas.FillColor = heatColor.WithAlpha(0.8f);
            canvas.FillCircle(center.X, center.Y, 3); // Small center dot
        }

        /// <summary>
        /// Updates the heat map with new location data
        /// </summary>
        /// <param name="locations">Updated list of location points</param>
        public void UpdateHeatMap(List<LocationPoint> locations)
        {
            Locations = locations;
        }
    }
}