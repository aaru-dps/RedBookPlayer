using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using RedBookPlayer.GUI.ViewModels;

namespace RedBookPlayer.GUI
{
    public class PlayerView : UserControl
    {
        /// <summary>
        /// Read-only access to the view model
        /// </summary>
        public PlayerViewModel PlayerViewModel => DataContext as PlayerViewModel;

        /// <summary>
        /// Initialize the UI based on the default theme
        /// </summary>
        public PlayerView() : this(null, null) { }

        /// <summary>
        /// Initialize the UI based on the default theme with an existing view model
        /// </summary>
        /// <param name="xaml">XAML data representing the theme, null for default</param>
        /// <param name="playerViewModel">Existing PlayerViewModel to load in instead of creating a new one</param>
        public PlayerView(PlayerViewModel playerViewModel) : this(null, playerViewModel) { }

        /// <summary>
        /// Initialize the UI based on the currently selected theme
        /// </summary>
        /// <param name="xaml">XAML data representing the theme, null for default</param>
        /// <param name="playerViewModel">Existing PlayerViewModel to load in instead of creating a new one</param>
        public PlayerView(string xaml, PlayerViewModel playerViewModel)
        {
            LoadTheme(xaml);

            if(playerViewModel != null)
                DataContext = playerViewModel;
            else
                DataContext = new PlayerViewModel();
        }

        #region Helpers

        /// <summary>
        /// Update the view model with new settings
        /// </summary>
        public void UpdateViewModel()
        {
            PlayerViewModel.SetLoadDataTracks(App.Settings.PlayDataTracks);
            PlayerViewModel.SetLoadHiddenTracks(App.Settings.PlayHiddenTracks);
        }

        /// <summary>
        /// Load the theme from a XAML, if possible
        /// </summary>
        /// <param name="xaml">XAML data representing the theme, null for default</param>
        private void LoadTheme(string xaml)
        {
            try
            {
                if(xaml != null)
                    new AvaloniaXamlLoader().Load(xaml, null, this);
                else
                    AvaloniaXamlLoader.Load(this);
            }
            catch
            {
                AvaloniaXamlLoader.Load(this);
            }
        }

        #endregion
    }
}