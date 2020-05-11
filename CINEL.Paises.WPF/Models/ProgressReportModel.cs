namespace CINEL.Paises.WPF.Models
{
    using System.Collections.Generic;
    public class ProgressReportModel
    {
        public int PercentComplete { get; set; } = 0;
        public string StatusMessage { get; set; }
        public List<Country> CountriesResolved { get; set; } = new List<Country>();
    }
}
