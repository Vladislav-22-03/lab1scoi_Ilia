using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Input;

namespace lab1scoi
{
    public partial class MainWindow : Window
    {
        public ObservableCollection<ImageItem> Images { get; } = new ObservableCollection<ImageItem>();
        private List<Point> curvePoints = new List<Point>();
        private Polyline curveLine;

        public MainWindow()
        {
            InitializeComponent();
            ImagesList.ItemsSource = Images;

            curvePoints.Add(new Point(0, 255));
            curvePoints.Add(new Point(255, 0));
            SetupCurveCanvas();
        }

        private void SetupCurveCanvas()
        {
            var canvas = CurveCanvas;
            if (canvas == null) return;

            canvas.Children.Clear();

            for (int i = 0; i <= 255; i += 32)
            {
                canvas.Children.Add(new Line
                {
                    X1 = i,
                    Y1 = 0,
                    X2 = i,
                    Y2 = 255,
                    Stroke = Brushes.LightGray,
                    StrokeThickness = 0.5
                });

                canvas.Children.Add(new Line
                {
                    X1 = 0,
                    Y1 = i,
                    X2 = 255,
                    Y2 = i,
                    Stroke = Brushes.LightGray,
                    StrokeThickness = 0.5
                });
            }

            canvas.Children.Add(new Line
            {
                X1 = 0,
                Y1 = 255,
                X2 = 255,
                Y2 = 255,
                Stroke = Brushes.Black,
                StrokeThickness = 1
            });

            canvas.Children.Add(new Line
            {
                X1 = 0,
                Y1 = 255,
                X2 = 0,
                Y2 = 0,
                Stroke = Brushes.Black,
                StrokeThickness = 1
            });

            curveLine = new Polyline
            {
                Stroke = Brushes.Blue,
                StrokeThickness = 2,
                Points = new PointCollection(curvePoints.Select(p => new Point(p.X, 255 - p.Y)))
            };
            canvas.Children.Add(curveLine);

            canvas.MouseDown += CurveCanvas_MouseDown;
            canvas.MouseMove += CurveCanvas_MouseMove;
            canvas.MouseUp += CurveCanvas_MouseUp;
        }

        private bool isDragging = false;
        private int draggedPointIndex = -1;

        private void CurveCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var canvas = sender as Canvas;
            if (canvas == null) return;

            Point mousePos = e.GetPosition(canvas);
            mousePos.Y = Math.Max(0, Math.Min(255, mousePos.Y));
            mousePos.X = Math.Max(0, Math.Min(255, mousePos.X));

            for (int i = 0; i < curvePoints.Count; i++)
            {
                if (Math.Abs(curvePoints[i].X - mousePos.X) < 10 &&
                    Math.Abs(255 - curvePoints[i].Y - mousePos.Y) < 10)
                {
                    isDragging = true;
                    draggedPointIndex = i;
                    return;
                }
            }

            if (!curvePoints.Any(p => Math.Abs(p.X - mousePos.X) < 20))
            {
                curvePoints.Add(new Point(mousePos.X, 255 - mousePos.Y));
                curvePoints = curvePoints.OrderBy(p => p.X).ToList();
                UpdateCurve();
                UpdateResultImage();
            }
        }

        private void CurveCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (!isDragging || draggedPointIndex < 0) return;

            var canvas = sender as Canvas;
            if (canvas == null) return;

            Point mousePos = e.GetPosition(canvas);
            mousePos.Y = Math.Max(0, Math.Min(255, mousePos.Y));
            mousePos.X = Math.Max(0, Math.Min(255, mousePos.X));

            if (draggedPointIndex == 0)
            {
                curvePoints[draggedPointIndex] = new Point(0, 255 - mousePos.Y);
            }
            else if (draggedPointIndex == curvePoints.Count - 1)
            {
                curvePoints[draggedPointIndex] = new Point(255, 255 - mousePos.Y);
            }
            else
            {
                double minX = curvePoints[draggedPointIndex - 1].X + 1;
                double maxX = curvePoints[draggedPointIndex + 1].X - 1;
                mousePos.X = Math.Max(minX, Math.Min(maxX, mousePos.X));
                curvePoints[draggedPointIndex] = new Point(mousePos.X, 255 - mousePos.Y);
            }

