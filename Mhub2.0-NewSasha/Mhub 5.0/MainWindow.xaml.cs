using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
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
    public partial class MainWindow : Window
    {
        public static MainWindow Instanse;
        public readonly static MainViewModel main = new MainViewModel();

        public readonly IEnumerable<Product> DefaultProdutcts = DataBase.DatebaseConection.Product;
        public readonly IEnumerable<ProductMaterial> DefaultProdutctsMaterial = DataBase.DatebaseConection.ProductMaterial;
        public readonly IEnumerable<Material> DefaultMaterial = DataBase.DatebaseConection.Material;

        private static readonly Dictionary<Sorting, string> SortingNames = new Dictionary<Sorting, string>()
        {
            { Sorting.AscendingName, "От А до Я" },
            { Sorting.DescendingName, "От Я до А" },
            { Sorting.AscendingCost, "По возрастанию цены" },
            { Sorting.DescendingCost, "По убыванию цены" }
        };

        private List<TypeProduct> ProductTypes = DataBase.DatebaseConection.TypeProduct.ToList();

        public MainWindow()
        {
            InitializeComponent();

            #region Sort component fill
            SortComponent.ItemsSource = SortingNames;
            SortComponent.DisplayMemberPath = "Value";
            SortComponent.SelectedItem = SortingNames.ToArray()[0];
            #endregion
            #region Filter component fill
            TypeProduct defaultValue = new TypeProduct()
            {
                id = 0,
                Name = "Ничего"
            };
            ProductTypes.Insert(0, defaultValue);
            FilterComponent.ItemsSource = ProductTypes;
            FilterComponent.DisplayMemberPath = "Name";
            FilterComponent.SelectedItem = defaultValue;
            #endregion
        }

        #region Сортировка
        private IEnumerable<Product> Sort(IEnumerable<Product> products)
        {
            Sorting currentSorting = ((KeyValuePair<Sorting, string>)SortComponent.SelectedItem).Key;
            switch (currentSorting)
            {
                case Sorting.AscendingName:
                    return products.OrderBy(product => product.Name);
                case Sorting.DescendingName:
                    return products.OrderByDescending(product => product.Name);
                case Sorting.AscendingCost:
                    return products.OrderBy(product => product.Min);
                case Sorting.DescendingCost:
                    return products.OrderByDescending(product => product.Min);
                default:
                    throw new ArgumentException();
            }
        }
        #endregion

        #region Поиск по названию продукта 
        private bool IsSearched(Product product) => Regex.IsMatch(product.Name.ToLower(), $@"({Search.Text.Trim().ToLower()})");
        #endregion

        #region Фильтры
        private bool IsFiltred(Product product)
        {
            TypeProduct filtredType = (FilterComponent.SelectedItem as TypeProduct);
            if (filtredType == null || filtredType.Name == "Ничего")
                return true;
            return product.TypeProduct == filtredType;
        }
        #endregion

        public void DoStuff()
        {
            main.Items.Clear();
            foreach (var item in Sort(DefaultProdutcts.Where(product => IsFiltred(product) && IsSearched(product))).ToList())
            {
                var query = from productMaterial in DefaultProdutctsMaterial
                            from material in DefaultMaterial
                            where productMaterial.idProduct == item.id && material.id == productMaterial.idMaterial
                            select material.Price;

                if (query != null)
                {
                    int price = 0;

                    foreach (var pr in query)
                        price += (int)pr;

                    item.Min = price;
                }

                main.Items.Add(item);
            }
            DataContext = main;
        }

        private void Search_TextChanged(object sender, TextChangedEventArgs e) => DoStuff();

        enum Sorting
        {
            AscendingName,
            DescendingName,
            AscendingCost,
            DescendingCost
        }

        private void FilterComponent_SelectionChanged(object sender, SelectionChangedEventArgs e) => DoStuff();

        private void SortComponent_SelectionChanged(object sender, SelectionChangedEventArgs e) => DoStuff();
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
        public readonly IEnumerable<Product> DefaultProdutcts = DataBase.DatebaseConection.Product;

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
        }));

        public MainViewModel()
        {
            Items = new ObservableCollection<Product>();

            foreach (Product product in DataBase.DatebaseConection.Product)
            {
                Items.Add(product);
            }
            ItemsPerPage = CountPage();
            Page = 1;
            Collection.Filter = item =>
            {
                int index = Items.IndexOf(item as Product);
                return index >= (Page - 1) * ItemsPerPage && index < Page * ItemsPerPage;
            };
        }

        public int CountPage()
        {
            return DefaultProdutcts.Count() < 20 ? 4 : 20;
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
}
#endregion