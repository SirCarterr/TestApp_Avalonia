using System;
using System.Collections.Generic;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using HtmlAgilityPack;
using ReactiveUI;
using TestApp_Avalonia.Models;
using Xceed.Wpf.Toolkit;

namespace TestApp_Avalonia.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        private Search search = new();
        private Playlist playlist = new();
        private List<Song> songs = new();

        public Search Search { get => search; set => this.RaiseAndSetIfChanged(ref search, value); }
        public Playlist Playlist { get => playlist; set => this.RaiseAndSetIfChanged(ref playlist, value); }
        public List<Song> Songs { get => songs; set => this.RaiseAndSetIfChanged(ref songs, value); }

        public MainWindowViewModel()
        {
            string workingDirectory = Environment.CurrentDirectory;
            string projectDirectory = Directory.GetParent(workingDirectory).Parent.Parent.FullName;

            HtmlDocument htmlDoc = new();
            htmlDoc.Load(projectDirectory + @"\Files\PageDefault.htm");

            bool isAlbum = Regex.IsMatch("albums", "(albums)");
            try
            {
                SetPlaylist(htmlDoc, isAlbum);
            }
            catch (Exception)
            {
                Playlist = new();
                Songs = new();
            }
        }

        public void OnClick() => OnParse();
        private async void OnParse()
        {
            if (string.IsNullOrEmpty(search.Url) || string.IsNullOrWhiteSpace(search.Url))
            {
                return;
            }

            if (!search.Url.StartsWith("https://"))
            {
                return;
            }

            var playlistTemp = playlist;
            var songsTemp = songs;
            bool isAlbum = Regex.IsMatch(Search.Url, "albums");

            string workingDirectory = Environment.CurrentDirectory;
            string projectDirectory = Directory.GetParent(workingDirectory).Parent.Parent.FullName;

            new WebClient().DownloadFile(Search.Url + @".mhtml", projectDirectory + @"\Files\Page.htm");
            HtmlDocument htmlDoc = new();
            htmlDoc.Load(projectDirectory + @"\Files\Page.htm");
            try
            {
                SetPlaylist(htmlDoc, isAlbum);
            }
            catch (Exception)
            {
                Playlist = playlistTemp;
                Songs = songsTemp;
                return;
            }
        }

        private async void SetPlaylist(HtmlDocument document, bool isAlbum)
        {
            if (!document.DocumentNode.SelectSingleNode("//body//div[@id='root']").ChildNodes.Any())
            {
                return;
            }

            var playlistNode = document.DocumentNode.SelectSingleNode("//body//music-detail-header[@image-src]");
            Playlist = new()
            {
                Name = playlistNode.Attributes["headline"].Value,
                ByArtist = playlistNode.Attributes["primary-text"].Value,
                Description = playlistNode.Attributes["tertiary-text"].Value
            };

            HttpClient client = new();
            var response = client.GetAsync(playlistNode.Attributes["image-src"].Value).Result;
            if (response.IsSuccessStatusCode)
            {
                using Stream stream = response.Content.ReadAsStream();
                Playlist.Avatar = new Bitmap(stream);
            }

            var songsNodes = document.DocumentNode.SelectNodes($"//body//music-container//music-container//{(isAlbum ? "music-text-row" : "music-image-row")}");
            for (int i = 0; i < songsNodes.Count; i++)
            {
                Song song;
                if (isAlbum)
                {
                    song = new()
                    {
                        Name = Regex.Replace(songsNodes[i].Attributes["data-key"].Value, @"(null\d*)", ""),
                        ByArtist = playlist.ByArtist,
                        Album = playlist.Name,
                    };
                }
                else
                {
                    song = new()
                    {
                        Name = songsNodes[i].Attributes["primary-text"].Value,
                        ByArtist = songsNodes[i].Attributes["secondary-text1"].Value,
                        Album = songsNodes[i].Attributes["secondary-text2"].Value,
                    };
                }

                var duration = document.DocumentNode.SelectNodes($"//body//music-container//music-container//{(isAlbum ? "music-text-row" : "music-image-row")}" +
                    $"//div[@class='content']//div[@class='col4']//music-link");
                song.Duration = duration[i].Attributes["title"].Value;
                Songs.Add(song);
            }
        }
    }
}
