using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;

namespace PupusasGo;

// ======================== CONVERTERS ========================

public class EqualsConverter : Avalonia.Data.Converters.IValueConverter
{
    public static readonly EqualsConverter Instance = new();

    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        return Equals(value, parameter);
    }

    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class InverseBoolConverter : Avalonia.Data.Converters.IValueConverter
{
    public static readonly InverseBoolConverter Instance = new();

    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        return value is bool b ? !b : false;
    }

    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        return value is bool b ? !b : false;
    }
}

// ======================== MODELOS ========================

public class Pupusa
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string Emoji { get; set; }
    public decimal Price { get; set; }
    public string Category { get; set; }
    public int Quantity { get; set; }
}

public class OrderItem
{
    public Pupusa Pupusa { get; set; }
    public int Quantity { get; set; }
    public decimal Subtotal => Pupusa.Price * Quantity;
}

public class Order
{
    public List<OrderItem> Items { get; set; } = new();
    public string DeliveryType { get; set; } = "Domicilio";
    public string CustomerName { get; set; }
    public string Phone { get; set; }
    public string Address { get; set; }
    public string PaymentMethod { get; set; } = "Efectivo";
    public decimal Subtotal => Items.Sum(i => i.Subtotal);
    public decimal Shipping => DeliveryType == "Domicilio" ? 1.00m : 0;
    public decimal Total => Subtotal + Shipping;
    public bool IsValid => Items.Any() &&
                          !string.IsNullOrEmpty(CustomerName) &&
                          !string.IsNullOrEmpty(Phone) &&
                          (DeliveryType != "Domicilio" || !string.IsNullOrEmpty(Address));
}

// ======================== RELAY COMMAND ========================

public class RelayCommand : ICommand
{
    private readonly Action _execute;
    private readonly Func<bool> _canExecute;

    public RelayCommand(Action execute, Func<bool> canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public event EventHandler CanExecuteChanged;

    public bool CanExecute(object parameter) => _canExecute?.Invoke() ?? true;

    public void Execute(object parameter) => _execute();
}

public class RelayCommand<T> : ICommand
{
    private readonly Action<T> _execute;
    private readonly Func<T, bool> _canExecute;

    public RelayCommand(Action<T> execute, Func<T, bool> canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public event EventHandler CanExecuteChanged;

    public bool CanExecute(object parameter) => _canExecute?.Invoke((T)parameter) ?? true;

    public void Execute(object parameter) => _execute((T)parameter);
}

// ======================== VIEWMODEL ========================

public class MainViewModel : INotifyPropertyChanged
{
    private string _selectedCategory = "Todas";
    private ObservableCollection<Pupusa> _filteredPupusas;
    private decimal _cartTotal;
    private int _totalItems;
    private bool _showCart = false;
    private Order _currentOrder = new Order();

    public ObservableCollection<Pupusa> AllPupusas { get; set; }
    public ObservableCollection<Pupusa> FilteredPupusas
    {
        get => _filteredPupusas;
        set { _filteredPupusas = value; OnPropertyChanged(); }
    }

    public List<string> Categories { get; } = new() { "Todas", "Tradicionales", "Especiales" };

    public string SelectedCategory
    {
        get => _selectedCategory;
        set
        {
            _selectedCategory = value;
            FilterPupusas();
            OnPropertyChanged();
        }
    }

    public decimal CartTotal
    {
        get => _cartTotal;
        set { _cartTotal = value; OnPropertyChanged(); }
    }

    public int TotalItems
    {
        get => _totalItems;
        set { _totalItems = value; OnPropertyChanged(); }
    }

    public bool ShowCart
    {
        get => _showCart;
        set { _showCart = value; OnPropertyChanged(); }
    }

    public Order CurrentOrder => _currentOrder;

    public string CustomerName
    {
        get => _currentOrder.CustomerName;
        set { _currentOrder.CustomerName = value; OnPropertyChanged(); }
    }

    public string Phone
    {
        get => _currentOrder.Phone;
        set { _currentOrder.Phone = value; OnPropertyChanged(); }
    }

    public string Address
    {
        get => _currentOrder.Address;
        set { _currentOrder.Address = value; OnPropertyChanged(); }
    }

    public string PaymentMethod
    {
        get => _currentOrder.PaymentMethod;
        set { _currentOrder.PaymentMethod = value; OnPropertyChanged(); }
    }

    public bool IsDelivery
    {
        get => _currentOrder.DeliveryType == "Domicilio";
        set
        {
            _currentOrder.DeliveryType = value ? "Domicilio" : "Retiro en local";
            OnPropertyChanged();
            OnPropertyChanged(nameof(Shipping));
            OnPropertyChanged(nameof(Total));
            OnPropertyChanged(nameof(IsAddressRequired));
            OnPropertyChanged(nameof(CartButtonText));
            UpdateCartTotal();
        }
    }

    public bool IsAddressRequired => IsDelivery;

    public decimal Subtotal => _currentOrder.Subtotal;
    public decimal Shipping => _currentOrder.Shipping;
    public decimal Total => _currentOrder.Total;

    public List<string> PaymentMethods { get; } = new() { "Efectivo", "Transferencia" };

    public string CartButtonText => $"🛒 Carrito · ${CartTotal:F2}";
    public string ConfirmButtonText => $"Confirmar pedido · ${Total:F2}";

    public ICommand IncreaseQuantityCommand { get; set; }
    public ICommand DecreaseQuantityCommand { get; set; }
    public ICommand ShowCartCommand { get; set; }
    public ICommand ConfirmOrderCommand { get; set; }
    public ICommand GoBackToMenuCommand { get; set; }

