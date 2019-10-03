﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ton.Tonlib.Api;
using Unigram.Common;
using Unigram.Controls;
using Unigram.Services;
using Unigram.Views.Wallet;
using Windows.UI.Xaml.Controls;

namespace Unigram.ViewModels.Wallet
{
    public class WalletSettingsViewModel : TonViewModelBase
    {
        public WalletSettingsViewModel(ITonlibService tonlibService, IProtoService protoService, ICacheService cacheService, ISettingsService settingsService, IEventAggregator aggregator)
            : base(tonlibService, protoService, cacheService, settingsService, aggregator)
        {
            ExportCommand = new RelayCommand(ExportExecute);
            DeleteCommand = new RelayCommand(DeleteExecute);
        }

        public RelayCommand ExportCommand { get; }
        private void ExportExecute()
        {
            // TODO: make this secure
            NavigationService.Navigate(typeof(WalletExportPage));
        }

        public RelayCommand DeleteCommand { get; }
        private async void DeleteExecute()
        {
            // TODO: make this secure
            var confirm = await TLMessageDialog.ShowAsync(Strings.Resources.WalletDeleteText, Strings.Resources.WalletDeleteTitle, Strings.Resources.OK, Strings.Resources.Cancel);
            if (confirm != ContentDialogResult.Primary)
            {
                return;
            }

            var publicKey = ProtoService.Options.GetValue<string>("x_wallet_public_key");
            var secret = Utils.StringToByteArray(ProtoService.Options.GetValue<string>("x_wallet_secret"));

            var local_password = Encoding.UTF8.GetBytes("local_passwordlocal_passwordlocal_passwordlocal_passwordlocal_pa");

            var response = await TonlibService.SendAsync(new DeleteKey(new Key(publicKey, secret)));
            if (response is Ok)
            {
                ProtoService.Send(new Telegram.Td.Api.SetOption("x_wallet_address", new Telegram.Td.Api.OptionValueEmpty()));
                ProtoService.Send(new Telegram.Td.Api.SetOption("x_wallet_public_key", new Telegram.Td.Api.OptionValueEmpty()));
                ProtoService.Send(new Telegram.Td.Api.SetOption("x_wallet_secret", new Telegram.Td.Api.OptionValueEmpty()));

                NavigationService.Navigate(typeof(WalletCreatePage));
            }
            else if (response is Error error)
            {
                await TLMessageDialog.ShowAsync(error.Message, error.Code.ToString(), Strings.Resources.OK);
            }
        }
    }
}
