namespace CINEL.Paises.WPF.Services
{
    using System.IO;
    public class DataService
    {
        public string PathMain { get; } = @"Data";
        public string PathFlags { get; } = @"Data\Flags";
        //public string PathDatabase { get; } = @"";
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
        }
    }
}
