﻿using MahApps.Metro.Controls;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ValorantCC.SubWindow;
namespace ValorantCC
{
    /// <summary>
    /// Interaction logic for ProfilesWindow.xaml
    /// </summary>

    public struct PublicProfile
    {
        public String owner { get; set; }
        public String sharecode { get; set; }
        public CrosshairProfile settings { get; set; }
        public int ID { get; set; }
    }

    public partial class ProfilesWindow : MetroWindow
    {
        public CrosshairProfile selected;
        private static API ValCCApi;
        private static MainWindow main;
        private static List<PublicProfile> PublicProfiles = new List<PublicProfile>();
        private int _offset = 0;
        public ProfilesWindow(CrosshairProfile current, API ValCCAPI)
        {
            InitializeComponent();
            main = (MainWindow)Application.Current.MainWindow;
            selected = current;
            ValCCApi = ValCCAPI;
            Utilities.Utils.Log("Community Profiles Loading...");
        }

        private async void ShareablesContainer_Loaded(object sender, RoutedEventArgs e)
        {
            LoadingPlaceHolder.Visibility = Visibility.Visible;
            Pagination.Visibility = Visibility.Collapsed;
            fetchErrorTxt.Visibility = Visibility.Collapsed;
            try
            {
                bool fetchSucc = await InitialFetch();
                if (!fetchSucc) fetchErrorTxt.Visibility = Visibility.Visible;
                else
                {
                    await RenderProfiles();
                }
            }
            catch (Exception ex)
            {
                Utilities.Utils.Log(ex.StackTrace.ToString());
            }
            LoadingPlaceHolder.Visibility = Visibility.Collapsed;
            Pagination.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// Returns the searched sharecode or the list of shareables depending if sharecode is provided or not.
        /// </summary>
        /// <param name="sharecode">Nullable. Searched Code</param>
        private async Task<bool> InitialFetch(String sharecode = null)
        {
            Utilities.Utils.Log("Disabling Search button.");
            btnSearchCode.IsEnabled = false;
            Utilities.Utils.Log("Clearing arrays.");
            PublicProfiles.Clear();
            ShareablesContainer.Children.Clear();
            List<ShareableProfile> Shareables;
            if (!string.IsNullOrWhiteSpace(sharecode))
            {
                Utilities.Utils.Log($"Fetching profile with code: {sharecode}");
                FetchResponse fetchResponse = await ValCCApi.Fetch(sharecode);
                if (!fetchResponse.success) return false;
                if (fetchResponse.data.Count < 1)
                {
                    fetchErrorTxt.Text = "User with corresponding sharecode was not found.";
                    btnSearchCode.IsEnabled = true;
                    return false;
                }
                Shareables = fetchResponse.data;
            }
            else
            {
                Utilities.Utils.Log($"Fetching profiles from server");
                FetchResponse fetchResponse = await ValCCApi.Fetch(Offset: _offset);
                if (!fetchResponse.success) return false;
                Shareables = fetchResponse.data;
                Pagination.Visibility = Visibility.Visible;
            }

            if (Shareables.Count == 0) return true;

            for (int i = 0; i < Shareables.Count; i++)
            {
                ShareableProfile currentShareable = Shareables[i];
                CrosshairProfile profile;
                profile = JsonConvert.DeserializeObject<CrosshairProfile>(Regex.Unescape(currentShareable.settings));
                PublicProfiles.Add(new PublicProfile() { owner = currentShareable.displayName, settings = profile, sharecode = currentShareable.shareCode, ID = i });
            }
            btnSearchCode.IsEnabled = true;
            return true;
        }

        /// <summary>
        /// Render the PublicProfiles var into frontend.
        /// </summary>
        private async Task<bool> RenderProfiles()
        {
            UIElementCollection shareablesElement = ShareablesContainer.Children;
            if (PublicProfiles.Count == 0) return true;

            for (int i = 0; i < PublicProfiles.Count; i++)
            {
                PublicProfile profile = PublicProfiles[i];
                shareablesElement.Add(await this.GenerateRender(profile));
            }
            return true;
        }
        private async void btnSearchCode_Click(object sender, RoutedEventArgs e)
        {
            LoadingPlaceHolder.Visibility = Visibility.Visible;
            Pagination.Visibility = Visibility.Collapsed;
            fetchErrorTxt.Visibility = Visibility.Collapsed;
            try
            {
                bool fetchSucc = await InitialFetch(SearchCode.Text);
                if (!fetchSucc) fetchErrorTxt.Visibility = Visibility.Visible;
                else
                    await RenderProfiles();
            }
            catch (Exception ex)
            {
                Utilities.Utils.Log(ex.StackTrace.ToString());
            }
            LoadingPlaceHolder.Visibility = Visibility.Collapsed;
        }

        public void shareBtnClicked(object sender, RoutedEventArgs e)
        {
            String shareCode = ((Button)sender).Name.Split('_')[1];
            Clipboard.SetText(shareCode);
            MessageWindow.Show("\"" + shareCode + "\" has been copied to clipboard!");
            Utilities.Utils.Log($"{shareCode} copied to clipboard.");
        }

        public void detailsBtnClicked(object sender, RoutedEventArgs e)
        {
            PublicProfile pressedPubProfile = PublicProfiles[Int32.Parse(((Button)sender).Name.Split('_')[1])];
            CrosshairProfile pressedProfile = pressedPubProfile.settings;
            MessageWindow.Show($"{pressedPubProfile.owner}'s Profile: {pressedProfile.ProfileName}\nPrimary:\n\tInner Lines: {pressedProfile.Primary.InnerLines.Opacity}, {pressedProfile.Primary.InnerLines.LineLength}, {pressedProfile.Primary.InnerLines.LineThickness}, {pressedProfile.Primary.InnerLines.LineOffset}\n\tOuter Lines: {pressedProfile.Primary.OuterLines.Opacity}, {pressedProfile.Primary.OuterLines.LineLength}, {pressedProfile.Primary.OuterLines.LineThickness}, {pressedProfile.Primary.OuterLines.LineOffset}\n\nADS:\n\tInner Lines: {pressedProfile.aDS.InnerLines.Opacity}, {pressedProfile.aDS.InnerLines.LineLength}, {pressedProfile.aDS.InnerLines.LineThickness}, {pressedProfile.aDS.InnerLines.LineOffset}\n\tOuter Lines: {pressedProfile.aDS.OuterLines.Opacity}, {pressedProfile.aDS.OuterLines.LineLength}, {pressedProfile.aDS.OuterLines.LineThickness}, {pressedProfile.aDS.OuterLines.LineOffset}");
            Utilities.Utils.Log($"Details button clicked.");
        }

        public void applyBtnClicked(object sender, RoutedEventArgs e)
        {
            Utilities.Utils.Log("Apply button clicked.");
            PublicProfile pressedPubProfile = PublicProfiles[Int32.Parse(((Button)sender).Name.Split('_')[1])];
            CrosshairProfile pressedProfile = pressedPubProfile.settings;
            selected.aDS = pressedProfile.aDS;
            selected.Primary = pressedProfile.Primary;
            selected.Sniper = pressedProfile.Sniper;
            selected.bUseAdvancedOptions = pressedProfile.bUseAdvancedOptions;
            selected.bUseCustomCrosshairOnAllPrimary = pressedProfile.bUseCustomCrosshairOnAllPrimary;
            selected.bUsePrimaryCrosshairForADS = pressedProfile.bUsePrimaryCrosshairForADS;

            main.primary_color.SelectedColor = new Color() { R = selected.Primary.Color.R, G = selected.Primary.Color.G, B = selected.Primary.Color.B, A = selected.Primary.Color.A };
            main.ads_color.SelectedColor = new Color() { R = selected.aDS.Color.R, G = selected.aDS.Color.G, B = selected.aDS.Color.B, A = selected.aDS.Color.A };

            main.prim_outline_color.SelectedColor = new Color() { R = selected.Primary.OutlineColor.R, G = selected.Primary.OutlineColor.G, B = selected.Primary.OutlineColor.B, A = selected.Primary.OutlineColor.A };
            main.ads_outline_color.SelectedColor = new Color() { R = selected.aDS.OutlineColor.R, G = selected.aDS.OutlineColor.G, B = selected.aDS.OutlineColor.B, A = selected.aDS.OutlineColor.A };

            main.sniper_dot_color.SelectedColor = new Color() { R = selected.Sniper.CenterDotColor.R, G = selected.Sniper.CenterDotColor.G, B = selected.Sniper.CenterDotColor.B, A = selected.Sniper.CenterDotColor.A };
            main.SelectedProfile = selected;
            main.Crosshair_load();
        }

        private void ExitClick(object sender, RoutedEventArgs e)
        {
            Close();
        }
        private void MinimizeClick(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void TabButtonEnter(object sender, MouseEventArgs e)
        {
            ((Button)sender).Foreground = new SolidColorBrush(Colors.Gray);
        }
        private void TabButtonLeave(object sender, MouseEventArgs e)
        {
            ((Button)sender).Foreground = new SolidColorBrush(Colors.White);
        }

        private void Border_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed) DragMove();
        }

