using Lunar.OCR.Core.Models;
using Microsoft.Toolkit.Uwp.UI.Controls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.UI.Text;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml;
using System.Text.Json;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using System.Linq;

namespace Lunar.OCR.Views
{
    public sealed partial class MainPage : Windows.UI.Xaml.Controls.Page
    {
        private bool _isContentDialogOpening;
        private readonly List<Thumbnail> _thumbnails = new List<Thumbnail>();
        private string _azureComputerVisionSubscriptionKey;
        private string _azureComputerVisionEndpoint;

        public MainPage()
        {
            InitializeComponent();

            for (int i = 1; i <= 17; i++)
            {
                _thumbnails.Add(new Thumbnail() { ImageURI = $"/Images/{i}.png" });
            }

            LanguageSelection.SelectedIndex = 0;

            DataContext = this;
        }

        public List<Thumbnail> Thumbnails => _thumbnails;

        private async void ButtonProcessImage_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            var picker = new FileOpenPicker();
            picker.ViewMode = PickerViewMode.Thumbnail;
            picker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
            picker.FileTypeFilter.Add(".jpg");
            picker.FileTypeFilter.Add(".jpeg");
            picker.FileTypeFilter.Add(".png");

            StorageFile imageFile = await picker.PickSingleFileAsync();
            if (imageFile != null)
            {
                await AnalysisImageAsync(imageFile);
            }
            else
            {
                ContentDialog dialog = new ContentDialog()
                {
                    Title = "Operation Cancelled",
                    Content = "No file is chosen.",
                    CloseButtonText = "Ok"
                };

                await dialog.ShowAsync();
            }
        }

        private async void Carousel_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var carousel = (Carousel)sender;

            var selectedImageFile = await StorageFile.GetFileFromApplicationUriAsync(
                new Uri(this.BaseUri, ((Thumbnail)carousel.SelectedItem).ImageURI));

            await AnalysisImageAsync(selectedImageFile);
        }

        private async Task<BitmapImage> GetBitmapAsync(byte[] data)
        {
            var bitmapImage = new BitmapImage();

            using (var stream = new InMemoryRandomAccessStream())
            {
                using (var writer = new DataWriter(stream))
                {
                    writer.WriteBytes(data);
                    await writer.StoreAsync();
                    await writer.FlushAsync();
                    writer.DetachStream();
                }

                stream.Seek(0);
                await bitmapImage.SetSourceAsync(stream);
            }

            return bitmapImage;
        }

        private async Task AnalysisImageAsync(StorageFile imageFile)
        {
            if (_isContentDialogOpening)
            {
                return;
            }

            WelcomeTextSection.Visibility = Visibility.Collapsed;
            ImageAndTextSection.Visibility = Visibility.Visible;

            ImageProgressRing.Visibility = Visibility.Visible;
            ImageHolder.Visibility = Visibility.Collapsed;

            await ShowSelectedImageAsync(imageFile);

            if (string.IsNullOrWhiteSpace(_azureComputerVisionEndpoint) || string.IsNullOrWhiteSpace(_azureComputerVisionSubscriptionKey))
            {
                _isContentDialogOpening = true;

                var azureComputerVisionContentDialog = new AzureComputerVisionContentDialog();
                var result = await azureComputerVisionContentDialog.ShowAsync();
                if (result == ContentDialogResult.Primary &&
                    (!string.IsNullOrWhiteSpace(azureComputerVisionContentDialog.Endpoint) && !string.IsNullOrWhiteSpace(azureComputerVisionContentDialog.Key)))
                {
                    _azureComputerVisionEndpoint = azureComputerVisionContentDialog.Endpoint;
                    _azureComputerVisionSubscriptionKey = azureComputerVisionContentDialog.Key;

                    await AnalyseUsingAzureComputerVisionAsync(imageFile);
                }

                _isContentDialogOpening = false;
            }
            else
            {
                await AnalyseUsingAzureComputerVisionAsync(imageFile);
            }

            ImageProgressRing.Visibility = Visibility.Collapsed;
            ImageHolder.Visibility = Visibility.Visible;
        }

        private async Task AnalyseUsingAzureComputerVisionAsync(StorageFile imageFile)
        {
            try
            {
                var computerVisionClient = Authenticate(_azureComputerVisionEndpoint, _azureComputerVisionSubscriptionKey);

                using (Stream stream = await imageFile.OpenStreamForReadAsync())
                {
                    string language = "en";
                    if (LanguageSelection.SelectedIndex == 1) language = "zh-Hans";
                    if (LanguageSelection.SelectedIndex == 2) language = "ko";
                    var textHeaders = await computerVisionClient.ReadInStreamAsync(stream, language);

                    string operationLocation = textHeaders.OperationLocation;

                    // Retrieve the URI where the extracted text will be stored from the Operation-Location header.
                    // We only need the ID and not the full URL
                    const int numberOfCharsInOperationId = 36;
                    string operationId = operationLocation.Substring(operationLocation.Length - numberOfCharsInOperationId);

                    // Extract the text
                    ReadOperationResult results;
                    do
                    {
                        results = await computerVisionClient.GetReadResultAsync(Guid.Parse(operationId));
                    }
                    while ((results.Status == OperationStatusCodes.Running ||
                        results.Status == OperationStatusCodes.NotStarted));

                    string text = "=======================\r\nPLAIN TEXT\r\n=======================\r\n";

                    var textUrlFileResults = results.AnalyzeResult.ReadResults;


                    int wordCount = 0;
                    double confidenceSum = 0;
                    foreach (ReadResult page in textUrlFileResults)
                    {
                        foreach (Line line in page.Lines)
                        {
                            wordCount += line.Words.Count();
                            confidenceSum += line.Words.Sum(w => w.Confidence);
                            text += line.Text + "\r\n";
                        }
                    }

                    text += "=======================\r\nJSON REPRESENTATION\r\n=======================\r\n";

                    text += JsonSerializer.Serialize(textUrlFileResults);

                    OutputTextTitle.Text = string.Format("Output Text (Average Confidence: {0})", (confidenceSum / wordCount).ToString("0.00"));

                    OutputText.Document.SetText(TextSetOptions.FormatRtf, text);
                }
            }
            catch (Exception ex)
            {
                await ShowExceptionMessageAsync(ex);
            }
        }

        private async Task ShowSelectedImageAsync(StorageFile imageFile)
        {
            try
            {
                using (Stream stream = await imageFile.OpenStreamForReadAsync())
                {
                    byte[] result;
                    using (var memoryStream = new MemoryStream())
                    {
                        stream.CopyTo(memoryStream);
                        result = memoryStream.ToArray();
                    }
                    ImageHolder.Source = await GetBitmapAsync(result);
                }
            }
            catch (Exception ex)
            {
                await ShowExceptionMessageAsync(ex);
            }
        }

        private ComputerVisionClient Authenticate(string endpoint, string key)
        {
            ComputerVisionClient client =
              new ComputerVisionClient(new ApiKeyServiceClientCredentials(key))
              { Endpoint = endpoint };
            return client;
        }

        private async Task ShowExceptionMessageAsync(Exception ex)
        {
            ContentDialog exceptionDialog = new ContentDialog()
            {
                Title = "Unexpected Error: " + ex.Message,
                Content = ex.ToString(),
                CloseButtonText = "Ok"
            };

            await exceptionDialog.ShowAsync();
        }
    }
}
