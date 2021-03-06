﻿using Ark.Payout.UI.Helpers;
using Ark.Payout.UI.Models;
using Ark.Payout.UI.Services;
using ArkNet;
using ArkNet.Utils;
using ArkNet.Utils.Enum;
using log4net;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows;
using System.Linq;
using System.Threading.Tasks;

namespace Ark.Payout.UI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string _passPhrase = null;
        private bool _arkNetLoaded = false;

        private static readonly ILog _log = LogManager.GetLogger(typeof(MainWindow));

        public MainWindow()
        {
            InitializeComponent();

            ArkClientsListView.ItemsSource = new List<ArkClientModel>();
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
        }

        private async void PayNowButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult messageBoxResult = System.Windows.MessageBox.Show("Are you sure?", "Pay Confirmation", MessageBoxButton.YesNo);
            if (messageBoxResult == MessageBoxResult.Yes)
            {
                using (new WaitCursor())
                {
                    var arkClientsToPay = (ArkClientsListView.ItemsSource as List<ArkClientModel>);
                    if (!arkClientsToPay.Any())
                    {
                        MessageBox.Show("No clients to pay");
                    }
                    else
                    {
                        var payClientTasks = new List<Task<ErrorIndexModel>>();
                        foreach (var clientToPay in arkClientsToPay)
                        {
                            _log.Info(String.Format("Attempting to pay {0}({1}) to address {2}", (clientToPay.AmountToBePaid / StaticProperties.ARK_DIVISOR), (long)clientToPay.AmountToBePaid, clientToPay.Address));
                            payClientTasks.Add(PayoutService.PayClient(clientToPay, _passPhrase, PaymentDescriptionTextBox.Text));
                        }

                        var errors = await Task.WhenAll(payClientTasks);

                        if (errors.SelectMany(x => x.ErrorClients).Any())
                        {
                            MessageBox.Show("Error paying some clients.  Check log for details");
                        }
                        else
                        {
                            MessageBox.Show("Finished paying clients without errors.  Check log for details");
                            ArkClientsListView.ItemsSource = new List<ArkClientModel>();
                            ArkClientsListView.Tag = null;
                            Refresh();
                        }
                    }
                }
            }
        }

        private async void GeneratePayoutListButton_Click(object sender, RoutedEventArgs e)
        {
            if (String.IsNullOrWhiteSpace(PassPhraseTextBox.Password))
            {
                MessageBox.Show("You must enter a passphrase");
                return;
            }

            var percent = Double.Parse(PercentPayoutTextBox.Text);
            if (percent <= 0 || percent > 100)
            {
                MessageBox.Show("Percent must be > 0 and <= 100");
                return;
            }

            var amount = Double.Parse(AmountPayoutTextBox.Text);
            if (amount / StaticProperties.ARK_DIVISOR > 1)
            {
                MessageBox.Show("Invalid Amount");
                return;
            }

            using (new WaitCursor())
            {
                _passPhrase = PassPhraseTextBox.Password;

                ArkClientsListView.ItemsSource = new List<ArkClientModel>();
                ArkClientsListView.Tag = null;

                try
                {
                    var clientsToPay = await PayoutService.GetClientsToPay(_passPhrase, Double.Parse(PercentPayoutTextBox.Text), Convert.ToInt64(amount * StaticProperties.ARK_DIVISOR));
                    ArkClientsListView.Tag = clientsToPay;
                    foreach (var clientToPay in clientsToPay.ArkClients)
                    {
                        (ArkClientsListView.ItemsSource as List<ArkClientModel>).Add(clientToPay);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(String.Format("Error generating payout list.  {0}.  Check log for additional details.", ex.Message));
                    _log.Error("Error generating payout list", ex);
                }
            }

            Refresh();
        }

        private void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            if (ArkClientsListView.SelectedItems != null && ArkClientsListView.SelectedItems.Count > 0)
            {
                var allItems = (ArkClientsListView.ItemsSource as List<ArkClientModel>);
                var selectedItems = ArkClientsListView.SelectedItems;
                foreach (var item in selectedItems)
                {
                    allItems.Remove(item as ArkClientModel);
                    (ArkClientsListView.Tag as ArkClientIndexModel).ArkClients.Remove(item as ArkClientModel);
                }
                ArkClientsListView.ItemsSource = allItems;
                Refresh();
            }
        }
        private async void LoadAccountDataButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_arkNetLoaded)
                await ArkNetApi.Instance.Start(NetworkType.MainNet);

            ArkNetApi.Instance.NetworkSettings.MaxNumOfBroadcasts = 10;

            _arkNetLoaded = true;

            GridPayout.IsEnabled = false;

            if (String.IsNullOrWhiteSpace(PassPhraseTextBox.Password))
            {
                MessageBox.Show("You must enter a passphrase");
                return;
            }

            using (new WaitCursor())
            {
                _passPhrase = PassPhraseTextBox.Password;

                ArkClientsListView.ItemsSource = new List<ArkClientModel>();
                ArkClientsListView.Tag = null;

                try
                {
                    var account = await PayoutService.GetAccount(_passPhrase);
                    AmountPayoutTextBox.Text = (Double.Parse(account.Balance.ToString()) / StaticProperties.ARK_DIVISOR).ToString();
                    GridPayout.IsEnabled = true;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(String.Format("Error loading account data.  {0}.  Check log for additional details.", ex.Message));
                    _log.Error("Error loading account data", ex);
                }
            }
            Refresh();
        }

        private void TextBox_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            e.Handled = !StaticMethods.IsTextAllowed(e.Text);
        }

        private void TextBox_Pasting(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(String)))
            {
                String text = (String)e.DataObject.GetData(typeof(String));
                if (!StaticMethods.IsTextAllowed(text))
                {
                    e.CancelCommand();
                }
            }
            else
            {
                e.CancelCommand();
            }
        }

        private void Refresh()
        {
            var clientsToPay = ArkClientsListView.Tag as ArkClientIndexModel;
            if (clientsToPay != null)
            {
                TotalArkInAccountValueLabel.Content = clientsToPay.ArkDelegateAccountBalanceUI;
                TotalFeesToPayValueLabel.Content = clientsToPay.FeesToPayUI;
                TotalArkToPayValueLabel.Content = clientsToPay.TotalArkToPayUI;
                TotalClientsToPayValueLabel.Content = clientsToPay.TotalClientsToPay;
            }
            else
            {
                TotalArkInAccountValueLabel.Content = 0;
                TotalFeesToPayValueLabel.Content = 0;
                TotalArkToPayValueLabel.Content = 0;
                TotalClientsToPayValueLabel.Content = 0;
            }
            ArkClientsListView.Items.Refresh();
        }
    }
}
