using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
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

// AGREGAR ESTE CONVERTER
public class NotNullConverter : Avalonia.Data.Converters.IValueConverter
{
    public static readonly NotNullConverter Instance = new();

    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        if (value == null)
            return false;

        if (value is string str)
            return !string.IsNullOrEmpty(str);

        return true;
    }

    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
// ======================== MODELOS ========================

public class Pupusa : INotifyPropertyChanged
{
    private int _quantity;
    private string _name;
    private string _description;
    private string _emoji;
    private decimal _price;
    private string _category;

    public int Id { get; set; }

    public string Name
    {
        get => _name;
        set { _name = value; OnPropertyChanged(); }
    }

    public string Description
    {
        get => _description;
        set { _description = value; OnPropertyChanged(); }
    }

    public string Emoji
    {
        get => _emoji;
        set { _emoji = value; OnPropertyChanged(); }
    }

    public decimal Price
    {
        get => _price;
        set { _price = value; OnPropertyChanged(); }
    }

    public string Category
    {
        get => _category;
        set { _category = value; OnPropertyChanged(); }
    }

    public int Quantity
    {
        get => _quantity;
        set
        {
            if (_quantity != value)
            {
                _quantity = Math.Max(0, Math.Min(20, value));
                OnPropertyChanged();
            }
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;
    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public class OrderItem : INotifyPropertyChanged
{
    private Pupusa _pupusa;
    private int _quantity;

    public Pupusa Pupusa
    {
        get => _pupusa;
        set
        {
            _pupusa = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(Subtotal));
        }
    }

    public int Quantity
    {
        get => _quantity;
        set
        {
            if (_quantity != value)
            {
                _quantity = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(Subtotal));
            }
        }
    }

    public decimal Subtotal => Pupusa?.Price * Quantity ?? 0;

    public event PropertyChangedEventHandler PropertyChanged;
    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public class Order : INotifyPropertyChanged
{
    private ObservableCollection<OrderItem> _items = new();
    private string _deliveryType = "Domicilio";
    private string _customerName;
    private string _phone;
    private string _address;
    private string _paymentMethod = "Efectivo";

    public ObservableCollection<OrderItem> Items
    {
        get => _items;
        set
        {
            _items = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(Subtotal));
            OnPropertyChanged(nameof(Total));
            OnPropertyChanged(nameof(IsValid));
        }
    }

    public string DeliveryType
    {
        get => _deliveryType;
        set
        {
            _deliveryType = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(Shipping));
            OnPropertyChanged(nameof(Total));
        }
    }

