using System;
using System.Collections.Generic;
using System.Windows.Documents;
using System.IO;
using System.Security.Cryptography;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

#if IS_VSIX
using Microsoft.VisualStudio.Shell;
#endif

using Hardcodet.Wpf.GenericTreeView;
using Wpf.Ui.Controls;
using Wpc = Wpf.Ui.Controls;
using Nethereum.Util;

using VsSolidity;
using VsSolidity.Ethereum;
using VsSolidity.UI.ViewModel;
using static VsSolidity.Result;
using Nethereum.Hex.HexTypes;

namespace VsSolidity.UI
{
    /// <summary>
    /// Interaction logic for BlockchainExplorerToolWindowControl.
    /// </summary>
    public partial class BlockchainExplorerToolWindowControl : UserControl
    {
        #region Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="BlockchainExplorerToolWindowControl"/> class.
        /// </summary>
        public BlockchainExplorerToolWindowControl() : base()
        {
            instance = this;
            InitializeComponent();

            // Items, TreeNodeStyle, TreeStyle, NodeSortDescriptions, SelectNodesOnRightClick and IsLazyLoading are
            // declared on the generic base TreeViewBase<BlockchainInfo>. Setting them in XAML makes WPF BAML resolve the
            // generic base type and throw NotImplementedException (Baml2006SchemaContext.ResolveBamlType), so they are
            // assigned here. Set styling/sort first, then bind Items last so the first render already has them applied.
            BlockchainExplorerTree.IsLazyLoading = false;
            BlockchainExplorerTree.SelectNodesOnRightClick = true;
            BlockchainExplorerTree.TreeStyle = (Style)FindResource("TreeViewStyle");
            BlockchainExplorerTree.TreeNodeStyle = (Style)FindResource("TreeViewItemStyle");
            BlockchainExplorerTree.NodeSortDescriptions = (System.Collections.Generic.IEnumerable<System.ComponentModel.SortDescription>)FindResource("AscendingNames");
            BlockchainExplorerTree.SetBinding(TreeViewBase<BlockchainInfo>.ItemsProperty,
                new System.Windows.Data.Binding(nameof(BlockchainViewModel.Objects)) { Source = (BlockchainViewModel)Resources["Blockchains"] });
#if IS_VSIX
            VSTheme.WatchThemeChanges();
            instance = this;
#endif
        }
        #endregion

