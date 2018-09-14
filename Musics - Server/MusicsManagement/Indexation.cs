﻿using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.IO;
using Musics___Server.MusicsInformation;
using Utility.Musics;
using Utility.Network.Dialog.Uploads;
using System.Threading.Tasks;
using System.Xml;
using System.Diagnostics;

namespace Musics___Server.MusicsManagement
{
    static class Indexation
    {
        public static List<Author> ServerMusics = new List<Author>();

        public static void InitRepository()
        {
            Directory.CreateDirectory(@"c:\AllMusics");
            Console.WriteLine("Directory created.");
            MusicsInfo.SetupMusics();
        }

        public static byte[] GetFileBinary(Music m)
            => System.IO.File.ReadAllBytes(m.ServerPath);

        public static IEnumerable<Music> GetAllMusics()
            => ServerMusics.SelectMany(x => x.Albums).SelectMany(x => x.Musics);

        public static bool IsElementExisting(IElement element)
        {
            switch (element.Type)
            {
                case ElementType.Author: return ServerMusics.Any(a => a.MID == element.MID);
                case ElementType.Album: return ServerMusics.SelectMany(x => x.Albums).Any(a => a.MID == element.MID);
                case ElementType.Music: return MusicsInfo.TryFindMusic(element.MID, out XmlNode node);
                case ElementType.Playlist: throw new NotImplementedException();
                default: throw new InvalidOperationException();
            }
        }

        public static int Do(bool UseMultiThreading)
        {
            string[] ArtistDirs = Directory.GetDirectories(@"c:\AllMusics");

            int NumberofMusics = 0;

            TagLib.File file;
            Console.WriteLine();

            Stopwatch indexationStopWatch = new Stopwatch();
            indexationStopWatch.Start();
            long startMemory = GC.GetTotalMemory(false);

            foreach (var n in ArtistDirs)
            {
                Author CurrentArtist = new Author(Path.GetFileName(n), n);

                string[] AlbumOfArtist = Directory.GetDirectories(n);

                Console.WriteLine(n);
                foreach (var a in AlbumOfArtist)
                {
                    if (!Path.GetFileNameWithoutExtension(a).Contains("-ignore"))
                    {
                        var CurrentAlbum = new Album(CurrentArtist, Path.GetFileName(a), a);
                        var musics = new ConcurrentBag<Music>();
                        CurrentArtist.Albums.Add(CurrentAlbum);
                        Console.Write($"  {CurrentAlbum.Name} [");

                        if (UseMultiThreading)
                        {
                            Parallel.ForEach(Directory.GetFiles(a), m =>
                            {
                                file = AddMusicToindexation(m, ref NumberofMusics, CurrentArtist, CurrentAlbum, musics);
                            });
                        }
                        else
                        {
                            foreach (var m in Directory.GetFiles(a))
                            {
                                file = AddMusicToindexation(m, ref NumberofMusics, CurrentArtist, CurrentAlbum, musics);
                            }
                        }
                        CurrentAlbum.Musics = musics.OrderBy(x => x.N).ToList();
                        Console.WriteLine("]");
                    }
                }
                ServerMusics.Add(CurrentArtist);
            }
            indexationStopWatch.Stop();
            Console.WriteLine("Indexation finished in {0} Ms, with {1} Mo of memory", indexationStopWatch.ElapsedMilliseconds, (GC.GetTotalMemory(false) - startMemory)/ 1000000);
            return NumberofMusics;
        }

        private static TagLib.File AddMusicToindexation(string m, ref int NumberofMusics, Author CurrentArtist, Album CurrentAlbum, ConcurrentBag<Music> musics)
        {
            TagLib.File file;
            if (Path.GetExtension(m) == ".mp3" || Path.GetExtension(m) == ".flac")
            {
                file = TagLib.File.Create(m);
                string Musicname = file.Tag.Title;
                if (Musicname == null)
                {
                    try
                    {
                        Musicname = Path.GetFileNameWithoutExtension(m).Split('-')[1].Remove(0, 1);
                    }
                    catch
                    {
                        Musicname = Path.GetFileNameWithoutExtension(m);
                    }
                }

                Music current = new Music(Musicname, CurrentArtist, CurrentAlbum, m)
                {
                    Format = Path.GetExtension(m),
                    Genre = file.Tag.Genres,
                    N = file.Tag.Track
                };
                if (current.Genre.Length == 0)
                {
                    current.Genre = new string[] { "Unknown" };
                }
                if (MusicsInfo.TryFindMusic(current.MID, out XmlNode node))
                {
                    current.Rating = MusicsInfo.GetMusicInfo(current.MID).Rating;
                }

                NumberofMusics++;
                musics.Add(current);

                Console.Write(".");
                return file;
            }
            return null;
        }

