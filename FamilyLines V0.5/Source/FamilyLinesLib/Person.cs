/*
 * Family.Show derived code provided under MS-PL license.
 */
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using GEDCOM.Net;
using KBS.FamilyLinesLib.Properties;

namespace KBS.FamilyLinesLib
{
    // A number of properties are deliberately written so the 'value' parameter is ignored for 'set'.
    // ReSharper disable ValueParameterNotUsed

    /// <summary>
    /// Representation for a single serializable Person.
    /// INotifyPropertyChanged allows properties of the Person class to
    /// participate as source in data bindings.
    /// </summary>
    [Serializable]
    public class Person : INotifyPropertyChanged, IEquatable<Person>, IDataErrorInfo
    {
        #region Fields and Constants

        private string id;
        private Gender gender;
        private bool isLiving;

        private Restriction restriction;

        private string firstName;
        private string lastName;
        private string prefix; // KBR add missing prefix support
        private string suffix;

        #region Historical (Family.Show) fields only kept for serialization from familyx
        private string occupation;
        private string occupationCitation;
        private string occupationSource;
        private string occupationLink;
        private string occupationCitationNote;
        private string occupationCitationActualText;

        private string education;
        private string educationCitation;
        private string educationSource;
        private string educationLink;
        private string educationCitationNote;
        private string educationCitationActualText;

        private string religion;
        private string religionCitation;
        private string religionSource;
        private string religionLink;
        private string religionCitationNote;
        private string religionCitationActualText;
        #endregion

        private DateTime? birthDate;
        private string birthDateDescriptor;
        private string birthPlace;
        private string birthCitation;
        private string birthSource;
        private string birthLink;
        private string birthCitationNote;
        private string birthCitationActualText;
 
        private DateTime? deathDate;
        private string deathDateDescriptor;
        private string deathPlace;
        private string deathCitation;
        private string deathSource;
        private string deathLink;
        private string deathCitationNote;
        private string deathCitationActualText;

        private string cremationPlace;
        private DateTime? cremationDate;
        private string cremationDateDescriptor;
        private string cremationCitation;
        private string cremationSource;
        private string cremationLink;
        private string cremationCitationNote;
        private string cremationCitationActualText;

        private string burialPlace;
        private DateTime? burialDate;
        private string burialDateDescriptor;
        private string burialCitation;
        private string burialSource;
        private string burialLink;
        private string burialCitationNote;
        private string burialCitationActualText;

        private string note;
        private readonly PhotoCollection photos;
        private readonly AttachmentCollection attachments;
        private Story story;
        private readonly RelationshipCollection relationships;

        // Events and Facts as imported from GEDCOM by Gedcom.NET.
        private readonly ObservableCollection<GEDEvent> m_events;
        private readonly ObservableCollection<GEDAttribute> m_facts;

        // Accessor for convenience converting existing code
        private GEDAttribute occupationFact;

        private int tree; // Which tree does this person belong to?

        private StringCollection m_picLinkCollection;
        private StringCollection m_picLinkDescCollection;
        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the unique identifier for each person.
        /// </summary>
        [XmlAttribute]
        public string Id
        {
            get { return id; }
            set
            {
                if (id != value)
                {
                    id = value;
                    OnPropertyChanged("Id");
                }
            }
        }

        /// <summary>
        /// Gets or sets the person's gender
        /// </summary>
        public Gender Gender
        {
            get { return gender; }
            set
            {
                if (gender != value)
                {
                    gender = value;
                    OnPropertyChanged("Gender");
                }
            }
        }

        /// <summary>
        /// Gets or sets the person's privacy level
        /// </summary>
        public Restriction Restriction
        {
            get { return restriction; }
            set
            {
                if (restriction != value)
                {
                    restriction = value;
                    OnPropertyChanged("Restriction");
                    OnPropertyChanged("HasRestriction");
                    OnPropertyChanged("IsLocked");
                    OnPropertyChanged("IsPrivate");
                    OnPropertyChanged("IsLockedIsLiving");
                    OnPropertyChanged("IsDeletable");

                }
            }
        }

        #region name details

        /// <summary>
        /// Gets first names
        /// </summary>
        public string FirstName
        {
            get { return firstName; }
            set
            {
                if (firstName != value)
                {
                    firstName = value;
                    OnPropertyChanged("FirstName");
                    OnPropertyChanged("Name");
                    OnPropertyChanged("FullName");
                }
            }

        }