        #region Event handlers
        private void OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is BlockchainExplorerTree tree && tree.SelectedItem != null)
            {
                if (tree.SelectedItem.Kind == BlockchainInfoKind.Account)
                {
                    BlockchainExplorerTree.EditAccountCmd.Execute(null, tree);
                }
                else if (tree.SelectedItem.Kind == BlockchainInfoKind.Contract)
                {
                    BlockchainExplorerTree.EditContractCmd.Execute(null, tree);
                }
            }
        }

        private void OnSelectedItemChanged(object sender, RoutedTreeItemEventArgs<BlockchainInfo> e)
        {

            if (sender is BlockchainExplorerTree tree && tree.SelectedItem != null)
            {
                
            }
        }

        private async void NewNetworkCmd_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            try
            {
                var window = (BlockchainExplorerToolWindowControl)sender;
                var tree = window.BlockchainExplorerTree;
                var item = GetSelectedItem(sender);
                var dw = new ToolWindowDialog(RootContentDialog)
                {
                    Title = "Add EVM Network",
                    PrimaryButtonIcon = new SymbolIcon(SymbolRegular.Save20),
                    Content = (StackPanel)TryFindResource("AddNetworkDialog"),
                    PrimaryButtonText = "Save",
                    CloseButtonText = "Cancel"
                };
                var sp = (StackPanel)dw.Content;
                var name = (Wpc.TextBox)((StackPanel)sp.Children[0]).Children[1];
                var rpcurl = (Wpc.TextBox)((StackPanel)sp.Children[1]).Children[1];
                var chainid = (Wpc.NumberBox)((StackPanel)sp.Children[2]).Children[1];
                var nid = "";
                string[] accts = Array.Empty<string>();
                var errors = (Wpc.TextBlock)((Grid)((StackPanel)sp.Children[3]).Children[0]).Children[0];
                var progressring = (Wpc.ProgressRing)((Grid)((StackPanel)sp.Children[3]).Children[0]).Children[1];
                var validForClose = false;
                dw.Closing += (d, args) =>
                {
                    args.Cancel = !validForClose;
                };
                dw.ButtonClicked += (cd, args) =>
                {
                    validForClose = false;
                    errors.Visibility = Visibility.Hidden;
                    if (args.Button == ContentDialogButton.Primary)
                    {
                        if (!string.IsNullOrEmpty(name.Text) && !string.IsNullOrEmpty(rpcurl.Text) && Uri.TryCreate(rpcurl.Text, UriKind.Absolute, out var _))
                        {
                            if (tree.RootItem.HasChild(name.Text, BlockchainInfoKind.Network))
                            {
                                ShowValidationErrors(errors, "Enter a unique name for the network name.");
                                return;
                            }
                            ShowProgressRing(progressring);
                            var text = rpcurl.Text;
#if IS_VSIX
                            var result = ThreadHelper.JoinableTaskFactory.Run(() => ExecuteAsync(Network.GetNetworkDetailsAsync(text)));
#else
                            var result = Task.Run(() => ExecuteAsync(Network.GetNetworkDetailsAsync(text))).Result;
#endif
                            HideProgressRing(progressring);
                            if (Succedeed(result, out var cnid))
                            {
                                if (!string.IsNullOrEmpty(chainid.Text) && cnid.Value.Item1 == BigInteger.Parse(chainid.Text))
                                {
                                    nid = cnid.Value.Item2;
                                    accts = cnid.Value.Item3;
                                    validForClose = true;
                                }
                                else if (string.IsNullOrEmpty(chainid.Text))
                                {
                                    chainid.Text = cnid.Value.Item1.ToString();
                                    nid = cnid.Value.Item2;
                                    accts = cnid.Value.Item3;
                                    validForClose = true;
                                }
                                else
                                {
                                    ShowValidationErrors(errors, string.Format("The specified chain id {0} does not match the chain id returned by the network endpoint: {1}.", chainid.Text, cnid.Value.Item1));
                                    return;
                                }
                            }
                            else
                            {
                                ShowValidationErrors(errors, "Error connecting to JSON-RPC URL: " + cnid.Exception.Message + " " + cnid.Exception.InnerException?.Message);
                                return;
                            }
                        }
                        else
                        {
                            ShowValidationErrors(errors, "Enter a network name and a valid JSON-RPC endpoint URL.");
                            return;
                        }
                    }
                    else
                    {
                        validForClose = true;
                    }
                };

                var r = await dw.ShowAsync();
                if (r != ContentDialogResult.Primary)
                {
                    name.Text = "";
                    rpcurl.Text = "";
                    chainid.Text = "";
                    nid = "";
                    accts = Array.Empty<string>();  
                    return;
                }
                var t = item.AddNetwork(name.Text, rpcurl.Text, BigInteger.Parse(chainid.Text), nid);
                var endpoints = t.GetChild("Endpoints", BlockchainInfoKind.Folder);
                endpoints.AddChild(rpcurl.Text, BlockchainInfoKind.Endpoint);
                var accounts = t.GetChild("Accounts", BlockchainInfoKind.Folder);
                foreach (var acct in accts)
                {
                    accounts.AddAccount(acct);   
                };
                if (!tree.RootItem.Save("BlockchainExplorerTree", out var ex))
                {
#if IS_VSIX
                    VSUtil.ShowModalErrorDialogBox("Error saving tree data: " + ex?.Message);
#else
                    System.Windows.MessageBox.Show("Error saving tree data: " +  ex?.Message);
#endif
                }
                tree.Refresh(); 
            }
            catch (Exception ex)
            {
#if IS_VSIX
                VSUtil.ShowModalErrorDialogBox(ex?.Message);
#else
                System.Windows.MessageBox.Show(ex?.Message);
#endif
            }
        }

        private async void NewEndpointCmd_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            try
            {
                var window = (BlockchainExplorerToolWindowControl)sender;
                var tree = window.BlockchainExplorerTree;
                // Launched from the Network node (or, historically, the Endpoints folder). Resolve the network's
                // Endpoints folder so the uniqueness check and the add target the same place.
                var endpointsFolder = tree.SelectedItem != null && tree.SelectedItem.Kind == BlockchainInfoKind.Network
                    ? tree.SelectedItem.GetOrAddChild("Endpoints", BlockchainInfoKind.Folder)
                    : tree.SelectedItem;
                var dw = new ToolWindowDialog(RootContentDialog)
                {
                    Title = "Add EVM network endpoint",
                    PrimaryButtonIcon = new SymbolIcon(SymbolRegular.Save20),
                    Content = (StackPanel)TryFindResource("AddEndpointDialog"),
                    PrimaryButtonText = "Save",
                    CloseButtonText = "Cancel",
                };
                var sp = (StackPanel)dw.Content;
                var rpcurl = (Wpc.TextBox)((StackPanel)sp.Children[0]).Children[1];
                var errors = (Wpc.TextBlock)((StackPanel)sp.Children[1]).Children[0];
                var validForClose = false;
                dw.ButtonClicked += (cd, args) =>
                {
                    validForClose = false;
                    errors.Visibility = Visibility.Hidden;
                    if (args.Button == ContentDialogButton.Primary)
                    {
                        if (!string.IsNullOrEmpty(rpcurl.Text) && Uri.TryCreate(rpcurl.Text, UriKind.Absolute, out var _))
                        {
                            if (endpointsFolder.HasChild(rpcurl.Text, BlockchainInfoKind.Endpoint))
                            {
                                ShowValidationErrors(errors, "Enter a unique network endpoint URL.");
                                return;
                            }
                            else
                            {
                                validForClose = true;
                            }
                        }
                        else
                        {
                            validForClose = false;
                            ShowValidationErrors(errors, "Enter a valid URL for the network JSON-RPC endpoint.");
                        }
                    }
                    else
                    {
                        validForClose = true;
                    }
                };
                dw.Closing += (d, args) =>
                {
                    args.Cancel = !validForClose;
                };
                var r = await dw.ShowAsync();
                if (r != ContentDialogResult.Primary)
                {
                    rpcurl.Text = "";
                    return;
                }

                var uri = new Uri(rpcurl.Text);

                endpointsFolder.AddChild(rpcurl.Text, BlockchainInfoKind.Endpoint);
                if (!tree.RootItem.Save("BlockchainExplorerTree", out var ex))
                {
                    System.Windows.MessageBox.Show("Error saving tree data: " + ex?.Message);
                }
                
            }
            catch (Exception ex)
            {
#if IS_VSIX
                VSUtil.ShowModalErrorDialogBox(ex?.Message);
#else
                System.Windows.MessageBox.Show(ex?.Message);
#endif
            }
        }

        private void DeleteEndpointCmd_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var item = GetSelectedItem(sender);
            item.Parent.DeleteChild(item);
        }

        private void DeleteEndpointCmd_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            var item = GetSelectedItem(sender);
            var endpoints = item.Parent.Parent.GetNetworkEndPoints();
            if (endpoints.Count() == 1)
            {
                e.CanExecute = false;
            }
            //
        }

        private void PropertiesCmd_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var item = GetSelectedItem(sender);
            var dw = new ToolWindowDialog(RootContentDialog)
            {
                Title = item.Name + " properties",
                PrimaryButtonIcon = new SymbolIcon(SymbolRegular.Save20),
                Content = (StackPanel)TryFindResource("AddEndpointDialog"),
                PrimaryButtonText = "Save",
                CloseButtonText = "Cancel",
            };
        }

        private async void NewFolderCmd_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            try
            {
                var item = GetSelectedItem(sender);
                var dw = new ToolWindowDialog(RootContentDialog)
                {
                    Title = "Add Folder",
                    PrimaryButtonIcon = new SymbolIcon(SymbolRegular.Save20),
                    Content = (StackPanel)TryFindResource("AddFolderDialog"),
                    PrimaryButtonText = "Save",
                    CloseButtonText = "Cancel",
                };
                var sp = (StackPanel)dw.Content;
                var foldername = (Wpc.TextBox)((StackPanel)sp.Children[0]).Children[1];
                var errors = (Wpc.TextBlock)((StackPanel)sp.Children[1]).Children[0];
                var validForClose = false;
                dw.ButtonClicked += (cd, args) =>
                {
                    validForClose = false;
                    errors.Visibility = Visibility.Hidden;
                    if (args.Button == ContentDialogButton.Primary)
                    {
                        if (!string.IsNullOrEmpty(foldername.Text))
                        {
                            if (item.HasChild(foldername.Text, BlockchainInfoKind.Endpoint))
                            {
                                ShowValidationErrors(errors, "Enter a unique folder name.");
                                return;
                            }
                            else
                            {
                                validForClose = true;
                            }
                        }
                        else
                        {
                            validForClose = false;
                            ShowValidationErrors(errors, "Enter a valid folder name.");
                        }
                    }
                    else
                    {
                        validForClose = true;
                    }
                };
                dw.Closing += (d, args) =>
                {
                    args.Cancel = !validForClose;
                };
                var r = await dw.ShowAsync();
                if (r != ContentDialogResult.Primary)
                {
                    foldername.Text = "";
                    return;
                }
                var f = item.AddChild(foldername.Text, BlockchainInfoKind.UserFolder);
                if (!item.Save("BlockchainExplorerTree", out var ex))
                {
#if IS_VSIX
                    VSUtil.ShowModalErrorDialogBox("Error saving tree data: " + ex?.Message);
#else
                    System.Windows.MessageBox.Show("Error saving tree data: " +  ex?.Message);
#endif
                }
            }
            catch (Exception ex)
            {
#if IS_VSIX
                VSUtil.ShowModalErrorDialogBox(ex?.Message);
#else
                System.Windows.MessageBox.Show(ex?.Message);
#endif
            }
        }

        private void DeleteFolderCmd_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var window = (BlockchainExplorerToolWindowControl)sender;
            var tree = window.BlockchainExplorerTree;
            var item = GetSelectedItem(sender);
            item.Parent.DeleteChild(item);
            if (!tree.RootItem.Save("BlockchainExplorerTree", out var ex))
            {
#if IS_VSIX
                VSUtil.ShowModalErrorDialogBox("Error saving tree data: " + ex?.Message);
#else
                System.Windows.MessageBox.Show("Error saving tree data: " +  ex?.Message);
#endif
            }
        }

        private void DeleteNetworkCmd_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var window = (BlockchainExplorerToolWindowControl)sender;
            var tree = window.BlockchainExplorerTree;
            var item = GetSelectedItem(sender);
            item.Parent.DeleteChild(item);
            if (!tree.RootItem.Save("BlockchainExplorerTree", out var ex))
            {
#if IS_VSIX
                VSUtil.ShowModalErrorDialogBox("Error saving tree data: " + ex?.Message);
#else
                System.Windows.MessageBox.Show("Error saving tree data: " +  ex?.Message);
#endif
            }
        }

        private void DeleteNetworkCmd_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            var item = GetSelectedItem(sender);
            if (item.Name == "//")
            {
                e.CanExecute = false;
            }
            else
            {
                e.CanExecute = true;
            }
        }

        private async void EditAccountCmd_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            try
            {
                var window = (BlockchainExplorerToolWindowControl)sender;
                var tree = window.BlockchainExplorerTree;
                var item = GetSelectedItem(sender);
                var dw = new ToolWindowDialog(RootContentDialog)
                {
                    Title = "Edit Account",
                    PrimaryButtonIcon = new SymbolIcon(SymbolRegular.Save20),
                    Content = (StackPanel)TryFindResource("EditAccountDialog"),
                    PrimaryButtonText = "Save",
                    CloseButtonText = "Cancel",
                };
                var sp = (StackPanel)dw.Content;
                var acctpubkey = (Wpc.TextBlock)((StackPanel)sp.Children[0]).Children[1];
                acctpubkey.Text = item.Name;
                var acctlabel = (Wpc.TextBox)((StackPanel)sp.Children[0]).Children[3];
                acctlabel.Text = (string)item.Data["Label"];
                var acctpkey = (Wpc.PasswordBox)((StackPanel)sp.Children[0]).Children[5];
                var errors = (Wpc.TextBlock)((StackPanel)sp.Children[1]).Children[0];
                // Show the account's existing key (masked); a blank box means the account has no key.
                acctpkey.Password = item.TryGetPrivateKey() ?? "";
                acctpkey.IsReadOnly = false;
                acctpkey.PlaceholderText = "";
                errors.Visibility = Visibility.Hidden;
                var validForClose = false;
                dw.ButtonClicked += (cd, args) =>
                {
                    validForClose = false;
                    errors.Visibility = Visibility.Hidden;
                    if (!IsValidPrivateKey(acctpkey.Password))
                    {
                        ShowValidationErrors(errors, "Enter a valid private key: 64 hex characters, optionally 0x-prefixed.");
                    }
                    else
                    {
                        validForClose = true;
                    }
                };
                dw.Closing += (d, args) =>
                {
                    args.Cancel = !validForClose;
                };
                var r = await dw.ShowAsync();
                if (r != ContentDialogResult.Primary)
                {
                    acctlabel.Text = (string)item.Data["Label"];
                    return;
                }
                item.Data["Label"] = acctlabel.Text;
                // The box is authoritative (pre-filled with the existing key): store what's there, or drop the key
                // if the user cleared it.
                if (!string.IsNullOrEmpty(acctpkey.Password))
                {
                    item.Data["PrivateKey"] = item.SetPrivateKey(acctpkey.Password);
                }
                else
                {
                    item.Data.Remove("PrivateKey");
                }
                if (!tree.RootItem.Save("BlockchainExplorerTree", out var ex))
                {
#if IS_VSIX
                    VSUtil.ShowModalErrorDialogBox("Error saving tree data: " + ex?.Message);
#else
                    System.Windows.MessageBox.Show("Error saving tree data: " + ex?.Message);
#endif
                }
                else
                {
                    tree.Refresh();
                }
            }
            catch (Exception ex)
            {
#if IS_VSIX
                VSUtil.ShowModalErrorDialogBox(ex?.Message);
#else
                System.Windows.MessageBox.Show(ex?.Message);
#endif
            }
        }

        private async void NewDeployProfileCmd_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            try
            {
                var window = (BlockchainExplorerToolWindowControl)sender;
                var tree = window.BlockchainExplorerTree;
                var item = GetSelectedItem(sender);
                if (item.Kind == BlockchainInfoKind.Folder && item.Name == "Deploy Profiles")
                {
                    item = item.Parent;
                }
                var dw = new ToolWindowDialog(RootContentDialog)
                {
                    Title = "Add Deploy Profile",
                    PrimaryButtonIcon = new SymbolIcon(SymbolRegular.Save20),
                    Content = (StackPanel)TryFindResource("AddDeployProfileDialog"),
                    PrimaryButtonText = "Save",
                    CloseButtonText = "Cancel",
                };
                var sp = (StackPanel)dw.Content;
                var name = (Wpc.TextBox)((StackPanel)sp.Children[0]).Children[1];
                var endpoint = (ComboBox)((StackPanel)sp.Children[1]).Children[1];
                var accounts = (ComboBox)((StackPanel)sp.Children[2]).Children[1];
                var pkey = (Wpc.PasswordBox)((StackPanel)sp.Children[3]).Children[1];
                var errors = (Wpc.TextBlock)((StackPanel)sp.Children[4]).Children[0];
                endpoint.ItemsSource = item.GetNetworkEndPoints();
                endpoint.SelectedIndex = 0;
                accounts.ItemsSource = item.GetNetworkAccounts();
                // The dialog is a shared resource, so its inputs keep their values between opens. Clear the name.
                name.Text = "";
                // The private-key box is a read-only display of the selected account's stored key (accounts own keys).
                pkey.Password = "";
                SelectionChangedEventHandler onAcctChanged = (s, ev) => ApplyAccountKeyState(item, (string)accounts.SelectedValue, pkey, editableWhenNoKey: false);
                accounts.SelectionChanged += onAcctChanged;
                var validForClose = false;

                dw.ButtonClicked += (cd, args) =>
                {
                    validForClose = false;
                    errors.Visibility = Visibility.Hidden;

                    if (args.Button == ContentDialogButton.Primary)
                    {
                        if (!string.IsNullOrEmpty(name.Text) && accounts.SelectedValue != null && endpoint.SelectedValue != null)
                        {
                            var dp = item.GetNetworkDeployProfiles();
                            if (dp.Contains(name.Text))
                            {
                                ShowValidationErrors(errors, "The " + name.Text + " deploy profile already exists.");
                            }
                            else
                            {
                                validForClose = true;
                            }
                        }
                        else
                        {
                            ShowValidationErrors(errors, "Enter a deploy profile name and select a valid endpoint and account");
                        }
                    }
                    else
                    {
                        validForClose = true;
                    }
                };

                dw.Closing += (d, args) =>
                {
                    args.Cancel = !validForClose;
                };

                var r = await dw.ShowAsync();
                accounts.SelectionChanged -= onAcctChanged;
                if (r != ContentDialogResult.Primary)
                {
                    name.Text = "";
                    endpoint.ItemsSource = null;
                    accounts.ItemsSource = null;
                    return;
                }
                else
                {
                    // The profile only references an account; the private key (if any) lives on the account.
                    item.GetChild("Deploy Profiles", BlockchainInfoKind.Folder).AddDeployProfile(name.Text, (string)endpoint.SelectedValue, (string)accounts.SelectedValue);
                }
              
                if (!tree.RootItem.Save("BlockchainExplorerTree", out var ex))
                {
#if IS_VSIX
                VSUtil.ShowModalErrorDialogBox("Error saving tree data: " + ex?.Message);
#else
                    System.Windows.MessageBox.Show("Error saving tree data: " + ex?.Message);
#endif
                }
                else
                {
                    tree.Refresh();
                }
            }
            catch (Exception ex)
            {
#if IS_VSIX
                VSUtil.ShowModalErrorDialogBox(ex?.Message);
#else
                System.Windows.MessageBox.Show(ex?.Message);
#endif
            }
        }

        private async void EditDeployProfileCmd_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            try
            {
                var window = (BlockchainExplorerToolWindowControl)sender;
                var tree = window.BlockchainExplorerTree;
                var item = GetSelectedItem(sender);
                var dw = new ToolWindowDialog(RootContentDialog)
                {
                    Title = "Edit Deploy Profile",
                    PrimaryButtonIcon = new SymbolIcon(SymbolRegular.Save20),
                    Content = (StackPanel)TryFindResource("AddDeployProfileDialog"),
                    PrimaryButtonText = "Save",
                    CloseButtonText = "Cancel",
                };
                var sp = (StackPanel)dw.Content;
                var name = (Wpc.TextBox)((StackPanel)sp.Children[0]).Children[1];
                var endpoint = (ComboBox)((StackPanel)sp.Children[1]).Children[1];
                var accounts = (ComboBox)((StackPanel)sp.Children[2]).Children[1];
                var pkey = (Wpc.PasswordBox)((StackPanel)sp.Children[3]).Children[1];
                var errors = (Wpc.TextBlock)((StackPanel)sp.Children[4]).Children[0];
                name.Text = item.Name;
                endpoint.ItemsSource = item.Parent.Parent.GetNetworkEndPoints();
                endpoint.SelectedValue = item.Data["Endpoint"];
                accounts.ItemsSource = item.Parent.Parent.GetNetworkAccounts();
                // Read-only display of the selected account's stored key (accounts own keys, not profiles).
                pkey.Password = "";
                SelectionChangedEventHandler onAcctChanged = (s, ev) => ApplyAccountKeyState(item.Parent.Parent, (string)accounts.SelectedValue, pkey, editableWhenNoKey: false);
                accounts.SelectionChanged += onAcctChanged;
                accounts.SelectedValue = item.Data["Account"];
                ApplyAccountKeyState(item.Parent.Parent, (string)accounts.SelectedValue, pkey, editableWhenNoKey: false);
                var validForClose = false;

                dw.ButtonClicked += (cd, args) =>
                {
                    validForClose = false;
                    errors.Visibility = Visibility.Hidden;

                    if (args.Button == ContentDialogButton.Primary)
                    {
                        if (!string.IsNullOrEmpty(name.Text) && accounts.SelectedValue != null && endpoint.SelectedValue != null)
                        {
                            var dp = item.Parent.Parent.GetNetworkDeployProfiles();
                            if (name.Text != item.Name && dp.Contains(name.Text))
                            {
                                ShowValidationErrors(errors, "The " + name.Text + " deploy profile already exists.");
                            }
                            else
                            {
                                validForClose = true;
                            }
                        }
                        else
                        {
                            ShowValidationErrors(errors, "Enter a deploy profile name and select a valid endpoint and account");
                        }
                    }
                    else
                    {
                        validForClose = true;
                    }
                };

                dw.Closing += (d, args) =>
                {
                    args.Cancel = !validForClose;
                };

                var r = await dw.ShowAsync();
                accounts.SelectionChanged -= onAcctChanged;
                if (r != ContentDialogResult.Primary)
                {
                    name.Text = "";
                    endpoint.ItemsSource = null;
                    accounts.ItemsSource = null;
                    return;
                }

                item.Name = name.Text;
                item.Data["Account"] = accounts.SelectedValue;
                item.Data["Endpoint"] = endpoint.SelectedValue;
                if (!tree.RootItem.Save("BlockchainExplorerTree", out var ex))
                {
#if IS_VSIX
                VSUtil.ShowModalErrorDialogBox("Error saving tree data: " + ex?.Message);
#else
                    System.Windows.MessageBox.Show("Error saving tree data: " + ex?.Message);
#endif
                }
                else
                {
                    tree.Refresh();
                }
            }
            catch (Exception ex)
            {
#if IS_VSIX
                VSUtil.ShowModalErrorDialogBox(ex?.Message);
#else
                System.Windows.MessageBox.Show(ex?.Message);
#endif
            }
        }

        private void DeleteDeployProfileCmd_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var window = (BlockchainExplorerToolWindowControl)sender;
            var tree = window.BlockchainExplorerTree;
            var item = GetSelectedItem(sender);
            item.Parent.DeleteChild(item);
            if (!tree.RootItem.Save("BlockchainExplorerTree", out var ex))
            {
#if IS_VSIX
                VSUtil.ShowModalErrorDialogBox("Error saving tree data: " + ex?.Message);
#else
                System.Windows.MessageBox.Show("Error saving tree data: " + ex?.Message);
#endif
            }
            tree.Refresh();
        }

        private async void NewAccountCmd_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            try
            {
                var window = (BlockchainExplorerToolWindowControl)sender;
                var tree = window.BlockchainExplorerTree;
                // Launched from the Network node. Get-or-create its "Accounts" folder so a network persisted
                // without one doesn't throw "Sequence contains no elements".
                var selected = GetSelectedItem(sender);
                if (selected == null || selected.Kind != BlockchainInfoKind.Network)
                {
#if IS_VSIX
                    VSUtil.ShowModalErrorDialogBox("Select a network in the Blockchain Explorer, then choose Add Account.", "Add Account");
#else
                    System.Windows.MessageBox.Show("Select a network in the Blockchain Explorer, then choose Add Account.");
#endif
                    return;
                }
                var item = selected.GetOrAddChild("Accounts", BlockchainInfoKind.Folder);
                var dw = new ToolWindowDialog(RootContentDialog)
                {
                    Title = "Add Account",
                    PrimaryButtonIcon = new SymbolIcon(SymbolRegular.Save20),
                    Content = (StackPanel)TryFindResource("AddAccountDialog"),
                    PrimaryButtonText = "Save",
                    CloseButtonText = "Cancel",
                };
                var sp = (StackPanel)dw.Content;
                var acctpubkey = (Wpc.TextBox)((StackPanel)sp.Children[0]).Children[1];
                var acctlabel = (Wpc.TextBox)((StackPanel)sp.Children[0]).Children[3];
                var acctpkey = (Wpc.PasswordBox)((StackPanel)sp.Children[0]).Children[5];
                var errors = (Wpc.TextBlock)((StackPanel)sp.Children[1]).Children[0];
                // Shared dialog resource: clear values retained from a previous open.
                acctpubkey.Text = "";
                acctlabel.Text = "";
                acctpkey.Password = "";
                acctpkey.IsReadOnly = false;
                var validForClose = false;
                dw.ButtonClicked += (cd, args) =>
                {
                    validForClose = false;
                    errors.Visibility = Visibility.Hidden;
                    if (string.IsNullOrEmpty(acctpubkey.Text))
                    {
                        ShowValidationErrors(errors, "Enter a valid account public key.");
                    }
                    else if (!IsValidPrivateKey(acctpkey.Password))
                    {
                        ShowValidationErrors(errors, "Enter a valid private key: 64 hex characters, optionally 0x-prefixed.");
                    }
                    else
                    {
                        validForClose = true;
                    }
                };
                dw.Closing += (d, args) =>
                {
                    args.Cancel = !validForClose;
                };
                var r = await dw.ShowAsync();
                if (r != ContentDialogResult.Primary)
                {
                    return;
                }
                item.AddAccount(acctpubkey.Text, acctlabel.Text, acctpkey.Password);
                if (!tree.RootItem.Save("BlockchainExplorerTree", out var ex))
                {
#if IS_VSIX
                    VSUtil.ShowModalErrorDialogBox("Error saving tree data: " + ex?.Message);
#else
                    System.Windows.MessageBox.Show("Error saving tree data: " + ex?.Message);
#endif
                }
                else
                {
                    tree.Refresh();
                }
            }
            catch (Exception ex)
            {
#if IS_VSIX
                VSUtil.ShowModalErrorDialogBox(ex?.Message);
#else
                System.Windows.MessageBox.Show(ex?.Message);
#endif
            }
        }

        private void DeleteAccountCmd_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var window = (BlockchainExplorerToolWindowControl)sender;
            var tree = window.BlockchainExplorerTree;
            var item = GetSelectedItem(sender);
            item.Parent.DeleteChild(item);
            if (!tree.RootItem.Save("BlockchainExplorerTree", out var ex))
            {
#if IS_VSIX
                VSUtil.ShowModalErrorDialogBox("Error saving tree data: " + ex?.Message);
#else
                System.Windows.MessageBox.Show("Error saving tree data: " + ex?.Message);
#endif
            }
            tree.Refresh();
        }

        private void CopyAccountAddressCmd_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var item = GetSelectedItem(sender);
            var address = item.Name;
            Clipboard.SetText(address);
        }

        private async void EditContractCmd_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            try
            {
                var window = (BlockchainExplorerToolWindowControl)sender;
                var tree = window.BlockchainExplorerTree;
                var item = GetSelectedItem(sender);
                var _sp = (StackPanel)TryFindResource("EditContractDialog");
                var dw = new ToolWindowDialog(RootContentDialog)
                {
                    Title = "Contract details: " + item.DisplayName,
                    Content = _sp,
                    // Deployed contracts are view-only here; "Run" opens the Run Contract dialog.
                    PrimaryButtonText = "Run",
                    PrimaryButtonIcon = new SymbolIcon(SymbolRegular.Run16),
                    CloseButtonText = "Close",
                };
                var sp = (StackPanel)(_sp).Children[0];
                var address = (Wpc.TextBox)(sp.Children[1]);
                var label = (Wpc.TextBox)(sp.Children[3]);
                var creator = (Wpc.TextBox)(sp.Children[5]);
                var transactionHash = (Wpc.TextBox)(sp.Children[7]);
                var deployedOn = (Wpc.TextBox)(sp.Children[9]);
                var abi = (Wpc.TextBox)(sp.Children[11]);
                //var sp1 = (StackPanel)((StackPanel)dw.Content).Children[1];
                //var errors = (Wpc.TextBlock) sp1.Children[0];
                address.Text = item.Name;
                label.Text = item.Data.ContainsKey("Label") ? (string)item.Data["Label"] : "";
                creator.Text = (string)item.Data["Creator"];
                transactionHash.Text = (string)item.Data["TransactionHash"];
                deployedOn.Text = (string)item.Data["DeployedOn"];
                abi.Text = (string)item.Data["Abi"];               
                var r = await dw.ShowAsync();
                if (r == ContentDialogResult.Primary)
                {
                    BlockchainExplorerTree.RunContractCmd.Execute(null, tree);
                }
            }
            catch (Exception ex)
            {
#if IS_VSIX
                VSUtil.ShowModalErrorDialogBox(ex?.Message, "View Contract error");
#else
                System.Windows.MessageBox.Show(ex?.Message);
#endif
            }
        }

        private void DeleteContractCmd_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var window = (BlockchainExplorerToolWindowControl)sender;
            var tree = window.BlockchainExplorerTree;
            var item = GetSelectedItem(sender);
            item.Parent.DeleteChild(item);
            SaveBlockchainTree(tree);
        }

        private async void RunContractCmd_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            try
            {
                var window = (BlockchainExplorerToolWindowControl)sender;
                var tree = window.BlockchainExplorerTree;
                var item = GetSelectedItem(sender);
                var _sp = (StackPanel)TryFindResource("RunContractDialog");
                var dw = new ToolWindowDialog(RootContentDialog)
                {
                    Title = "Run contract " + item.DisplayName,
                    Content = _sp,
                    CloseButtonText = "Close",
                };
           
                var transactCheckBox = (CheckBox)((_sp).Children[0]);
                var transactPanel = (StackPanel)((_sp).Children[1]);
                var fromAccountComboBox = (ComboBox)((StackPanel)(transactPanel).Children[0]).Children[1];
                var privateKeyPasswordBox = (Wpc.PasswordBox)((StackPanel)(transactPanel).Children[0]).Children[3];
                var estimateGasRadioButton = (RadioButton) ((StackPanel)(((StackPanel)transactPanel.Children[0]).Children[4])).Children[1];
                var customGasRadioButton = (RadioButton) ((StackPanel) (((StackPanel)(((StackPanel)transactPanel.Children[0]).Children[4])).Children[2])).Children[0];
                var customGasNumberBox = (Wpc.TextBox)((StackPanel)(((StackPanel)(((StackPanel)transactPanel.Children[0]).Children[4])).Children[2])).Children[1];
                // List the network's saved accounts (item is the contract: Contract -> "Contracts" folder -> Network).
                var accounts = item.Parent.Parent.GetNetworkAccounts().ToList();
                fromAccountComboBox.ItemsSource = accounts;
                // Default the "from" account to the contract's deployer (Creator) when it's one of the saved accounts.
                // The user can pick another, and a supplied private key overrides it with the key's address.
                var creator = item.Data.ContainsKey("Creator") ? (string)item.Data["Creator"] : null;
                fromAccountComboBox.SelectedItem = (creator != null && accounts.Contains(creator)) ? creator
                    : (accounts.Count > 0 ? accounts[0] : null);
                // The dialog is a shared resource, so its inputs persist between opens. Clear the private-key box so a
                // stale value from a previous run can't be silently reused.
                privateKeyPasswordBox.Password = "";
                // Read-only when the chosen account has a stored key (it's used); otherwise the user can type one.
                SelectionChangedEventHandler onAcctChanged = (s, ev) => ApplyAccountKeyState(item.Parent.Parent, (string)fromAccountComboBox.SelectedItem, privateKeyPasswordBox, editableWhenNoKey: true);
                fromAccountComboBox.SelectionChanged += onAcctChanged;
                ApplyAccountKeyState(item.Parent.Parent, (string)fromAccountComboBox.SelectedItem, privateKeyPasswordBox, editableWhenNoKey: true);
                transactCheckBox.Checked += (s, ev) =>
                {
                    transactPanel.IsEnabled = true;
                };
                transactCheckBox.Unchecked += (s, ev) =>
                {
                    transactPanel.IsEnabled = false;
                };
                estimateGasRadioButton.Checked += (s, ev) =>
                {
                    customGasNumberBox.IsEnabled = false;
                };
                customGasRadioButton.Checked += (s, ev) =>
                {
                    customGasNumberBox.IsEnabled = true;
                };  
                var formPanel = (StackPanel)(_sp).Children[2];
                var statusPanel = ((StackPanel)(_sp).Children[3]);
                // Effective key for a transaction: the selected account's stored key takes precedence; otherwise the
                // key typed into the box (which is empty/disabled when a stored key exists). May be empty — that's
                // allowed, since the node may manage the account (e.g. a local simulator).
                Func<string> resolveKey = () =>
                {
                    var stored = item.Parent.Parent.GetNetworkAccount((string)fromAccountComboBox.SelectedItem)?.TryGetPrivateKey();
                    return !string.IsNullOrEmpty(stored) ? stored : privateKeyPasswordBox.Password;
                };
                await CreateRunContractFormAsync(formPanel, statusPanel, item.Data, transactCheckBox, fromAccountComboBox, resolveKey, () => (estimateGasRadioButton.IsChecked ?? false) ? null : new HexBigInteger(long.TryParse(customGasNumberBox.Text, out var cg) ? cg : 3000000L));
                dw.ButtonClicked += (cd, args) => { };
                dw.Closing += (d, args) => { };
                await dw.ShowAsync();
                fromAccountComboBox.SelectionChanged -= onAcctChanged;
            }
            catch (Exception ex)
            {
#if IS_VSIX
                VSUtil.ShowModalErrorDialogBox(ex?.Message, "Run Contract Error");
#else
                System.Windows.MessageBox.Show(ex?.Message);
#endif
            }
        }
        #endregion

        #region Methods
        private BlockchainInfo GetSelectedItem(object sender)
        {
            var window = (BlockchainExplorerToolWindowControl)sender;
            var tree = window.BlockchainExplorerTree;
            return tree.SelectedItem;
        }

        private void SaveBlockchainTree(BlockchainExplorerTree tree)
        {
            if (!tree.RootItem.Save("BlockchainExplorerTree", out var ex))
            {
#if IS_VSIX
                VSUtil.ShowModalErrorDialogBox("Error saving tree data: " + ex?.Message);
#else
                System.Windows.MessageBox.Show("Error saving tree data: " +  ex?.Message);
#endif
            }
        }

        // A secp256k1 private key is 32 bytes: 64 hex characters, optionally 0x-prefixed. Empty is allowed (the field is optional).
        private static bool IsValidPrivateKey(string s)
        {
            if (string.IsNullOrEmpty(s)) return true;
            var k = s.Trim();
            if (k.StartsWith("0x") || k.StartsWith("0X")) k = k.Substring(2);
            return k.Length == 64 && k.All(Uri.IsHexDigit);
        }

        // Reflects the selected account's stored key into a deploy/run dialog's private-key box: when a key exists the
        // box shows it (masked) and is read-only; when none exists the box is blank. With editableWhenNoKey (the run
        // dialog) a keyless account leaves the box editable so a transient key can be typed for the transaction;
        // otherwise (deploy profile) the box stays read-only and informational. Returns true when a key is stored.
        private static bool ApplyAccountKeyState(BlockchainInfo network, string accountAddress, Wpc.PasswordBox pkey, bool editableWhenNoKey)
        {
            var stored = network?.GetNetworkAccount(accountAddress)?.TryGetPrivateKey();
            if (!string.IsNullOrEmpty(stored))
            {
                pkey.Password = stored;
                pkey.IsReadOnly = true;
            }
            else
            {
                pkey.Password = "";
                pkey.IsReadOnly = !editableWhenNoKey;
            }
            pkey.PlaceholderText = "";
            return !string.IsNullOrEmpty(stored);
        }

        private void ShowValidationErrors(Wpc.TextBlock textBlock, string message)
        {
            textBlock.Visibility = Visibility.Visible;
            textBlock.Text = message;
        }

        private void HideValidationErrors(Wpc.TextBlock textBlock) => textBlock.Visibility = Visibility.Hidden; 

        private void ShowProgressRing(ProgressRing progressRing)
        {
            progressRing.IsEnabled = true;
            progressRing.Visibility = Visibility.Visible;
        }

        private void HideProgressRing(ProgressRing progressRing)
        {
            progressRing.IsEnabled = false;
            progressRing.Visibility = Visibility.Hidden;
        }

        // Shows the spinning ring + status text while a contract function runs (mirrors the deploy tool window).
        private void ShowRunProgress(StackPanel progressPanel, Wpc.TextBlock statusText, string text)
        {
            statusText.Text = text;
            progressPanel.Visibility = Visibility.Visible;
        }

        private void HideRunProgress(StackPanel progressPanel) => progressPanel.Visibility = Visibility.Hidden;

        private void ShowValidationSuccess(StackPanel successPanel, Wpc.TextBlock successTextBlock, string message)
        {
            successPanel.Visibility = Visibility.Visible;
            successTextBlock.Text = message;
        }   

        private void HideValidationSuccess(StackPanel successPanel) => successPanel.Visibility = Visibility.Hidden;

        private async Task CreateRunContractFormAsync(StackPanel form, StackPanel statusPanel, Dictionary<string, object> contractData, CheckBox transactCheckBox, ComboBox fromAccount, Func<string> privateKey, Func<HexBigInteger> gas)
        {
            form.Children.Clear();
            var errors = (Wpc.TextBlock)((Grid)statusPanel.Children[0]).Children[0];
            var progressPanel = (StackPanel)((Grid)statusPanel.Children[0]).Children[1];
            var statusText = (Wpc.TextBlock)progressPanel.Children[1];
            var successPanel = ((StackPanel)((Grid)statusPanel.Children[0]).Children[2]);
            var successTextBlock = (Wpc.TextBlock) successPanel.Children[1];
            var address = (string)contractData["Address"];
            var rpcurl = (string)contractData["Endpoint"];
            var abi = (string)contractData["Abi"];
            var _abi = Contract.DeserializeABI(abi);

            ShowRunProgress(progressPanel, statusText, "Loading contract balance…");
            var balr = await ThreadHelper.JoinableTaskFactory.RunAsync(() => ExecuteAsync(Network.GetBalance(rpcurl, address)));
            HideRunProgress(progressPanel);
            if (balr.IsSuccess)
            {
                HideValidationErrors(errors);
                var hsp = new StackPanel()
                {
                    Orientation = Orientation.Horizontal,
                };
                hsp.Children.Add(new Label()
                {
                    Content = "Contract Balance: ",
                    VerticalAlignment = VerticalAlignment.Center,
                });
                hsp.Children.Add(new Wpc.TextBlock()
                {
                    Text = $"{UnitConversion.Convert.FromWei(balr.Value, UnitConversion.EthUnit.Ether)} ETH",
                    FontWeight= FontWeights.Bold,
                    VerticalAlignment = VerticalAlignment.Center,
                });
                form.Children.Add(hsp); 
            }
            else
            {
                ShowValidationErrors(errors, $"Could not retrieve balance for contract. {balr.FailureMessage}");
                return;
            }
                       
            foreach (var function in _abi.Functions)
            {
                var vsp = new StackPanel()
                {
                    Orientation = Orientation.Vertical,
                };
                vsp.Children.Add(new Separator()
                {
                    Margin = new Thickness(0, 8, 0, 8),
                    Height = 1
                });

                var button = new Wpc.Button()
                {
                    Name = function.Name + "_Button",
                    Content = function.Name,
                    MinWidth = 75.0,
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    FontSize = 11.0,
                    Margin = new Thickness(0, 4, 0, 0)
                };
                // Orange to emphasize that clicking runs the function immediately, matching Remix IDE's run buttons.
                // The dedicated template keeps the orange through hover/pressed states.
                button.SetResourceReference(FrameworkElement.StyleProperty, "RunContractFunctionButtonStyle");

                if (function.InputParameters != null && function.InputParameters.Count() > 0)
                {
                    foreach (var p in function.InputParameters)
                    {
                        var sp = new StackPanel()
                        {
                            Orientation = Orientation.Horizontal,
                            VerticalAlignment = VerticalAlignment.Top,
                            HorizontalAlignment = HorizontalAlignment.Left,
                            Margin = new Thickness(4, 4, 0, 2)
                        };
                        var lbl = new Wpc.TextBlock { Width = 100, VerticalAlignment = VerticalAlignment.Bottom};
                        lbl.Inlines.Add(new Run() { Text = p.Name });
                        lbl.Inlines.Add(new Run() { Text = $" ({p.Type}): ", FontStyle = FontStyles.Italic, FontSize=9.0});
                        var tb = new Wpc.TextBox() { Name = $"Param{p.Name}TextBox", Width = 150, VerticalAlignment = VerticalAlignment.Bottom };
                        tb.SetResourceReference(FrameworkElement.StyleProperty, Microsoft.VisualStudio.Shell.VsResourceKeys.ThemedDialogTextBoxStyleKey);
                        sp.Children.Add(lbl);
                        sp.Children.Add(tb);
                        vsp.Children.Add(sp);
                    }

                    button.Click += async (s, e) =>
                    {
                        var paramVals = GetContractFunctionParams(vsp, function.InputParameters.ToDictionary(ip => ip.Name, ip => ip.Type), out string paramError);
                        if (!string.IsNullOrEmpty(paramError))
                        {
                            ShowValidationErrors(errors, $"Error parsing function parameters: \n{paramError}");
                            VSUtil.LogToVsSolidityWindow($"\n========== Call contract {address} at {rpcurl} failed.==========\nError parsing function parameters: {paramError}");
                            return;
                        }
                        else if (paramVals.Length != function.InputParameters.Count())
                        {
                            ShowValidationErrors(errors, $"The {function.Name} function requires {function.InputParameters.Count()} parameters.");
                            VSUtil.LogToVsSolidityWindow($"\n========== Call contract {address} at {rpcurl} failed.==========\nThe {function.Name} function requires {function.InputParameters.Count()} parameters.");
                            return;
                        }

                        // view/pure (constant) functions never change state, so always call them (eth_call) to read
                        // the decoded return value. Sending them as a transaction would only return a tx hash.
                        bool transact = (transactCheckBox.IsChecked ?? false) && !function.Constant;
                        if (transact && string.IsNullOrEmpty((string)fromAccount.SelectedItem))
                        {
                            ShowValidationErrors(errors, "Enter a valid from address to send the transaction from.");
                            return;
                        }

                        HideValidationErrors(errors);
                        HideValidationSuccess(successPanel);
                        // RunAsync (awaited) keeps the UI thread free so the progress ring actually animates.
                        ShowRunProgress(progressPanel, statusText, $"Running {function.Name}…");
                        Result<string> r;
                        if (transact)
                        {
                            r = await ThreadHelper.JoinableTaskFactory.RunAsync(() => ExecuteAsync(Network.SendContractTransactionAsync(rpcurl, address, abi, function.Name, (string)fromAccount.SelectedItem, privateKey: privateKey(), gas:gas(), functionInput: paramVals)));
                        }
                        else
                        {
                            r = await ThreadHelper.JoinableTaskFactory.RunAsync(() => ExecuteAsync(Network.CallContractAsync(rpcurl, address, abi, function.Name, functionInput: paramVals)));
                        }
                        HideRunProgress(progressPanel);
                        string desc = transact ? "transaction" : "call";
                        if (r.IsSuccess)
                        {
                            // A transaction returns a tx hash, not the function's return value; a call returns the value.
                            string resultLabel = transact ? "returned transaction hash" : "returned";
                            HideValidationErrors(errors);
                            ShowValidationSuccess(successPanel, successTextBlock, $"Function {function.Name}({paramVals.Select(v => v.ToString()).JoinWith(",")}) {resultLabel}: {r.Value}");
                            VSUtil.LogToVsSolidityWindow($"\n========== Call contract {address} at {rpcurl} succeeded.==========\n[{desc}] {address +":" + function.Name}({function.InputParameters.Select((p,i) =>p.Type + " " + p.Name + ":" +paramVals.ElementAt(i)).JoinWith(", ")}) {resultLabel}: {r.Value}");
                        }
                        else
                        {
                            HideValidationSuccess(successPanel);
                            ShowValidationErrors(errors, $"Error calling function {function.Name}({paramVals.Select(v => v.ToString()).JoinWith(",")}): {r.FailureMessage}");
                            VSUtil.LogToVsSolidityWindow($"\n========== Call contract {address} at {rpcurl} failed.==========\n[{desc}] {address + ":" + function.Name}({function.InputParameters.Select((p, i) => p.Type + " " + p.Name + ":" + paramVals.ElementAt(i)).JoinWith(", ")}): {r.FailureMessage}");
                        }
                    };
                }                
                else
                {
                    button.Click += async (s, e) =>
                    {
                        // view/pure (constant) functions never change state, so always call them (eth_call) to read
                        // the decoded return value. Sending them as a transaction would only return a tx hash.
                        bool transact = (transactCheckBox.IsChecked ?? false) && !function.Constant;
                        if (transact && string.IsNullOrEmpty((string)fromAccount.SelectedItem))
                        {
                            ShowValidationErrors(errors, "Enter a valid from address to send the transaction from.");
                            return;
                        }

                        HideValidationErrors(errors);
                        HideValidationSuccess(successPanel);
                        ShowRunProgress(progressPanel, statusText, $"Running {function.Name}…");
                        Result<string> r;
                        if (transact)
                        {
                            r = await ThreadHelper.JoinableTaskFactory.RunAsync(() => ExecuteAsync(Network.SendContractTransactionAsync(rpcurl, address, abi, function.Name, (string)fromAccount.SelectedItem, privateKey: privateKey(), gas:gas())));
                        }
                        else
                        {
                            r = await ThreadHelper.JoinableTaskFactory.RunAsync(() => ExecuteAsync(Network.CallContractAsync(rpcurl, address, abi, function.Name)));
                        }
                        HideRunProgress(progressPanel);
                        string desc = transact ? "transaction" : "call";
                        if (r.IsSuccess)
                        {
                            // A transaction returns a tx hash, not the function's return value; a call returns the value.
                            string resultLabel = transact ? "returned transaction hash" : "returned";
                            HideValidationErrors(errors);
                            ShowValidationSuccess(successPanel, successTextBlock, $"Function {function.Name} {resultLabel}: {r.Value}");
                            VSUtil.LogToVsSolidityWindow($"\n========== Call contract {address} at {rpcurl} succeeded.==========\n[{desc}] {address + ":" + function.Name} {resultLabel}: {r.Value}");
                        }
                        else
                        {
                            HideValidationSuccess(successPanel);
                            ShowValidationErrors(errors, $"Error calling function: {r.FailureMessage}");
                            VSUtil.LogToVsSolidityWindow($"\n========== Call contract {address} at {rpcurl} failed.==========\n[{desc}] {address + ":" + function.Name}: {r.FailureMessage}");
                        }
                        
                    };
                }
                vsp.Children.Add(button);
                form.Children.Add(vsp);               
            }
        }

        private object[] GetContractFunctionParams(StackPanel form, Dictionary<string, string> paramTypes, out string error)
        {
            error = null;
            Dictionary<string, (string, string)> paramValues = new Dictionary<string, (string, string)>();
            foreach (var child in form.Children)
            {
                if (child is StackPanel sp && sp.Children.Count == 2 && sp.Children[0] is Wpc.TextBlock lbl && sp.Children[1] is Wpc.TextBox tb)
                {
                    var paramName = (Run)lbl.Inlines.FirstInline;
                    if (paramTypes.ContainsKey(paramName.Text) && !string.IsNullOrEmpty(tb.Text))
                    {
                        paramValues[paramName.Text] = (paramTypes[paramName.Text], tb.Text);
                    }
                }
            }
            try
            {                 
                return Contract.ParseFunctionParameterValues(paramValues);
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return Array.Empty<object>();
            }            
        }
        #endregion

        #region Properties
        public static bool ControlIsLoaded => instance != null;
        #endregion

        #region Fields
        internal BlockchainExplorerToolWindow window;
        internal static BlockchainExplorerToolWindowControl instance;
        #endregion

        
    }
}
