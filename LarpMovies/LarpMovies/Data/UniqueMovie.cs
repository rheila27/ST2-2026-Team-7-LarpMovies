using LarpMovies.Models;
using Microsoft.Extensions.Validation;
using System.ComponentModel.DataAnnotations;

namespace LarpMovies.Data
{
    //validaciq za tova da ne se povtarqt filmi v bazata
    //po zaglavie i po rejisyor
    //shte vidq dali da ne gi suedinq na edno moje bi
    public abstract class UniqueMovie : ValidationAttribute
    {
        protected abstract bool Exists(IDatabase _db, string value, int bezId);

        protected abstract string PopUp { get; }

        protected override ValidationResult? IsValid(
            object? value,
            ValidationContext valid)
        {
            if (value is not string text || string.IsNullOrWhiteSpace(text))
            {
                return ValidationResult.Success;
            }

            var _db = valid.GetService(typeof(IDatabase)) as IDatabase ?? Database.Instance;

            var contact = valid.ObjectInstance as Movie;
            var bezId = contact?.Id ?? 0;

            if (!Exists(_db, text, bezId))
            {
                return ValidationResult.Success;
            }

            return new ValidationResult(
                ErrorMessage ?? PopUp,
                new[] { valid.MemberName! });
        }
    }

    public sealed class UniqueMovieTitle : UniqueMovie
    {
        protected override string PopUp =>
            "A movie with this name already exists in this database";

        protected override bool Exists(
            IDatabase _db,
            string value,
            int bezId)
        {
            return _db.TitleExists(value, bezId);
        }
    }

    public sealed class UniqueMovieDirector : UniqueMovie
    {
        protected override string PopUp =>
            "A movie with this director already exists in this database.";

        protected override bool Exists(
            IDatabase _db,
            string value,
            int bezId)
        {
            return _db.DirectorExists(value, bezId);
        }
    }
}