        private void LoadingPlaceHolder_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (LoadingPlaceHolder.Visibility == Visibility.Visible)
                WpfAnimatedGif.ImageBehavior.GetAnimationController(LoadingPlaceHolder)?.Play();
            else
                WpfAnimatedGif.ImageBehavior.GetAnimationController(LoadingPlaceHolder)?.Pause();
        }

        private async void btnNextOffset_MouseUp(object sender, MouseButtonEventArgs e)
        {
            LoadingPlaceHolder.Visibility = Visibility.Visible;
            Pagination.Visibility = Visibility.Collapsed;
            fetchErrorTxt.Visibility = Visibility.Collapsed;
            _offset += 20;
            try
            {
                bool fetchSucc = await InitialFetch();
                if (!fetchSucc) fetchErrorTxt.Visibility = Visibility.Visible;
                else
                {
                    await RenderProfiles();
                }
            }
            catch (Exception ex)
            {
                Utilities.Utils.Log(ex.StackTrace.ToString());
            }
            LoadingPlaceHolder.Visibility = Visibility.Collapsed;
            Pagination.Visibility = Visibility.Visible;
        }

        private async void btnPreviousOffset_MouseUp(object sender, MouseButtonEventArgs e)
        {
            LoadingPlaceHolder.Visibility = Visibility.Visible;
            Pagination.Visibility = Visibility.Collapsed;
            fetchErrorTxt.Visibility = Visibility.Collapsed;
            if (_offset >= 20) _offset -= 20;
            try
            {
                bool fetchSucc = await InitialFetch();
                if (!fetchSucc) fetchErrorTxt.Visibility = Visibility.Visible;
                else
                {
                    await RenderProfiles();
                }
            }
            catch (Exception ex)
            {
                Utilities.Utils.Log(ex.StackTrace.ToString());
            }
            LoadingPlaceHolder.Visibility = Visibility.Collapsed;
            Pagination.Visibility = Visibility.Visible;
        }
    }
}
