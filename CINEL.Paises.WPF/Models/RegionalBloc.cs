using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CINEL.Paises.WPF.Models
{
    public class RegionalBloc
    {
        public string Acronym { get; set; }
        public string Name { get; set; }
        public List<string> OtherAcronyms { get; set; }
        public List<string> OtherNames { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }
}
