﻿using Musics___Client.API.Events;
using Musics___Client.API.Tracker;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Utility.Musics;
using Utility.Network.Dialog.Requests;
using Utility.Network.Tracker.Identity;

namespace Musics___Client.UI
{
    public partial class FavoriteControl : UserControl
    {
        public FavoriteControl()
        {
            InitializeComponent();
        }

        public List<ServerIdentity> ServerList = new List<ServerIdentity>();

        public List<Music> LikedMusics = new List<Music>();
        public List<Music> SelectedFavorites = new List<Music>();

        public event EventHandler<RequestBinairiesEventArgs> SearchEvent;
        protected virtual void OnSearchEvent(RequestBinairiesEventArgs e) => SearchEvent?.Invoke(this, e);

        public event EventHandler<PlayEventArgs> PlayAllEvent;
        protected virtual void OnPlayAllEvent(PlayEventArgs e) => PlayAllEvent?.Invoke(this, e);

        public void UpdateSelectedFavorites()
        {
            SelectedFavorites.Clear();
            SelectedFavorites = (from val in LikedMusics
                                 where val.Genre.First() == UILikedMusicsList.SelectedItem.ToString()
                                 select val).ToList();
            UILikedMusicsList.Items.Clear();
            foreach (var m in SelectedFavorites)
            {
                UILikedMusicsList.Items.Add(m.Title);
            }
        }

        private void UILikedMusicsList_DoubleClick(object sender, EventArgs e)
        {
            if (UILikedMusicsList.SelectedItem != null && SelectedFavorites.Count == 0)
            {
                UpdateSelectedFavorites();
            }
            else if (UILikedMusicsList.SelectedItem != null)
            {
                OnSearchEvent(new RequestBinairiesEventArgs(SelectedFavorites[UILikedMusicsList.SelectedIndex]));
            }
        }

        public void UpdateFavorites(List<Music> Favorites)
        {
            Invoke((MethodInvoker)delegate
            {
                UILikedMusicsList.Items.Clear();
                foreach (var m in (from val in Favorites select val.Genre.First()).Cast<string>().Distinct().ToList())
                {
                    //UILikedMusicsList.Items.Remove(m);
                    UILikedMusicsList.Items.Add(m);
                }
                var tmp = Favorites;
                LikedMusics = tmp;
            });    
        }

        public void ClearAll()
        {
            LikedMusics.Clear();
            UILikedMusicsList.Items.Clear();
            SelectedFavorites.Clear();
        }

        private void UIFavoritesBack_Click(object sender, EventArgs e)
        {
            SelectedFavorites.Clear();
            UpdateFavorites(LikedMusics);
        }

        private void UIPlayFavorites_Click(object sender, EventArgs e)
        {
            if (LikedMusics.Count >= 1)
                OnPlayAllEvent(new PlayEventArgs(new Album(new Author(""), "", LikedMusics.ToArray())));
        }

        private void UIServerSelector_SelectedIndexChanged(object sender, EventArgs e)
        {
            RequestFavorite();
        }

        private void RequestFavorite()
        {
            if (UIServerSelector.SelectedItem != null)
                ServerManagerService.Instance.SendToServer(new RequestFavorites(ServerManagerService.Instance.Me.UID), ServerList[UIServerSelector.SelectedIndex]);
        }

        public void AddServer(ServerIdentity si)
        {
            ServerList.Add(si);
            UIServerSelector.Items.Add(si.IPEndPoint.ToString());
            UIServerSelector.SelectedIndex = UIServerSelector.Items.Count - 1;
            RequestFavorite();
        }

        public void RemoveServer(ServerIdentity si)
        {
            ServerList.Remove(si);
            UIServerSelector.Items.Clear();
            UIServerSelector.Items.AddRange(ServerList.Select(x => x.IPEndPoint.ToString()).ToArray());
        }
    }
}
