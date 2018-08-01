﻿using System;
using Utility.Network.Users;
using Utility.Musics;

namespace Utility.Network.Dialog.Edits
{
    [Serializable]
    public class EditRequest
    {
        public TypesEdit TypeOfEdit { get; set; }

        public string UserToEdit { get; set; }
        public Rank NewRankOfUser { get; set; }

        public object ObjectToEdit { get; set; }
        public string NewName { get; set; }
        public Element TypeOfObject { get; set; }

        public EditRequest(string UIDToEdit, Rank NewRank)
        {
            UserToEdit = UIDToEdit;
            NewRankOfUser = NewRank;
            TypeOfEdit = TypesEdit.Users;
        }

        public EditRequest(object ToEdit, string NewTitle, Element TypeOf)
        {
            ObjectToEdit = ToEdit;
            NewName = NewTitle;
            TypeOfObject = TypeOf;
            TypeOfEdit = TypesEdit.Musics;
        }
    }
    public enum TypesEdit
    {
        Users,
        Musics
    }
}
