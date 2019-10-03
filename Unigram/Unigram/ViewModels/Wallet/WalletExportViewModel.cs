﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ton.Tonlib.Api;
using Unigram.Collections;
using Unigram.Common;
using Unigram.Controls;
using Unigram.Services;
using Unigram.ViewModels.Delegates;
using Unigram.Views.Wallet;
using Windows.UI.Xaml.Navigation;

namespace Unigram.ViewModels.Wallet
{
    public class WalletExportViewModel : TonViewModelBase, IDelegable<IWalletExportDelegate>
    {
        private DateTimeOffset _openedAt;

        public IWalletExportDelegate Delegate { get; set; }

        public WalletExportViewModel(ITonlibService tonlibService, IProtoService protoService, ICacheService cacheService, ISettingsService settingsService, IEventAggregator aggregator)
            : base(tonlibService, protoService, cacheService, settingsService, aggregator)
        {
            SendCommand = new RelayCommand(SendExecute);
        }

        private IList<WalletWordViewModel> _items;
        public IList<WalletWordViewModel> Items
        {
            get => _items;
            set => Set(ref _items, value);
        }

        public override async Task OnNavigatedToAsync(object parameter, NavigationMode mode, IDictionary<string, object> state)
        {
            if (mode == NavigationMode.New)
            {
                _openedAt = DateTimeOffset.Now;
            }

            IList<string> wordList = null;
            if (TonlibService.TryGetCreationState(out WalletCreationState creationState))
            {
                wordList = creationState.WordList;
            }
            else
            {
                var publicKey = ProtoService.Options.GetValue<string>("x_wallet_public_key");
                var secret = Utils.StringToByteArray(ProtoService.Options.GetValue<string>("x_wallet_secret"));

                var local_password = Encoding.UTF8.GetBytes("local_passwordlocal_passwordlocal_passwordlocal_passwordlocal_pa");

                var privateKey = new InputKey(new Key(publicKey, secret), local_password);

                var response = await TonlibService.SendAsync(new ExportKey(privateKey));
                if (response is ExportedKey exportedKey)
                {
                    wordList = exportedKey.WordList;
                }
                else if (response is Error error)
                {

                }
            }

            if (wordList == null)
            {
                return;
            }

            var items = new List<WalletWordViewModel>();

            for (int i = 0; i < 12; i++)
            {
                items.Add(new WalletWordViewModel { Index = i + 1, Text = wordList[i] });
                items.Add(new WalletWordViewModel { Index = i + 13, Text = wordList[i + 12] });
            }

            Items = items;
            Delegate?.UpdateWordList(items);
        }

        public RelayCommand SendCommand { get; }
        private async void SendExecute()
        {
            if (TonlibService.IsCreating)
            {
                var wait = 60;

#if DEBUG
                wait = 6;
#endif

                var difference = DateTimeOffset.Now - _openedAt;
                if (difference < TimeSpan.FromSeconds(wait))
                {
                    await TLMessageDialog.ShowAsync(Strings.Resources.WalletSecretWordsAlertText, Strings.Resources.WalletSecretWordsAlertTitle, Strings.Resources.WalletSecretWordsAlertButton);
                    return;
                }

                NavigationService.Navigate(typeof(WalletTestPage));
            }
            else
            {
                NavigationService.Navigate(typeof(WalletPage));
            }
        }
    }
}
