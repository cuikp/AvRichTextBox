using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using AvRichTextBox;
using System;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace CustomControls;

public class NumericSpinnerViewModel
{

}

public partial class NumericSpinner : UserControl 
{
   public delegate void ValueChangedHandler(double value);
   public event ValueChangedHandler? ValueChanged;

   public delegate void UserValueChangedHandler(double value);
   public event UserValueChangedHandler? UserValueChanged;

   public NumericSpinner()
   {
      InitializeComponent();

      this.DataContext = this;

      Step = 1;

      DownKeyTimer.Tick += DownKeyTimer_Tick;
      DownKeyTimer.Interval = new TimeSpan(0, 0, 0, 0, 200);

     
      CmdUp.AddHandler(InputElement.PointerPressedEvent, CmdUp_PointerPressed, RoutingStrategies.Tunnel);
      CmdUp.AddHandler(InputElement.PointerReleasedEvent, CmdUp_PointerReleased, RoutingStrategies.Tunnel);
      CmdDown.AddHandler(InputElement.PointerPressedEvent, CmdDown_PointerPressed, RoutingStrategies.Tunnel);
      CmdDown.AddHandler(InputElement.PointerReleasedEvent, CmdDown_PointerReleased, RoutingStrategies.Tunnel);

      Value = 20;

   }

   private double _Step = 1;
   public double Step { get { return _Step; } set { _Step = value; } }

   private void CmdUp_Click(object? sender, RoutedEventArgs e) { Value += (ControlIsDown ? Step * 10 : Step); UserValueChanged?.Invoke(Value); }
   private void CmdDown_Click(object? sender, RoutedEventArgs e) { Value -= (ControlIsDown ? Step * 10 : Step); UserValueChanged?.Invoke(Value); }

   internal DispatcherTimer DownKeyTimer = new();

   bool ControlIsDown = false;
   bool DownKeyDown = false;

   private void DownKeyTimer_Tick(object? sender, EventArgs e) 
   { 
      int fac = DownKeyDown ? -1 : 1;  
      Value += (ControlIsDown ? Step * 10 : Step) * fac;
      UserValueChanged?.Invoke(Value);
   }
   
   private void CmdDown_PointerPressed(object? sender, PointerPressedEventArgs e) 
   { 
      DownKeyDown = true; 
      ControlIsDown = e.KeyModifiers.HasFlag(KeyModifiers.Control); 
      DownKeyTimer.Start();
   }
   
   private void CmdUp_PointerPressed(object? sender, PointerPressedEventArgs e) 
   { 
      ControlIsDown = e.KeyModifiers.HasFlag(KeyModifiers.Control); 
      DownKeyTimer.Start();
   }

   private void CmdDown_PointerReleased(object? sender, PointerReleasedEventArgs e) { DownKeyDown = false; DownKeyTimer.Stop();  }
   private void CmdUp_PointerReleased(object? sender, PointerReleasedEventArgs e) { DownKeyTimer.Stop(); ;  }

   public static readonly StyledProperty<ISolidColorBrush> TextBackgroundProperty = AvaloniaProperty.Register<NumericSpinner, ISolidColorBrush>(nameof(TextBackground));

   public SolidColorBrush TextBackground
   {
      get => (SolidColorBrush)GetValue(TextBackgroundProperty);
      set => SetValue(TextBackgroundProperty, value); 
   }

   public static readonly StyledProperty<Thickness> TextPaddingProperty = AvaloniaProperty.Register<NumericSpinner, Thickness>(nameof(TextPadding));

   public Thickness TextPadding
   {
      get => (Thickness)GetValue(TextPaddingProperty); 
      set => SetValue(TextPaddingProperty, value);
   }

   public static readonly StyledProperty<double> ValueProperty = AvaloniaProperty.Register<NumericSpinner, double>(nameof(Value), 80, defaultBindingMode: BindingMode.TwoWay);

   public double Value
   {
      get => (double)GetValue(ValueProperty);
      set
      {
         if (value < MinValue) value = MinValue;
         if (value > MaxValue) value = MaxValue;

         if (Value != value)
         {
            SetValue(ValueProperty, value);
            ValueChanged?.Invoke(value);
         }
         
      }
   }

   protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
   {
      base.OnPropertyChanged(change);

      //if (change.Property.Name == nameof(Value))
      //{
      //   double Val = (double)change.GetNewValue<double>();
      //   ValueChanged?.Invoke(Val);
      //}
      
   }

   public static readonly StyledProperty<double> MaxValueProperty = AvaloniaProperty.Register<NumericSpinner, double>(nameof(MaxValue));

   public double MaxValue
   {
      get { return (double)GetValue(MaxValueProperty); }
      set { SetValue(MaxValueProperty, value); }
   }

   private void MaxValuePropertyChanged(double Val) { MaxValue = Val; }

   public static readonly StyledProperty<double> MinValueProperty = AvaloniaProperty.Register<NumericSpinner, double>(nameof(MinValue));

   public double MinValue
   {
      get { return (double)GetValue(MinValueProperty); }
      set { SetValue(MinValueProperty, value); }
   }

   private void MinValuePropertyChanged(double Val) { MinValue = Val; }


}

public class DoubleToStringConverter : IValueConverter
{
   public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
   {
      return value?.ToString() ?? "";
   }

   public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
   {
      if (double.TryParse(value as string, out double result))
         return result;
      return 0; // Default fallback
   }
}
