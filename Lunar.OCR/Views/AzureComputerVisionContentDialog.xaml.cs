using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Content Dialog item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Lunar.OCR.Views
{
    public sealed partial class AzureComputerVisionContentDialog : ContentDialog
    {
        public static readonly DependencyProperty EndpointProperty = DependencyProperty.Register(
            "Endpoint", typeof(string), typeof(AzureComputerVisionContentDialog), new PropertyMetadata(default(string)));

        public static readonly DependencyProperty KeyProperty = DependencyProperty.Register(
            "Key", typeof(string), typeof(AzureComputerVisionContentDialog), new PropertyMetadata(default(string)));

        public AzureComputerVisionContentDialog()
        {
            this.InitializeComponent();
        }

        public string Endpoint
        {
            get { return (string)GetValue(EndpointProperty); }
            set { SetValue(EndpointProperty, value); }
        }

        public string Key
        {
            get { return (string)GetValue(KeyProperty); }
            set { SetValue(KeyProperty, value); }
        }

        private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
        }

        private void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
        }
    }
}
