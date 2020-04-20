namespace CINEL.Paises.WPF.Services
{
    using Models;
    using System.Net;
    using System.Threading.Tasks;

    public class NetworkService
    {
        public Response CheckConnection()
        {
            var client = new WebClient();

            try
            {
                using (client.OpenRead("http://clients3.google.com/generate_204"))
                {
                    return new Response
                    {
                        IsSuccess = true
                    };
                }
            }
            catch
            {
                return new Response
                {
                    IsSuccess = false,
                    Message = "Verifique a ligação à internet."
                };
            }
        }

        public async Task GetFlag(Country sel)
        {
            WebClient webClient = new WebClient();
            
            try
            {
                webClient.DownloadFileAsync(new System.Uri(sel.Flag), $@"Data\Flags\{sel.Alpha3Code.ToLower()}.svg");
            }
            catch
            {
                //TODO
            }
            finally
            {
                webClient.Dispose();
            }

        }
    }
}
