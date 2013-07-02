using FamilyLinesLib;
using GEDCOM.Net;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;

namespace KBS.FamilyLinesLib
{
    /// <summary>
    /// Folder creater.
    /// </summary>
    public static class KmlFolderFactory
    {
        #region Create Folder for Export Type

        public static KmlFolder CreateFolder(ExportType type, ExportOptions options, IEnumerable<Person> people)
        {
            if (type == ExportType.Lifetimes)
                return CreateLife(people);

            return CreateFolderWithTimeStamp(type, options, people);
        }

        /// <summary>
        /// Create folder w/o TimeStamp
        /// </summary>
        /// <param name="type"></param>
        /// <param name="options"></param>
        /// <param name="people"></param>
        /// <returns></returns>
        public static FolderOfFolder CreateFolderWithTimeStamp(ExportType type, ExportOptions options, IEnumerable<Person> people)
        {
            var children = new List<FolderOfPlaces>();
            if (options.HasFlag(ExportOptions.Births))
            {
                children.Add(CreatePlacesForOption(type, people, KBS.FamilyLinesLib.Properties.Resources.Births, "Birth"));
            }
            if (options.HasFlag(ExportOptions.Deaths))
            {
                children.Add(CreatePlacesForOption(type, people, KBS.FamilyLinesLib.Properties.Resources.Deaths, "Death"));
            }
            if (options.HasFlag(ExportOptions.Burials))
            {
                children.Add(CreatePlacesForOption(type, people, KBS.FamilyLinesLib.Properties.Resources.Burials, "Burial"));
            }
            if (options.HasFlag(ExportOptions.Cremations))
            {
                children.Add(CreatePlacesForOption(type, people, KBS.FamilyLinesLib.Properties.Resources.Cremations, "Cremation"));
            }
            if (options.HasFlag(ExportOptions.Marriages))
            {
                var places = new List<KmlPlaceMark>();
                foreach (var person in people)
                {
                    //Get filtered relationships and create anonymous type
                    var filtered = person.Relationships
                        .Where(item => item.RelationshipType == RelationshipType.Spouse &&
                            !String.IsNullOrEmpty(((SpouseRelationship)item).MarriagePlace))
                        .Select(item => new
                        {
                            FullName = person.FullName,
                            MarriagePlace = ((SpouseRelationship)item).MarriagePlace,
                            YearOfMarriage = ((SpouseRelationship)item).MarriageDate.HasValue ? ((SpouseRelationship)item).MarriageDate.Value.Year.ToString(CultureInfo.CurrentCulture) : "-",
                            Gender = person.Gender
                        });

                    if (filtered.Count() == 0)
                        continue;

                    var placeProperty = filtered.First().GetType().GetProperty("MarriagePlace");
                    places.AddRange(CreatePlacesForOption(type, filtered, KBS.FamilyLinesLib.Properties.Resources.Marriages, "Marriage").Children);
                }

                var folder = new FolderOfPlaces(KBS.FamilyLinesLib.Properties.Resources.Marriages);
                folder.Children = places.ToArray();
                children.Add(folder);
            }
            if (options.HasFlag(ExportOptions.Divorces))
            {
                var places = new List<KmlPlaceMark>();
                foreach (var person in people)
                {
                    var filtered = person.Relationships
                        .Where(item => item.RelationshipType == RelationshipType.Spouse &&
                            !String.IsNullOrEmpty(((SpouseRelationship)item).DivorcePlace))
                        .Select(item => new
                        {
                            FullName = person.FullName,
                            DivorcePlace = ((SpouseRelationship)item).DivorcePlace,
                            YearOfDivorce = ((SpouseRelationship)item).DivorceDate.HasValue ? ((SpouseRelationship)item).DivorceDate.Value.Year.ToString(CultureInfo.CurrentCulture) : "-",
                            Gender = person.Gender
                        });

                    if (filtered.Count() == 0)
                        continue;

                    var placeProperty = filtered.First().GetType().GetProperty("DivorcePlace");
                    places.AddRange(CreatePlacesForOption(type, filtered, KBS.FamilyLinesLib.Properties.Resources.Divorces, "Divorce").Children);
                }

                var folder = new FolderOfPlaces(KBS.FamilyLinesLib.Properties.Resources.Divorces);
                folder.Children = places.ToArray();
                children.Add(folder);
            }

            var name = type == ExportType.PlacesWithTimes ? Properties.Resources.Events : Properties.Resources.People; //Export type
            var result = new FolderOfFolder(name);
            result.Children = children.ToArray();

            return result;
        }

