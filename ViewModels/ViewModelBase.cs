using CommunityToolkit.Mvvm.ComponentModel;

namespace RevHub.ViewModels;

/// <summary>
/// ViewModel 基类
/// 提供 INotifyPropertyChanged、INotifyPropertyChanging 和 SetProperty 支持
/// </summary>
public abstract class ViewModelBase : ObservableObject
{
}