        /// <summary>
        /// Gets middle names and append to first name for version 4
        /// </summary>
        public string MiddleName
        {
            get { return null; }
            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    firstName += " " + value;
                    OnPropertyChanged("FirstName");
                    OnPropertyChanged("Name");
                    OnPropertyChanged("FullName");
                }
            }
        }

        /// <summary>
        ///Gets last name
        /// </summary>
        public string LastName
        {
            get { return lastName; }
            set
            {
                if (lastName != value)
                {
                    lastName = value;
                    OnPropertyChanged("LastName");
                    OnPropertyChanged("Name");
                    OnPropertyChanged("FullName");
                }
            }
        }

        /// <summary>
        /// Gets the person's name in the format FirstName LastName.
        /// </summary>
        public string Name
        {
            get
            {
                string name = "";
                if (!string.IsNullOrEmpty(FirstName))
                    name += firstName;
                if (!string.IsNullOrEmpty(LastName))
                    name += " " + lastName;
                return name;
            }
        }

        /// <summary>
        /// Gets the person's fully qualified name: Prefix FirstName LastName Suffix
        /// </summary>
        public string FullName
        {
            get
            {
                // KBR add missing prefix support
                // KBR use trailing spaces plus Trim to deal with missing prefix/first/etc bits
                string fullName = "";
                if (!string.IsNullOrEmpty(prefix))
                    fullName += prefix + " ";
                if (!string.IsNullOrEmpty(firstName))
                    fullName += firstName + " ";
                if (!string.IsNullOrEmpty(lastName))
                    fullName += lastName + " ";
                if (!string.IsNullOrEmpty(suffix))
                    fullName += suffix;
                return fullName.Trim();
            }
        }

        /// <summary>
        /// Gets or sets the text that appears before the first name (title, etc)
        /// </summary>
        public string Prefix
        {
            get { return prefix; }
            set
            {
                if (prefix != value)
                {
                    prefix = value;
                    OnPropertyChanged("Prefix");
                    OnPropertyChanged("FullName");
                }
            }
        }

        /// <summary>
        /// Gets or sets the text that appears after the last name providing additional information about the person
        /// </summary>
        public string Suffix
        {
            get { return suffix; }
            set
            {
                if (suffix != value)
                {
                    suffix = value;
                    OnPropertyChanged("Suffix");
                    OnPropertyChanged("FullName");
                }
            }
        }

        #endregion

        #region age

        /// <summary>
        /// The age of the person.
        /// </summary>
        public int? Age
        {
            get
            {
                if (BirthDate == null)
                    return null;

                //Do not show  age  of dead person if no death date is entered.
                if (!isLiving && DeathDate == null)
                    return null;

                // Determine the age of the person based on just the year.
                DateTime startDate = BirthDate.Value;
                DateTime endDate = (IsLiving || DeathDate == null) ? DateTime.Now : DeathDate.Value;
                int age = endDate.Year - startDate.Year;

                // Compensate for the month and day of month (if they have not had a birthday this year).
                if (endDate.Month < startDate.Month ||
                    (endDate.Month == startDate.Month && endDate.Day < startDate.Day))
                    age--;

                return Math.Max(0, age);
            }
        }

        /// <summary>
        /// The age group of the person.
        /// </summary>
        [XmlIgnore]
        public AgeGroup AgeGroup
        {
            get
            {
                AgeGroup ageGroup = AgeGroup.Unknown;

                if (Age.HasValue)
                {
                    // The AgeGroup enumeration is defined later in this file. It is up to the Person
                    // class to define the ages that fall into the particular age groups  
                    if (Age >= 0 && Age < 20)
                        ageGroup = AgeGroup.Youth;
                    else if (Age >= 20 && Age < 40)
                        ageGroup = AgeGroup.Adult;
                    else if (Age >= 40 && Age < 70)
                        ageGroup = AgeGroup.MiddleAge;
                    else
                        ageGroup = AgeGroup.Senior;
                }
                return ageGroup;
            }
        }

        /// <summary>
        /// The year the person was born
        /// </summary>
        public string YearOfBirth
        {
            get
            {
                if (birthDate.HasValue)
                    return birthDate.Value.Year.ToString(CultureInfo.CurrentCulture);
                return "-";
            }
        }

        /// <summary>
        /// The year the person died
        /// </summary>
        public string YearOfDeath
        {
            get
            {
                if (deathDate.HasValue && !isLiving)
                    return deathDate.Value.Year.ToString(CultureInfo.CurrentCulture);
                return "-";
            }
        }

        /// <summary>
        /// The year burial of the person
        /// </summary>
        public string YearOfBurial
        {
            get
            {
                if (burialDate.HasValue && !isLiving)
                    return burialDate.Value.Year.ToString(CultureInfo.CurrentCulture);
                return "-";
            }
        }


        /// <summary>
        /// The year cremation of the person
        /// </summary>
        public string YearOfCremation
        {
            get
            {
                if (cremationDate.HasValue && !isLiving)
                    return cremationDate.Value.Year.ToString(CultureInfo.CurrentCulture);
                return "-";
            }
        }

        /// <summary>
        /// Gets or sets whether the person is still alive or deceased.
        /// </summary>
        public bool IsLiving
        {
            get
            {
                if (Individual != null && Individual.Dead != !isLiving)
                    Debug.WriteLine(Name + ": death mismatch");
                return isLiving;
            }
            set
            {
                if (isLiving != value)
                {
                    isLiving = value;
                    OnPropertyChanged("IsLiving");
                    OnPropertyChanged("Age");
                    OnPropertyChanged("IsLockedIsLiving");
                }
            }
        }

        #endregion

        #region birth details

        [XmlIgnore]
        private string _localBirthDateString;
       
        /// <summary>
        /// A string variant of the BirthDate for GUI entry and validation.
        /// </summary>
        [XmlIgnore] 
        public string BirthDateString
        {
            get
            {
                return _localBirthDateString;
            }
            set
            {
                _localBirthDateString = value;
                DateTime test = IsValidDate(value);
                if (test != DateTime.MinValue)
                {
                    BirthDate = test;
                }
                OnPropertyChanged("BirthDateString");
            }
        }

        [Localizable(false)] 
        private readonly string[] validFormats = {
                                            "d", "yyyy",
                                            "M-d-yyyy", "M/d/yyyy",
                                            "d-M-yyyy", "d/M/yyyy",
                                            "MM-d-yyyy", "MM/d/yyyy",
                                            "d-MM-yyyy", "d/MM/yyyy",
                                            "MMM-d-yyyy", "MMM/d/yyyy",
                                            "d-MMM-yyyy", "d/MMM/yyyy"
                                        };
        private DateTime IsValidDate(string value)
        {
            DateTime testDate;
            if (DateTime.TryParseExact(value, validFormats, CultureInfo.CurrentCulture, DateTimeStyles.None, out testDate))
                return testDate;
            return DateTime.MinValue;
        }

        /// <summary>
        /// Gets or sets the person's birth date.  This property can be null.
        /// </summary>
        public DateTime? BirthDate
        {
            get { return birthDate; }
            set
            {
                if (birthDate == null || birthDate != value)
                {
                    birthDate = value;
                    if (birthDate.HasValue)
                        _localBirthDateString = birthDate.Value.ToShortDateString();
                    OnPropertyChanged("BirthDate");
                    OnPropertyChanged("Age");
                    OnPropertyChanged("AgeGroup");
                    OnPropertyChanged("YearOfBirth");
                    OnPropertyChanged("BirthMonthAndDay");
                    OnPropertyChanged("BirthDateAndPlace");
                    OnPropertyChanged("BirthDateString");
                }
            }
        }

        /// <summary>
        /// Gets or sets the person's birth date descriptor [ABT, AFT, BEF]
        /// </summary>
        public string BirthDateDescriptor
        {
            get { return birthDateDescriptor; }
            set
            {
                if (birthDateDescriptor == null || birthDateDescriptor != value)
                {
                    birthDateDescriptor = value;
                    OnPropertyChanged("BirthDateDescriptor");
                }
            }
        }

        /// <summary>
        /// Gets or sets the person's place of birth [1 BIRT\2 PLAC]
        /// </summary>
        public string BirthPlace
        {
            get { return birthPlace; }
            set
            {
                if (birthPlace != value)
                {
                    birthPlace = value;
                    OnPropertyChanged("BirthPlace");
                    OnPropertyChanged("BirthDateAndPlace");
                    OnPropertyChanged("HasBirthPlace");
                }
            }
        }

        /// <summary>
        /// Gets or sets the person's birth citation [1 BIRT/2 SOUR/3 PAGE]
        /// </summary>
        public string BirthCitation
        {
            get { return birthCitation; }
            set
            {
                if (birthCitation != value)
                {
                    birthCitation = value;
                    OnPropertyChanged("BirthCitation");
                    OnPropertyChanged("HasCitations");
                }
            }
        }

        /// <summary>
        /// Gets or sets the person's birth source [1 BIRT\2 SOUR]
        /// </summary>
        public string BirthSource
        {
            get { return birthSource; }
            set
            {
                if (birthSource != value)
                {
                    birthSource = value;
                    OnPropertyChanged("BirthSource");
                    OnPropertyChanged("HasCitations");
                }
            }
        }

        /// <summary>
        /// Gets or sets the person's birth link [1 BIRT\2 SOUR\3 OBJE where type is URL,
        /// or [URL from 1 BIRT\2 SOUR\3 NOTE]
        /// </summary>
        public string BirthLink
        {
            get { return birthLink; }
            set
            {
                if (birthLink != value)
                {
                    birthLink = value;
                    OnPropertyChanged("BirthLink");
                }
            }
        }

        /// <summary>
        /// Gets or sets the person's birth citation note [1 BIRT/2 SOUR/3 NOTE]
        /// </summary>
        public string BirthCitationNote
        {
            get { return birthCitationNote; }
            set
            {
                if (birthCitationNote != value)
                {
                    birthCitationNote = value;
                    OnPropertyChanged("BirthCitationNote");
                }
            }
        }

        /// <summary>
        /// Gets or sets the person's birth citation actual text
        /// </summary>
        public string BirthCitationActualText
        {
            get { return birthCitationActualText; }
            set
            {
                if (birthCitationActualText != value)
                {
                    birthCitationActualText = value;
                    OnPropertyChanged("BirthCitationActualText");
                }
            }
        }

        /// <summary>
        /// Gets the month and day the person was born in. This property can be null.
        /// </summary>
        [XmlIgnore]
        public string BirthMonthAndDay
        {
            get
            {
                if (birthDate == null || DateTimeFormatInfo.CurrentInfo == null)
                    return null;
                return birthDate.Value.ToString(
                    DateTimeFormatInfo.CurrentInfo.MonthDayPattern,
                    CultureInfo.CurrentCulture);
            }
        }

        /// <summary>
        /// Gets a friendly string for BirthDate and Place
        /// </summary>
        [XmlIgnore]
        public string BirthDateAndPlace
        {
            get
            {
                if (birthDate == null || DateTimeFormatInfo.CurrentInfo == null)
                    return null;

                var returnValue = new StringBuilder();
                returnValue.Append("Born "); // TODO LOCALIZATION ISSUE
                returnValue.Append(
                    birthDate.Value.ToString(
                        DateTimeFormatInfo.CurrentInfo.ShortDatePattern,
                        CultureInfo.CurrentCulture));

                if (!string.IsNullOrEmpty(birthPlace))
                {
                    returnValue.Append(", ");
                    returnValue.Append(birthPlace);
                }

                return returnValue.ToString();
            }
        }

        #endregion

        #region death details

        [XmlIgnore]
        private string _localDeathDateString;

        /// <summary>
        /// A string variant of the DeathDate for GUI entry and validation.
        /// </summary>
        [XmlIgnore]
        public string DeathDateString
        {
            get
            {
                return _localDeathDateString;
            }
            set
            {
                _localDeathDateString = value;
                DateTime test = IsValidDate(value);
                if (test != DateTime.MinValue)
                {
                    DeathDate = test;
                }
                OnPropertyChanged("DeathDateString");
            }
        }

        /// <summary>
        /// Gets or sets the person's death of death.  This property can be null.
        /// </summary>
        public DateTime? DeathDate
        {
            get { return deathDate; }
            set
            {
                if (deathDate != value)
                {
                    deathDate = value;
                    if (deathDate.HasValue)
                        _localDeathDateString = deathDate.Value.ToShortDateString();
                    OnPropertyChanged("DeathDate");
                    OnPropertyChanged("Age");
                    OnPropertyChanged("YearOfDeath");
                    OnPropertyChanged("DeathDateString");
                }
            }
        }

        /// <summary>
        /// Gets or sets the person's death date descriptor
        /// </summary>
        public string DeathDateDescriptor
        {
            get { return deathDateDescriptor; }
            set
            {
                if (deathDateDescriptor == null || deathDateDescriptor != value)
                {
                    deathDateDescriptor = value;
                    OnPropertyChanged("DeathDateDescriptor");
                }
            }
        }

        /// <summary>
        /// Gets or sets the person's place of death
        /// </summary>
        public string DeathPlace
        {
            get { return deathPlace; }
            set
            {
                if (deathPlace != value)
                {
                    deathPlace = value;
                    OnPropertyChanged("DeathPlace");
                    OnPropertyChanged("HasDeathPlace");
                }
            }
        }

        /// <summary>
        /// Gets or sets the person's place of death citation
        /// </summary>
        public string DeathCitation
        {
            get { return deathCitation; }
            set
            {
                if (deathCitation != value)
                {
                    deathCitation = value;
                    OnPropertyChanged("DeathCitation");
                    OnPropertyChanged("HasCitations");
                }
            }
        }

        /// <summary>
        /// Gets or sets the person's place of death source
        /// </summary>
        public string DeathSource
        {
            get
            {
                if (Individual != null && Individual.Death != null && Individual.Death.Sources.Count > 0)
                {
                    var src = Individual.Death.Sources[0]; // TODO more than 1
                    var rec = Individual.Database[src.Source] as GedcomSourceRecord;
                    return rec == null ? "" : rec.Title;
                }
                return deathSource;
            }
            set
            {
                if (deathSource != value)
                {
                    deathSource = value;
                    OnPropertyChanged("DeathSource");
                }
            }
        }

        /// <summary>
        /// Gets or sets the person's death link
        /// </summary>
        public string DeathLink
        {
            get { return deathLink; }
            set
            {
                if (deathLink != value)
                {
                    deathLink = value;
                    OnPropertyChanged("DeathLink");
                }
            }
        }

        /// <summary>
        /// Gets or sets the person's death citation note
        /// </summary>
        public string DeathCitationNote
        {
            get { return deathCitationNote; }
            set
            {
                if (deathCitationNote != value)
                {
                    deathCitationNote = value;
                    OnPropertyChanged("DeathCitationNote");
                }
            }
        }

        /// <summary>
        /// Gets or sets the person's death citation actual text
        /// </summary>
        public string DeathCitationActualText
        {
            get { return deathCitationActualText; }
            set
            {
                if (deathCitationActualText != value)
                {
                    deathCitationActualText = value;
                    OnPropertyChanged("DeathCitationActualText");
                }
            }
        }

        #endregion

        #region cremation details

        /// <summary>
        /// Gets or sets cremation place
        /// </summary>
        public string CremationPlace
        {
            get { return cremationPlace; }
            set
            {
                if (cremationPlace != value)
                {
                    cremationPlace = value;
                    OnPropertyChanged("CremationPlace");
                    OnPropertyChanged("HasCremationPlace");
                }
            }
        }

        /// <summary>
        /// Gets or sets cremation date
        /// </summary>
        public DateTime? CremationDate
        {
            get { return cremationDate; }
            set
            {
                if (cremationDate == null || cremationDate != value)
                {
                    cremationDate = value;
                    OnPropertyChanged("CremationDate");
                }
            }
        }

        /// <summary>
        /// Gets or sets the person's cremation date descriptor
        /// </summary>
        public string CremationDateDescriptor
        {
            get { return cremationDateDescriptor; }
            set
            {
                if (cremationDateDescriptor == null || cremationDateDescriptor != value)
                {
                    cremationDateDescriptor = value;
                    OnPropertyChanged("CremationDateDescriptor");
                }
            }
        }

        /// <summary>
        /// Gets or sets the person's cremation citation
        /// </summary>
        public string CremationCitation
        {
            get { return cremationCitation; }
            set
            {
                if (cremationCitation != value)
                {
                    cremationCitation = value;
                    OnPropertyChanged("CremationCitation");
                    OnPropertyChanged("HasCitations");
                }
            }
        }

        /// <summary>
        /// Gets or sets the person's cremation source
        /// </summary>
        public string CremationSource
        {
            get { return cremationSource; }
            set
            {
                if (cremationSource != value)
                {
                    cremationSource = value;
                    OnPropertyChanged("CremationSource");
                    OnPropertyChanged("HasCitations");
                }
            }
        }

        /// <summary>
        /// Gets or sets the person's cremation link
        /// </summary>
        public string CremationLink
        {
            get { return cremationLink; }
            set
            {
                if (cremationLink != value)
                {
                    cremationLink = value;
                    OnPropertyChanged("CremationLink");
                }
            }
        }

        /// <summary>
        /// Gets or sets the person's cremation citation note
        /// </summary>
        public string CremationCitationNote
        {
            get { return cremationCitationNote; }
            set
            {
                if (cremationCitationNote != value)
                {
                    cremationCitationNote = value;
                    OnPropertyChanged("CremationCitationNote");
                }
            }
        }

        /// <summary>
        /// Gets or sets the person's cremation citation actual text
        /// </summary>
        public string CremationCitationActualText
        {
            get { return cremationCitationActualText; }
            set
            {
                if (cremationCitationActualText != value)
                {
                    cremationCitationActualText = value;
                    OnPropertyChanged("CremationCitationActualText");
                }
            }
        }

        #endregion

        #region burial details

        /// <summary>
        /// Gets or sets burial place
        /// </summary>
        public string BurialPlace
        {
            get { return burialPlace; }
            set
            {
                if (burialPlace != value)
                {
                    burialPlace = value;
                    OnPropertyChanged("BurialPlace");
                    OnPropertyChanged("HasBurialPlace");
                }
            }
        }

        /// <summary>
        /// Gets or sets the person's burial citation
        /// </summary>
        public string BurialCitation
        {
            get { return burialCitation; }
            set
            {
                if (burialCitation != value)
                {
                    burialCitation = value;
                    OnPropertyChanged("BurialCitation");
                    OnPropertyChanged("HasCitations");
                }
            }
        }

        /// <summary>
        /// Gets or sets the person's burial source
        /// </summary>
        public string BurialSource
        {
            get { return burialSource; }
            set
            {
                if (burialSource != value)
                {
                    burialSource = value;
                    OnPropertyChanged("BurialSource");
                    OnPropertyChanged("HasCitations");
                }
            }
        }

        /// <summary>
        /// Gets or sets the person's burial link
        /// </summary>
        public string BurialLink
        {
            get { return burialLink; }
            set
            {
                if (burialLink != value)
                {
                    burialLink = value;
                    OnPropertyChanged("BurialLink");
                }
            }
        }

        /// <summary>
        /// Gets or sets burial date
        /// </summary>
        public DateTime? BurialDate
        {
            get { return burialDate; }
            set
            {
                if (burialDate == null || burialDate != value)
                {
                    burialDate = value;
                    OnPropertyChanged("BurialDate");
                }
            }
        }

        /// <summary>
        /// Gets or sets the person's burial date descriptor
        /// </summary>
        public string BurialDateDescriptor
        {
            get { return burialDateDescriptor; }
            set
            {
                if (burialDateDescriptor == null || burialDateDescriptor != value)
                {
                    burialDateDescriptor = value;
                    OnPropertyChanged("BurialDateDescriptor");
                }
            }
        }

        /// <summary>
        /// Gets or sets the person's burial citation note
        /// </summary>
        public string BurialCitationNote
        {
            get { return burialCitationNote; }
            set
            {
                if (burialCitationNote != value)
                {
                    burialCitationNote = value;
                    OnPropertyChanged("BurialCitationNote");
                }
            }
        }

        /// <summary>
        /// Gets or sets the person's burial citation actual text
        /// </summary>
        public string BurialCitationActualText
        {
            get { return burialCitationActualText; }
            set
            {
                if (burialCitationActualText != value)
                {
                    burialCitationActualText = value;
                    OnPropertyChanged("BurialCitationActualText");
                }
            }
        }

        #endregion

        #region occupation details

        public GEDAttribute OccupationFact
        {
            get
            {
                return occupationFact;
            }
            set
            {
                occupationFact = value;
                // TODO update fact list
            }
        }

        /// <summary>
        /// Gets or sets the person's occupation
        /// </summary>
        public string Occupation
        {
            get { return occupation; }
            set
            {
                if (occupation != value)
                {
                    occupation = value;
                    OnPropertyChanged("Occupation");
                }
            }
        }

        /// <summary>
        /// Gets or sets the person's occupation link
        /// </summary>
        public string OccupationLink
        {
            get { return occupationLink; }
            set
            {
                if (occupationLink != value)
                {
                    occupationLink = value;
                    OnPropertyChanged("OccupationLink");
                }
            }
        }

        /// <summary>
        /// Gets or sets the person's occupation citation
        /// </summary>
        public string OccupationCitation
        {
            get { return occupationCitation; }
            set
            {
                if (occupationCitation != value)
                {
                    occupationCitation = value;
                    OnPropertyChanged("OccupationCitation");
                    OnPropertyChanged("HasCitations");
                }
            }
        }

        /// <summary>
        /// Gets or sets the person's occupation source
        /// </summary>
        public string OccupationSource
        {
            get { return occupationSource; }
            set
            {
                if (occupationSource != value)
                {
                    occupationSource = value;
                    OnPropertyChanged("OccupationSource");
                    OnPropertyChanged("HasCitations");
                }
            }
        }

        /// <summary>
        /// Gets or sets the person's occupation citation note
        /// </summary>
        public string OccupationCitationNote
        {
            get { return occupationCitationNote; }
            set
            {
                if (occupationCitationNote != value)
                {
                    occupationCitationNote = value;
                    OnPropertyChanged("OccupationCitationNote");
                }
            }
        }

        /// <summary>
        /// Gets or sets the person's occupation citation actual text
        /// </summary>
        public string OccupationCitationActualText
        {
            get { return occupationCitationActualText; }
            set
            {
                if (occupationCitationActualText != value)
                {
                    occupationCitationActualText = value;
                    OnPropertyChanged("OccupationCitationActualText");
                }
            }
        }

        #endregion

        #region education details


        /// <summary>
        /// Gets or sets the person's education
        /// </summary>
        public string Education
        {
            get { return education; }
            set
            {
                if (education != value)
                {
                    education = value;
                    OnPropertyChanged("Education");
                }
            }
        }

        /// <summary>
        /// Gets or sets the person's education citation
        /// </summary>
        public string EducationCitation
        {
            get { return educationCitation; }
            set
            {
                if (educationCitation != value)
                {
                    educationCitation = value;
                    OnPropertyChanged("EducationCitation");
                    OnPropertyChanged("HasCitations");
                }
            }
        }

        /// <summary>
        /// Gets or sets the person's education source
        /// </summary>
        public string EducationSource
        {
            get { return educationSource; }
            set
            {
                if (educationSource != value)
                {
                    educationSource = value;
                    OnPropertyChanged("EducationSource");
                    OnPropertyChanged("HasCitations");
                }
            }
        }

        /// <summary>
        /// Gets or sets the person's education link
        /// </summary>
        public string EducationLink
        {
            get { return educationLink; }
            set
            {
                if (educationLink != value)
                {
                    educationLink = value;
                    OnPropertyChanged("EducationLink");
                }
            }
        }

        /// <summary>
        /// Gets or sets the person's education citation note
        /// </summary>
        public string EducationCitationNote
        {
            get { return educationCitationNote; }
            set
            {
                if (educationCitationNote != value)
                {
                    educationCitationNote = value;
                    OnPropertyChanged("EducationCitationNote");
                }
            }
        }

        /// <summary>
        /// Gets or sets the person's education citation actual text
        /// </summary>
        public string EducationCitationActualText
        {
            get { return educationCitationActualText; }
            set
            {
                if (educationCitationActualText != value)
                {
                    educationCitationActualText = value;
                    OnPropertyChanged("EducationCitationActualText");
                }
            }
        }

        #endregion

        #region religion details

        /// <summary>
        /// Gets or sets the person's religion
        /// </summary>
        public string Religion
        {
            get { return religion; }
            set
            {
                if (religion != value)
                {
                    religion = value;
                    OnPropertyChanged("Religion");
                }
            }
        }

        /// <summary>
        /// Gets or sets the person's religion citation
        /// </summary>
        public string ReligionCitation
        {
            get { return religionCitation; }
            set
            {
                if (religionCitation != value)
                {
                    religionCitation = value;
                    OnPropertyChanged("ReligionCitation");
                    OnPropertyChanged("HasCitations");
                }
            }
        }

        /// <summary>
        /// Gets or sets the person's religion link
        /// </summary>
        public string ReligionLink
        {
            get { return religionLink; }
            set
            {
                if (religionLink != value)
                {
                    religionLink = value;
                    OnPropertyChanged("ReligionLink");
                }
            }
        }

        /// <summary>
        /// Gets or sets the person's religion Source
        /// </summary>
        public string ReligionSource
        {
            get { return religionSource; }
            set
            {
                if (religionSource != value)
                {
                    religionSource = value;
                    OnPropertyChanged("ReligionSource");
                    OnPropertyChanged("HasCitations");
                }
            }
        }

        /// <summary>
        /// Gets or sets the person's religion citation note
        /// </summary>
        public string ReligionCitationNote
        {
            get { return religionCitationNote; }
            set
            {
                if (religionCitationNote != value)
                {
                    religionCitationNote = value;
                    OnPropertyChanged("ReligionCitationNote");
                }
            }
        }

        /// <summary>
        /// Gets or sets the person's religion citation actual text
        /// </summary>
        public string ReligionCitationActualText
        {
            get { return religionCitationActualText; }
            set
            {
                if (religionCitationActualText != value)
                {
                    religionCitationActualText = value;
                    OnPropertyChanged("ReligionCitationActualText");
                }
            }
        }

        #endregion

        #region other details

        /// <summary>
        /// Gets or sets the person's note  (field)
        /// </summary>
        public string Note
        {
            get { return note; }
            set
            {
                if (note != value)
                {
                    note = value;
                    OnPropertyChanged("Note");
                    OnPropertyChanged("HasNote");
                }
            }
        }

        /// <summary>
        /// Gets the person's story file (rich text)
        /// </summary>
        public Story Story
        {
            get { return story; }
            set
            {
                if (story != value)
                {
                    story = value;
                    OnPropertyChanged("Story");
                    OnPropertyChanged("HasNote");
                }
            }
        }

        public StringCollection Links
        {
            get { return m_picLinkCollection; }
            set
            {
                if (m_picLinkCollection != value)
                {
                    m_picLinkCollection = value;
                    OnPropertyChanged("Links");
                }
            }
        }

        public StringCollection LinkDesc
        {
            get { return m_picLinkDescCollection; }
            set
            {
                if (m_picLinkDescCollection != value)
                {
                    m_picLinkDescCollection = value;
                    OnPropertyChanged("LinkDescs");
                }
            }
        }
        /// <summary>
        /// Gets or sets the photos associated with the person
        /// </summary>
        public PhotoCollection Photos
        {
            get 
            { 
                OnPropertyChanged("HasPhotos");
                return photos;
            }
        }

        /// <summary>
        /// Gets or sets the attachments associated with the person
        /// </summary>
        public AttachmentCollection Attachments
        {
            get
            {
                OnPropertyChanged("HasAttachments");
                OnPropertyChanged("AttachmentList");
                return attachments;
            }
        }

        /// <summary>
        /// Gets or sets the person's graphical identity
        /// </summary>
        [XmlIgnore, System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "value")]
        public string Avatar
        {
            get
            {
                if (photos != null && photos.Count > 0)
                {
                    foreach (Photo photo in photos)
                    {
                        if (photo.IsAvatar)
                            return photo.FullyQualifiedPath;
                    }
                }

                return "";
            }
            set
            {
                // This setter is used for change notification.
                OnPropertyChanged("Avatar");
                OnPropertyChanged("HasAvatar");
            }
        }

        /// <summary>
        /// Determines whether a person is deletable.
        /// </summary>
        [XmlIgnore]
        public bool IsDeletable
        {
            get
            {
                // This person is locked so you cannot delete them
                if (restriction == Restriction.Locked)
                    return false;

                // With a few exceptions, anyone with less than 3 relationships is deletable
                if (relationships.Count < 3)
                {
                    // The person has 2 spouses. Since they connect their spouses, they are not deletable.
                    if (Spouses.Count == 2)
                        return false;

                    // The person is connecting two generations
                    if (Parents.Count == 1 && Children.Count == 1)
                        return false;

                    // The person is connecting inlaws
                    if (Parents.Count == 1 && Spouses.Count == 1)
                        return false;

                    // Anyone else with less than 3 relationships is deletable
                    return true;
                }

                // More than 3 relationships, however the relationships are from only Children. 
                if (Children.Count > 0 && Parents.Count == 0 && Siblings.Count == 0 && Spouses.Count == 0)
                    return true;

                // More than 3 relationships. The relationships are from siblings. Deletable since siblings are connected to each other or the parent.
                if (Siblings.Count > 0 && Parents.Count >= 0 && Spouses.Count == 0 && Children.Count == 0)
                    return true;

                // This person has complicated dependencies that does not allow deletion.
                return false;
            }
        }

        #endregion

        [XmlIgnore]
        public int Tree
        {
            get { return tree; }
            set 
            { 
                if (tree != value)
                {
                    tree = value;
                    OnPropertyChanged("Tree");
                } 
            }
        }

        #region relationship details

        /// <summary>
        /// Collections of relationship connection for the person
        /// </summary>
        public RelationshipCollection Relationships
        {
            get { return relationships; }
        }

        /// <summary>
        /// Accessor for the person's spouse(s)
        /// </summary>
        [XmlIgnore]
        public Collection<Person> Spouses
        {
            get
            {
                Collection<Person> spouses = new Collection<Person>();
                foreach (Relationship rel in relationships)
                {
                    if (rel.RelationshipType == RelationshipType.Spouse)
                        spouses.Add(rel.RelationTo);
                }
                return spouses;
            }
        }

        [XmlIgnore]
        public Collection<Person> CurrentSpouses
        {
            get
            {
                Collection<Person> spouses = new Collection<Person>();
                foreach (Relationship rel in relationships)
                {
                    if (rel.RelationshipType == RelationshipType.Spouse)
                    {
                        SpouseRelationship spouseRel = rel as SpouseRelationship;
                        if (spouseRel != null && spouseRel.SpouseModifier == SpouseModifier.Current)
                            spouses.Add(rel.RelationTo);
                    }
                }
                return spouses;
            }
        }

        [XmlIgnore]
        public Collection<Person> PreviousSpouses
        {
            get
            {
                Collection<Person> spouses = new Collection<Person>();
                foreach (Relationship rel in relationships)
                {
                    if (rel.RelationshipType == RelationshipType.Spouse)
                    {
                        SpouseRelationship spouseRel = rel as SpouseRelationship;
                        if (spouseRel != null && spouseRel.SpouseModifier == SpouseModifier.Former)
                            spouses.Add(rel.RelationTo);
                    }
                }
                return spouses;
            }
        }

        /// <summary>
        /// Accessor for the person's children
        /// </summary>
        [XmlIgnore]
        public Collection<Person> Children
        {
            get
            {
                Collection<Person> children = new Collection<Person>();
                foreach (Relationship rel in relationships)
                {
                    if (rel.RelationshipType == RelationshipType.Child)
                        children.Add(rel.RelationTo);
                }
                return children;
            }
        }

        [XmlIgnore]
        public Collection<Person> NaturalChildren
        {
            get
            {
                Collection<Person> children = new Collection<Person>();
                foreach (Relationship rel in relationships)
                {
                    if (rel.RelationshipType == RelationshipType.Child)
                    {
                        ChildRelationship childRel = rel as ChildRelationship;
                        if (childRel != null && childRel.ParentChildModifier == ParentChildModifier.Natural)
                            children.Add(rel.RelationTo);
                    }
                }
                return children;
            }
        }

        [XmlIgnore]
        public Collection<Person> AdoptedChildren
        {
            get
            {
                Collection<Person> children = new Collection<Person>();
                foreach (Relationship rel in relationships)
                {
                    if (rel.RelationshipType == RelationshipType.Child)
                    {
                        ChildRelationship childRel = rel as ChildRelationship;
                        if (childRel != null && childRel.ParentChildModifier == ParentChildModifier.Adopted)
                            children.Add(rel.RelationTo);
                    }
                }
                return children;
            }
        }

        [XmlIgnore]
        public Collection<Person> FosteredChildren
        {
            get
            {
                Collection<Person> children = new Collection<Person>();
                foreach (Relationship rel in relationships)
                {
                    if (rel.RelationshipType == RelationshipType.Child)
                    {
                        ChildRelationship childRel = rel as ChildRelationship;
                        if (childRel != null && childRel.ParentChildModifier == ParentChildModifier.Foster)
                            children.Add(rel.RelationTo);
                    }
                }
                return children;
            }
        }

        /// <summary>
        /// Accessor for the person's natural parents
        /// </summary>
        [XmlIgnore]
        public Collection<Person> NaturalParents
        {
            get
            {
                Collection<Person> parents = new Collection<Person>();
                foreach (Relationship rel in relationships)
                {

                    if (rel.RelationshipType == RelationshipType.Parent)
                    {
                        ParentRelationship parentRel = rel as ParentRelationship;
                        if (parentRel != null && parentRel.ParentChildModifier == ParentChildModifier.Natural)
                            parents.Add(rel.RelationTo);
                    }
                }
                return parents;
            }
        }


        /// <summary>
        /// Accessor for all of the person's parents
        /// </summary>
        [XmlIgnore]
        public Collection<Person> Parents
        {
            get
            {
                Collection<Person> parents = new Collection<Person>();
                foreach (Relationship rel in relationships)
                {
                    if (rel.RelationshipType == RelationshipType.Parent)
                        parents.Add(rel.RelationTo);
                }
                return parents;
            }
        }

        /// <summary>
        /// Accessor for the person's siblings
        /// </summary>
        [XmlIgnore]
        public Collection<Person> Siblings
        {
            get
            {
                Collection<Person> siblings = new Collection<Person>();
                foreach (Relationship rel in relationships)
                {
                    if (rel.RelationshipType == RelationshipType.Sibling)
                        siblings.Add(rel.RelationTo);
                }
                return siblings;
            }
        }

        /// <summary>
        /// Accessor for the person's half siblings. A half sibling is a person
        /// that contains one or more same parents as the person, but does not 
        /// contain all of the same parents.
        /// </summary>
        [XmlIgnore]
        public Collection<Person> HalfSiblings
        {
            get
            {
                // List that is returned.
                Collection<Person> halfSiblings = new Collection<Person>();

                // Get list of full siblings (a full sibling cannot be a half sibling).
                Collection<Person> siblings = Siblings;

                // Iterate through each parent, and determine if the parent's children
                // are half siblings.
                foreach (Person parent in Parents)
                {
                    foreach (Person child in parent.Children)
                    {
                        if (child != this && !siblings.Contains(child) &&
                            !halfSiblings.Contains(child))
                        {
                            halfSiblings.Add(child);
                        }
                    }
                }

                return halfSiblings;
            }
        }

        /// <summary>
        /// Get the person's parents as a ParentSet object
        /// </summary>
        [XmlIgnore]
        public ParentSet ParentSet
        {
            get
            {
                // Only need to get the parent set if there are two parents.
                if (Parents.Count == 2)
                {
                    ParentSet parentSet = new ParentSet(Parents[0], Parents[1]);
                    return parentSet;
                }
                return null;
            }
        }

        /// <summary>
        /// Get the possible combination of parents when editting this person or adding this person's sibling.
        /// </summary>
        [XmlIgnore]
        public ParentSetCollection PossibleParentSets
        {
            get
            {
                ParentSetCollection parentSets = new ParentSetCollection();

                foreach (Person parent in Parents)
                {
                    foreach (Person spouse in parent.Spouses)
                    {
                        ParentSet ps = new ParentSet(parent, spouse);

                        // Don't add the same parent set twice.
                        if (!parentSets.Contains(ps))
                            parentSets.Add(ps);
                    }
                }

                return parentSets;
            }
        }

