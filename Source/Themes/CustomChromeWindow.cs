using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace UI
{
    [TemplatePart(Name = "PART_Close", Type = typeof(Button))]
    [TemplatePart(Name = "PART_Minimize", Type = typeof(Button))]
    [TemplatePart(Name = "PART_Maximize", Type = typeof(Button))]
	public class CustomChromeWindow : Window
	{
		public static readonly DependencyProperty HoverImageProperty = DependencyProperty.RegisterAttached("HoverImage",
																										   typeof(string),
																										   typeof(
																											CustomChromeWindow
																											));
		public static readonly DependencyProperty NormalImageProperty = DependencyProperty.RegisterAttached("NormalImage",
																										   typeof(string),
																										   typeof(
																											CustomChromeWindow
																											));
		public static readonly DependencyProperty PressedImageProperty = DependencyProperty.RegisterAttached("PressedImage",
																										   typeof(string),
																										   typeof(
																											CustomChromeWindow
																											));
		static CustomChromeWindow()
		{
            DefaultStyleKeyProperty.OverrideMetadata(typeof(CustomChromeWindow),
                                                     new FrameworkPropertyMetadata(
                                                        typeof(CustomChromeWindow)));
		}

		public static void SetHoverImage(DependencyObject obj, string source)
		{
			obj.SetValue(HoverImageProperty, source);
		}

		public static void SetNormalImage(DependencyObject obj, string source)
		{
			obj.SetValue(NormalImageProperty, source);
		}

		public static void SetPressedImage(DependencyObject obj, string source)
		{
			obj.SetValue(PressedImageProperty, source);
		}

		public static string GetHoverImage(DependencyObject obj)
		{
			return (string)obj.GetValue(HoverImageProperty);
		}

		public static string GetNormalImage(DependencyObject obj)
		{
			return (string)obj.GetValue(NormalImageProperty);
		}

		public static string GetPressedImage(DependencyObject obj)
		{
			return (string)obj.GetValue(PressedImageProperty);
		}

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            AttachToVisualTree();
        }

		private void AttachToVisualTree()
		{
			// Close Button
			Button closeButton = GetChildControl<Button>("PART_Close");
			if (closeButton != null)
			{
				closeButton.Click += OnCloseButtonClick;
			}

			// Minimize Button
			Button minimizeButton = GetChildControl<Button>("PART_Minimize");
			if (minimizeButton != null)
			{
				minimizeButton.Click += OnMinimizeButtonClick;
			}

			// Maximize Button
			Button maximizeButton = GetChildControl<Button>("PART_Maximize");
			if (maximizeButton != null)
			{
				maximizeButton.Click += OnMaximizeButtonClick;
			}

			// Title Bar
			Panel titleBar = GetChildControl<Panel>("PART_TitleBar");
			if (titleBar != null)
			{
				titleBar.MouseLeftButtonDown += OnTitleBarMouseDown;
			}
		}

		private void OnMaximizeButtonClick(object sender, RoutedEventArgs e)
		{
			ToggleMaximize();
		}

		private void ToggleMaximize()
		{
			this.WindowState = (this.WindowState == WindowState.Maximized)
								?
									WindowState.Normal
								: WindowState.Maximized;
		}

		private void OnTitleBarMouseDown(object sender, MouseButtonEventArgs e)
		{
			if (ResizeMode != ResizeMode.NoResize && e.ClickCount == 2)
			{
				ToggleMaximize();
				return;
			}
			this.DragMove();
		}

		protected T GetChildControl<T>(string ctrlName) where T : DependencyObject
		{
			T ctrl = GetTemplateChild(ctrlName) as T;
			return ctrl;
		}

		private void OnCloseButtonClick(object sender, RoutedEventArgs e)
		{
			Close();
		}

		private void OnMinimizeButtonClick(object sender, RoutedEventArgs e)
		{
			WindowState = WindowState.Minimized;
		}


	}
}