            UpdateCurve();
            UpdateResultImage();
        }

        private void CurveCanvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            isDragging = false;
            draggedPointIndex = -1;
        }

        private void UpdateCurve()
        {
            if (curveLine == null) return;
            curveLine.Points = new PointCollection(curvePoints.Select(p => new Point(p.X, 255 - p.Y)));
        }

        private byte ApplyCurve(byte value)
        {
            if (curvePoints.Count == 0) return value;

            for (int i = 0; i < curvePoints.Count - 1; i++)
            {
                if (value >= curvePoints[i].X && value <= curvePoints[i + 1].X)
                {
                    double t = (value - curvePoints[i].X) / (curvePoints[i + 1].X - curvePoints[i].X);
                    double interpolatedY = curvePoints[i].Y + t * (curvePoints[i + 1].Y - curvePoints[i].Y);
                    return (byte)Math.Max(0, Math.Min(255, interpolatedY));
                }
            }
            return value;
        }

        private void ResetCurve_Click(object sender, RoutedEventArgs e)
        {
            curvePoints.Clear();
            curvePoints.Add(new Point(0, 255));
            curvePoints.Add(new Point(255, 0));
            UpdateCurve();
            UpdateResultImage();
        }

        private void CurveCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            UpdateResultImage();
        }

        private void UpdateHistogram(WriteableBitmap image)
        {
            var canvas = HistogramCanvas;
            if (canvas == null || image == null) return;

            canvas.Children.Clear();

            int width = image.PixelWidth;
            int height = image.PixelHeight;
            int stride = width * 4;
            byte[] pixels = new byte[height * stride];
            image.CopyPixels(pixels, stride, 0);

            int[] redHist = new int[256];
            int[] greenHist = new int[256];
            int[] blueHist = new int[256];

            for (int i = 0; i < pixels.Length; i += 4)
            {
                blueHist[pixels[i]]++;
                greenHist[pixels[i + 1]]++;
                redHist[pixels[i + 2]]++;
            }

            int maxOverall = Math.Max(redHist.Max(), Math.Max(greenHist.Max(), blueHist.Max()));
            double canvasWidth = canvas.ActualWidth;
            double canvasHeight = canvas.ActualHeight;

            for (int i = 0; i < 256; i++)
            {
                double redHeight = redHist[i] / (double)maxOverall * canvasHeight;
                canvas.Children.Add(new Rectangle
                {
                    Width = canvasWidth / 256,
                    Height = redHeight,
                    Fill = new SolidColorBrush(Color.FromArgb(128, 255, 0, 0)),
                    Margin = new Thickness(i * canvasWidth / 256, canvasHeight - redHeight, 0, 0)
                });

                double greenHeight = greenHist[i] / (double)maxOverall * canvasHeight;
                canvas.Children.Add(new Rectangle
                {
                    Width = canvasWidth / 256,
                    Height = greenHeight,
                    Fill = new SolidColorBrush(Color.FromArgb(128, 0, 255, 0)),
                    Margin = new Thickness(i * canvasWidth / 256, canvasHeight - greenHeight, 0, 0)
                });

                double blueHeight = blueHist[i] / (double)maxOverall * canvasHeight;
                canvas.Children.Add(new Rectangle
                {
                    Width = canvasWidth / 256,
                    Height = blueHeight,
                    Fill = new SolidColorBrush(Color.FromArgb(128, 0, 0, 255)),
                    Margin = new Thickness(i * canvasWidth / 256, canvasHeight - blueHeight, 0, 0)
                });
            }
        }

        private WriteableBitmap ApplyCurveToImage(WriteableBitmap image)
        {
            if (image == null) return null;

            WriteableBitmap result = new WriteableBitmap(image);
            int width = result.PixelWidth;
            int height = result.PixelHeight;
            int stride = width * 4;
            byte[] pixels = new byte[height * stride];
            result.CopyPixels(pixels, stride, 0);

            for (int i = 0; i < pixels.Length; i += 4)
            {
                pixels[i] = ApplyCurve(pixels[i]);
                pixels[i + 1] = ApplyCurve(pixels[i + 1]);
                pixels[i + 2] = ApplyCurve(pixels[i + 2]);
            }

            result.WritePixels(new Int32Rect(0, 0, width, height), pixels, stride, 0);
            return result;
        }

        private WriteableBitmap ApplyBlendMode(WriteableBitmap baseImage, BitmapImage overlayImage,
            string blendMode, double opacity, bool useRed, bool useGreen, bool useBlue)
        {
            if (baseImage == null || overlayImage == null) return baseImage;

            WriteableBitmap overlay = new WriteableBitmap(overlayImage);
            if (baseImage.PixelWidth != overlay.PixelWidth || baseImage.PixelHeight != overlay.PixelHeight)
            {
                overlay = ResizeImage(overlay, baseImage.PixelWidth, baseImage.PixelHeight);
            }

            int width = baseImage.PixelWidth;
            int height = baseImage.PixelHeight;
            int stride = width * 4;
            byte[] basePixels = new byte[height * stride];
            byte[] overlayPixels = new byte[height * stride];
            byte[] resultPixels = new byte[height * stride];

            baseImage.CopyPixels(basePixels, stride, 0);
            overlay.CopyPixels(overlayPixels, stride, 0);

            for (int i = 0; i < basePixels.Length; i += 4)
            {
                byte baseA = basePixels[i + 3];
                byte baseR = basePixels[i + 2];
                byte baseG = basePixels[i + 1];
                byte baseB = basePixels[i];

                byte overlayA = (byte)(overlayPixels[i + 3] * opacity);
                byte overlayR = (byte)(useRed ? overlayPixels[i + 2] * opacity : 0);
                byte overlayG = (byte)(useGreen ? overlayPixels[i + 1] * opacity : 0);
                byte overlayB = (byte)(useBlue ? overlayPixels[i] * opacity : 0);

                byte resultA, resultR, resultG, resultB;

                switch (blendMode)
                {
                    case "Add":
                        resultA = (byte)Math.Min(baseA + overlayA, 255);
                        resultR = (byte)Math.Min(baseR + overlayR, 255);
                        resultG = (byte)Math.Min(baseG + overlayG, 255);
                        resultB = (byte)Math.Min(baseB + overlayB, 255);
                        break;
                    case "Subtract":
                        resultA = (byte)Math.Abs(baseA - overlayA);
                        resultR = (byte)Math.Abs(baseR - overlayR);
                        resultG = (byte)Math.Abs(baseG - overlayG);
                        resultB = (byte)Math.Abs(baseB - overlayB);
                        break;
                    case "Multiply":
                        resultA = (byte)(baseA * overlayA / 255);
                        resultR = (byte)(baseR * overlayR / 255);
                        resultG = (byte)(baseG * overlayG / 255);
                        resultB = (byte)(baseB * overlayB / 255);
                        break;
                    case "Divide":
                        resultA = baseA;
                        resultR = (byte)(overlayR == 0 ? 255 : Math.Min(baseR / overlayR, 255));
                        resultG = (byte)(overlayG == 0 ? 255 : Math.Min(baseG / overlayG, 255));
                        resultB = (byte)(overlayB == 0 ? 255 : Math.Min(baseB / overlayB, 255));
                        break;
                    case "Average":
                        resultA = (byte)((baseA + overlayA) / 2);
                        resultR = (byte)((baseR + overlayR) / 2);
                        resultG = (byte)((baseG + overlayG) / 2);
                        resultB = (byte)((baseB + overlayB) / 2);
                        break;
                    case "Min":
                        resultA = Math.Min(baseA, overlayA);
                        resultR = Math.Min(baseR, overlayR);
                        resultG = Math.Min(baseG, overlayG);
                        resultB = Math.Min(baseB, overlayB);
                        break;
                    case "Max":
                        resultA = Math.Max(baseA, overlayA);
                        resultR = Math.Max(baseR, overlayR);
                        resultG = Math.Max(baseG, overlayG);
                        resultB = Math.Max(baseB, overlayB);
                        break;
                    default: // Normal
                        resultA = (byte)(baseA * (1 - opacity) + overlayA * opacity);
                        resultR = (byte)(baseR * (1 - opacity) + overlayR * opacity);
                        resultG = (byte)(baseG * (1 - opacity) + overlayG * opacity);
                        resultB = (byte)(baseB * (1 - opacity) + overlayB * opacity);
                        break;
                }

                resultPixels[i + 3] = resultA;
                resultPixels[i + 2] = resultR;
                resultPixels[i + 1] = resultG;
                resultPixels[i] = resultB;
            }

            WriteableBitmap result = new WriteableBitmap(baseImage);
            result.WritePixels(new Int32Rect(0, 0, width, height), resultPixels, stride, 0);
            return result;
        }

        private WriteableBitmap ResizeImage(WriteableBitmap source, int width, int height)
        {
            var transform = new ScaleTransform(
                (double)width / source.PixelWidth,
                (double)height / source.PixelHeight);

            var transformed = new TransformedBitmap(source, transform);

            WriteableBitmap result = new WriteableBitmap(width, height,
                source.DpiX, source.DpiY, source.Format, source.Palette);

            int stride = width * (transformed.Format.BitsPerPixel / 8);
            byte[] pixels = new byte[height * stride];
            transformed.CopyPixels(pixels, stride, 0);
            result.WritePixels(new Int32Rect(0, 0, width, height), pixels, stride, 0);

            return result;
        }

        private void LoadImages_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Image files (*.jpg, *.png, *.bmp)|*.jpg;*.png;*.bmp",
                Multiselect = true
            };

            if (dialog.ShowDialog() == true)
            {
                foreach (string filename in dialog.FileNames)
                {
                    try
                    {
                        var bitmap = new BitmapImage(new Uri(filename));
                        Images.Add(new ImageItem
                        {
                            Image = bitmap,
                            FileName = System.IO.Path.GetFileName(filename)
                        });
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка загрузки {filename}: {ex.Message}",
                            "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                UpdateResultImage();
            }
        }

        private void UpdateResultImage()
        {
            try
            {
                if (Images.Count == 0)
                {
                    ImageDisplay.Source = null;
                    HistogramCanvas.Children.Clear();
                    return;
                }

                int maxWidth = Images.Max(i => i.Image.PixelWidth);
                int maxHeight = Images.Max(i => i.Image.PixelHeight);

                WriteableBitmap result = new WriteableBitmap(
                    maxWidth, maxHeight, 96, 96, PixelFormats.Pbgra32, null);

                foreach (var image in Images)
                {
                    result = ApplyBlendMode(
                        result,
                        image.Image,
                        image.BlendMode,
                        image.Opacity,
                        image.UseRedChannel,
                        image.UseGreenChannel,
                        image.UseBlueChannel);
                }

                if (CurveCheckBox.IsChecked == true)
                {
                    result = ApplyCurveToImage(result);
                }

                ImageDisplay.Source = result;
                UpdateHistogram(result);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeleteImage_Click(object sender, RoutedEventArgs e)
        {
            if (((Button)sender).DataContext is ImageItem item)
            {
                Images.Remove(item);
                UpdateResultImage();
            }
        }

        private void SaveResult_Click(object sender, RoutedEventArgs e)
        {
            if (ImageDisplay.Source is BitmapSource image)
            {
                var dialog = new SaveFileDialog
                {
                    Filter = "PNG Image|*.png|JPEG Image|*.jpg|BMP Image|*.bmp"
                };

                if (dialog.ShowDialog() == true)
                {
                    try
                    {
                        BitmapEncoder encoder;
                        if (dialog.FileName.EndsWith(".jpg"))
                            encoder = new JpegBitmapEncoder();
                        else if (dialog.FileName.EndsWith(".bmp"))
                            encoder = new BmpBitmapEncoder();
                        else
                            encoder = new PngBitmapEncoder();

                        using (var stream = new FileStream(dialog.FileName, FileMode.Create))
                        {
                            encoder.Frames.Add(BitmapFrame.Create(image));
                            encoder.Save(stream);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка сохранения: {ex.Message}",
                            "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void CheckBox_CheckedChanged(object sender, RoutedEventArgs e)
        {
            UpdateResultImage();
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateResultImage();
        }

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            UpdateResultImage();
        }
    }

    public class ImageItem : INotifyPropertyChanged
    {
        private double _opacity = 1.0;
        private string _blendMode = "Normal";
        private bool _useRed = true;
        private bool _useGreen = true;
        private bool _useBlue = true;

        public BitmapImage Image { get; set; }
        public string FileName { get; set; }

        public double Opacity
        {
            get => _opacity;
            set { _opacity = value; OnPropertyChanged(); }
        }

        public string BlendMode
        {
            get => _blendMode;
            set { _blendMode = value; OnPropertyChanged(); }
        }

        public bool UseRedChannel
        {
            get => _useRed;
            set { _useRed = value; OnPropertyChanged(); }
        }

        public bool UseGreenChannel
        {
            get => _useGreen;
            set { _useGreen = value; OnPropertyChanged(); }
        }

        public bool UseBlueChannel
        {
            get => _useBlue;
            set { _useBlue = value; OnPropertyChanged(); }
        }

        public string ImageInfo => Image == null
            ? "Размер: N/A, DPI: N/A"
            : $"Размер: {Image.PixelWidth}x{Image.PixelHeight}, DPI: {Image.DpiX}x{Image.DpiY}";

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}