#endregion

        #region "Has" variables

        /// <summary>
        /// Calculated property that returns parent information
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "value")]
        [XmlIgnore]
        public bool HasParents
        {
            get
            {
                return (Parents.Count >= 1);
            }
            set
            {
                // This setter is used for change notification.
                OnPropertyChanged("HasParents");
                if (Parents.Count >= 2)  //There must be at least 2 parents for a parent set
                {
                    OnPropertyChanged("PossibleParentSets");
                }
                OnPropertyChanged("Parents");
            }
        }


        /// <summary>
        /// Calculated property that returns true if a person has a restriction
        /// </summary>
        [XmlIgnore]
        public bool HasRestriction
        {
            get
            {
                return (restriction==Restriction.Locked || restriction==Restriction.Private);
            }
            set
            {
                // This setter is used for change notification.
                OnPropertyChanged("HasRestriction");
            }
        }

        /// <summary>
        /// Gets or sets whether a death related field is editable
        /// Returns false if the person is not locked or is dead.
        /// </summary>
        [XmlIgnore]
        public bool IsLockedIsLiving
        {
            get { return (restriction == Restriction.Locked || isLiving ); }
            set
            {
                OnPropertyChanged("IsLockedIsLiving");
            }
        }

        /// <summary>
        /// Gets or sets whether the a non death related field is editable
        /// Returns false if the person is not locked
        /// </summary>
        [XmlIgnore]
        public bool IsLocked
        {
            get { return (restriction == Restriction.Locked); }
            set
            {
                OnPropertyChanged("IsLocked");
            }
        }

        /// <summary>
        /// Returns true if the person is private
        /// </summary>
        [XmlIgnore]
        public bool IsPrivate
        {
            get { return (restriction == Restriction.Private); }
            set
            {
                OnPropertyChanged("IsPrivate");
            }
        }

        /// <summary>
        /// Calculated property that returns if a person has siblings
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "value")]
        [XmlIgnore]
        public bool HasSiblings
        {
            get
            {
                return (Siblings.Count >= 1);
            }
            set
            {
                // This setter is used for change notification.
                OnPropertyChanged("HasSiblings");
                OnPropertyChanged("Siblings");
            }
        }

        /// <summary>
        /// Calculated property that returns whether the person has 1 or more spouse(s).
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "value")]
        [XmlIgnore]
        public bool HasSpouse
        {
            get
            {
                return (Spouses.Count >= 1);
            }
            set
            {
                // This setter is used for change notification.
                OnPropertyChanged("HasSpouse");
                OnPropertyChanged("Spouses");
            }
        }

        /// <summary>
        /// Calculated property that returns whether the person has an avatar photo.
        /// </summary>
        [XmlIgnore]
        public bool HasAvatar
        {
            get
            {
                if (photos != null && photos.Count > 0)
                {
                    foreach (Photo photo in photos)
                    {
                        if (photo.IsAvatar)
                            return true;
                    }
                }

                return false;
            }
        }

        /// <summary>
        /// Calculated property that returns whether the person has any photos
        /// </summary>
        [XmlIgnore]
        public bool HasPhoto
        {
            get
            {
                return (photos.Count > 0);
            }
        }

        /// <summary>
        /// Calculated property that returns whether the person has a birth place
        /// </summary>
        [XmlIgnore]
        public bool HasBirthPlace
        {
            get { return !(string.IsNullOrEmpty(birthPlace)); }
        }

        /// <summary>
        /// Calculated property that returns whether the person has a death place
        /// </summary>
        [XmlIgnore]
        public bool HasDeathPlace
        {
            get
            {
                return !(string.IsNullOrEmpty(deathPlace));
            }
        }

        /// <summary>
        /// Calculated property that returns whether the person has a cremation place
        /// </summary>
        [XmlIgnore]
        public bool HasCremationPlace
        {
            get
            {
                return !(string.IsNullOrEmpty(cremationPlace));
            }
        }

        /// <summary>
        /// Calculated property that returns whether the person has a burial place
        /// </summary>
        [XmlIgnore]
        public bool HasBurialPlace
        {
            get
            {
                return !(string.IsNullOrEmpty(burialPlace));
            }
        }

        /// <summary>
        /// Calculated property that returns whether the person has any complete citations
        /// </summary>
        [XmlIgnore]
        public bool HasCitations
        {
            get
            {
                if ( ( string.IsNullOrEmpty(religionSource) || string.IsNullOrEmpty(religionCitation)) &&
                     ( string.IsNullOrEmpty(burialSource) || string.IsNullOrEmpty(burialCitation)) &&
                     ( string.IsNullOrEmpty(deathSource) || string.IsNullOrEmpty(deathCitation)) &&
                     ( string.IsNullOrEmpty(educationSource) || string.IsNullOrEmpty(educationCitation)) &&
                     ( string.IsNullOrEmpty(birthSource) || string.IsNullOrEmpty(birthCitation)) &&
                     ( string.IsNullOrEmpty(occupationSource) || string.IsNullOrEmpty(occupationCitation)) &&
                     ( string.IsNullOrEmpty(cremationSource) || string.IsNullOrEmpty(cremationCitation)) )                 
                    return false;
                return true;
            }
        }

        /// <summary>
        /// Calculated property that returns whether the person has a note
        /// </summary>
        [XmlIgnore]
        public bool HasNote
        {
            get
            {
                return !string.IsNullOrEmpty(note);
            }
        }

        /// <summary>
        /// Calculated property that returns whether the person has any attachments
        /// </summary>
        [XmlIgnore]
        public bool HasAttachments
        {
            get
            {
                return (attachments.Count > 0);
            }
        }

        #endregion

        #region Relationship text

        /// <summary>
        /// Calculated property that returns string text for this person's parents
        /// </summary>
        [XmlIgnore]
        public string ParentsText
        {
            get
            {
                string[] parentsTexts = {"", ""};

                int parentCount = 0;
//                string parentsText = string.Empty;
                foreach (Relationship rel in relationships)
                {
                    if (rel.RelationshipType == RelationshipType.Parent)
                    {
                        ParentRelationship parents = rel as ParentRelationship;
                        if (parents != null && parents.ParentChildModifier == ParentChildModifier.Natural)
                        {
                            if (parentCount == 0)
                            {
                                parentsTexts[0] = parents.PersonFullName;
//                                parentsText += parents.PersonFullName;
                            }
                            if (parentCount == 1)
                            {
                                parentsTexts[1] = parents.PersonFullName;
//                                parentsText += " " + Resources.And + " " + parents.PersonFullName;
                            }
                            parentCount += 1;

                            if (parentCount > 1)
                                break; // TODO no support for three or more natural parents
                        }
                    }
                }

                //if (!string.IsNullOrEmpty(parentsText))
                //    return " " + Resources.Of + " " + parentsText;

                switch (gender)
                {
                    case Gender.Male:
                        {
                            switch (parentCount)
                            {
                                case 1:
                                    return string.Format(Resources.SonP1, parentsTexts[0]);
                                case 2:
                                    return string.Format(Resources.SonP2, parentsTexts[0], parentsTexts[1]);
                                default:
                                    return Resources.Son;
                            }
                        }
                    case Gender.Female:
                        {
                            switch (parentCount)
                            {
                                case 1:
                                    return string.Format(Resources.DaughterP1, parentsTexts[0]);
                                case 2:
                                    return string.Format(Resources.DaughterP2, parentsTexts[0], parentsTexts[1]);
                                default:
                                    return Resources.Daughter;
                            }
                        }
                    default:
                        {
                            switch (parentCount)
                            {
                                case 1:
                                    return string.Format(Resources.UnkP1, parentsTexts[0]);
                                case 2:
                                    return string.Format(Resources.UnkP2, parentsTexts[0], parentsTexts[1]);
                                default:
                                    return Resources.Unknown;
                            }
                        }
                }
            }
        }

        /// <summary>
        /// Calculated property that returns string text for this person's siblings
        /// </summary>
        [XmlIgnore]
        public string SiblingsText
        {
            get
            {
                switch (Siblings.Count)
                {
                    case 0:
                        return "";
                    case 1:
                        return sibSex1(Siblings[0]);
                    default:
                        return sibSexMany(Siblings);
                }
            }
        }

        /// <summary>
        /// Handle 2 or more siblings.
        /// </summary>
        /// <param name="siblings"></param>
        /// <returns></returns>
        private string sibSexMany(Collection<Person> siblings)
        {
            int sibCount = siblings.Count;
            string val;
            switch (gender)
            {
                case Gender.Male:
                    val = string.Format(Resources.BrotherToBase, siblings[0].Name);
                    break;
                case Gender.Female:
                    val = string.Format(Resources.SisterToBase, siblings[0].Name);
                    break;
                default:
                    val = string.Format(Resources.UnknownToBase, siblings[0].Name);
                    break;
            }

            for (int i = 1; i < sibCount-1; i++)
            {
                val += string.Format(Resources.AndName, siblings[i].Name);
            }
            val += string.Format(Resources.AndNamePeriod, siblings[sibCount - 1].Name);
            return val;
        }

        /// <summary>
        /// Special case of only one sibling.
        /// </summary>
        /// <param name="person"></param>
        /// <returns></returns>
        private string sibSex1(Person person)
        {
            switch (gender)
            {
                case Gender.Male:
                    return string.Format(Resources.BrotherTo, person.Name);
                case Gender.Female:
                    return string.Format(Resources.SisterTo, person.Name);
                default:
                    return string.Format(Resources.UnknownTo, person.Name);
            }
        }

        /// <summary>
        /// Calculated property that returns string that describes this person to their spouses.
        /// </summary>
        [XmlIgnore]
        public string SpouseRelationshipText
        {
            get
            {
                // TODO just use 'partner'?
                switch (gender)
                {
                    case Gender.Male:
                        return Resources.Husband;
                    case Gender.Female:
                        return Resources.Wife;
                    default:
                        return "Unknown"; // TODO embedded string
                }
            }
        }

        /// <summary>
        /// Calculated property that returns string text for this person's spouses.
        /// </summary>
        [XmlIgnore]
        public string SpousesText
        {
            get
            {
                Collection<Person> spouses = Spouses;

                string spousesText = string.Empty;
                if (spouses.Count > 0)
                {
                    spousesText = spouses[0].Name;

                    if (spouses.Count == 2)
                        spousesText += " " + Resources.And + " " + spouses[1].Name;
                    else
                    {
                        for (int i = 1; i < spouses.Count; i++)
                        {
                            if (i == spouses.Count - 1)
                                spousesText += ", " + Resources.And + " " + spouses[i].Name;
                            else
                                spousesText += ", " + spouses[i].Name;
                        }
                    }
                }
                if (!string.IsNullOrEmpty(spousesText))
                    return " " + Resources.To + " " + spousesText;
                return spousesText;
            }
        }

        /// <summary>
        /// Calculated property that returns string that describes this person to their children.
        /// </summary>
        [XmlIgnore]
        public string ChildRelationshipText
        {
            get
            {
                switch (gender)
                {
                    case Gender.Male:
                        return Resources.Father;
                    case Gender.Female:
                        return Resources.Mother;
                    default:
                        return "Unknown"; // TODO embedded string
                }
            }
        }

        /// <summary>
        /// Calculated property that returns string text for this person's children.
        /// </summary>
        [XmlIgnore]
        public string ChildrenText
        {
            get
            {
                int i = 1;
                string childrensText = string.Empty;
                foreach (Relationship rel in relationships)
                {

                    if (rel.RelationshipType == RelationshipType.Child)
                    {

                        ChildRelationship children = rel as ChildRelationship;
                        if (children != null && children.ParentChildModifier == ParentChildModifier.Natural)
                        {
                            if (i == 1)
                                childrensText += children.PersonFullName;
                            if (i != 1)
                                childrensText += " " + Resources.And + " " + children.PersonFullName;
                            i += 1;
                        }
                    }

                }

                if (!string.IsNullOrEmpty(childrensText))
                    return " " + Resources.To + " " + childrensText;
                return childrensText;
            }

        }

        #endregion

        #region Events and Attributes (Version 2)

        public ObservableCollection<GEDEvent> Events
        {
            get { return m_events; }
        }

        public ObservableCollection<GEDAttribute> Facts
        {
            get { return m_facts; }
        }

        #endregion
        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new instance of a person object.
        /// Each new instance will be given a unique identifier.
        /// This parameterless constructor is also required for serialization.
        /// </summary>
        public Person()
        {
            id = Guid.NewGuid().ToString();
            relationships = new RelationshipCollection();
            photos = new PhotoCollection();
            attachments = new AttachmentCollection();
            firstName = Resources.Unknown;
            isLiving = true;
            restriction = Restriction.None;
            tree = -1;

            m_events = new ObservableCollection<GEDEvent>();
            m_facts = new ObservableCollection<GEDAttribute>();
            m_picLinkCollection = new StringCollection();
            m_picLinkDescCollection = new StringCollection();
        }

        /// <summary>
        /// Creates a new instance of the person class with the firstname and the lastname.
        /// </summary>
        public Person(string firstNames, string _lastName)
            : this()
        {
            //Use the first name if specified, if not, the default first name is used.
            if (!string.IsNullOrEmpty(firstNames))
                firstName = firstNames;

            lastName = _lastName;
        }

        /// <summary>
        /// Creates a new instance of the person class with the firstname, the lastname, and gender
        /// </summary>
        public Person(string firstName, string lastName, Gender _gender)
            : this(firstName, lastName)
        {
            gender = _gender;
        }

        // TODO MOVE THIS TO GEDCOMIMPORT??
        // KBR 01/30/2013 A ctor which builds an internal class from the Gedcom.Net instance
        // At this time, the Gedcom.Net instance is "read-only".
        public Person(GedcomIndividualRecord indiv)
            : this()
        {
            Individual = indiv;

            gender = indiv.Sex;

            var names = indiv.GetName();
            if (names != null)
            {
                firstName = indiv.FirstName;
                lastName = indiv.LastName;

                prefix = indiv.GetName().Prefix;
                suffix = indiv.GetName().Suffix;
            }

            id = indiv.XRefID;

            switch (indiv.RestrictionNotice)
            {
                case GedcomRestrictionNotice.Locked:
                    Restriction = Restriction.Locked;
                    break;
                case GedcomRestrictionNotice.Privacy:
                    Restriction = Restriction.Private; // TODO does 'private' mean 'not visible'?
                    break;
                default:
                    Restriction = Restriction.None;
                    break;
            }

            isLiving = !indiv.Dead;

            // TODO spacing/paragraph issues in displayed notes
            // TODO more than one note!
            if (indiv.Notes != null && indiv.Notes.Count > 0)
            {
                var _note = indiv.Notes[0];

                var gnNoteRec = indiv.Database[_note] as GedcomNoteRecord;
                note = gnNoteRec != null ? gnNoteRec.Text : _note;
            }

            // TODO replicate for events/facts/families/marriages?
            // Photos and attachments
            bool firstPhoto = true;
            foreach (var mmediaID in indiv.Multimedia)
            {
                var mmedia = indiv.Database[mmediaID];
                var mmRec = mmedia as GedcomMultimediaRecord;
                if (mmRec != null)
                {
                    foreach (var gnMMediaFile in mmRec.Files)
                    {
                        // This'll be true for URLs
                        if (gnMMediaFile.Format == "URL") // Do NOT translate
                        {
                            LinkDesc.Add(mmRec.Title);
                            Links.Add(gnMMediaFile.Filename);
                        }
                        else
                        {
                            if (gnMMediaFile.Basepath == null || gnMMediaFile.Filename == null)
                                continue;

                            var filePath = Path.Combine(gnMMediaFile.Basepath, gnMMediaFile.Filename);
                            if (File.Exists(filePath))
                            {
                                // TODO extend supported photos
                                if (App.IsPhotoFileSupported(filePath))
                                {
                                    Photo photo = new Photo(filePath) { IsAvatar = firstPhoto };
                                    firstPhoto = false;
                                    Photos.Add(photo);
                                }

                                // TODO extend supported attachments
                                else if (App.IsAttachmentFileSupported(filePath))
                                {
                                    Attachment attachment = new Attachment(filePath);
                                    Attachments.Add(attachment);
                                }

                                // TODO store all other file types for later re-export
                            }
                        }
                    }
                }
            }

            // TODO transfer to m_events

            // TODO this should be turned into a generalized 'Event'
            if (indiv.Birth != null)
            {
                // TODO this is a hack: code separates birth date/descriptor everywhere
                // TODO use gedcomdate
                birthDate = null;
                if (indiv.Birth.Date != null)
                {
                    birthDate = indiv.Birth.Date.DateTime1;
                    birthDateDescriptor = indiv.Birth.Date.PeriodAbbrev; // TODO some values don't translate
                }

                birthPlace = indiv.Birth.Place != null ? indiv.Birth.Place.Name : "";

                if (indiv.Birth.Sources != null && indiv.Birth.Sources.Count > 0)
                {
                    var src = indiv.Birth.Sources[0];
                    birthCitationActualText = src.Text; // TODO: src.Text or src2.TextText?
                    var src2 = src.Database[src.Source] as GedcomSourceRecord;
                    if (src2 != null)
                    {
                        birthSource = src2.Title;
                    }
                    if (src.Notes != null && src.Notes.Count > 0)
                    {
                        birthCitationNote = src.Notes[0];
                    }
                }
            }

            if (indiv.Death != null)
            {
                // TODO this is a hack: code separates death date/descriptor everywhere
                // TODO use gedcomdate
                deathDate = null;
                if (indiv.Death.Date != null)
                {
                    deathDate = indiv.Death.Date.DateTime1;
                    deathDateDescriptor = indiv.Death.Date.PeriodAbbrev; // TODO some values don't translate
                }

                deathPlace = indiv.Death.Place != null ? indiv.Death.Place.Name : "";

                if (indiv.Death.Sources != null && indiv.Death.Sources.Count > 0)
                {
                    var src = indiv.Death.Sources[0];
                    deathCitationActualText = src.Text; // TODO: src.Text or src2.TextText?
                    var src2 = src.Database[src.Source] as GedcomSourceRecord;
                    if (src2 != null)
                    {
                        deathSource = src2.Title;
                    }
                    if (src.Notes != null && src.Notes.Count > 0)
                    {
                        deathCitationNote = src.Notes[0];
                    }
                }
            }

            foreach (var individualEvent in indiv.Attributes)
                m_facts.Add(new GEDAttribute(individualEvent));

            foreach (var individualEvent in indiv.Events)
                m_events.Add(new GEDEvent(individualEvent));
        }

        #endregion

        #region INotifyPropertyChanged Members

        /// <summary>
        /// INotifyPropertyChanged requires a property called PropertyChanged.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Fires the event for the property when it changes.
        /// </summary>
        [Localizable(false)]
        public virtual void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        #region IEquatable Members

        /// <summary>
        /// Determine equality between two person classes
        /// </summary>
        public bool Equals(Person other)
        {
            return (Id == other.Id);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Get the spouse relationship for the specified spouse.
        /// </summary>
        public SpouseRelationship GetSpouseRelationship(Person spouse)
        {
            foreach (Relationship relationship in relationships)
            {
                SpouseRelationship spouseRelationship = relationship as SpouseRelationship;
                if (spouseRelationship != null)
                {
                    if (spouseRelationship.RelationTo.Equals(spouse))
                        return spouseRelationship;
                }
            }

            return null;
        }

        public ChildRelationship GetParentChildRelationship(Person child)
        {
            foreach (Relationship relationship in relationships)
            {
                ChildRelationship childRelationship = relationship as ChildRelationship;
                if (childRelationship != null)
                {
                    if (childRelationship.RelationTo.Equals(child))
                        return childRelationship;
                }
            }

            return null;
        }

        /// <summary>
        /// Gets the combination of parent sets for this person and his/her spouses
        /// </summary>
        /// <returns></returns>
        public ParentSetCollection MakeParentSets()
        {
            ParentSetCollection parentSets = new ParentSetCollection();

            foreach (Person spouse in Spouses)
            {
                ParentSet ps = new ParentSet(this, spouse);

                // Don't add the same parent set twice.
                if (!parentSets.Contains(ps))
                    parentSets.Add(ps);
            }

            return parentSets;
        }

        /// <summary>
        /// Called to delete the person's story
        /// </summary>
        public void DeleteStory()
        {
            if (story != null)
            {
                story.Delete();
                story = null;
            }
        }

        public override string ToString()
        {
            return Name;
        }

        // KBR NOTE: Experimental for KML
        public Collection<SpouseRelationship> GetMarriages()
        {
            var marrs = new Collection<SpouseRelationship>();
            foreach (var relationship in relationships)
            {
                var spouseRelationship = relationship as SpouseRelationship;
                if (spouseRelationship != null && spouseRelationship.MarriageDate.HasValue)
                {
                    marrs.Add(spouseRelationship);
                }
            }
            return marrs;
        }


        #endregion

        #region IDataErrorInfo Members

        public string Error
        {
            get { return null; }
        }

        public string this[string columnName]
        {
            get
            {
                string result = "";
                switch (columnName)
                {
                    case "BirthDateString":
                        if (_localBirthDateString != null &&
                            _localBirthDateString.Trim().Length != 0 &&
                            IsValidDate(_localBirthDateString) == DateTime.MinValue)
                            result = Resources.InvalidDate;
                        break;
                    case "BirthDate":
                        if (BirthDate == DateTime.MinValue)
                            result = Resources.InvalidDate;
                        break;
                    case "DeathDateString":
                        if (_localDeathDateString != null &&
                            _localDeathDateString.Trim().Length != 0 &&
                            IsValidDate(_localDeathDateString) == DateTime.MinValue)
                            result = Resources.InvalidDate;
                        break;
                    case "DeathDate":
                        if (DeathDate == DateTime.MinValue)
                            result = Resources.InvalidDate;
                        break;
                    case "CremationDate":
                        if (CremationDate == DateTime.MinValue)
                            result = Resources.InvalidDate;
                        break;
                    case "BurialDate":
                        if (BurialDate == DateTime.MinValue)
                            result = Resources.InvalidDate;
                        break;
                }
                return result;
            }
        }

        #endregion

        [XmlIgnore]
        public GedcomIndividualRecord Individual { get; set; }

        [XmlIgnore]
        public bool HasEvents { get { return m_events.Count > 0; } }

        [XmlIgnore]
        public bool HasFacts { get { return m_facts.Count > 0; } }

        public IList<GEDEvent> GetEvents(GedcomEvent.GedcomEventType evType)
        {
            // TODO custom events
            return m_events.Where(gedEvent => gedEvent.Type == evType).ToList();
        }

        public IList<GEDAttribute> GetFacts(GedcomEvent.GedcomEventType evType)
        {
            // BUG opened windsor.familyx, m_facts not empty but m_facts[0] is null
            // TODO custom attributes
            return m_facts.Where(gedEvent => gedEvent.Type == evType).ToList();
        }

        /// <summary>
        /// Called to validate the person's data - TODO extend for issue 1538(?)
        /// </summary>
        public void Validate()
        {
            if (!isLiving) 
                return;

            // 1. If any of the current spouses died over 100 yrs ago, this person should not be living:
            foreach (Person curSpouse in CurrentSpouses)
            {
                if (curSpouse.DeathDate != null)
                {
                    if ((DateTime.Now.Year - curSpouse.DeathDate.Value.Year) > 100)
                    {
                        isLiving = false;
                        break;
                    }
                }
            }

            // 2. If this person has a child who died over 100 years ago, this person should not be living:
            foreach (Person child in Children)
            {
                if (child.DeathDate != null)
                {
                    if ((DateTime.Now.Year - child.DeathDate.Value.Year) > 100)
                    {
                        isLiving = false;
                        break;
                    }
                }
            }

        }
    }

    // ReSharper restore ValueParameterNotUsed

    /// <summary>
    /// Enumeration of the person's restriction
    /// </summary>
    public enum Restriction
    {
        None, Locked, Private
    }

    /// <summary>
    /// Enumeration of the person's age group
    /// </summary>
    public enum AgeGroup
    {
        Unknown, Youth, Adult, MiddleAge, Senior
    }

    /// <summary>
    /// Representation for a Parent couple.  E.g. Bob and Sue
    /// </summary>
    public class ParentSet : IEquatable<ParentSet>
    {
        private Person firstParent;

        private Person secondParent;

        public Person FirstParent
        {
            get { return firstParent; }
            set { firstParent = value; }
        }

        public Person SecondParent
        {
            get { return secondParent; }
            set { secondParent = value; }
        }

        public ParentSet(Person firstParent, Person secondParent)
        {
            this.firstParent = firstParent;
            this.secondParent = secondParent;
        }

        public string Name
        {
            get
            {
                string name = "";
                name += firstParent.Name + " + " + secondParent.Name;
                return name;
            }
        }

        // Parameterless contstructor required for serialization
        public ParentSet() { }

        #region IEquatable<ParentSet> Members

        /// <summary>
        /// Determine equality between two ParentSet classes.  Note: Bob and Sue == Sue and Bob
        /// </summary>
        public bool Equals(ParentSet other)
        {
            if (other != null)
            {
                if (firstParent.Equals(other.firstParent) && secondParent.Equals(other.secondParent))
                    return true;

                if (firstParent.Equals(other.secondParent) && secondParent.Equals(other.firstParent))
                    return true;
            }

            return false;
        }

        #endregion
    }

    /// <summary>
    /// Collection of ParentSet objects.
    /// </summary>
    public class ParentSetCollection : Collection<ParentSet> { }
}
