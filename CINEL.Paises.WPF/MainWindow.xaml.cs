namespace CINEL.Paises.WPF
{
    using System;
    using System.Collections.Generic;
    using System.Drawing.Imaging;
    using System.IO;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Input;
    using System.Windows.Media.Imaging;
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
        private DataService _dataService;
        private NetworkService _networkService;
        private List<Country> _countries;
        //private List<Flag> _flags;

        #endregion

        public MainWindow()
        {
            InitializeComponent();
            _apiService = new ApiService();
            _dataService = new DataService();
            _networkService = new NetworkService();
            //_flags = new List<Flag>();
            StartRoutine();
        }

        /// <summary>
        /// Checks for internet connection and starts program in
        /// online or offline mode according to network conditions.
        /// </summary>
        private async void StartRoutine()
        {
            lblStatus.Content = "Checking connection.";
            var conn = _networkService.CheckConnection();
            pBarStatus.Value = 30; //TODO: Implement ProgressReport

            if (conn.IsSuccess)
            { // Connection Exists
                lblStatus.Content = "Connection successful. Loading from the internet.";
                pBarStatus.Value = 70;
                await LoadApiCountries();
                lblStatus.Content = "Connection successful. Fetching flags.";
                pBarStatus.Value = 85;
                await FetchFlags();
                await ConvertFlags();
            }
            else
            { // Connection Unavailable
                lblStatus.Content = "Connection not available. Loading from local database.";
                LoadLocalCountries();
            }

            if(_countries.Count == 0)
            { // if 0, assume no countries were loaded
                lblStatus.Content = "Could not load data. Please try after connecting to the internet.";
                return;
            }

            lBoxCountries.ItemsSource = _countries; // populate list with countries
            lblHintDoubleClick.Visibility = Visibility.Visible;
            lblStatus.Content = $"Successfully loaded {_countries.Count} countries.";
            pBarStatus.Value = 100;
        }

        /// <summary>
        /// Loads Countries from local database on disk.
        /// </summary>
        private void LoadLocalCountries()
        {
            //_countries = _dataService.GetData();
        }

        /// <summary>
        /// Loads Countries from the specified API.
        /// </summary>
        /// <returns></returns>
        private async Task LoadApiCountries()
        {

            var response = await _apiService.GetCountries
                ("https://restcountries.eu", "/rest/v2/all");

            _countries = (List<Country>)response.Result;
        }

        /// <summary>
        /// Event for user double click on countries list
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void lBoxCountries_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            lblHintDoubleClick.Visibility = Visibility.Hidden;
            var sel = lBoxCountries.SelectedItem as Country;
            CleanFields();
            PopulateFields(sel);
            lblHintTooltip.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// Clears all information fields into a default position
        /// so that only properties with info can be shown on new
        /// selection.
        /// </summary>
        private void CleanFields()
        {
            imageFlag.Source = null;
            lblCountryName.Content = "-";
            lblCountryNativeName.Content = "-";
            lblCountryAlpha2.Content = "-";
            lblCountryAlpha3.Content = "-";
        }

        /// <summary>
        /// Populates all information fields with properties
        /// from the selection.
        /// </summary>
        /// <param name="sel"></param>
        private void PopulateFields(Country sel)
        {
            //ImageSourceConverter isc = new ImageSourceConverter();
            //var flag = _flags.Find(x => sel.Alpha3Code == x.Alpha3Code).FlagImage;
            //if(flag != null)
            //{
            //    imageFlag.Source = (ImageSource)isc.ConvertFrom(flag);
            //}
            if (File.Exists($@"{_dataService.PathFlags}\{sel.Alpha3Code.ToLower()}.jpg")) //TODO: possibly move to _dataService
            {
                var uri = new Uri(AppDomain.CurrentDomain.BaseDirectory + $@"{_dataService.PathFlags}\{sel.Alpha3Code.ToLower()}.jpg", UriKind.Absolute);
                var bitmap = new BitmapImage(uri);
                imageFlag.Source = bitmap;
            }
            lblCountryName.Content = sel.Name;
            lblCountryNativeName.Content = sel.NativeName;
            lblCountryAlpha2.Content = sel.Alpha2Code;
            lblCountryAlpha3.Content = sel.Alpha3Code;
        }

        /// <summary>
        /// Uses NetworkService to download all
        /// flag .svg 's.
        /// </summary>
        /// <returns></returns>
        private async Task FetchFlags()
        {
            foreach(var country in _countries)
            {
                await _networkService.GetFlag(country);
            }
        }

        private async Task ConvertFlags() // TODO: Improve memory usage & async
        {
            foreach (var country in _countries)
            {
                try
                {
                    var svg = SvgDocument.Open($@"{_dataService.PathFlags}\{country.Alpha3Code.ToLower()}.svg");
                    using (var bitmap = svg.Draw())
                    {
                        bitmap.Save($@"{_dataService.PathFlags}\{country.Alpha3Code.ToLower()}.jpg", ImageFormat.Jpeg);
                    }
                }
                catch
                {
                    //TODO
                }
            }
        }

        ///// <summary>
        ///// Converts flags and loads them into memory
        ///// </summary>
        ///// <returns></returns>
        //private async Task ConvertFlags()
        //{
        //    foreach(var country in _countries)
        //    {
        //        try
        //        {
        //            var svg = SvgDocument.Open($@"{_dataService.PathFlags}\{country.Alpha3Code.ToLower()}.svg");
        //            _flags.Add(new Flag
        //            {
        //                Alpha3Code = country.Alpha3Code,
        //                FlagImage = svg.Draw()
        //            });
        //        }
        //        catch
        //        {
        //            //TODO
        //        }
        //    }
        //}
    }
}
