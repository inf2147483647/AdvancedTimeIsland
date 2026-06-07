using Avalonia.Controls; // 修正：使用 Avalonia 的 Controls 命名空间
using AdvancedTimeIsland.ViewModels.Main;

namespace AdvancedTimeIsland.Views.Main;

public partial class AdvancedDateControl : UserControl
{
    public AdvancedDateControl(AdvancedDateViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}