    public string CustomerName
    {
        get => _customerName;
        set { _customerName = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsValid)); }
    }

    public string Phone
    {
        get => _phone;
        set { _phone = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsValid)); }
    }

    public string Address
    {
        get => _address;
        set { _address = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsValid)); }
    }

    public string PaymentMethod
    {
        get => _paymentMethod;
        set { _paymentMethod = value; OnPropertyChanged(); }
    }

    public decimal Subtotal => Items.Sum(i => i.Subtotal);
    public decimal Shipping => DeliveryType == "Domicilio" ? 1.00m : 0;
    public decimal Total => Subtotal + Shipping;

    public bool IsValid => Items.Any() &&
                          !string.IsNullOrEmpty(CustomerName) &&
                          !string.IsNullOrEmpty(Phone) &&
                          (DeliveryType != "Domicilio" || !string.IsNullOrEmpty(Address));

    public event PropertyChangedEventHandler PropertyChanged;
    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
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

    public void RaiseCanExecuteChanged()
    {
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
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

    public void RaiseCanExecuteChanged()
    {
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}

// ======================== VALIDATION HELPERS ========================

public static class ValidationHelper
{
    // Expresión regular para validar que el nombre solo contenga letras, espacios, apóstrofes y guiones
    private static readonly Regex NameRegex = new Regex(@"^[a-zA-ZáéíóúÁÉÍÓÚñÑ\s\-']+$");

    public static bool IsValidName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return false;

        if (name.Length < 2 || name.Length > 50)
            return false;

        // Verificar que no contenga números
        if (name.Any(char.IsDigit))
            return false;

        // Verificar que solo contenga caracteres permitidos
        return NameRegex.IsMatch(name.Trim());
    }

    public static bool IsValidPhone(string phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
            return false;

        var phoneRegex = new Regex(@"^\+?[\d\s\-\(\)]{7,15}$");
        return phoneRegex.IsMatch(phone.Trim());
    }

    public static bool IsValidAddress(string address)
    {
        return !string.IsNullOrWhiteSpace(address) && address.Length >= 5;
    }

    public static string GetNameError(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "El nombre es obligatorio";
        if (name.Length < 2)
            return "El nombre debe tener al menos 2 caracteres";
        if (name.Length > 50)
            return "El nombre no puede exceder los 50 caracteres";
        if (name.Any(char.IsDigit))
            return "El nombre no puede contener números";
        if (!NameRegex.IsMatch(name.Trim()))
            return "El nombre solo puede contener letras, espacios, apóstrofes y guiones";
        return null;
    }

    public static string GetPhoneError(string phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
            return "El teléfono es obligatorio";
        if (!IsValidPhone(phone))
            return "Ingrese un número de teléfono válido (ej: 1234-5678)";
        return null;
    }

    public static string GetAddressError(string address, bool isRequired)
    {
        if (!isRequired)
            return null;
        if (string.IsNullOrWhiteSpace(address))
            return "La dirección es obligatoria para envío a domicilio";
        if (address.Length < 5)
            return "La dirección debe ser más específica (mínimo 5 caracteres)";
        return null;
    }
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
    private bool _isUpdating = false;
    private string _nameError;
    private string _phoneError;
    private string _addressError;
    private bool _hasErrors;
    private bool _canConfirm;



    public ObservableCollection<Pupusa> AllPupusas { get; set; }

    public ObservableCollection<Pupusa> FilteredPupusas
    {
        get => _filteredPupusas;
        set
        {
            _filteredPupusas = value;
            OnPropertyChanged();
        }
    }


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
        set
        {
            _currentOrder.CustomerName = value;
            OnPropertyChanged();
            ValidateName();
        }
    }

    public string Phone
    {
        get => _currentOrder.Phone;
        set
        {
            _currentOrder.Phone = value;
            OnPropertyChanged();
            ValidatePhone();
        }
    }

    public string Address
    {
        get => _currentOrder.Address;
        set
        {
            _currentOrder.Address = value;
            OnPropertyChanged();
            ValidateAddress();
        }
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
            ValidateAddress();
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

    // Propiedades de validación
    public string NameError
    {
        get => _nameError;
        private set { _nameError = value; OnPropertyChanged(); }
    }

    public string PhoneError
    {
        get => _phoneError;
        private set { _phoneError = value; OnPropertyChanged(); }
    }

    public string AddressError
    {
        get => _addressError;
        private set { _addressError = value; OnPropertyChanged(); }
    }

    public bool HasErrors
    {
        get => _hasErrors;
        private set { _hasErrors = value; OnPropertyChanged(); }
    }

    public bool CanConfirm
    {
        get => _canConfirm;
        private set
        {
            _canConfirm = value;
            OnPropertyChanged();
            // Forzar actualización del comando
            (ConfirmOrderCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }
    }

    public ICommand IncreaseQuantityCommand { get; set; }
    public ICommand DecreaseQuantityCommand { get; set; }
    public ICommand ShowCartCommand { get; set; }
    public ICommand ConfirmOrderCommand { get; set; }
    public ICommand GoBackToMenuCommand { get; set; }
    public ICommand ClearErrorsCommand { get; set; }

    public MainViewModel()
    {
        InitializePupusas();
        FilterPupusas();
        UpdateCartTotal();

        IncreaseQuantityCommand = new RelayCommand<Pupusa>(IncreaseQuantity);
        DecreaseQuantityCommand = new RelayCommand<Pupusa>(DecreaseQuantity);
        ShowCartCommand = new RelayCommand(ToggleCart);
        ConfirmOrderCommand = new RelayCommand(ConfirmOrder, () => CanConfirm);
        GoBackToMenuCommand = new RelayCommand(GoBackToMenu);
        ClearErrorsCommand = new RelayCommand(ClearErrors);

        // Suscribirse a cambios en las pupusas una sola vez
        foreach (var pupusa in AllPupusas)
        {
            pupusa.PropertyChanged += OnPupusaPropertyChanged;
        }

        // Inicializar validaciones
        ValidateAll();
    }

    private void OnPupusaPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Pupusa.Quantity) && !_isUpdating)
        {
            UpdateCartTotal();
        }
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
        IEnumerable<Pupusa> filtered;
        if (SelectedCategory == "Todas")
            filtered = AllPupusas;
        else
            filtered = AllPupusas.Where(p => p.Category == SelectedCategory);

        FilteredPupusas = new ObservableCollection<Pupusa>(filtered);
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
        _isUpdating = true;
        try
        {
            CartTotal = AllPupusas.Sum(p => p.Price * p.Quantity);
            TotalItems = AllPupusas.Sum(p => p.Quantity);

            // Actualizar items del carrito
            _currentOrder.Items.Clear();
            foreach (var pupusa in AllPupusas.Where(p => p.Quantity > 0))
            {
                _currentOrder.Items.Add(new OrderItem { Pupusa = pupusa, Quantity = pupusa.Quantity });
            }

            // Notificar todos los cambios
            OnPropertyChanged(nameof(Subtotal));
            OnPropertyChanged(nameof(Shipping));
            OnPropertyChanged(nameof(Total));
            OnPropertyChanged(nameof(CartButtonText));
            OnPropertyChanged(nameof(ConfirmButtonText));
            OnPropertyChanged(nameof(CurrentOrder));

            // Actualizar estado del botón
            UpdateCanConfirm();
        }
        finally
        {
            _isUpdating = false;
        }
    }

    private void ToggleCart()
    {
        ShowCart = !ShowCart;
        if (ShowCart)
        {
            ValidateAll();
        }
    }

    private void GoBackToMenu()
    {
        ShowCart = false;
    }

    // ======================== MÉTODOS DE VALIDACIÓN ========================

    private void ValidateName()
    {
        NameError = ValidationHelper.GetNameError(CustomerName);
        UpdateCanConfirm();
    }

    private void ValidatePhone()
    {
        PhoneError = ValidationHelper.GetPhoneError(Phone);
        UpdateCanConfirm();
    }

    private void ValidateAddress()
    {
        AddressError = ValidationHelper.GetAddressError(Address, IsAddressRequired);
        UpdateCanConfirm();
    }

    private void ValidateAll()
    {
        ValidateName();
        ValidatePhone();
        ValidateAddress();
        UpdateCanConfirm();
    }

    private void ClearErrors()
    {
        NameError = null;
        PhoneError = null;
        AddressError = null;
        UpdateCanConfirm();
    }

    private void UpdateCanConfirm()
    {
        // Verificar si hay items en el carrito
        bool hasItems = _currentOrder.Items.Any();

        // Verificar si hay errores de validación
        bool hasValidationErrors = !string.IsNullOrEmpty(NameError) ||
                                  !string.IsNullOrEmpty(PhoneError) ||
                                  !string.IsNullOrEmpty(AddressError);

        // Verificar campos requeridos no vacíos
        bool hasRequiredFields = !string.IsNullOrEmpty(CustomerName) &&
                                !string.IsNullOrEmpty(Phone) &&
                                (!IsAddressRequired || !string.IsNullOrEmpty(Address));

        CanConfirm = hasItems && !hasValidationErrors && hasRequiredFields;
        HasErrors = hasValidationErrors || !hasRequiredFields;

        OnPropertyChanged(nameof(ConfirmButtonText));
    }

    // ======================== CONFIRMAR PEDIDO ========================

    private async void ConfirmOrder()
    {
        if (!CanConfirm)
            return;

        if (!_currentOrder.Items.Any())
        {
            await ShowMessage("Error", "El carrito está vacío");
            return;
        }

        // Validaciones adicionales antes de confirmar
        if (!ValidationHelper.IsValidName(CustomerName))
        {
            await ShowMessage("Error", ValidationHelper.GetNameError(CustomerName));
            return;
        }

        if (!ValidationHelper.IsValidPhone(Phone))
        {
            await ShowMessage("Error", ValidationHelper.GetPhoneError(Phone));
            return;
        }

        if (IsDelivery && !ValidationHelper.IsValidAddress(Address))
        {
            await ShowMessage("Error", ValidationHelper.GetAddressError(Address, true));
            return;
        }

        // Mostrar resumen final antes de confirmar
        var summaryMessage = $"📋 Resumen del pedido\n\n";
        summaryMessage += $"Items: {TotalItems} pupusas\n";
        foreach (var item in _currentOrder.Items)
        {
            summaryMessage += $"  • {item.Quantity}x {item.Pupusa.Name} - ${item.Subtotal:F2}\n";
        }
        summaryMessage += $"\nSubtotal: ${Subtotal:F2}\n";
        summaryMessage += $"Envío: ${Shipping:F2}\n";
        summaryMessage += $"Total: ${Total:F2}\n\n";
        summaryMessage += $"Cliente: {CustomerName}\n";
        summaryMessage += $"Teléfono: {Phone}\n";
        summaryMessage += $"Tipo: {_currentOrder.DeliveryType}\n";
        if (IsDelivery)
            summaryMessage += $"Dirección: {Address}\n";
        summaryMessage += $"Pago: {PaymentMethod}";

        var confirmResult = await ShowConfirmationDialog("Confirmar pedido", summaryMessage);
        if (!confirmResult)
            return;

        var orderNumber = $"ORD-{DateTime.Now:yyyyMMdd-HHmmss}";
        var message = $"✅ ¡Pedido confirmado!\n\nNúmero de orden: {orderNumber}\n";
        message += $"Total: ${Total:F2}\n";
        message += $"Método de pago: {PaymentMethod}\n";
        message += $"Tipo: {_currentOrder.DeliveryType}";

        await ShowMessage("Confirmación", message);

        // Resetear cantidades
        foreach (var pupusa in AllPupusas)
        {
            pupusa.Quantity = 0;
        }

        // Limpiar formulario
        CustomerName = string.Empty;
        Phone = string.Empty;
        Address = string.Empty;
        PaymentMethod = "Efectivo";
        IsDelivery = true;

        UpdateCartTotal();
        ValidateAll();
        ShowCart = false;
    }

    private async Task<bool> ShowConfirmationDialog(string title, string message)
    {
        try
        {
            var mainWindow = Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
                ? desktop.MainWindow as Window
                : null;

            if (mainWindow != null)
            {
                var result = false;
                var dialog = new Window
                {
                    Title = title,
                    Width = 400,
                    Height = 350,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Content = new StackPanel
                    {
                        Spacing = 10,
                        Margin = new Avalonia.Thickness(20),
                        Children =
                        {
                            new ScrollViewer
                            {
                                Content = new TextBlock
                                {
                                    Text = message,
                                    TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                                    MaxHeight = 200
                                },
                                MaxHeight = 200
                            },
                            new StackPanel
                            {
                                Orientation = Avalonia.Layout.Orientation.Horizontal,
                                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                                Spacing = 10,
                                Children =
                                {
                                    new Button
                                    {
                                        Content = "✓ Confirmar",
                                        Width = 100,
                                        Background = Avalonia.Media.Brushes.Green,
                                        Foreground = Avalonia.Media.Brushes.White,
                                        Margin = new Avalonia.Thickness(0, 10, 0, 0)
                                    },
                                    new Button
                                    {
                                        Content = "✕ Cancelar",
                                        Width = 100,
                                        Background = Avalonia.Media.Brushes.Red,
                                        Foreground = Avalonia.Media.Brushes.White,
                                        Margin = new Avalonia.Thickness(0, 10, 0, 0)
                                    }
                                }
                            }
                        }
                    }
                };

                var confirmButton = (Button)((StackPanel)((StackPanel)dialog.Content).Children[1]).Children[0];
                var cancelButton = (Button)((StackPanel)((StackPanel)dialog.Content).Children[1]).Children[1];

                confirmButton.Click += (s, e) => { result = true; dialog.Close(); };
                cancelButton.Click += (s, e) => { result = false; dialog.Close(); };

                await dialog.ShowDialog(mainWindow);
                return result;
            }
        }
        catch (Exception)
        {
            await ShowMessage(title, message);
            return true;
        }
        return false;
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
                    Width = 350,
                    Height = 200,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Content = new StackPanel
                    {
                        Spacing = 10,
                        Margin = new Avalonia.Thickness(20),
                        Children =
                        {
                            new ScrollViewer
                            {
                                Content = new TextBlock
                                {
                                    Text = message,
                                    TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                                    MaxHeight = 100
                                },
                                MaxHeight = 100
                            },
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