    public MainViewModel()
    {
        InitializePupusas();
        FilterPupusas();
        UpdateCartTotal();

        IncreaseQuantityCommand = new RelayCommand<Pupusa>(IncreaseQuantity);
        DecreaseQuantityCommand = new RelayCommand<Pupusa>(DecreaseQuantity);
        ShowCartCommand = new RelayCommand(ToggleCart);
        ConfirmOrderCommand = new RelayCommand(ConfirmOrder);
        GoBackToMenuCommand = new RelayCommand(GoBackToMenu);
    }

    private void InitializePupusas()
    {
        AllPupusas = new ObservableCollection<Pupusa>
        {
            new() { Id = 1, Name = "Revueltas", Description = "Frijol, queso y chicharrón",
                   Emoji = "🫓", Price = 0.75m, Category = "Tradicionales" },
            new() { Id = 2, Name = "Queso con loroco", Description = "Queso fresco y loroco",
                   Emoji = "🧀", Price = 0.80m, Category = "Especiales" },
            new() { Id = 3, Name = "Chicharrón", Description = "Chicharrón molido",
                   Emoji = "🌶️", Price = 0.75m, Category = "Tradicionales" },
            new() { Id = 4, Name = "Ayote", Description = "De temporada",
                   Emoji = "🌽", Price = 0.80m, Category = "Especiales" }
        };
    }

    private void FilterPupusas()
    {
        if (SelectedCategory == "Todas")
            FilteredPupusas = new ObservableCollection<Pupusa>(AllPupusas);
        else
            FilteredPupusas = new ObservableCollection<Pupusa>(
                AllPupusas.Where(p => p.Category == SelectedCategory));
    }

    private void IncreaseQuantity(Pupusa pupusa)
    {
        if (pupusa.Quantity < 20)
        {
            pupusa.Quantity++;
            UpdateCartTotal();
        }
    }

    private void DecreaseQuantity(Pupusa pupusa)
    {
        if (pupusa.Quantity > 0)
        {
            pupusa.Quantity--;
            UpdateCartTotal();
        }
    }

    private void UpdateCartTotal()
    {
        CartTotal = AllPupusas.Sum(p => p.Price * p.Quantity);
        TotalItems = AllPupusas.Sum(p => p.Quantity);

        _currentOrder.Items.Clear();
        foreach (var pupusa in AllPupusas.Where(p => p.Quantity > 0))
        {
            _currentOrder.Items.Add(new OrderItem { Pupusa = pupusa, Quantity = pupusa.Quantity });
        }

        OnPropertyChanged(nameof(Subtotal));
        OnPropertyChanged(nameof(Shipping));
        OnPropertyChanged(nameof(Total));
        OnPropertyChanged(nameof(CartButtonText));
        OnPropertyChanged(nameof(ConfirmButtonText));
    }

    private void ToggleCart()
    {
        ShowCart = !ShowCart;
        OnPropertyChanged(nameof(ShowCart));
    }

    private void GoBackToMenu()
    {
        ShowCart = false;
        OnPropertyChanged(nameof(ShowCart));
    }

    private async void ConfirmOrder()
    {
        if (!_currentOrder.Items.Any())
        {
            await ShowMessage("Error", "El carrito está vacío");
            return;
        }

        if (string.IsNullOrEmpty(CustomerName))
        {
            await ShowMessage("Error", "Por favor ingrese su nombre");
            return;
        }

        if (string.IsNullOrEmpty(Phone))
        {
            await ShowMessage("Error", "Por favor ingrese su teléfono");
            return;
        }

        if (IsDelivery && string.IsNullOrEmpty(Address))
        {
            await ShowMessage("Error", "Por favor ingrese su dirección para el envío a domicilio");
            return;
        }

        var orderNumber = $"ORD-{DateTime.Now:yyyyMMdd-HHmmss}";
        var message = $"¡Pedido confirmado!\n\nNúmero de orden: {orderNumber}\n";
        message += $"Total: ${Total:F2}\n";
        message += $"Método de pago: {PaymentMethod}\n";
        message += $"Tipo: {_currentOrder.DeliveryType}";

        await ShowMessage("Confirmación", message);

        foreach (var pupusa in AllPupusas)
        {
            pupusa.Quantity = 0;
        }

        CustomerName = string.Empty;
        Phone = string.Empty;
        Address = string.Empty;
        PaymentMethod = "Efectivo";
        IsDelivery = true;

        UpdateCartTotal();
        ShowCart = false;
        OnPropertyChanged(nameof(ShowCart));
    }

    private async Task ShowMessage(string title, string message)
    {
        try
        {
            var mainWindow = Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
                ? desktop.MainWindow as Window
                : null;

            if (mainWindow != null)
            {
                var dialog = new Window
                {
                    Title = title,
                    Width = 300,
                    Height = 150,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Content = new StackPanel
                    {
                        Spacing = 10,
                        Margin = new Avalonia.Thickness(20),
                        Children =
                        {
                            new TextBlock { Text = message, TextWrapping = Avalonia.Media.TextWrapping.Wrap },
                            new Button
                            {
                                Content = "OK",
                                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                                Width = 80,
                                Margin = new Avalonia.Thickness(0, 10, 0, 0)
                            }
                        }
                    }
                };

                var okButton = (Button)((StackPanel)dialog.Content).Children[1];
                okButton.Click += (s, e) => dialog.Close();

                await dialog.ShowDialog(mainWindow);
            }
        }
        catch (Exception)
        {
            Console.WriteLine($"{title}: {message}");
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;
    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

// ======================== MAIN WINDOW ========================

public partial class MainWindow : Window
{
    private MainViewModel _viewModel;

    public MainWindow()
    {
        InitializeComponent();
        _viewModel = new MainViewModel();
        DataContext = _viewModel;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}