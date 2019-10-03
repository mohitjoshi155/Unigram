﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Template10.Mvvm;
using Ton.Tonlib.Api;
using Unigram.Collections;
using Unigram.Common;
using Unigram.Controls;
using Unigram.Services;
using Unigram.Views.Wallet;
using Windows.Storage;
using Windows.UI.Xaml.Navigation;

namespace Unigram.ViewModels.Wallet
{
    public class WalletImportViewModel : TonViewModelBase
    {
        public WalletImportViewModel(ITonlibService tonlibService, IProtoService protoService, ICacheService cacheService, ISettingsService settingsService, IEventAggregator aggregator)
            : base(tonlibService, protoService, cacheService, settingsService, aggregator)
        {
            Items = new MvxObservableCollection<WalletWordViewModel>();

            for (int i = 0; i < 12; i++)
            {
                Items.Add(new WalletWordViewModel { Index = i + 1 });
                Items.Add(new WalletWordViewModel { Index = i + 13 });
            }

            SendCommand = new RelayCommand(SendExecute);
        }

        public MvxObservableCollection<WalletWordViewModel> Items { get; private set; }

        private IList<string> _hints;
        public IList<string> Hints
        {
            get => _hints;
            set => Set(ref _hints, value);
        }

        public override async Task OnNavigatedToAsync(object parameter, NavigationMode mode, IDictionary<string, object> state)
        {
            var response = await TonlibService.SendAsync(new GetBip39Hints());
            if (response is Bip39Hints hints)
            {
                Hints = hints.Words;
            }
            else
            {
                Hints = null;
            }
        }

        public RelayCommand SendCommand { get; }
        private async void SendExecute()
        {
            var local_password = Encoding.UTF8.GetBytes("local_passwordlocal_passwordlocal_passwordlocal_passwordlocal_pa");
            var words = Items.OrderBy(x => x.Index).Select(x => x.Text).ToArray();

            var response = await TonlibService.SendAsync(new ImportKey(local_password, new byte[0], new ExportedKey(words)));
            if (response is Key key)
            {
                var address = TonlibService.Execute(new WalletGetAccountAddress(new WalletInitialAccountState(key.PublicKey))) as AccountAddress;

                ProtoService.Send(new Telegram.Td.Api.SetOption("x_wallet_public_key", new Telegram.Td.Api.OptionValueString(key.PublicKey)));
                ProtoService.Send(new Telegram.Td.Api.SetOption("x_wallet_secret", new Telegram.Td.Api.OptionValueString(Utils.ByteArrayToString(key.Secret))));
                ProtoService.Send(new Telegram.Td.Api.SetOption("x_wallet_address", new Telegram.Td.Api.OptionValueString(address.AccountAddressValue)));

                NavigationService.Navigate(typeof(WalletPage));
            }
            else if (response is Error error)
            {
                await TLMessageDialog.ShowAsync(Strings.Resources.WalletImportAlertText, Strings.Resources.WalletImportAlertTitle, Strings.Resources.OK);
                await TLMessageDialog.ShowAsync(error.Message, error.Code.ToString(), Strings.Resources.OK);
            }
        }
    }

    public class WalletWordViewModel : BindableBase
    {
        private int _index;
        public int Index
        {
            get => _index;
            set => Set(ref _index, value);
        }

        private string _text = string.Empty;
        public string Text
        {
            get => _text;
            set => Set(ref _text, value);
        }
    }
}
