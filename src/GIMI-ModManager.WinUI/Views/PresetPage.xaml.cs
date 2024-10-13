using Windows.ApplicationModel.DataTransfer;
using Windows.System;
using GIMI_ModManager.WinUI.Services.AppManagement;
using GIMI_ModManager.WinUI.ViewModels;
using GIMI_ModManager.WinUI.Views.Controls;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace GIMI_ModManager.WinUI.Views;

public sealed partial class PresetPage : Page
{
    public PresetViewModel ViewModel { get; } = App.GetService<PresetViewModel>();

    public PresetPage()
    {
        InitializeComponent();
        PresetsList.DragItemsCompleted += PresetsList_DragItemsCompleted;
    }

    private async void PresetsList_DragItemsCompleted(ListViewBase sender, DragItemsCompletedEventArgs args)
    {
        if (args.DropResult == DataPackageOperation.Move && ViewModel.ReorderPresetsCommand.CanExecute(null))
        {
            await ViewModel.ReorderPresetsCommand.ExecuteAsync(null);
        }
    }

    private async void UIElement_OnKeyDown(object sender, KeyRoutedEventArgs e)
    {
        var presetVm = (ModPresetVm)((EditableTextBlock)sender).DataContext;

        if (e.Key == VirtualKey.Enter && ViewModel.RenamePresetCommand.CanExecute(presetVm))
        {
            await ViewModel.RenamePresetCommand.ExecuteAsync(presetVm);
        }
    }

    private TextBlock CreateTextBlock(string text)
    {
        return new TextBlock
        {
            Text = text,
            TextWrapping = TextWrapping.WrapWholeWords,
            IsTextSelectionEnabled = true
        };
    }

    private async void ButtonBase_OnClick(object sender, RoutedEventArgs e)
    {
        var dialog = new ContentDialog
        {
            Title = "预设是如何工作的",
            CloseButtonText = "Close",
            DefaultButton = ContentDialogButton.Close,
            Content = new StackPanel
            {
                Spacing = 16,
                Children =
                {
                    CreateTextBlock(
                        "预设是一个包含启用模组(Mods)及其设置的列表。JASM在模组.JASM_ModConfig.json中读取和存储模组首选项。"),
                    CreateTextBlock(
                        "当你创建一个新的预设时,JASM 会创建一个已启用模组的列表,以及存储在这些模组中的设置。因此,当您稍后应用该预设时,它将只启用这些模组,并应用预设中存储的设置"),

                    CreateTextBlock(
                        "您可以让 JASM 处理 3Dmigoto 的重新加载，方法是启动 Elevator 并勾选 Auto Sync 复选框。但是，您也可以手动进行此操作，方法是勾选 Show Manual Controls 复选框，手动保存和加载首选项，然后使用 F10 键刷新 3Dmigoto。(译者表示Elevator它用于自动向游戏发送F10键以刷新模组)"),

                    CreateTextBlock(
                        "您也可以完全忽略此页面的预设部分，只使用手动控制来保存模组首选项."
                    )
                }
            }
        };

        await App.GetService<IWindowManagerService>().ShowDialogAsync(dialog).ConfigureAwait(false);
    }

    private void DragHandleIcon_OnPointerEntered(object sender, PointerRoutedEventArgs e) =>
        ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.SizeAll);

    private void DragHandleIcon_OnPointerExited(object sender, PointerRoutedEventArgs e) =>
        ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.Arrow);
}