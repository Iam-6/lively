using Lively.Common;
using Lively.Grpc.Client;
using Lively.UI.WinUI.Extensions;
using Lively.UI.WinUI.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.Web.WebView2.Core;
using System;

namespace Lively.UI.WinUI.Views.Pages
{
    public sealed partial class PatreonSupportersView : Page
    {
        private readonly PatreonSupportersViewModel viewModel;

        public PatreonSupportersView()
        {
            this.InitializeComponent();
            this.viewModel = App.Services.GetRequiredService<PatreonSupportersViewModel>();
            this.DataContext = viewModel;

            // Error when setting in xaml
            WebView.DefaultBackgroundColor = ((SolidColorBrush)App.Current.Resources["ApplicationPageBackgroundThemeBrush"]).Color;
            // Set website to reflect app interface
            var pageTheme = App.Services.GetRequiredService<IUserSettingsClient>().Settings.ApplicationTheme switch
            {
                AppTheme.Auto => "auto", // Website handles theme change based on WebView change.
                AppTheme.Light => "light",
                AppTheme.Dark => "dark",
                _ => "auto",
            };
            var accentColorDark1 = ((Windows.UI.Color)App.Current.Resources["SystemAccentColorDark1"]).ToHex().Substring(1);
            var accentColorLight1 = ((Windows.UI.Color)App.Current.Resources["SystemAccentColorLight1"]).ToHex().Substring(1);
            var param = $"?theme={pageTheme}&colorLight={accentColorLight1}&colorDark={accentColorDark1}";

            var url = viewModel.IsBetaBuild ?
                $"https://www.rocksdanister.com/lively-webpage/supporters/{param}" :
                $"https://www.rocksdanister.com/lively/supporters/{param}";
            WebView.Source = LinkUtil.SanitizeUrl(url);
        }

        private void WebView_CoreWebView2Initialized(WebView2 sender, CoreWebView2InitializedEventArgs args)
        {
            if (args.Exception != null)
            {
                viewModel.SupportersFetchError = args.Exception.ToString();
            }
            else
            {
                WebView.CoreWebView2.NewWindowRequested += CoreWebView2_NewWindowRequested;
                // Theme need to set css, ref: https://github.com/MicrosoftEdge/WebView2Feedback/issues/4426
                WebView.CoreWebView2.Profile.PreferredColorScheme = CoreWebView2PreferredColorScheme.Auto;
                // Don't allow contextmenu and devtools
                WebView.CoreWebView2.Settings.AreBrowserAcceleratorKeysEnabled = false;
                WebView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
            }
        }

        private void CoreWebView2_NewWindowRequested(Microsoft.Web.WebView2.Core.CoreWebView2 sender, Microsoft.Web.WebView2.Core.CoreWebView2NewWindowRequestedEventArgs args)
        {
            // Prevent popups
            if (!args.IsUserInitiated)
                return;

            args.Handled = true;
            LinkUtil.OpenBrowser(args.Uri);
        }

        private void WebView_NavigationStarting(WebView2 sender, Microsoft.Web.WebView2.Core.CoreWebView2NavigationStartingEventArgs args)
        {
            // Stay in page
            if (args.IsRedirected)
                args.Cancel = true;
            else
                WebViewProgress.Visibility = Visibility.Visible;
        }

        private void WebView_NavigationCompleted(WebView2 sender, CoreWebView2NavigationCompletedEventArgs args)
        {
            WebViewProgress.Visibility = Visibility.Collapsed;
        }

        public void OnClose()
        {
            WebView.Close();
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            // Unloaded is not reliable when used in dialog
        }
    }
}