        #endregion

        #region Create Folder of Places

        /// <summary>
        /// Create folder with specific option.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="type"></param>
        /// <param name="people"></param>
        /// <param name="name"></param>
        /// <param name="option"></param>
        /// <returns></returns>
        private static FolderOfPlaces CreatePlacesForOption<T>(ExportType type, IEnumerable<T> people, string name, string option)
        {
            //Get place property : "Births"+"Place"
            var placeProperty = typeof(T).GetProperty(option + "Place");
            var filteredPeople = people.Where(item => placeProperty.GetValue(item, null) != null && !String.IsNullOrEmpty(placeProperty.GetValue(item, null).ToString()));
            //Get time property, if without time, it is null.
            var timeProperty = type == ExportType.PlacesWithTimes ? typeof(T).GetProperty("YearOf" + option) : null;
            return CreatePlaces(name,
                filteredPeople, placeProperty, timeProperty);
        }

        /// <summary>
        /// Create folder of places with specific PropertyInfos
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name"></param>
        /// <param name="people"></param>
        /// <param name="placeProperty"></param>
        /// <param name="timeProperty"></param>
        /// <param name="descriptionProperty"></param>
        /// <returns></returns>
        public static FolderOfPlaces CreatePlaces<T>(string name, IEnumerable<T> people, PropertyInfo placeProperty, PropertyInfo timeProperty = null, PropertyInfo descriptionProperty = null)
        {
            //Specific name property
            var nameProperty = typeof(T).GetProperty("FullName");
            //Specific gender property
            var genderProperty = typeof(T).GetProperty("Gender");
            //Must contain place property
            if (placeProperty == null)
                throw new ArgumentNullException("placeProperty");
            //description is place as default
            if (descriptionProperty == null)
                descriptionProperty = placeProperty;

            var result = new FolderOfPlaces(name);
            //Generate places
            var children = people.Select(item =>
                new PersonPlaceMarkWithTimeStamp(nameProperty.GetValue(item, null).ToString(),
                    placeProperty.GetValue(item, null).ToString(),
                    descriptionProperty.GetValue(item, null).ToString(),
                    (Gender)genderProperty.GetValue(item, null))
                    {
                        TimeStamp = timeProperty == null ? null : new KmlTimeStamp(timeProperty.GetValue(item, null).ToString())
                    });
            result.Children = children.ToArray();
            return result;
        }

        /// <summary>
        /// Create a folder for life.
        /// </summary>
        /// <param name="people"></param>
        /// <returns></returns>
        public static FolderOfPlaces CreateLife(IEnumerable<Person> people)
        {
            var result = new FolderOfPlaces(KBS.FamilyLinesLib.Properties.Resources.People);
            var children = new List<PersonPlaceMarkWithTimeSpan>();

            foreach (var person in people)
            {
                var place = string.Empty;

                if (!string.IsNullOrEmpty(person.BirthPlace) && string.IsNullOrEmpty(place))
                    place = person.BirthPlace;

                if (!string.IsNullOrEmpty(person.DeathPlace) && string.IsNullOrEmpty(place))
                    place = person.DeathPlace;
                children.Add(new PersonPlaceMarkWithTimeSpan(person.FullName, place, place, person.Gender) { TimeSpan = new KmlTimeSpan(person.YearOfBirth, person.YearOfDeath) });
            }
            result.Children = children.ToArray();

            return result;
        }

        #endregion
    }
}
