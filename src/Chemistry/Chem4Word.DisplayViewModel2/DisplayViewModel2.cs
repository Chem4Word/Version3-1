using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Chem4Word.Model2;

namespace Chem4Word.DisplayViewModel2
{
    public class DisplayViewModel2
    {


        public DisplayViewModel2(Model2.Model chemistryModel)
        {
            Model = chemistryModel;
        }
        #region Properties

        public  Model2.Model Model { get; }

        #endregion
    }
}
