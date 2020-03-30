namespace CINEL.Paises.WPF
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
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
    using Models;
    using Services;
    using Svg;

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Atributes
        private ApiService _apiService;
        private List<Country> _countries;

        #endregion

        public MainWindow()
        {
            InitializeComponent();
            _apiService = new ApiService();
        }

        private async Task LoadApiCountries()
        {

            var response = await _apiService.GetCountries
                ("https://restcountries.eu", "/rest/v2/all");

            _countries = (List<Country>)response.Result;

        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            await LoadApiCountries();
            lBox.ItemsSource = _countries;
            
        }

        private void lBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            //TODO: Download svg; Convert to image; Save to disk; Load into WPF from disk
            //var sel = lBox.SelectedItem as Country;

            //imageFlag.Source = null;

            //BitmapImage bitmap = new BitmapImage();
            //bitmap.BeginInit();
            //bitmap.UriSource = new Uri(sel.Flag, UriKind.Absolute);
            //bitmap.EndInit();

            ////var svgTest = new SvgDocument();

            ////svgTest.BaseUri = new Uri(sel.Flag);
            ////var test = svgTest.Draw();


            //imageFlag.Source = bitmap;
        }
    }
}
