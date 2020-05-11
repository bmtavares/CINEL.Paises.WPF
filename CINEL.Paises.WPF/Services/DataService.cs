namespace CINEL.Paises.WPF.Services
{
    using System;
    using System.Collections.Generic;
    using System.Data.SQLite;
    using System.IO;
    using System.Windows;
    using CINEL.Paises.WPF.Models;

    public class DataService
    {
        #region Properties
        public string PathMain { get; } = @"Data";
        public string PathFlags { get; } = @"Data\Flags";
        public string PathDatabase { get; } = @"Data\Countries.sqlite";
        #endregion
        #region Atributes
        private SQLiteConnection _connection;
        private SQLiteCommand _command;
        #endregion

        public DataService()
        {
            if (!Directory.Exists(PathMain))
            {
                Directory.CreateDirectory(PathMain);
            }
            if (!Directory.Exists(PathFlags))
            {
                Directory.CreateDirectory(PathFlags);
            }

            CreateTables();
        }

        /// <summary>
        /// Prepares string (parameter/data) for input into sql database
        /// </summary>
        /// <param name="str">String to sanatize</param>
        private string SanitizeString(string str)
        {
            return str == null ? null : str.Replace("'", "''");
        }

        /// <summary>
        /// Executes a non query on the local SQLLite database.
        /// </summary>
        /// <param name="sqlcommand">SqlCommand to execute.</param>
        private void LocalNonQuery(string sqlcommand)
        {
            try
            {
                _command = new SQLiteCommand(sqlcommand, _connection);
                _command.ExecuteNonQuery();
            }
            catch(Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// Initializes the database
        /// </summary>
        private void CreateTables()
        {
            try
            {
                _connection = new SQLiteConnection("Data Source =" + PathDatabase);
                _connection.Open();

                // Create main table
                string sqlcommand =
                    "CREATE TABLE IF NOT EXISTS countries(" +
                    "Name VARCHAR(250)," +
                    "Alpha2Code CHAR(2)," +
                    "Alpha3Code CHAR(3) PRIMARY KEY," +
                    "Capital VARCHAR(250)," +
                    "Region VARCHAR(250)," +
                    "Subregion VARCHAR(250)," +
                    "Population INT," +
                    "Demonym VARCHAR(250)," +
                    "Area REAL," +
                    "Gini REAL," +
                    "NativeName VARCHAR(250)," +
                    "NumericCode VARCHAR(50)," +
                    "Flag VARCHAR(250)," +
                    "Cioc VARCHAR(250)" +
                    ");";
                LocalNonQuery(sqlcommand);

                // Create suporting tables
                sqlcommand =
                    "CREATE TABLE IF NOT EXISTS topLevelDomains(" +
                    "Alpha3Code CHAR(3) REFERENCES countries(Alpha3Code)," +
                    "Domain VARCHAR(50)," +
                    "PRIMARY KEY(Alpha3Code, Domain)" +
                    ");";
                LocalNonQuery(sqlcommand);

                sqlcommand =
                    "CREATE TABLE IF NOT EXISTS callingCodes(" +
                    "Alpha3Code CHAR(3) REFERENCES countries(Alpha3Code)," +
                    "CallingCode VARCHAR(50)," +
                    "PRIMARY KEY(Alpha3Code, CallingCode)" +
                    ");";
                LocalNonQuery(sqlcommand);

                sqlcommand =
                    "CREATE TABLE IF NOT EXISTS altSpellings(" +
                    "Alpha3Code CHAR(3) REFERENCES countries(Alpha3Code)," +
                    "AltSpelling VARCHAR(250)," +
                    "PRIMARY KEY(Alpha3Code, AltSpelling)" +
                    ");";
                LocalNonQuery(sqlcommand);

                sqlcommand =
                    "CREATE TABLE IF NOT EXISTS latLngs(" +
                    "Alpha3Code CHAR(3) PRIMARY KEY REFERENCES countries(Alpha3Code)," +
                    "Lat REAL," +
                    "Lng REAL" +
                    ");";
                LocalNonQuery(sqlcommand);

                sqlcommand =
                    "CREATE TABLE IF NOT EXISTS timeZones(" +
                    "Alpha3Code CHAR(3) REFERENCES countries(Alpha3Code)," +
                    "TimeZone VARCHAR(250)," +
                    "PRIMARY KEY(Alpha3Code, TimeZone)" +
                    ");";
                LocalNonQuery(sqlcommand);

                sqlcommand =
                    "CREATE TABLE IF NOT EXISTS borders(" +
                    "Alpha3Code CHAR(3) REFERENCES countries(Alpha3Code)," +
                    "Border CHAR(3)," +
                    "PRIMARY KEY(Alpha3Code, Border)" +
                    ");";
                LocalNonQuery(sqlcommand);

                sqlcommand =
                    "CREATE TABLE IF NOT EXISTS currencies(" +
                    "Code CHAR(3) PRIMARY KEY," +
                    "Name VARCHAR(250)," +
                    "Symbol VARCHAR(1)" +
                    ");";
                LocalNonQuery(sqlcommand);

                sqlcommand =
                    "CREATE TABLE IF NOT EXISTS languages(" +
                    "Iso639_2 CHAR(3) PRIMARY KEY," +
                    "Iso639_1 CHAR(2)," +
                    "Name VARCHAR(250)," +
                    "NativeName VARCHAR(250)" +
                    ");";
                LocalNonQuery(sqlcommand);

                sqlcommand =
                    "CREATE TABLE IF NOT EXISTS translations(" +
                    "Alpha3Code CHAR(3) PRIMARY KEY REFERENCES countries(Alpha3Code)," +
                    "De VARCHAR(250)," +
                    "Es VARCHAR(250)," +
                    "Fr VARCHAR(250)," +
                    "Ja VARCHAR(250)," +
                    "It VARCHAR(250)," +
                    "Br VARCHAR(250)," +
                    "Pt VARCHAR(250)," +
                    "Nl VARCHAR(250)," +
                    "Hr VARCHAR(250)," +
                    "Fa VARCHAR(250)" +
                    ");";
                LocalNonQuery(sqlcommand);

                sqlcommand =
                    "CREATE TABLE IF NOT EXISTS regionalBlocs(" +
                    "Acronym VARCHAR(50) PRIMARY KEY," +
                    "Name VARCHAR(250)" +
                    ");";
                LocalNonQuery(sqlcommand);

                sqlcommand =
                    "CREATE TABLE IF NOT EXISTS otherAcronyms(" +
                    "Acronym VARCHAR(50) REFERENCES regionalBlocs(Acronym)," +
                    "OtherAcronym VARCHAR(50)," +
                    "PRIMARY KEY(Acronym, OtherAcronym)" +
                    ");";
                LocalNonQuery(sqlcommand);

                sqlcommand =
                    "CREATE TABLE IF NOT EXISTS otherNames(" +
                    "Acronym VARCHAR(50) REFERENCES regionalBlocs(Acronym)," +
                    "OtherNames VARCHAR(250)," +
                    "PRIMARY KEY(Acronym, OtherNames)" +
                    ");";
                LocalNonQuery(sqlcommand);

                //Create connective tables
                sqlcommand =
                    "CREATE TABLE IF NOT EXISTS countries_currencies(" +
                    "Alpha3Code CHAR(3) REFERENCES countries(Alpha3Code)," +
                    "Code CHAR(3) REFERENCES currencies(Code)," +
                    "PRIMARY KEY(Alpha3Code, Code)" +
                    ");";
                LocalNonQuery(sqlcommand);

                sqlcommand =
                    "CREATE TABLE IF NOT EXISTS countries_languages(" +
                    "Iso639_2 CHAR(3) REFERENCES languages(Iso639_2)," +
                    "Alpha3Code CHAR(3) REFERENCES countries(Alpha3Code)," +
                    "PRIMARY KEY(Iso639_2, Alpha3Code)" +
                    ");";
                LocalNonQuery(sqlcommand);

                sqlcommand =
                    "CREATE TABLE IF NOT EXISTS countries_regionalBlocs(" +
                    "Acronym VARCHAR(50) REFERENCES regionalBlocks(Acronym)," +
                    "Alpha3Code CHAR(3) REFERENCES countries(Alpha3Code)," +
                    "PRIMARY KEY(Acronym, Alpha3Code)" +
                    ");";
                LocalNonQuery(sqlcommand);

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Database error");
            }
        }

        /// <summary>
        /// Reads data from the local database into a usable list
        /// </summary>
        /// <returns>Country list</returns>
        public List<Country> GetData()
        {
            List<Country> countries = new List<Country>();
            List<Currency> masterCurrencies = new List<Currency>();
            List<Language> masterLanguages = new List<Language>();
            List<RegionalBloc> masterRegionalBlocs = new List<RegionalBloc>();

            try
            {
                _command = new SQLiteCommand(_connection);
                SQLiteCommand supportCommand = new SQLiteCommand(_connection);
                _command.CommandText = "SELECT * FROM currencies";
                SQLiteDataReader reader = _command.ExecuteReader();
                SQLiteDataReader supportReader;
                while (reader.Read())
                {
                    masterCurrencies.Add(new Currency
                    {
                        Code = (string)reader["Code"],
                        Name = (string)reader["Name"],
                        Symbol = (string)reader["Symbol"]
                    });
                }
                reader.Close();

                _command.CommandText = "SELECT * FROM languages";
                reader = _command.ExecuteReader();
                while (reader.Read())
                {
                    masterLanguages.Add(new Language
                    {
                        ISO639_1 = (string)reader["ISO639_1"],
                        ISO639_2 = (string)reader["ISO639_2"],
                        Name = (string)reader["Name"],
                        NativeName = (string)reader["NativeName"]
                    });
                }
                reader.Close();

                List<string> otherAcronyms = new List<string>();
                List<string> otherNames = new List<string>();
              
                _command.CommandText = "SELECT * FROM regionalBlocs";
                reader = _command.ExecuteReader();
                while (reader.Read())
                {
                    otherAcronyms.Clear();
                    otherNames.Clear();
                    supportCommand.CommandText = $"SELECT * FROM otherAcronyms WHERE Acronym = '{(string)reader["Acronym"]}'";
                    supportReader = supportCommand.ExecuteReader();
                    while (supportReader.Read())
                    {
                        otherAcronyms.Add((string)supportReader["OtherAcronym"]);
                    }
                    supportReader.Close();

                    supportCommand.CommandText = $"SELECT * FROM otherNames WHERE Acronym = '{(string)reader["Acronym"]}'";
                    supportReader = supportCommand.ExecuteReader();
                    while (supportReader.Read())
                    {
                        otherNames.Add((string)supportReader["OtherNames"]);
                    }
                    supportReader.Close();

                    masterRegionalBlocs.Add(new RegionalBloc
                    {
                        Acronym = (string)reader["Acronym"],
                        Name = (string)reader["Name"],
                        OtherAcronyms = new List<string>(otherAcronyms),
                        OtherNames = new List<string>(otherNames)
                    });
                }
                reader.Close();

                _command.CommandText = "SELECT * FROM countries";
                reader = _command.ExecuteReader();
                while (reader.Read())
                {
                    string a3Code = (string)reader["Alpha3Code"]; // Will be used repeatedly
                    List<string> topLevelDomains = new List<string>();
                    List<string> callingCodes = new List<string>();
                    List<string> altSpellings = new List<string>();
                    List<double> latLng = new List<double>();
                    List<string> timezones = new List<string>();
                    List<string> borders = new List<string>();
                    List<Currency> currencies = new List<Currency>();
                    List<Language> languages = new List<Language>();
                    List<RegionalBloc> regionalBlocs = new List<RegionalBloc>();
                    Translation translation = new Translation();

                    //TopLevelDomains
                    supportCommand.CommandText = "SELECT Domain FROM topLevelDomains " +
                        $"WHERE Alpha3Code = '{a3Code}'";
                    supportReader = supportCommand.ExecuteReader();
                    while (supportReader.Read())
                    {
                        topLevelDomains.Add((string)supportReader["Domain"]);
                    }
                    supportReader.Close();

                    //CallingCodes
                    supportCommand.CommandText = "SELECT CallingCode FROM callingCodes " +
                        $"WHERE Alpha3Code = '{a3Code}'";
                    supportReader = supportCommand.ExecuteReader();
                    while (supportReader.Read())
                    {
                        callingCodes.Add((string)supportReader["CallingCode"]);
                    }
                    supportReader.Close();

                    //AltSpellings
                    supportCommand.CommandText = "SELECT AltSpelling FROM altSpellings " +
                        $"WHERE Alpha3Code = '{a3Code}'";
                    supportReader = supportCommand.ExecuteReader();
                    while (supportReader.Read())
                    {
                        altSpellings.Add((string)supportReader["AltSpelling"]);
                    }
                    supportReader.Close();

                    //LatLng
                    supportCommand.CommandText = "SELECT Lat, Lng FROM latLngs " +
                        $"WHERE Alpha3Code = '{a3Code}'";
                    supportReader = supportCommand.ExecuteReader();
                    while (supportReader.Read())
                    {
                        latLng.Add((double)supportReader["Lat"]);
                        latLng.Add((double)supportReader["Lng"]);
                    }
                    supportReader.Close();

                    //Timezones
                    supportCommand.CommandText = "SELECT Timezone FROM timezones " +
                        $"WHERE Alpha3Code = '{a3Code}'";
                    supportReader = supportCommand.ExecuteReader();
                    while (supportReader.Read())
                    {
                        timezones.Add((string)supportReader["Timezone"]);
                    }
                    supportReader.Close();

                    //Borders
                    supportCommand.CommandText = "SELECT Border FROM borders " +
                        $"WHERE Alpha3Code = '{a3Code}'";
                    supportReader = supportCommand.ExecuteReader();
                    while (supportReader.Read())
                    {
                        borders.Add((string)supportReader["Border"]);
                    }
                    supportReader.Close();

                    //Languages
                    supportCommand.CommandText = "SELECT Iso639_2 FROM countries_languages " +
                        $"WHERE Alpha3Code = '{a3Code}'";
                    supportReader = supportCommand.ExecuteReader();
                    while (supportReader.Read())
                    {
                        languages.Add(masterLanguages.Find(x => x.ISO639_2 == (string)supportReader["Iso639_2"]));
                    }
                    supportReader.Close();

                    //Currencies
                    supportCommand.CommandText = "SELECT Code FROM countries_currencies " +
                        $"WHERE Alpha3Code = '{a3Code}'";
                    supportReader = supportCommand.ExecuteReader();
                    while (supportReader.Read())
                    {
                        currencies.Add(masterCurrencies.Find(x => x.Code == (string)supportReader["Code"]));
                    }
                    supportReader.Close();

                    //RegionalBlocs
                    supportCommand.CommandText = "SELECT Acronym FROM countries_regionalBlocs " +
                        $"WHERE Alpha3Code = '{a3Code}'";
                    supportReader = supportCommand.ExecuteReader();
                    while (supportReader.Read())
                    {
                        regionalBlocs.Add(masterRegionalBlocs.Find(x => x.Acronym == (string)supportReader["Acronym"]));
                    }
                    supportReader.Close();

                    //Translations
                    supportCommand.CommandText = "SELECT De, Es, Fr, Ja, It, Br, Pt, Nl, Hr, Fa FROM translations " +
                        $"WHERE Alpha3Code = '{a3Code}'";
                    supportReader = supportCommand.ExecuteReader();
                    while (supportReader.Read())
                    {
                        translation = new Translation()
                        {
                            de = (string)supportReader["De"],
                            es = (string)supportReader["Es"],
                            fr = (string)supportReader["Fr"],
                            ja = (string)supportReader["Ja"],
                            it = (string)supportReader["It"],
                            br = (string)supportReader["Br"],
                            pt = (string)supportReader["Pt"],
                            nl = (string)supportReader["Nl"],
                            hr = (string)supportReader["Hr"],
                            fa = (string)supportReader["Fa"]
                        };
                    }
                    supportReader.Close();

                    countries.Add(new Country
                    {
                        Name = (string)reader["Name"],
                        Alpha2Code = (string)reader["Alpha2Code"],
                        Alpha3Code = a3Code,
                        Capital = (string)reader["Capital"],
                        Region = (string)reader["Region"],
                        Subregion = (string)reader["Subregion"],
                        Population = (int)reader["Population"],
                        Demonym = (string)reader["Demonym"],
                        Area = (double)reader["Area"],
                        Gini = (double)reader["Gini"],
                        NativeName = (string)reader["NativeName"],
                        NumericCode = (string)reader["NumericCode"],
                        Flag = (string)reader["Flag"],
                        Cioc = (string)reader["Cioc"],
                        Languages = new List<Language>(languages),
                        Currencies = new List<Currency>(currencies),
                        RegionalBlocs = new List<RegionalBloc>(regionalBlocs),
                        TopLevelDomain = new List<string>(topLevelDomains),
                        CallingCodes = new List<string>(callingCodes),
                        AltSpellings = new List<string>(altSpellings),
                        Timezones = new List<string>(timezones),
                        Borders = new List<string>(borders),
                        LatLng = new List<double>(latLng),
                        Translations = translation
                    });
                }

                return countries;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Database Error");
                return null;
            }
        }

        /// <summary>
        /// Saves data from the Country list into the database
        /// </summary>
        /// <param name="countries">Countries</param>
        public void SaveData(IProgress<ProgressReportModel> progress, List<Country> countries)
        {
            ProgressReportModel report = new ProgressReportModel();
            try
            {
                foreach(var country in countries)
                {

                    report.CountriesResolved.Add(country);
                    report.PercentComplete = (report.CountriesResolved.Count * 100) / countries.Count;
                    report.StatusMessage = $"Saving country data for offline viewing {report.CountriesResolved.Count}/{countries.Count}";
                    progress.Report(report);

                    string sqlcommand = string.Format(
                        "INSERT OR REPLACE INTO countries " +
                        "VALUES('{0}','{1}','{2}','{3}','{4}','{5}',{6},'{7}','{8}','{9}','{10}','{11}','{12}','{13}')",
                        SanitizeString(country.Name),
                        country.Alpha2Code,
                        country.Alpha3Code,
                        SanitizeString(country.Capital),
                        SanitizeString(country.Region),
                        SanitizeString(country.Subregion),
                        country.Population,
                        SanitizeString(country.Demonym),
                        country.Area,
                        country.Gini,
                        SanitizeString(country.NativeName),
                        country.NumericCode,
                        SanitizeString(country.Flag),
                        SanitizeString(country.Cioc)
                        );
                    LocalNonQuery(sqlcommand);

                    foreach(var domain in country.TopLevelDomain)
                    {
                        sqlcommand = string.Format("INSERT OR REPLACE INTO topLevelDomains VALUES" +
                            "('{0}','{1}')",
                            country.Alpha3Code, domain);
                        LocalNonQuery(sqlcommand);
                    }

                    foreach (var callingCode in country.CallingCodes)
                    {
                        sqlcommand = string.Format("INSERT OR REPLACE INTO callingCodes VALUES" +
                            "('{0}','{1}')",
                            country.Alpha3Code, callingCode);
                        LocalNonQuery(sqlcommand);
                    }

                    foreach (var altSpelling in country.AltSpellings)
                    {
                        sqlcommand = string.Format("INSERT OR REPLACE INTO altSpellings VALUES" +
                            "('{0}','{1}')",
                            country.Alpha3Code, SanitizeString(altSpelling));
                        LocalNonQuery(sqlcommand);
                    }

                    if(country.LatLng.Count != 0)
                    {
                        sqlcommand = string.Format("INSERT OR REPLACE INTO latLngs VALUES" +
                        "('{0}','{1}','{2}')",
                        country.Alpha3Code, country.LatLng[0], country.LatLng[1]);
                        LocalNonQuery(sqlcommand);
                    }
                    else
                    {
                        sqlcommand = string.Format("INSERT OR REPLACE INTO latLngs VALUES" +
                        "('{0}','{1}','{2}')",
                        country.Alpha3Code, null, null);
                        LocalNonQuery(sqlcommand);
                    }

                    foreach (var timeZone in country.Timezones)
                    {
                        sqlcommand = string.Format("INSERT OR REPLACE INTO timeZones VALUES" +
                            "('{0}','{1}')",
                            country.Alpha3Code, timeZone);
                        LocalNonQuery(sqlcommand);
                    }

                    foreach (var border in country.Borders)
                    {
                        sqlcommand = string.Format("INSERT OR REPLACE INTO borders VALUES" +
                            "('{0}','{1}')",
                            country.Alpha3Code, border);
                        LocalNonQuery(sqlcommand);
                    }

                    foreach (var currency in country.Currencies)
                    {
                        sqlcommand = string.Format("INSERT OR REPLACE INTO currencies VALUES" +
                            "('{0}','{1}','{2}')",
                            currency.Code, SanitizeString(currency.Name), currency.Symbol);
                        LocalNonQuery(sqlcommand);

                        sqlcommand = string.Format("INSERT OR REPLACE INTO countries_currencies VALUES" +
                            "('{0}','{1}')",
                            country.Alpha3Code, currency.Code);
                        LocalNonQuery(sqlcommand);
                    }

                    foreach (var language in country.Languages)
                    {
                        sqlcommand = string.Format("INSERT OR REPLACE INTO languages VALUES" +
                            "('{0}','{1}','{2}','{3}')",
                            language.ISO639_2, language.ISO639_1,
                            SanitizeString(language.Name), SanitizeString(language.NativeName));
                        LocalNonQuery(sqlcommand);

                        sqlcommand = string.Format("INSERT OR REPLACE INTO countries_languages VALUES" +
                            "('{0}','{1}')",
                            language.ISO639_2, country.Alpha3Code);
                        LocalNonQuery(sqlcommand);
                    }

                    var translation = country.Translations;
                    sqlcommand = string.Format("INSERT OR REPLACE INTO translations VALUES" +
                        "('{0}','{1}','{2}','{3}','{4}','{5}','{6}','{7}','{8}','{9}','{10}')",
                        country.Alpha3Code,
                        SanitizeString(translation.de),
                        SanitizeString(translation.es),
                        SanitizeString(translation.fr),
                        SanitizeString(translation.ja),
                        SanitizeString(translation.it),
                        SanitizeString(translation.br),
                        SanitizeString(translation.pt),
                        SanitizeString(translation.nl),
                        SanitizeString(translation.hr),
                        SanitizeString(translation.fa)
                        );
                    LocalNonQuery(sqlcommand);

                    foreach (var regionalBloc in country.RegionalBlocs)
                    {
                        sqlcommand = string.Format("INSERT OR REPLACE INTO regionalBlocs VALUES" +
                            "('{0}','{1}')",
                            regionalBloc.Acronym, SanitizeString(regionalBloc.Name));
                        LocalNonQuery(sqlcommand);

                        foreach(var other in regionalBloc.OtherAcronyms)
                        {
                            sqlcommand = string.Format("INSERT OR REPLACE INTO otherAcronyms VALUES" +
                            "('{0}','{1}')",
                            regionalBloc.Acronym, other);
                            LocalNonQuery(sqlcommand);
                        }

                        foreach (var other in regionalBloc.OtherNames)
                        {
                            sqlcommand = string.Format("INSERT OR REPLACE INTO otherNames VALUES" +
                            "('{0}','{1}')",
                            regionalBloc.Acronym, SanitizeString(other));
                            LocalNonQuery(sqlcommand);
                        }

                        sqlcommand = string.Format("INSERT OR REPLACE INTO countries_regionalBlocs VALUES" +
                            "('{0}','{1}')",
                            regionalBloc.Acronym, country.Alpha3Code);
                        LocalNonQuery(sqlcommand);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Database error");
            }
        }

        /// <summary>
        /// Checks if a specific file is free for operations 
        /// or in use by another proccess.
        /// 
        /// Credit: https://stackoverflow.com/questions/876473/is-there-a-way-to-check-if-a-file-is-in-use
        /// </summary>
        /// <param name="file">File to check</param>
        /// <returns>True: File in use</returns>
        public bool IsFileLocked(FileInfo file)
        {
            try
            {
                using (FileStream stream = file.Open(FileMode.Open, FileAccess.Read, FileShare.None))
                {
                    stream.Close();
                }
            }
            catch (IOException)
            {
                //the file is unavailable because it is:
                //still being written to
                //or being processed by another thread
                //or does not exist (has already been processed)
                return true;
            }

            //file is not locked
            return false;
        }

        ///// <summary>
        ///// Deletes the whole database.
        ///// </summary>
        //public void DeleteData()
        //{
        //    try
        //    {
        //        if (File.Exists(PathDatabase))
        //        {
        //            File.Delete(PathDatabase);
        //        }
        //        CreateTables();
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show(ex.Message, "Database error");
        //    }
        //}
    }
}
