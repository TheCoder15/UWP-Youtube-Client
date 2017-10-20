﻿using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.ApplicationModel.DataTransfer;
using Google.Apis.YouTube.v3;
using Google.Apis.Services;
using Google.Apis.Auth.OAuth2;
using Google.Apis.YouTube.v3.Data;
using VideoLibrary;
using YTApp.Classes;
using YTApp.Pages;


namespace YTApp
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private string YoutubeID = "";

        public List<SubscriptionDataType> subscriptionsList = new List<SubscriptionDataType>();

        List<SearchListResponse> youtubeVideos = new List<SearchListResponse>();

        public string OAuthToken;

        BackgroundWorker bg = new BackgroundWorker();

        public MainPage()
        {
            this.InitializeComponent();
            DoWork();
        }

        private async void DoWork()
        {
            UserCredential credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(new ClientSecrets {
            ClientId = "957928808020-pa0lopl3crh565k6jd4djaj36rm1d9i5.apps.googleusercontent.com",
            ClientSecret = "oB9U6yWFndnBqLKIRSA0nYGm" }, new[] { YouTubeService.Scope.Youtube }, "user", CancellationToken.None);

            // Create the service.
            var service = new YouTubeService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = "Youtube Viewer",
            });

            string nextPageToken;
            var tempSubscriptions = GetSubscriptions(null, service);
            foreach (Subscription sub in tempSubscriptions.Items)
            {
                var subscription = new SubscriptionDataType();
                subscription.Id = sub.Snippet.ResourceId.ChannelId;
                subscription.Thumbnail = new BitmapImage(new Uri(sub.Snippet.Thumbnails.Medium.Url));
                subscription.Title = sub.Snippet.Title;
                subscription.NewVideosCount = Convert.ToString(sub.ContentDetails.NewItemCount);
                subscriptionsList.Add(subscription);
            }
            if (tempSubscriptions.NextPageToken != null)
            {
                nextPageToken = tempSubscriptions.NextPageToken;
                while (nextPageToken != null)
                {
                    var tempSubs = GetSubscriptions(nextPageToken, service);
                    foreach (Subscription sub in tempSubs.Items)
                    {
                        var subscription = new SubscriptionDataType();
                        subscription.Id = sub.Snippet.ResourceId.ChannelId;
                        subscription.Thumbnail = new BitmapImage(new Uri(sub.Snippet.Thumbnails.Medium.Url));
                        subscription.Title = sub.Snippet.Title;
                        subscription.NewVideosCount = Convert.ToString(sub.ContentDetails.NewItemCount);
                        subscriptionsList.Add(subscription);
                    }
                    nextPageToken = tempSubs.NextPageToken;
                }
            }
            subscriptionsList.Sort((x, y) => string.Compare(x.Title, y.Title));
            
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            Window.Current.CoreWindow.KeyDown += Event_KeyDown;
            SearchBox.Focus(FocusState.Keyboard);
        }

        #region Search

        #region Events

        private void Search_Click(object sender, RoutedEventArgs e)
        {
            contentFrame.Navigate(typeof(SearchPage), new Params() { mainPageRef = this });
        }

        private void SearchBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                if (Uri.IsWellFormedUriString(SearchBox.Text, UriKind.Absolute))
                {
                    try
                    {
                        var youTube = YouTube.Default;
                        var video = youTube.GetVideo(SearchBox.Text);
                        StartVideo(video.Uri);
                    }
                    catch { }
                }
                contentFrame.Navigate(typeof(SearchPage), new Params() { mainPageRef = this });
            }
        }

        public class Params
        {
            public MainPage mainPageRef { get; set; }
        }

        #endregion

        #region API

        private SubscriptionListResponse GetSubscriptions(string NextPageToken, YouTubeService service)
        {
            var subscriptions = service.Subscriptions.List("snippet, contentDetails");
            subscriptions.PageToken = NextPageToken;
            subscriptions.Mine = true;
            subscriptions.MaxResults = 50;

            return subscriptions.Execute();
        }
        #endregion

        #endregion

        #region Media Viewer

        #region Methods

        public void StartVideo(string URL)
        {
            viewer.Visibility = Visibility.Visible;
            viewer.Source = new Uri(URL);
            viewer.TransportControls.Focus(FocusState.Programmatic);
            var _displayRequest = new Windows.System.Display.DisplayRequest();
            _displayRequest.RequestActive();
        }

        public void StopVideo()
        {
            if (MediaElementContainer.Width != 640)
            {
                MediaElementContainer.Width = 640;
                MediaElementContainer.Height = 360;
            }
            else
            {
                MediaElementContainer.Width = Double.NaN;
                MediaElementContainer.Height = Double.NaN;
            }
        }

        private void Storyboard_Completed(object sender, object e)
        {
            MediaElementContainer.Height = Double.NaN;
            MediaElementContainer.Width = Double.NaN;
        }

        #endregion

        #region Events


        private void MinimizeMediaElement_Click(object sender, RoutedEventArgs e)
        {
            StopVideo();
        }

        private void CloseMediaElement_Click(object sender, RoutedEventArgs e)
        {
            viewer.Stop();
            viewer.Visibility = Visibility.Collapsed;
        }

        private void viewer_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            viewer.IsFullWindow = !viewer.IsFullWindow;
            if (viewer.CurrentState == Windows.UI.Xaml.Media.MediaElementState.Playing)
            {
                viewer.Pause();
            }
            else
            {
                viewer.Play();
            }
        }

        private void Event_KeyDown(object sender, KeyEventArgs e)
        {
            if (viewer.IsFullWindow && e.VirtualKey == Windows.System.VirtualKey.Escape) { viewer.IsFullWindow = false; }
            if (viewer.CurrentState == Windows.UI.Xaml.Media.MediaElementState.Playing && e.VirtualKey == Windows.System.VirtualKey.Space && viewer.Visibility == Visibility.Visible)
            {
                viewer.Pause();
            }
            else if (e.VirtualKey == Windows.System.VirtualKey.Space && viewer.Visibility == Visibility.Visible)
            {
                viewer.Play();
            }
        }

        private void viewer_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (viewer.CurrentState == Windows.UI.Xaml.Media.MediaElementState.Playing)
            {
                viewer.Pause();
            }
            else
            {
                viewer.Play();
            }
        }

        private void viewer_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            var tappedItem = (UIElement)e.OriginalSource;
            var attachedFlyout = (MenuFlyout)FlyoutBase.GetAttachedFlyout(viewer.TransportControls);

            attachedFlyout.ShowAt(tappedItem, e.GetPosition(tappedItem));
        }

        #endregion

        #endregion

        #region MediaElementButton Management
        private void viewer_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            MinimizeMediaElement.Visibility = Visibility.Visible;
            FadeIn.Begin();
        }

        private void viewer_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            FadeOut.Completed += MediaButtonCompleted;
            FadeOut.Begin();
        }

        private void MediaButtonCompleted(object sender, object e)
        {
            if (MinimizeMediaElement.Opacity == 0) { MinimizeMediaElement.Visibility = Visibility.Collapsed; }
        }
        #endregion

        private void Flyout_CopyLink(object sender, RoutedEventArgs e)
        {
            var dataPackage = new DataPackage();
            dataPackage.SetText("https://youtu.be/" + YoutubeID);
            Clipboard.SetContent(dataPackage);
        }

        private void Flyout_CopyLinkAtTime(object sender, RoutedEventArgs e)
        {
            var dataPackage = new DataPackage();
            dataPackage.SetText("https://youtu.be/" + YoutubeID + "?t=" + Convert.ToInt32(viewer.Position.TotalSeconds) + "s");
            Clipboard.SetContent(dataPackage);
        }


        private void HamburgerButton_Click(object sender, RoutedEventArgs e)
        {
            SideBarSplitView.IsPaneOpen = !SideBarSplitView.IsPaneOpen;
        }

        private void HamburgerButton_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            Window.Current.CoreWindow.PointerCursor = new CoreCursor(CoreCursorType.Hand, 1);
        }

        private void HamburgerButton_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            Window.Current.CoreWindow.PointerCursor = new CoreCursor(CoreCursorType.Arrow, 2);
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            contentFrame.Navigate(typeof(HomePage), new Params() { mainPageRef = this });
        }

        private async Task Test()
        {
            UserCredential credential;
            credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
    new ClientSecrets
    {
        ClientId = "957928808020-pa0lopl3crh565k6jd4djaj36rm1d9i5.apps.googleusercontent.com",
        ClientSecret = "oB9U6yWFndnBqLKIRSA0nYGm"
    }, new[] { YouTubeService.Scope.Youtube }, "user", CancellationToken.None);

            // Create the service.
            var service = new YouTubeService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = "Youtube Viewer",
            });

            var subscriptions = service.Subscriptions.List("snippet, contentDetails");
            subscriptions.MaxResults = 50;
            subscriptions.Mine = true;
            await subscriptions.ExecuteAsync();
            DoWork();
        }
    }
}