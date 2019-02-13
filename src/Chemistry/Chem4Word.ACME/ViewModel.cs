namespace Chem4Word.ACME
{
    public class ViewModel
    {


        public ViewModel(Model2.Model chemistryModel)
        {
            Model = chemistryModel;
        }
        #region Properties

        public  Model2.Model Model { get; }

        #endregion
    }
}
