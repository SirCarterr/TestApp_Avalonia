using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using Avalonia.Media.Imaging;
using HtmlAgilityPack;
using ReactiveUI;
using TestApp_Avalonia.Models;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System.Threading;
using Avalonia.Collections;
using OpenQA.Selenium.Interactions;
using DynamicData;

namespace TestApp_Avalonia.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        private Search search = new();
        private Playlist playlist = new() { Name = "Name", ByArtist = "Artist", Description = "Information" };
        private AvaloniaList<Song> songs = new();
        private Bitmap avatar;

        public Search Search { get => search; set => this.RaiseAndSetIfChanged(ref search, value); }
        public Playlist Playlist { get => playlist; set => this.RaiseAndSetIfChanged(ref playlist, value); }
        public AvaloniaList<Song> Songs
        {
            get => songs;
            set
            {
                songs = value;
                this.RaiseAndSetIfChanged(ref songs, value);
            }
        }
        public Bitmap Avatar { get => avatar ; set => this.RaiseAndSetIfChanged(ref avatar, value); }

        public MainWindowViewModel()
        {
            string workingDirectory = Environment.CurrentDirectory;
            string projectDirectory = Directory.GetParent(workingDirectory).Parent.Parent.FullName;

            using var fileStream = File.OpenRead(projectDirectory + @"\Files\record-default.png");
            avatar = new(fileStream);

            //HtmlDocument htmlDoc = new();
            //htmlDoc.Load(projectDirectory + @"\Files\PageDefault.htm");

            //bool isAlbum = Regex.IsMatch("albums", "(albums)");
            //SetPlaylist(htmlDoc, isAlbum);


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

            Songs.Clear();
            Playlist = new();

            bool isAlbum = Regex.IsMatch(Search.Url!, "albums");

            var browser = new ChromeDriver();
            browser.Navigate().GoToUrl(search.Url);
            Thread.Sleep(3000);
            var element = browser.FindElement(By.TagName("html"));
            Actions actions = new Actions(browser);
            actions.MoveToElement(element);

            HtmlDocument htmlDoc = new();
            for (int i = 0; i < 5; i++)
            {
                actions.ScrollByAmount(0, element.Size.Height);
                actions.Perform();
                Thread.Sleep(1000);
                htmlDoc.LoadHtml(browser.PageSource);
                SetPlaylist(htmlDoc, isAlbum);
            }
            //var html = browser.PageSource;

            //HtmlDocument htmlDoc = new();
            //htmlDoc.LoadHtml(html);
            //SetPlaylist(htmlDoc, isAlbum);
            //browser.Close();
            browser.Dispose();
        }

        private async void SetPlaylist(HtmlDocument document, bool isAlbum)
        {
            try
            {
                if (!document.DocumentNode.SelectSingleNode("//body//div[@id='root']").ChildNodes.Any())
                {
                    return;
                }

                var playlistNode = document.DocumentNode.SelectSingleNode("//body//music-detail-header[@image-src]");

                if (Playlist.Name == null)
                {
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
                        Avatar = new Bitmap(stream);
                    }
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
                            Name = Regex.Replace(songsNodes[i].Attributes["primary-text"].Value, "(&amp;)", "&"),
                            ByArtist = Regex.Replace(songsNodes[i].Attributes["secondary-text-1"].Value, "(&amp;)", "&"),
                            Album = Regex.Replace(songsNodes[i].Attributes["secondary-text-2"].Value, "(&amp;)", "&"),
                        };
                    }

                    var duration = document.DocumentNode.SelectNodes($"//body//music-container//music-container//{(isAlbum ? "music-text-row" : "music-image-row")}//div[@class='content']//div[@class='col4']");
                    song.Duration = duration[i].FirstChild.Attributes["title"].Value;
                    if (songs.FirstOrDefault(s => s.Name.Equals(song.Name) && s.ByArtist.Equals(song.ByArtist)) == null)
                    {
                        songs.Add(song);
                    }
                }
            }
            catch
            {
                Playlist = new() { Name = "Name", ByArtist = "Artist", Description = "Information" };
                Songs = new();

                string workingDirectory = Environment.CurrentDirectory;
                string projectDirectory = Directory.GetParent(workingDirectory).Parent.Parent.FullName;

                using var fileStream = File.OpenRead(projectDirectory + @"\Files\record-default.png");
                avatar = new(fileStream);
            }
        }
    }
}
