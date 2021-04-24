using Lunar.OCR.Core.Models;
using Microsoft.Toolkit.Uwp.UI.Controls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Tesseract;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.UI.Text;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml;
using Lunar.OCR.Models;
using System.Text.Json;

namespace Lunar.OCR.Views
{
    public sealed partial class MainPage : Windows.UI.Xaml.Controls.Page
    {
        private readonly List<Thumbnail> _thumbnails = new List<Thumbnail>();

        public MainPage()
        {
            InitializeComponent();

            for (int i = 1; i <= 17; i++)
            {
                _thumbnails.Add(new Thumbnail() { ImageURI = $"/Images/{i}.png" });
            }

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
            WelcomeTextSection.Visibility = Visibility.Collapsed;
            ImageAndTextSection.Visibility = Visibility.Visible;

            ImageProgressRing.Visibility = Visibility.Visible;
            ImageHolder.Visibility = Visibility.Collapsed;

            try
            {
                byte[] result;
                using (Stream stream = await imageFile.OpenStreamForReadAsync())
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        stream.CopyTo(memoryStream);
                        result = memoryStream.ToArray();
                    }
                }

                ImageHolder.Source = await GetBitmapAsync(result);

                string language = "eng";
                if (LanguageSelection.SelectedIndex == 1) language = "chi_sim";
                if (LanguageSelection.SelectedIndex == 2) language = "kor";

                using (var engine = new TesseractEngine(@"./TrainedData", language, EngineMode.Default))
                {
                    using (var img = Pix.LoadFromMemory(result))
                    {
                        using (var page = engine.Process(img))
                        {
                            var text = "=======================\r\nPLAIN TEXT\r\n=======================\r\n";

                            text += page.GetText();

                            OutputTextTitle.Text = string.Format("Output Text (Mean Confidence: {0})", page.GetMeanConfidence());

                            var recognisedTexts = new List<RecognisedText>();

                            var pgLevel = PageIteratorLevel.Word;

                            using (var iter = page.GetIterator())
                            {
                                do
                                {
                                    if (iter.TryGetBoundingBox(pgLevel, out Rect boundary))
                                    {
                                        var reading = iter.GetText(pgLevel);

                                        recognisedTexts.Add(new RecognisedText
                                        {
                                            Text = reading,
                                            Boundary = boundary
                                        });
                                    }
                                } while (iter.Next(pgLevel));
                            }

                            text += "=======================\r\nJSON REPRESENTATION\r\n=======================\r\n";

                            text += JsonSerializer.Serialize(recognisedTexts);

                            OutputText.Document.SetText(TextSetOptions.FormatRtf, text);


                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ContentDialog exceptionDialog = new ContentDialog()
                {
                    Title = "Unexpected Error: " + ex.Message,
                    Content = ex.ToString(),
                    CloseButtonText = "Ok"
                };

                await exceptionDialog.ShowAsync();
            }

            ImageProgressRing.Visibility = Visibility.Collapsed;
            ImageHolder.Visibility = Visibility.Visible;
        }
    }
}