        public static void ModifyElement(Element originalElement, string newName, string[] genres)
        {
            switch (originalElement.Type)
            {
                case ElementType.Music:
                    ModifyMusic(originalElement, newName, genres);
                    break;

                case ElementType.Album:
                    ModifyAlbum(originalElement, newName);
                    break;
                case ElementType.Author:
                case ElementType.Playlist:
                    throw new NotImplementedException();
                default:
                    throw new InvalidOperationException();
            }

            //if (originalElement is Author)
            //{
            //    //Author tmpOrigin = Origin as Author;
            //    //foreach (var a in Indexation.ServerMusics)
            //    //{
            //    //    if(tmpOrigin.MID == a.MID)
            //    //    {
            //    //        a.Name = NewName;
            //    //        a.MID = Hash.SHA256Hash(a.Name + ElementType.Author);

            //    //        //Directory.Move(a.ServerPath, Directory.GetParent(a.ServerPath) + "/" + a.Name);

            //    //        foreach (var al in a.albums)
            //    //        {
            //    //            al.Author = a;
            //    //            foreach(var m in al.Musics)
            //    //            {
            //    //                m.Author = a;
            //    //            }
            //    //        }

            //    //        return;
            //    //    }
            //    //}
            //}
        }
        public static void ModifyMusic(Element originalElement, string newName, string[] genres)
        {
            Music foundMusic = GetMusic(originalElement);
            if (foundMusic != null)
            {
                foundMusic.Title = newName;
                foundMusic.MID = Hash.SHA256Hash(foundMusic.Title + foundMusic.Author.Name);

                TagLib.File file = TagLib.File.Create(foundMusic.ServerPath);
                file.Tag.Title = foundMusic.Title;

                if (genres != null)
                {
                    foundMusic.Genre = genres;
                    file.Tag.Genres = genres;
                }

                file.Save();

                MusicsInfo.EditMusicsInfo(originalElement.MID, foundMusic);
            }
        }

        public static void ModifyAlbum(Element originalElement, string newName)
        {
            Album foundAlbum = GetAlbum(originalElement);
            if (foundAlbum != null)
            {
                foundAlbum.Name = newName;
                foundAlbum.MID = Hash.SHA256Hash(foundAlbum.Name + ElementType.Album);

                Directory.Move(foundAlbum.ServerPath, Directory.GetParent(foundAlbum.ServerPath) + "/" + foundAlbum.Name);
                foundAlbum.Musics.ToList().ForEach(m => m.Album = foundAlbum);
            }
        }

        public static void SaveAllInfos()
        {
            MusicsInfo.SaveMusicsInfo(GetAllMusics());
        }

        public static bool AddElement(UploadMusic tmp)
        {
            if (!Indexation.IsElementExisting(tmp.MusicPart.Musics.First().Author))
            {
                AddAuthor(tmp);
            }

            if (Indexation.IsElementExisting(tmp.MusicPart))
            {
                if (IsElementExisting(tmp.MusicPart.Musics.First()))
                {
                    return false;
                }
                else
                {
                    if (AddMusic(tmp))
                        return true;
                }
            }
            else
            {
                string path = AddAlbum(tmp);
                var music = tmp.MusicPart.Musics.First();
                string MusicPath = Path.Combine(path, music.Title + music.Format);
                System.IO.File.WriteAllBytes(MusicPath, tmp.MusicPart.Musics.First().FileBinary);
                MusicsInfo.SaveMusicInfo(tmp.MusicPart.Musics.First());
                foreach (var a in ServerMusics)
                {
                    if (a.MID == music.Author.MID)
                    {
                        music.FileBinary = null;
                        music.ServerPath = MusicPath;
                        music.Author = a;

                        Album tmpAl = new Album(a, tmp.MusicPart.Name, path);
                        music.Album = tmpAl;
                        tmpAl.Add(music);
                        a.Albums.Add(tmpAl);
                        return true;
                    }
                }
            }
            return false;
        }

        private static string AddAlbum(UploadMusic tmp)
        {
            string firstpath = GetElementPath(tmp.MusicPart.Musics.First().Author);
            string path = Path.Combine(firstpath, string.Join("", tmp.MusicPart.Name.Split(Path.GetInvalidFileNameChars())));
            Directory.CreateDirectory(path);
            return path;
        }

        private static bool AddMusic(UploadMusic tmp)
        {
            var music = tmp.MusicPart.Musics.First();
            string path = Path.Combine(GetElementPath(tmp.MusicPart), music.Title + music.Format);
            System.IO.File.WriteAllBytes(path, tmp.MusicPart.Musics.First().FileBinary);
            MusicsInfo.SaveMusicInfo(tmp.MusicPart.Musics.First());
            var album = GetAlbum(tmp.MusicPart.MID);
            if (album != null)
            {
                music.FileBinary = null;
                music.ServerPath = path;
                music.Author = album.Author ;
                music.Album = album;
                album.Add(music);
                return true;
            }
            return false;
        }

        private static void AddAuthor(UploadMusic tmp)
        {
            var music = tmp.MusicPart.Musics.First();
            string path = Path.Combine("c:\\AllMusics", music.Author.Name);
            Directory.CreateDirectory(path);
            ServerMusics.Add(new Author(music.Author.Name, path));
        }

        public static Album GetAlbum(Element element)
            => GetAlbum(element.MID);
        public static Album GetAlbum(string MID)
            => ServerMusics.SelectMany(x => x.Albums).SingleOrDefault(x => x.MID == MID);

        public static Author GetAuthor(Element element)
            => GetAuthor(element.MID);
        public static Author GetAuthor(string MID)
           => ServerMusics.SingleOrDefault(x => x.MID == MID);

        public static Music GetMusic(Element element)
            => GetMusic(element.MID);
        public static Music GetMusic(string MID)
            => GetAllMusics().SingleOrDefault(m => m.MID == MID);

        public static string GetElementPath(Element element)
        {
            switch (element.Type)
            {
                case ElementType.Author: return GetAuthor(element)?.ServerPath;
                case ElementType.Album: return GetAlbum(element)?.ServerPath;
                case ElementType.Music: return GetMusic(element)?.ServerPath;
                default: throw new InvalidOperationException();
            }
        }
    }
}
