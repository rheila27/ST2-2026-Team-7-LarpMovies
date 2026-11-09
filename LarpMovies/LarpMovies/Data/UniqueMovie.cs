using LarpMovies.Models;
using Microsoft.Extensions.Validation;
using System.ComponentModel.DataAnnotations;

namespace LarpMovies.Data
{
    //validaciq za tova da ne se povtarqt filmi v bazata
    //po zaglavie i po rejisyor
    public abstract class UniqueMovie : ValidationAttribute
    {
        protected abstract bool Exists(IDatabase db, string value, int bezId);
        protected abstract string PopUp { get; }

        protected override ValidationResult? IsValid(object? value, ValidationContext valid)
        {
            if (value is not string text || string.IsNullOrWhiteSpace(text))
            {
                return ValidationResult.Success;
            }

            var db = valid.GetService(typeof(IDatabase)) as IDatabase ?? Database.Instance;

            // Взимаме Id-то динамично (работи и за Movie, и за Director)
            int bezId = 0;
            var idProp = valid.ObjectType.GetProperty("Id");
            if (idProp != null)
            {
                var val = idProp.GetValue(valid.ObjectInstance);
                if (val is int intId)
                {
                    bezId = intId;
                }
            }

            if (!Exists(db, text, bezId))
            {
                return ValidationResult.Success;
            }

            return new ValidationResult(ErrorMessage ?? PopUp, new[] { valid.MemberName! });
        }
    }

    public sealed class UniqueMovieTitle : UniqueMovie
    {
        protected override string PopUp => "A movie with this name already exists in this database.";

        protected override bool Exists(IDatabase db, string value, int bezId)
        {
            return db.TitleExists(value, bezId);
        }
    }

    public sealed class UniqueMovieDirector : UniqueMovie
    {
        protected override string PopUp => "A director with this name already exists in this database.";

        protected override bool Exists(IDatabase db, string value, int bezId)
        {
            return db.DirectorExists(value, bezId);
        }
    }
}
