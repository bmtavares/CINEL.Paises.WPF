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
        #region Properties
        public string APIUrlBase { get; } = "https://restcountries.eu";
        public string APIController { get; } = "/rest/v2/all";
        #endregion

        #region Atributes
        private ApiService _apiService;
        private DataService _dataService;
        private NetworkService _networkService;
        private List<Country> _countries;
        #endregion

        public MainWindow()
        {
            InitializeComponent();
            _apiService = new ApiService();
            _dataService = new DataService();
            _networkService = new NetworkService();
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

                await Task.Run(() => _dataService.SaveData(_countries));

                await FetchFlags();
                await ConvertFlags();
            }
            else
            { // Connection Unavailable
                lblStatus.Content = "Connection not available. Loading from local database.";
                MessageBox.Show(conn.Message, "Could not connect");
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
            _countries = _dataService.GetData();
        }

        /// <summary>
        /// Loads Countries from the specified API.
        /// </summary>
        /// <returns></returns>
        private async Task LoadApiCountries()
        {

            var response = await _apiService.GetCountries
                (APIUrlBase, APIController);

            _countries = (List<Country>)response.Result;
        }

        /// <summary>
        /// Event for user double click on countries list
        /// </summary>
        private void lBoxCountries_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            lblHintDoubleClick.Visibility = Visibility.Hidden;
            var sel = lBoxCountries.SelectedItem as Country;
            CleanFields();
            PopulateFields(sel);
            lblHintTooltip.Visibility = Visibility.Visible;
            lBoxBorders.Visibility = Visibility.Visible;
            lBoxCurrencies.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// Clears all information fields into a default position
        /// so that only properties with info can be shown on new
        /// selection.
        /// </summary>
        private void CleanFields()
        {
            imageFlag.Source = null; // TODO: Change to "unavailable" icon
            lblCountryName.Content = "-";
            lblCountryNativeName.Content = "-";
            lblCountryAlpha2.Content = "-";
            lblCountryAlpha3.Content = "-";
            lBoxBorders.ItemsSource = null;
            lBoxCurrencies.ItemsSource = null;
        }

        /// <summary>
        /// Populates all information fields with properties
        /// from the selection.
        /// </summary>
        /// <param name="sel">Selected Country</param>
        private void PopulateFields(Country sel)
        {
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
            lBoxBorders.ItemsSource = sel.Borders;
            lBoxCurrencies.ItemsSource = sel.Currencies;
        }

        /// <summary>
        /// Uses NetworkService to download all flag .svg 's.
        /// </summary>
        private async Task FetchFlags()
        {
            foreach(var country in _countries)
            {
                await _networkService.GetFlag(country);
            }
        }

        /// <summary>
        /// Attempts to convert every .svg flag of Country list.
        /// FetchFlags() should run successfully at least once before usage.
        /// </summary>
        /// <returns></returns>
        private async Task ConvertFlags() // TODO: Look into ways to improve memory usage
        {
            foreach (var country in _countries)
            {
                try
                {
                    var svg = SvgDocument.Open($@"{_dataService.PathFlags}\{country.Alpha3Code.ToLower()}.svg");
                    using (var bitmap = svg.Draw()) // TODO: Set image size to standard for all flags
                    {
                        await Task.Run(() => bitmap.Save($@"{_dataService.PathFlags}\{country.Alpha3Code.ToLower()}.jpg", ImageFormat.Jpeg));
                    }

                }
                catch(ArgumentException ex)
                {
                    switch (country.Alpha3Code)
                    {
                        case "IOT":
                        case "SHN":
                            // hardcoded due to the following:
                            // using <Svg> conversion will error out due to yet unidentified issues with the rendering of the original files
                            // catching exception to always use local files and prevent more processing during each cycle
                            break;
                        default:
                            MessageBox.Show(ex.Message, "Error");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error");
                }
            }
        }
    }
}
