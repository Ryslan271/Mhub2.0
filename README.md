https://ru.stackoverflow.com/questions/615927/wpf-%d0%a2%d0%b0%d0%b1%d0%bb%d0%b8%d1%86%d0%b0-xaml/616413#616413

using System;
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

namespace Mhub_5._0
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static MainWindow Instanse;
        static MainViewModel main = new MainViewModel();
        IOrderedQueryable<Product> productsSorted = DataBase.DatebaseConection.Product;
        IOrderedQueryable<Product> EmptyProducts;

        private static List<string> DictionarySortType = new List<string>()
        {
            "Ничего",
            "От а до я",
            "От я до а",
            "По цене",
            "По возрастанию id",
            "По убыванию id"
        };

        List<string> DictionaryFilterType = new List<string>() { };

        public MainWindow()
        {
            InitializeComponent();
        }

        #region Window_Loaded
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            EmptyProducts = productsSorted;

            var query = from typeProduct in DataBase.DatebaseConection.TypeProduct
                        select typeProduct;

            DictionaryFilterType.Add("Ничего");
            foreach (var item in query)
                DictionaryFilterType.Add(item.Name.ToString());

            Instanse = this; //-

            SortComponent.ItemsSource = DictionarySortType;

            FilterComponent.ItemsSource = DictionaryFilterType;
        }
        #endregion

        #region Сортировка
        private async void SortComponent_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var numberItem = SortComponent.SelectedIndex;

            productsSorted = SortComponent_Result(numberItem);

            main.Items.Clear();

            // если поле поиска не пустое то перебираем на его основе если же пустое то создаем новый маин и записываем уже его
            if (Search.Text != "")
            {
                foreach (var item in await Task.Run(() => AddQuery()))
                    main.Items.Add(item);
            }
            else
            {
                foreach (var item in await Task.Run(() => UbdateDB(productsSorted)))
                    main.Items.Add(item);
            }

            DataContext = main;
        }

        public IOrderedQueryable<Product> SortComponent_Result(int numberItem)
        {
            switch (numberItem)
            {
                case 0:
                    productsSorted = EmptyProducts;
                    break;
                case 1:
                    productsSorted = productsSorted.OrderBy(p => p.TypeProduct.Name);
                    break;
                case 2:
                    productsSorted = productsSorted.OrderByDescending(p => p.TypeProduct.Name);
                    break;
                case 3:
                    productsSorted = productsSorted.OrderBy(p => p.Min);
                    break;
                case 4:
                    productsSorted = productsSorted.OrderBy(p => p.id);
                    break;
                case 5:
                    productsSorted = productsSorted.OrderByDescending(p => p.id);
                    break;
            }
            return productsSorted;
        }

        public async Task<ObservableCollection<Product>> UbdateDB(IOrderedQueryable<Product> query)
        {
            ObservableCollection<Product> comboList = new ObservableCollection<Product>();

            foreach (var product in query)
                comboList.Add(product);

            return await Task.FromResult(comboList);
        }
        #endregion

        #region Поиск по названию продукта 
        public async Task<IEnumerable<Product>> AddQuery()
        {
            var empFiltered = from Emp in productsSorted
                              let ename = Emp.Name.ToLower()
                              where
                                  ename.StartsWith(Search.Text.ToLower().Trim())
                                  || ename.Contains(Search.Text.ToLower().Trim())
                              select Emp;

            return await Task.FromResult(empFiltered);
        }

        private async void Search_TextChanged(object sender, TextChangedEventArgs e)
        {

            main.Items.Clear();

            if (Search.Text != "")
            {
                foreach (var item in await Task.Run(() => AddQuery()))
                {
                    main.Items.Add(item);
                }
            }
            else
            {
                if (SortComponent.Text != "")
                {
                    foreach (var item in productsSorted)
                    {
                        main.Items.Add(item);
                    }
                }
                else
                {
                    foreach (var item in SortComponent_Result(SortComponent.SelectedIndex))
                    {
                        main.Items.Add(item);
                    }
                }
            }

            DataContext = main;
        }
        #endregion

        #region Фильтры
        private async void FilterComponent_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

            string source = FilterComponent.SelectedItem.ToString();
            main.Items.Clear();

            foreach (var item in await Task.Run(() => AsyncFilter(productsSorted, source)))
            {
                main.Items.Add(item);
            }

            DataContext = main;
        }
        private async Task<IEnumerable<Product>> AsyncFilter(IOrderedQueryable<Product> productsFilter, string source)
        {
            var empFiltered = from Emp in productsFilter
                              where Emp.TypeProduct.Name.ToLower() == source.ToLower()
                              select Emp;

            return await Task.FromResult(empFiltered);
        }
        #endregion
    }

    /// <summary>
    ///  генератор списка с числами по порядку
    /// </summary>
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

    public class NotifyPropertyChanged : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    /// <summary>
    ///  реализация ICommand для удобного использования команд
    /// </summary>
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

    /// <summary>
    ///  для работы в ListBox и ItemsControl
    /// </summary>
    public class MainViewModel : NotifyPropertyChanged
    {
        private int _itemsPerPage;
        private ObservableCollection<Product> _items;
        private int _page;
        private ICommand _setPageCommand;

        private ICollectionView Collection => CollectionViewSource.GetDefaultView(Items);

        public ObservableCollection<Product> Items
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

            // ((MainWindow.Instanse.Buttons.Items[(MainWindow.Instanse.DataContext as MainViewModel).Page] as Button).Content as TextBlock).TextDecorations = TextDecorations.Underline;
        }));

        public MainViewModel()
        {
            Items = new ObservableCollection<Product>();

            foreach (var product in DataBase.DatebaseConection.Product)
            {
                Items.Add(product);
            }
            ItemsPerPage = 4;
            Page = 1;
            Collection.Filter = item =>
            {
                int index = Items.IndexOf(item as Product);
                return index >= (Page - 1) * ItemsPerPage && index < Page * ItemsPerPage;
            };
        }
    }

    #region Конвертор материалов
    [ValueConversion(typeof(Product), typeof(string))]
    public class MaterialsConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is Product product))
                return string.Empty;

            var materialNames = from product_material in DataBase.DatebaseConection.ProductMaterial.ToArray()
                                where product_material.idProduct == product.id
                                select product_material.Material.Name;

            return $"{(materialNames.Count() == 0 ? "Материалов нет" : "Материалы: ")}{string.Join(", ", materialNames)}";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
    #endregion
}

