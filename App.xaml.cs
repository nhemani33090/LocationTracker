namespace LocationTracker
{
    /// <summary>
    /// Main application class that manages the app lifecycle
    /// Entry point for the .NET MAUI application
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// Constructor initializes the application
        /// Sets up the main page and application-wide resources
        /// </summary>
        public App()
        {
            InitializeComponent();

            // Set the main page as the starting page
            MainPage = new MainPage();
        }

        /// <summary>
        /// Called when the application is created
        /// Override to perform initialization tasks
        /// </summary>
        protected override void OnStart()
        {
            base.OnStart();
            System.Diagnostics.Debug.WriteLine("Location Tracker App Started");
        }

        /// <summary>
        /// Called when the application goes to sleep (background)
        /// </summary>
        protected override void OnSleep()
        {
            base.OnSleep();
            System.Diagnostics.Debug.WriteLine("Location Tracker App Sleeping");
        }

        /// <summary>
        /// Called when the application resumes from sleep
        /// </summary>
        protected override void OnResume()
        {
            base.OnResume();
            System.Diagnostics.Debug.WriteLine("Location Tracker App Resumed");
        }
    }
}