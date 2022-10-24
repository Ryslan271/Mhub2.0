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

        private static Dictionary<SortType, string> DictionarySortType = new Dictionary<SortType, string>()
        {
            {  SortType.FromAToZ, "От а до я" },
            {  SortType.FromZToA, "От я до а" },
            {  SortType.Cost, "По цене" },
            {  SortType.IdUp, "По возрастанию id" },
            {  SortType.IdDown, "По убыванию id" }
        };

        List<string> DictionaryFilterType = new List<string>(){};

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var query = from typeProduct in DataBase.DatebaseConection.TypeProduct
                        select typeProduct;

            foreach (var item in query)
                DictionaryFilterType.Add(item.Name.ToString());

            Instanse = this;

            SortComponent.ItemsSource = DictionarySortType;
            SortComponent.DisplayMemberPath = $"Value";

            FilterComponent.ItemsSource = DictionaryFilterType;
        }

        public enum SortType
        {
            FromAToZ,
            FromZToA,
            Cost,
            IdUp,
            IdDown
        }
        private async void SortComponent_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var numberItem = SortComponent.SelectedIndex;

            switch (numberItem)
            {
                case 0:
                    productsSorted = productsSorted.OrderBy(p => p.TypeProduct.Name);
                    break;
                case 1:
                    productsSorted = productsSorted.OrderByDescending(p => p.TypeProduct.Name);
                    break;
                case 2:
                    productsSorted = productsSorted.OrderBy(p => p.Min);
                    break;
                case 3:
                    productsSorted = productsSorted.OrderBy(p => p.id);
                    break;
                case 4:
                    productsSorted = productsSorted.OrderByDescending(p => p.id);
                    break;
            }

            main.Items.Clear();

            // если поле поиска не пустое то перебираем на его основе если же пустое то создаем новый маин и записываем уже его
            if (Search.Text != "")
            {
                foreach (var item in AddQuery())
                    main.Items.Add(item);
            }
            else
            {
                foreach (var item in await UbdateDB(productsSorted))
                    main.Items.Add(item);
            }

            DataContext = main;
        }
        public Task<ObservableCollection<Product>> UbdateDB(IOrderedQueryable<Product> query)
        {
            ObservableCollection<Product> comboList = new ObservableCollection<Product>();

            foreach (var product in query)
                comboList.Add(product);

            return Task.FromResult(comboList);
        }

        #region Поиск по названию продукта 
        public IEnumerable<Product> AddQuery()
        {
            var empFiltered = from Emp in productsSorted
                              let ename = Emp.Name.ToLower()
                              where
                                  ename.StartsWith(Search.Text.ToLower().Trim())
                                  || ename.Contains(Search.Text.ToLower().Trim())
                              select Emp;

            return empFiltered;
        }

        private void Search_TextChanged(object sender, TextChangedEventArgs e)
        {
            main.Items.Clear();

            foreach (var item in AddQuery())
            {
                main.Items.Add(item);
            }

            DataContext = main;
        }
        #endregion

        // не доделал фильтры 
        private void FilterComponent_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ObservableCollection<Product> productsFilter = main.Items;

            main.Items.Clear();

            var empFiltered = from Emp in productsFilter
                              let ename = Emp.TypeProduct.Name.ToLower()
                              where
                                  ename == e.Source.ToString()
                              select Emp;

            foreach (var item in empFiltered)
            {
                main.Items.Add(item);
            }
            DataContext = main;
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

            // не сделал подчеркивание ((MainWindow.Instanse.Buttons.Items[(MainWindow.Instanse.DataContext as MainViewModel).Page - 1] as Button).Content as TextBlock).TextDecorations = TextDecorations.Underline;

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
}

