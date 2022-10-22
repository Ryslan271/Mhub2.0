using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Mhub_2._0
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static Dictionary<SortType, string> DictionarySortType = new Dictionary<SortType, string>()
        {
            {  SortType.FromAToZ, "От а до я" },
            {  SortType.FromZToA, "От я до а" },
            {  SortType.Cost, "По цене" },
            {  SortType.IdUp, "По возрастанию id" },
            {  SortType.IdDown, "По убыванию id" }
        };

        public static MainWindow Instanse;
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Instanse = this;
            SortComponent.ItemsSource = DictionarySortType;
            SortComponent.DisplayMemberPath = $"Value";
        }

        private void SortTypeSelect(object sender, SelectionChangedEventArgs e)
        {
            
        }
        public enum SortType
        {
            FromAToZ,
            FromZToA,
            Cost,
            IdUp,
            IdDown
        }
    }

    #region генератор списка с числами по порядку
    public class PagerConverter : IMultiValueConverter
    {
        private IEnumerable<int> PagesGenerator()
        {
            int i = 1;
            while (i <= DataBase.DatebaseConection.Product.Count() / 4)
            {
                yield return i++;
            }
        }

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values[0] is int count && values[1] is int itemsPerPage)
            {
                int pages = (int)Math.Ceiling((double)count / itemsPerPage);
                return new List<int>(PagesGenerator().Take(pages));
            }
            return null;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => null;
    }
    #endregion

    public class NotifyPropertyChanged : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    // реализация ICommand для удобного использования команд
    public class RelayCommand : ICommand
    {
        private readonly Action<object> _execute;
        private readonly Func<object, bool> _canExecute;

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public RelayCommand(Action<object> execute, Func<object, bool> canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter) => _canExecute == null || _canExecute(parameter);
        public void Execute(object parameter) => _execute(parameter);
    }
    public class MainViewModel : NotifyPropertyChanged
    {
        private int _itemsPerPage;
        private ObservableCollection<Item> _items;
        private int _page;
        private ICommand _setPageCommand;

        private ICollectionView Collection => CollectionViewSource.GetDefaultView(Items);

        public ObservableCollection<Item> Items
        {
            get => _items;
            set
            {
                _items = value;
                OnPropertyChanged();
            }
        }

        public int Page
        {
            get => _page;
            set
            {
                _page = value;
                OnPropertyChanged();
                Collection.Refresh(); // сообщает ICollectionView, что надо перефильтроваться
            }
        }

        public int ItemsPerPage
        {
            get => _itemsPerPage;
            set
            {
                _itemsPerPage = value;
                OnPropertyChanged();
                Collection.Refresh();
            }
        }

        public ICommand SetPageCommand => _setPageCommand ?? (_setPageCommand = new RelayCommand(parameter =>
        {
            if (parameter is int page)
            {
                Page = page;
            }

            // не сделал подчеркивание ((MainWindow.Instanse.Buttons.Items[(MainWindow.Instanse.DataContext as MainViewModel).Page - 1] as Button).Content as TextBlock).TextDecorations = TextDecorations.Underline;

        }));

        public MainViewModel()
        {
            Items = new ObservableCollection<Item>();
            foreach(var product in DataBase.DatebaseConection.Product)
            {
                Items.Add(new Item {
                                        TypeText = product.TypeProduct.Name,    
                                        Separator = "|",
                                        ProductText = product.Name,
                                        Id = product.id.ToString(),
                                        Material = $"Материалы: asd",
                                        DescriptionText = "Стоимость",
                                        MinText = product.Min.ToString()
                                    }
                         );
            }
            ItemsPerPage = 4;
            Page = 1;
            Collection.Filter = item => {
                int index = Items.IndexOf(item as Item);
                return index >= (Page - 1) * ItemsPerPage && index < Page * ItemsPerPage;
            };
        }
    }
}


