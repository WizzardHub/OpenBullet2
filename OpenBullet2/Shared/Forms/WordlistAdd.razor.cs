﻿using Blazor.FileReader;
using Blazored.Modal;
using Blazored.Modal.Services;
using Microsoft.AspNetCore.Components;
using OpenBullet2.Entities;
using OpenBullet2.Helpers;
using OpenBullet2.Repositories;
using OpenBullet2.Services;
using RuriLib.Extensions;
using RuriLib.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace OpenBullet2.Shared.Forms
{
    public partial class WordlistAdd : IDisposable
    {
        [CascadingParameter] public BlazoredModalInstance BlazoredModal { get; set; }

        [Inject] private IModalService ModalService { get; set; }
        [Inject] private IFileReaderService FileReaderService { get; set; }
        [Inject] private RuriLibSettingsService RuriLibSettings { get; set; }
        [Inject] private PersistentSettingsService PersistentSettings { get; set; }
        [Inject] private IWordlistRepository WordlistRepo { get; set; }

        private ElementReference inputTypeFileElement;
        private List<string> wordlistTypes;
        private WordlistEntity wordlist;
        private MemoryStream memoryStream;
        private long max;
        private long value;
        private decimal progress;
        private string baseDirectory = ".";
        private bool validUpload = false;

        private Node selectedNode = null;
        private List<Node> nodes = new() { };

        protected override async Task OnInitializedAsync()
        {
            wordlistTypes = RuriLibSettings.Environment.WordlistTypes.Select(w => w.Name).ToList();

            wordlist = new WordlistEntity
            {
                Name = "My Wordlist",
                Type = wordlistTypes.First()
            };

            baseDirectory = Directory.Exists("UserData") ? "UserData" : Directory.GetCurrentDirectory();
            await LoadTree(baseDirectory);
        }

        private async Task ProcessUploadedWordlist()
        {
            max = 0;
            value = 0;
            StateHasChanged();
            var files = (await FileReaderService.CreateReference(inputTypeFileElement).EnumerateFilesAsync()).ToList();
            var file = files.FirstOrDefault();

            if (file == null)
                return;

            var fileInfo = await file.ReadFileInfoAsync();
            wordlist.Name = Path.GetFileNameWithoutExtension(fileInfo.Name);
            max = fileInfo.Size;
            StateHasChanged();

            using (var fs = await file.OpenReadAsync())
            {
                var buffer = new byte[20480];
                memoryStream = new MemoryStream();
                int count;
                while ((count = await fs.ReadAsync(buffer, 0, buffer.Length)) != 0)
                {
                    value += count;
                    progress = ((decimal)fs.Position * 100) / fs.Length;
                    await InvokeAsync(StateHasChanged);
                    await Task.Delay(1);
                    await memoryStream.WriteAsync(buffer, 0, count);
                }
            }
            StateHasChanged();
            validUpload = true;
        }

        private async Task LoadTree(string baseDirectory)
        {
            if (!Directory.Exists(baseDirectory))
            {
                await js.AlertError(Loc["Uh-Oh"], Loc["DirectoryNonExistant"]);
                return;
            }

            if (!PersistentSettings.OpenBulletSettings.SecuritySettings.AllowSystemWideFileAccess &&
                !baseDirectory.IsSubPathOf(Directory.GetCurrentDirectory()))
            {
                await js.AlertError(Loc["Unauthorized"], Loc["SystemWideFileAccessDisabled"]);
                return;
            }

            nodes = Directory.GetFileSystemEntries(baseDirectory).Select(e => new Node(e)).ToList();
            var paths = nodes.Select(n => n.Path).ToArray();
            StateHasChanged();
        }

        private async Task Upload()
        {
            if (!validUpload)
            {
                await js.AlertError(Loc["Uh-Oh"], Loc["UploadFileFirst"]);
                return;
            }

            memoryStream.Seek(0, SeekOrigin.Begin);
            var fileName = Path.Combine("UserData", "Wordlists", Guid.NewGuid().ToString() + ".txt").Replace('\\', '/');
            FileStream fs = new(fileName, FileMode.Create);
            memoryStream.CopyTo(fs);
            fs.Close();
            wordlist.FileName = fileName;
            wordlist.Total = File.ReadLines(fileName).Count();

            BlazoredModal.Close(ModalResult.Ok(wordlist));
        }

        private async Task SelectFile()
        {
            if (selectedNode == null || selectedNode.IsDirectory)
            {
                await js.AlertError(Loc["Uh-Oh"], Loc["SelectFileFirst"]);
                return;
            }

            wordlist.FileName = selectedNode.Path;
            wordlist.Total = File.ReadLines(selectedNode.Path).Count();
            BlazoredModal.Close(ModalResult.Ok(wordlist));
        }

        public void Dispose()
            => memoryStream?.Close();

        ~WordlistAdd()
            => Dispose();
    }

    public class Node
    {
        public string Path { get; }
        public bool IsDirectory => (File.GetAttributes(Path) & FileAttributes.Directory) == FileAttributes.Directory;
        public string Name => System.IO.Path.GetFileName(Path);

        public IEnumerable<Node> Children
            => children ?? Directory.GetFileSystemEntries(Path).Select(e => new Node(e)).ToArray();

        private readonly Node[] children = null;

        public Node(string path)
        {
            Path = path;
        }
    }
}