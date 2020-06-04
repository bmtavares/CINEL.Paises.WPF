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
    using Microsoft.Maps.MapControl.WPF;
    using Models;
    using Services;
    using System.Linq;
    using System.Windows.Media;
    using System.Collections;

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

                await FetchFlags(progress);
                btnHideLoading.Visibility = Visibility.Visible;
                await ConvertFlags(progress);

                await Task.Run(() => _dataService.SaveData(progress, _countries));
                pBarStatus.Visibility = Visibility.Hidden;
            }
            else
            { // Connection Unavailable
                MessageBox.Show(conn.Message, "Could not connect");
                lblStatus.Content = "Connection not available." + Environment.NewLine + 
                                    "Loading from local database.";
                LoadLocalCountries();
            }

            if (_countries.Count == 0)
            { // if 0, assume no countries were loaded
                lblStatus.Content = "Could not load data." + Environment.NewLine +
                                    "Please try again after" + Environment.NewLine +
                                    "connecting to the internet.";
                return;
            }

            if (gridLoading.Visibility == Visibility.Visible)
            {
                HideLoading();
                gridFinish.Visibility = Visibility.Visible;
                lblFinish.Content = $"Successfully loaded {_countries.Count} countries.{Environment.NewLine}" +
                                    "Click a country from the list to check it's information.";
                pBarStatus.Visibility = Visibility.Hidden;
            }
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
            ProgressReportModel report = new ProgressReportModel
            {
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
        /// Updates status elements each time a report is issued
        /// </summary>
        private void ReportProgress(object sender, ProgressReportModel e)
        {
            pBarStatus.Value = e.PercentComplete;
            lblStatus.Content = e.StatusMessage;
        }

        /// <summary>
        /// Finds all distinct Regional Blocs within the countries list
        /// (to be used in filtering ComboBox)
        /// </summary>
        /// <returns>Prepared RegionalBloc list</returns>
        private List<RegionalBloc> GenerateComboBlocs()
        {
            List<RegionalBloc> rb = new List<RegionalBloc>();
            foreach (var country in _countries)
            {
                foreach (var bloc in country.RegionalBlocs)
                {
                    if (rb.Find(x => x.Name == bloc.Name) == null)
                    {
                        rb.Add(bloc);
                    }
                }
            }

            return rb.OrderBy(x => x.Name).ToList();
        }

        /// <summary>
        /// Finds all distinct Continents(Regions) within the countries list
        /// (to be used in filtering ComboBox)
        /// </summary>
        /// <returns>Prepared RegionalBloc list</returns>
        private List<String> GenerateComboContinents()
        {
            List<String> regions = new List<String>();

            foreach (var country in _countries)
            {
                if (regions.Find(x => x == country.Region) == null)
                {
                    regions.Add(country.Region);
                }

            }

            return regions.OrderBy(x => x).ToList();
        }

        /// <summary>
        /// Clears all information fields into a default position
        /// so that only properties with info can be shown on new
        /// selection.
        /// </summary>
        private void CleanFields()
        {
            imageFlag.Source = new BitmapImage(new Uri("Resources/flagnotfound.png", UriKind.Relative));

            lblCountryName.Content = string.Empty;
            lblCountryNativeName.Content = string.Empty;
            lblCountryRegionNSub.Content = string.Empty;
            lblCountryAlphaCodes.Content = string.Empty;
            lblCountryCapital.Content = string.Empty;
            lblCountryPopulation.Content = "No info";
            lblCountryGini.Content = "No info";
            lblCountryArea.Content = "No info";
            lBoxBorders.ItemsSource = null;
            lBoxCurrencies.ItemsSource = null;
            lBoxLanguages.ItemsSource = null;
            lBoxTranslations.ItemsSource = null;
            uxMap.Center = new Location(0, 0);
            uxMap.ZoomLevel = 1;
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
            lblCountryNativeName.Content = $"{EmptyStringPropertyCatcher(sel.NativeName)} ({EmptyStringPropertyCatcher(sel.Demonym)})";
            lblCountryRegionNSub.Content = $"{EmptyStringPropertyCatcher(sel.Region)} / {EmptyStringPropertyCatcher(sel.Subregion)}";
            lblCountryAlphaCodes.Content = $"{EmptyStringPropertyCatcher(sel.Alpha2Code)} / {EmptyStringPropertyCatcher(sel.Alpha3Code)}";
            lblCountryCapital.Content = EmptyStringPropertyCatcher(sel.Capital);

            if (sel.Population != 0)
            {
                lblCountryPopulation.Content = sel.Population.ToString();
            }
            if (sel.Gini != 0)
            {
                lblCountryGini.Content = sel.Gini.ToString();
            }
            if (sel.Area != 0)
            {
                lblCountryArea.Content = $"{sel.Area} km²";
            }

            lBoxTranslations.ItemsSource = PopulateTranslations(sel);
            lBoxBorders.ItemsSource = sel.Borders;
            lBoxCurrencies.ItemsSource = sel.Currencies;
            lBoxLanguages.ItemsSource = sel.Languages;

            if (sel.LatLng.Count == 2)
            {
                uxMap.Center = new Location(sel.LatLng[0], sel.LatLng[1]);
                uxMap.ZoomLevel = 4;
            }

        }

        /// <summary>
        /// Builds a list of translations from the selected Country model
        /// </summary>
        private List<string> PopulateTranslations(Country sel)
        {
            List<string> result = new List<string>();

            result.Add($"🇩🇪 {EmptyStringPropertyCatcher(sel.Translations.de)}");
            result.Add($"🇪🇸 {EmptyStringPropertyCatcher(sel.Translations.es)}");

            result.Add($"🇫🇷 {EmptyStringPropertyCatcher(sel.Translations.fr)}");
            result.Add($"🇯🇵 {EmptyStringPropertyCatcher(sel.Translations.ja)}");

            result.Add($"🇮🇹 {EmptyStringPropertyCatcher(sel.Translations.it)}");
            result.Add($"🇧🇷 {EmptyStringPropertyCatcher(sel.Translations.br)}");

            result.Add($"🇵🇹 {EmptyStringPropertyCatcher(sel.Translations.pt)}");
            result.Add($"🇳🇱 {EmptyStringPropertyCatcher(sel.Translations.nl)}");

            result.Add($"🇭🇷 {EmptyStringPropertyCatcher(sel.Translations.hr)}");
            result.Add($"🇮🇷 {EmptyStringPropertyCatcher(sel.Translations.fa)}");

            return result;
        }

        /// <summary>
        /// Checks strings from the Country model for information
        /// to display.
        /// </summary>
        private string EmptyStringPropertyCatcher(string propertyToCheck)
        {
            if (string.IsNullOrEmpty(propertyToCheck))
            {
                return "No info";
            }
            else
            {
                return propertyToCheck;
            }
        }

        /// <summary>
        /// Common code used after essencial actions for viewing are performed to
        /// hide and show, the loading and information grids respectively.
        /// </summary>
        private void HideLoading()
        {
            gridCountries.Visibility = Visibility.Visible;
            lBoxCountries.ItemsSource = _countries.OrderBy(x => x.Name); // populate list with countries
                                                                         // it is ordered due to being reachable from offline mode
                                                                         // if saving is stopped during online mode, info is saved out of order
                                                                         //cBoxFilter.ItemsSource = GenerateComboBlocs();
            gridFinish.Visibility = Visibility.Visible;
            lblFinish.Content = $"Saving will continue in the background for offline use." + Environment.NewLine +
                "Click a country from the list to check it's details." + Environment.NewLine +
                "At any time you can use the filter options at the top of the list.";
            gridLoading.Visibility = Visibility.Hidden;
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
                report.StatusMessage = $"Downloading flags{Environment.NewLine}({report.CountriesResolved.Count} of {_countries.Count})";
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
        {
            ProgressReportModel report = new ProgressReportModel();

            foreach (var country in _countries)
            {
                report.CountriesResolved.Add(country);
                report.PercentComplete = (report.CountriesResolved.Count * 100) / _countries.Count;
                report.StatusMessage = $"Converting flags into viewable format{Environment.NewLine}({report.CountriesResolved.Count} of {_countries.Count})";
                progress.Report(report);

                try
                {
                    var file = new FileInfo($@"{_dataService.PathFlags}\{country.Alpha3Code.ToLower()}.svg");
                    do
                    {
                        if (!_dataService.IsFileLocked(file))
                        {
                            var svg = SvgDocument.Open($@"{_dataService.PathFlags}\{country.Alpha3Code.ToLower()}.svg");
                            using (var bitmap = svg.Draw())
                            {
                                await Task.Run(() => bitmap.Save($@"{_dataService.PathFlags}\{country.Alpha3Code.ToLower()}.jpg", ImageFormat.Jpeg));
                            }
                        }
                    } while (_dataService.IsFileLocked(file));
                }
                catch (ArgumentException ex)
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

        #region UXActions
        /// <summary>
        /// Event for user double click on countries list
        /// </summary>
        private void lBoxCountries_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            gridFinish.Visibility = Visibility.Hidden;
            var sel = lBoxCountries.SelectedItem as Country;
            if (sel != null)
            {
                CleanFields();
                PopulateFields(sel);
                gridCountryInfo.Visibility = Visibility.Visible;
            }
        }

        /// <summary>
        /// Event for user double click on borders list inside country info
        /// </summary>
        private void lBoxBorders_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (lBoxBorders.SelectedItem != null)
            {
                var sel = _countries.Find(x => x.Alpha3Code == lBoxBorders.SelectedItem.ToString());
                CleanFields();
                PopulateFields(sel);
            }
        }

        /// <summary>
        /// Event for user click on loading grid button
        /// (visually sends the saving process to the background)
        /// </summary>
        private void btnHideLoading_Click(object sender, RoutedEventArgs e)
        {
            HideLoading();
        }

        /// <summary>
        /// Event button "All" click
        /// Rearranges UI and loads all the countries to the list
        /// </summary>
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            cBoxFilter.Visibility = Visibility.Hidden;
            lBoxCountries.Height = 441;
            lBoxCountries.Margin = new Thickness(1, 35, 0, 0);
            cBoxFilter.ItemsSource = null;
            lBoxCountries.ItemsSource = _countries;
            btnShowAll.BorderBrush = (SolidColorBrush)(new BrushConverter().ConvertFrom("#FF254B9C"));
            btnShowRB.BorderBrush = (SolidColorBrush)(new BrushConverter().ConvertFrom("#FFBDBDBD"));
            btnShowContinent.BorderBrush = (SolidColorBrush)(new BrushConverter().ConvertFrom("#FFBDBDBD"));
        }

        /// <summary>
        /// Event button "Regional Blocs" click
        /// Rearranges UI and loads desired filtering options into combobox
        /// </summary>
        private void btnShowRB_Click(object sender, RoutedEventArgs e)
        {
            cBoxFilter.Visibility = Visibility.Visible;
            lBoxCountries.Height = 411;
            lBoxCountries.Margin = new Thickness(1, 65, 0, 0);
            cBoxFilter.ItemsSource = null;
            cBoxFilter.ItemsSource = GenerateComboBlocs();
            cBoxFilter.SelectedIndex = 0;
            btnShowAll.BorderBrush = (SolidColorBrush)(new BrushConverter().ConvertFrom("#FFBDBDBD"));
            btnShowRB.BorderBrush = (SolidColorBrush)(new BrushConverter().ConvertFrom("#FF254B9C"));
            btnShowContinent.BorderBrush = (SolidColorBrush)(new BrushConverter().ConvertFrom("#FFBDBDBD"));
        }

        /// <summary>
        /// Event button "Continents" Click
        /// Rearranges UI and loads desired filtering options into combobox
        /// </summary>
        private void btnShowContinent_Click(object sender, RoutedEventArgs e)
        {
            cBoxFilter.Visibility = Visibility.Visible;
            lBoxCountries.Height = 411;
            lBoxCountries.Margin = new Thickness(1, 65, 0, 0);
            cBoxFilter.ItemsSource = null;
            cBoxFilter.ItemsSource = GenerateComboContinents();
            cBoxFilter.SelectedIndex = 0;
            btnShowAll.BorderBrush = (SolidColorBrush)(new BrushConverter().ConvertFrom("#FFBDBDBD"));
            btnShowRB.BorderBrush = (SolidColorBrush)(new BrushConverter().ConvertFrom("#FFBDBDBD"));
            btnShowContinent.BorderBrush = (SolidColorBrush)(new BrushConverter().ConvertFrom("#FF254B9C"));
        }

        /// <summary>
        /// Event for selection change on filter combo box
        /// (filters the countries with the selected item onto the list)
        /// </summary>
        private void cBoxRegionalBlocs_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            var bloc = cBoxFilter.SelectedItem as RegionalBloc;

            if (bloc != null)
            {
                lBoxCountries.ItemsSource = _countries.FindAll(x => x.RegionalBlocs.Any(y => y.Name == bloc.Name))
                                            .OrderBy(x => x.Name);
            }
            else //assume tab is Continents/Region
            {
                var region = cBoxFilter.SelectedItem as string;

                lBoxCountries.ItemsSource = _countries.FindAll(x => x.Region == region).OrderBy(x => x.Name);
            }
        }
        #endregion
    }
}
