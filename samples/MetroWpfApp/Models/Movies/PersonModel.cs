using DevExpress.Mvvm;
using System.Diagnostics;

namespace MovieWpfApp.Models
{
    [DebuggerDisplay("Name={Name}")]
    public sealed class PersonModel : BindableBase, ICloneable<PersonModel>
    {
        #region Properties

        public required string Name
        {
            get => GetProperty(() => Name);
            set { SetProperty(() => Name, value); }
        }

        #endregion

        #region Methods

        public PersonModel Clone()
        {
            return new PersonModel() { Name = Name };
        }

        object ICloneable.Clone()
        {
            return Clone();
        }

        public override bool Equals(object? obj)
        {
            return obj is PersonModel model && string.Equals(Name, model.Name);
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }

        public override string ToString()
        {
            return Name;
        }

        #endregion
    }
}
