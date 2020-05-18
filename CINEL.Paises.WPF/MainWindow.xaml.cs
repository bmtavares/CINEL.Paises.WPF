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
    using Svg;
    using Models;
    using Services;
    using Microsoft.Maps.MapControl.WPF;

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
            Progress<ProgressReportModel> progress = new Progress<ProgressReportModel>();
            progress.ProgressChanged += ReportProgress;

            if (conn.IsSuccess)
            { // Connection Exists
                await LoadApiCountries(progress);

                await Task.Run(() => _dataService.SaveData(progress,_countries));

                await FetchFlags(progress);
                await ConvertFlags(progress);
            }
            else
            { // Connection Unavailable
                MessageBox.Show(conn.Message, "Could not connect");
                lblStatus.Content = "Connection not available. Loading from local database.";
                LoadLocalCountries();
            }

            if(_countries.Count == 0)
            { // if 0, assume no countries were loaded
                lblStatus.Content = "Could not load data. Please try again after connecting to the internet.";
                return;
            }

            lblIcon.Visibility = Visibility.Hidden;
            gridCountries.Visibility = Visibility.Visible;
            lBoxCountries.ItemsSource = _countries; // populate list with countries
            gridLoading.Visibility = Visibility.Hidden;
            gridFinish.Visibility = Visibility.Visible;
            lblFinish.Content = $"Successfully loaded {_countries.Count} countries.{Environment.NewLine}" +
                $"Double click a country from the list to check it's information.";
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
        private async Task LoadApiCountries(IProgress<ProgressReportModel> progress)
        {
            ProgressReportModel report = new ProgressReportModel{
                PercentComplete = 0,
                StatusMessage = "Fetching new data from the internet."
            };
            progress.Report(report);

            var response = await _apiService.GetCountries
                (APIUrlBase, APIController);

            report = new ProgressReportModel
            {
                PercentComplete = 100,
                StatusMessage = "Fetch complete."
            };
            progress.Report(report);

            _countries = (List<Country>)response.Result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ReportProgress(object sender, ProgressReportModel e)
        {
            pBarStatus.Value = e.PercentComplete;
            lblStatus.Content = e.StatusMessage;
        }

        /// <summary>
        /// Event for user double click on countries list
        /// </summary>
        private void lBoxCountries_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            gridFinish.Visibility = Visibility.Hidden;
            var sel = lBoxCountries.SelectedItem as Country;
            CleanFields();
            PopulateFields(sel);
            gridCountryInfo.Visibility = Visibility.Visible;
        }

        private void lBoxBorders_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if(lBoxBorders.SelectedItem != null)
            {
                var sel = _countries.Find(x => x.Alpha3Code == lBoxBorders.SelectedItem.ToString());
                CleanFields();
                PopulateFields(sel);
            }
        }

        /// <summary>
        /// Clears all information fields into a default position
        /// so that only properties with info can be shown on new
        /// selection.
        /// </summary>
        private void CleanFields()
        {
            // TODO: Insert no flag
            //if (_noflag != null)
            //    imageFlag.Source = _noflag;
            //else
                imageFlag.Source = null;

            lblCountryName.Content = string.Empty;
            lblCountryNativeName.Content = string.Empty;
            lblCountryAlpha2.Content = string.Empty;
            lblCountryAlpha3.Content = string.Empty;
            lblCountryCapital.Content = string.Empty;
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
            if (File.Exists($@"{_dataService.PathFlags}\{sel.Alpha3Code.ToLower()}.jpg"))
            {
                var uri = new Uri(AppDomain.CurrentDomain.BaseDirectory + $@"{_dataService.PathFlags}\{sel.Alpha3Code.ToLower()}.jpg", UriKind.Absolute);
                var bitmap = new BitmapImage(uri);
                imageFlag.Source = bitmap;
            }
            lblCountryName.Content = sel.Name;
            lblCountryNativeName.Content = $"{sel.NativeName} ({sel.Demonym})";
            lblCountryAlpha2.Content = sel.Alpha2Code;
            lblCountryAlpha3.Content = sel.Alpha3Code;
            lblCountryCapital.Content = sel.Capital;
            lBoxBorders.ItemsSource = sel.Borders;
            lBoxCurrencies.ItemsSource = sel.Currencies;

            if(sel.LatLng.Count == 2)
            {
                uxMap.Center = new Location(sel.LatLng[0], sel.LatLng[1]);
                uxMap.ZoomLevel = 3;
            }
            else
            { // certain countries do not have a location value. Sets default
                uxMap.Center = new Location(0, 0);
                uxMap.ZoomLevel = 1;
            }
            
        }

        /// <summary>
        /// Uses NetworkService to download all flag .svg 's.
        /// </summary>
        private async Task FetchFlags(IProgress<ProgressReportModel> progress)
        {
            ProgressReportModel report = new ProgressReportModel();

            foreach (var country in _countries)
            {
                report.CountriesResolved.Add(country);
                report.PercentComplete = (report.CountriesResolved.Count * 100) / _countries.Count;
                report.StatusMessage = $"Downloading flags {report.CountriesResolved.Count}/{_countries.Count}";
                progress.Report(report);

                await _networkService.GetFlag(country);
            }
        }

        /// <summary>
        /// Attempts to convert every .svg flag of Country list.
        /// FetchFlags() should run successfully at least once before usage.
        /// </summary>
        /// <returns></returns>
        private async Task ConvertFlags(IProgress<ProgressReportModel> progress)
        // TODO: Look into ways to improve memory usage
        // https://stackoverflow.com/questions/53903784/transform-svg-string-to-bitmap-ideally-in-memory-in-c-sharp possible?
        {
            ProgressReportModel report = new ProgressReportModel();

            foreach (var country in _countries)
            {
                report.CountriesResolved.Add(country);
                report.PercentComplete = (report.CountriesResolved.Count * 100) / _countries.Count;
                report.StatusMessage = $"Converting flags into viewable format {report.CountriesResolved.Count}/{_countries.Count}";
                progress.Report(report);

                try
                {
                    var file = new FileInfo($@"{_dataService.PathFlags}\{country.Alpha3Code.ToLower()}.svg");
                    if (_dataService.IsFileLocked(file))
                    {
                        do
                        {
                            var svg = SvgDocument.Open($@"{_dataService.PathFlags}\{country.Alpha3Code.ToLower()}.svg");
                            using (var bitmap = svg.Draw()) // TODO: Set image size to standard for all flags
                            {
                                await Task.Run(() => bitmap.Save($@"{_dataService.PathFlags}\{country.Alpha3Code.ToLower()}.jpg", ImageFormat.Jpeg));
                            }
                        } while (_dataService.IsFileLocked(file));
                    }
                    else
                    {
                        var svg = SvgDocument.Open($@"{_dataService.PathFlags}\{country.Alpha3Code.ToLower()}.svg");
                        using (var bitmap = svg.Draw())
                        {
                            await Task.Run(() => bitmap.Save($@"{_dataService.PathFlags}\{country.Alpha3Code.ToLower()}.jpg", ImageFormat.Jpeg));
                        }
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
