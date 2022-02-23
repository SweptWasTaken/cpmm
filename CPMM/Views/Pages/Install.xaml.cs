﻿// This Source Code Form is subject to the terms of the GNU GPL-3.0.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://www.gnu.org/licenses/gpl-3.0.en.html.
// Copyright (C) 2022 Leszek Pomianowski and CPMM Contributors.
// All Rights Reserved.

using CPMM.Code;
using CPMM.Core.Installer;
using CPMM.Core.Mods;
using Lepo.i18n;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using WPFUI.Controls.Interfaces;

namespace CPMM.Views.Pages
{
    internal class InstallData : ViewData
    {
        private bool _enableSelectButton = true;

        public bool EnableSelectButton
        {
            get => _enableSelectButton;
            set => UpdateProperty(ref _enableSelectButton, value, nameof(EnableSelectButton));
        }

        private bool _enableInstallButton = false;

        public bool EnableInstallButton
        {
            get => _enableInstallButton;
            set => UpdateProperty(ref _enableInstallButton, value, nameof(EnableInstallButton));
        }

        private string _modificationPath = Translator.String("global.fileNotSelected");

        public string ModificationPath
        {
            get => _modificationPath;
            set => UpdateProperty(ref _modificationPath, value, nameof(ModificationPath));
        }

        private bool _dialogMultiSelect = false;

        public bool DialogMultiSelect
        {
            get => _dialogMultiSelect;
            set => UpdateProperty(ref _dialogMultiSelect, value, nameof(DialogMultiSelect));
        }

        private IEnumerable<IMod> _parsedMods = new IMod[] { };

        public IEnumerable<IMod> ParsedMods
        {
            get => _parsedMods;
            set => UpdateProperty(ref _parsedMods, value, nameof(ParsedMods));
        }

        private Visibility _listVisibility = Visibility.Collapsed;
        public Visibility ListVisibility
        {
            get => _listVisibility;
            set => UpdateProperty(ref _listVisibility, value, nameof(ListVisibility));
        }
    }

    /// <summary>
    /// Interaction logic for Install.xaml
    /// </summary>
    public partial class Install : INavigable
    {
        internal InstallData InstallDataStack { get; } = new();

        internal OpenFileDialog DialogSelector;

        internal readonly ModInstaller Installer = new();

        internal readonly List<ExtractingResult> UnpackedMods = new List<ExtractingResult>();

        public Install()
        {
            InitializeComponent();
            DialogSelector = CreateDialog();

            var userDownloads = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Downloads"
            );

            if (Directory.Exists(userDownloads))
                DialogSelector.InitialDirectory = userDownloads;

            Installer.GameRootDirectory = GH.Settings.Options.GameRootDirectory;

            DataContext = InstallDataStack;
        }

        public void OnNavigationRequest(INavigation sender, object current)
        {
            // Navigated
        }

        private OpenFileDialog CreateDialog()
        {
            var dialogFilter = Translator.String("global.dialog.cyberMods") + " (*.7z;*.zip;*.rar)|*.7z;*.zip;*.rar";
            dialogFilter += "|All we had to do, was follow the damn train, CJ!(*.hotcoffee)|*.exe";
            dialogFilter += "|" + Translator.String("global.dialog.allFiles") + " (*.*)|*.*";

            return new OpenFileDialog()
            {
                Title = Translator.String("global.dialog.selectMod"),
                Filter = dialogFilter,
                CheckPathExists = true,
                Multiselect = InstallDataStack.DialogMultiSelect
            };
        }

        private async void ButtonSelect_OnClick(object sender, RoutedEventArgs e)
        {
            if (!DialogSelector.ShowDialog() ?? false)
                return;

            InstallDataStack.EnableSelectButton = false;
            InstallDataStack.EnableInstallButton = false;
            InstallDataStack.ListVisibility = Visibility.Collapsed;

#if DEBUG
            System.Diagnostics.Debug.WriteLine($"INFO | {DialogSelector.FileName} selected, Thread: {System.Threading.Thread.CurrentThread.ManagedThreadId}", "CPMM");
#endif
            await Installer.ClearTemps();

            if (InstallDataStack.DialogMultiSelect)
                await TryPrepareMultiple(DialogSelector.FileNames);
            else
                await TryPrepareSingle(DialogSelector.FileName);

            InstallDataStack.ParsedMods = await Installer.ParseModsAsync(UnpackedMods);

            if (InstallDataStack.ParsedMods.Any())
            {
                InstallDataStack.EnableInstallButton = true;
                InstallDataStack.ListVisibility = Visibility.Visible;
            }

            if (InstallDataStack.DialogMultiSelect)
                if (DialogSelector.FileNames.Length > 1)
                    InstallDataStack.ModificationPath = Translator.Plural("global.selectedFiles.single", "global.selectedFiles.plural", DialogSelector.FileNames.Length);
                else
                    InstallDataStack.ModificationPath = DialogSelector.FileNames[0] ?? Translator.String("global.fileNotSelected");
            else
                InstallDataStack.ModificationPath = DialogSelector.FileName;

            InstallDataStack.EnableSelectButton = true;
        }

        private async Task TryPrepareSingle(string filePath)
        {
            UnpackedMods.Clear();

            var unpackingResult = await Installer.TryUnpackAsync(filePath);

            if (unpackingResult.Status == ExtractingResult.ExtractingStatus.Success)
                UnpackedMods.Add(unpackingResult);
        }

        private async Task TryPrepareMultiple(string[] filePaths)
        {
            UnpackedMods.Clear();

            foreach (var singleFile in filePaths)
            {
                var unpackingResult = await Installer.TryUnpackAsync(singleFile);

                if (unpackingResult.Status == ExtractingResult.ExtractingStatus.Success)
                    UnpackedMods.Add(unpackingResult);
            }
        }

        private void ButtonCancel_OnClick(object sender, RoutedEventArgs e)
        {
            //throw new NotImplementedException();
        }

        private void ButtonInstall_OnClick(object sender, RoutedEventArgs e)
        {
            //throw new NotImplementedException();
        }
    }
}