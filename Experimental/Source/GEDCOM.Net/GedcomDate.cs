/*
 *  $Id: GedcomDate.cs 200 2008-11-30 14:34:07Z davek $
 * 
 *  Copyright (C) 2007 David A Knight <david@ritter.demon.co.uk>
 *
 *  This program is free software; you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation; either version 2 of the License, or
 *  (at your option) any later version.
 *
 *  This program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License
 *  along with this program; if not, write to the Free Software
 *  Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA
 *
 */

using System;
using System.Globalization;
using System.IO;

namespace GEDCOM.Net
{
	
	/// <summary>
	/// Defines a date, allowing partial dates, date ranges etc.
	/// </summary>
	public class GedcomDate : GedcomRecord
	{
		#region Enums
		
		public enum GedcomDateType
		{
			Gregorian,
			Julian,
			Hebrew,
			French,
			Roman,
			Unknown
		}
		
		public enum GedcomDatePeriod
		{
			Exact,
			After,
			Before,
			Between,
			About,
			Calculated,
			Estimate,
			Interpretation,
			Range
		}
		
		#endregion
		
		#region Variables
				
		private GedcomDateType _DateType;
		private GedcomDatePeriod _DatePeriod;
		
		private string _Time;
		private string _Date1;
		private int _partsParsed1;
		private string _Date2;
		private int _partsParsed2;
				
		private DateTime? _dateTime1;
		private DateTime? _dateTime2;
		
		static Calendar _gregorian = null;
		static Calendar _julian = null;
		static Calendar _hebrew = null;
		
		private string _period = null;
		
		static readonly string[] _shortMonths = new string[] {
			"JAN", "FEB", "MAR", "APR", "MAY",
			"JUN", "JUL", "AUG", "SEP", "OCT",
			"NOV", "DEC"
		};
		// non standard
		static readonly string[] _shortMonthsPunc = new string[] {
			"JAN.", "FEB.", "MAR.", "APR.", "MAY.",
			"JUN.", "JUL.", "AUG.", "SEP.", "OCT.",
			"NOV.", "DEC."
		};
		// non standard
		static readonly string[] _shortMonthsExt = new string[] {
			"JAN", "FEB", "MAR", "APR", "MAY",
			"JUN", "JUL", "AUG", "SEPT", "OCT",
			"NOV", "DEC"
		};
		// non standard
		static readonly string[] _shortMonthsExtPunc = new string[] {
			"JAN.", "FEB.", "MAR.", "APR.", "MAY.",
			"JUN.", "JUL.", "AUG.", "SEPT.", "OCT.",
			"NOV.", "DEC."
		};
		static readonly string[] _longMonths = new string[] {
			"JANUARY", "FEBRUARY", "MARCH", "APRIL", "MAY",
			"JUNE", "JULY", "AUGUST", "SEPTEMBER", "OCTOBER",
			"NOVEMBER", "DECEMBER"
		};
		
		static readonly string[] _shortFrenMonths = new string[] {
			"VEND", "BRUM", "FRIM", "NIVO", "PLUB",
			"VENT", "GERM", "FLOR", "PRAI", "MESS",
			"THER", "FRUC", "COMP"
		};
		// non standard
		static readonly string[] _shortFrenMonthsPunc = new string[] {
			"VEND.", "BRUM.", "FRIM.", "NIVO.", "PLUB.",
			"VENT.", "GERM.", "FLOR.", "PRAI.", "MESS.",
			"THER.", "FRUC.", "COMP."
		};
		
		static readonly string[] _longFrenMonths = new string[] {
			"VENDEMIAIRE", "BRUMAIRE", "FRIMAIRE", "NIVOSE", "PLUVIOSE",
			"VENTOSE", "GERMINAL", "FLOREAL", "PRAIRIAL", "MESSIDOR",
			"THERMIDOR", "FRUCTIDOR", "JOUR_COMPLEMENTAIRS"
		};
		
		static readonly string[] _shortHebrMonths = new string[] {
			"TSH", "CSH", "KSL", "TVT", "SHV",
			"ADR", "ADS", "NSN", "IYR", "SVN",
			"TMZ", "AAV", "ELL"
		};
		// non standard
		static readonly string[] _shortHebrMonthsPunc = new string[] {
			"TSH.", "CSH.", "KSL.", "TVT.", "SHV.",
			"ADR.", "ADS.", "NSN.", "IYR.", "SVN.",
			"TMZ.", "AAV.", "ELL."
		};
		
		static readonly string[] _longHebrMonths = new string[] {
			"TISHRI", "CHESHCAN", "KISLEV", "TEVAT", "SHEVAT",
			"ADAR", "ADAR SHENI", "NISAN", "IYAR", "SIVAN",
			"TAMMUZ", "AV", "ELUL"
		};
		
		static readonly string[][] _monthNames = new string[][] { 
			_shortMonths, 
			_shortMonthsPunc,
			_shortMonthsExt,
			_shortMonthsExtPunc,
			_longMonths, 
			_shortFrenMonths, 
			_shortFrenMonthsPunc,
			_longFrenMonths,
			_shortHebrMonths,
			_shortHebrMonthsPunc,
			_longHebrMonths
		};				
				
		#endregion
		
		#region Constructors
		
		public GedcomDate()
		{
			_Date1 = string.Empty;
			_Date2 = string.Empty;
		}
		
		public GedcomDate(GedcomDatabase database) : this()
		{
			_database = database;
		}
		
		#endregion
		
		#region Properties
		
		public override GedcomRecordType RecordType
		{
			get { return GedcomRecordType.Date; }	
		}
		
		public override string GedcomTag
		{
			get { return "DATE"; }	
		}

		public GedcomDateType DateType
		{
			get { return _DateType; }
			set 
			{ 
				if (value != _DateType)
				{
					_DateType = value; 
					Changed();
				}
			}
		}
		
		public GedcomDatePeriod DatePeriod
		{
			get { return _DatePeriod; }
			set 
			{
				if (value != _DatePeriod)
				{
					_DatePeriod = value; 
					_period = null;
					Changed();
				}
			}
		}
		
		public string Time
		{
			get { return _Time; }
			set 
			{
				if (value != _Time)
				{
					_Time = value; 
					Changed();
				}
			}
		}
		
		public string Date1
		{
			get { return _Date1; }
			set 
			{ 
				if (value != _Date1)
				{
					_Date1 = value;
					_period = null;
					Changed();
				}
			}
		}
		
		public DateTime? DateTime1
		{
			get { return _dateTime1; }
		}
		
		public string Date2
		{
			get { return _Date2; }
			set 
			{ 
				if (value != _Date2)
				{
					_Date2 = value;
					_period = null;
					Changed();
				}
			}
		}
		
		public DateTime? DateTime2
		{
			get { return _dateTime2; }
		}
		
		// Util properties to get the date as a string
		
		// FIXME: cache this value, clear cache when _DatePeriod / _Date1 / _Date2 / _Time change
		public string DateString
		{
			get
			{
				string ret;
				
				if (!string.IsNullOrEmpty(Time))
				{
					ret = Period + " " + Time;	
				}
				else
				{
					ret = Period;
				}
				
				return ret;
			}
		}
		
		public string Period
		{
			get
			{
				if (_period == null)
				{
					switch (_DatePeriod)
					{
						case GedcomDatePeriod.Exact:
							_period = _Date1;
							break;
						case GedcomDatePeriod.After:
							_period = string.Format("AFT {0}", _Date1);
							break;
						case GedcomDatePeriod.Before:
							_period = string.Format("BEF {0}", _Date1);
							break;
						case GedcomDatePeriod.Between:
							// FIXME: this is a hack as we don't parse _Date2 in 
							// properly yet and just end up with it all in _Date1
							if (string.IsNullOrEmpty(_Date2))
							{
								_period = string.Format("BET {0}", _Date1);
							}
							else
							{	
								_period = string.Format("BET {0} AND {1}", _Date1, _Date2);
							}
							break;
						case GedcomDatePeriod.About:
							_period = string.Format("ABT {0}", _Date1);
							break;
						case GedcomDatePeriod.Calculated:
							_period = string.Format("CAL {0}", _Date1);
							break;
						case GedcomDatePeriod.Estimate:
							_period = string.Format("EST {0}", _Date1);
							break;
						case GedcomDatePeriod.Interpretation:
							_period = string.Format("INT {0}", _Date1);
							break;
						case GedcomDatePeriod.Range:
							// FIXME: this is a hack as we don't parse _Date2 in 
							// properly yet and just end up with it all in _Date1
							if (string.IsNullOrEmpty(_Date2))
							{
								_period = string.Format("FROM {0}", _Date1);
							}
							else
							{	
								_period = string.Format("FROM {0} TO {1}", _Date1, _Date2);
							}
							break;
					}
				}
	
				return _period;
			}
		}
		
		#endregion

		#region Methods
		
		public static bool operator < (GedcomDate a, GedcomDate b)
		{
			int ret = GedcomDate.CompareByDate(a, b);
			
			return (ret < 0);
		}
		
		public static bool operator > (GedcomDate a, GedcomDate b)
		{
			int ret = GedcomDate.CompareByDate(a, b);
			
			return (ret > 0);
		}

        public static bool operator !=(GedcomDate a, GedcomDate b)
        {
            return (!(a == b));
        }

		public static bool operator == (GedcomDate a, GedcomDate b)
		{
			bool ret = false;
			
			bool anull = object.Equals(a, null);
			bool bnull = object.Equals(b, null);
			
			if (!anull && !bnull)
			{
				ret = (GedcomDate.CompareByDate(a, b) == 0);
			}
			else if (anull && bnull)
			{
				ret = true;
			}
			
			return ret;
		}
		
		private static int CompareNullableDateTime(DateTime? dateaDate, DateTime? datebDate)
		{
			int ret = 0;
			
			if (dateaDate.HasValue && datebDate.HasValue)
			{
				ret = DateTime.Compare(dateaDate.Value, datebDate.Value);
			}
			else if (!dateaDate.HasValue)
			{
				ret = -1;
			}
			else
			{
				ret = 1;
			}
			
			return ret;
		}
		
		public static int CompareByDate(GedcomDate datea, GedcomDate dateb)
		{
            if (datea == null)
            {
                if (dateb == null)
                    return 0;
                return -1;
            }
		    if (dateb == null)
                return 1;

			int ret = CompareNullableDateTime(datea.DateTime1, dateb.DateTime1);
			
			if (ret == 0)
			{
				ret = CompareNullableDateTime(datea.DateTime2, dateb.DateTime2);
				
				if (ret == 0)
				{
					ret = string.Compare(datea.Date1, dateb.Date1, true);
					if (ret == 0)
					{
						ret = string.Compare(datea.Date2, dateb.Date2, true);
					}
				}
			}			
			
			return ret;
		}
		
		public float IsMatch(GedcomDate date)
		{
			float match = 0F;
			
			if (Date1 == date.Date1 && Date2 == date.Date2 && DatePeriod == date.DatePeriod)
			{
				match = 100.0F;			
			}
			else
			{
				// compare date components in DateTime if present
				
				float matches = 0;
				int parts = 0;
				
				DateTime? dateDate1 = date.DateTime1;
				DateTime? dateDate2 = date.DateTime2;
				
				// same type, nice and simple,
				// range is the same as between as far as we are concerned
				// for instance an Occupation could have been FROM a TO B
				// or BETWEEN a AND b
				// logic doesn't hold for one off events such a birth, but
				// then a Range doesn't make sense for those anyway so if
				// we have one should be safe to assume it is Between
				if (DateType == date.DateType && 
					(DatePeriod == date.DatePeriod||
					(DatePeriod == GedcomDatePeriod.Range && date.DatePeriod == GedcomDatePeriod.Between) ||
					(DatePeriod == GedcomDatePeriod.Between && date.DatePeriod == GedcomDatePeriod.Range)))
				{
					matches ++;
					
					// checked 1 value
					parts ++;
					
					parts += 3;
					float date1Match = DateHelper.MatchDateTimes(DateTime1, dateDate1);
					
					// correct for number of date parts parsed
					date1Match *= (_partsParsed1 / 3.0F);
					
					matches += date1Match;
					
					parts += 3;
					float date2Match= DateHelper.MatchDateTimes(DateTime2, dateDate2);
					
					// correct for number of date parts parsed
					date2Match *= (_partsParsed2 / 3.0F);
					
					matches += date2Match;
										
					match = (matches / parts) * 100.0F;
				}
			}
			
			
			
			return match;
		}
		
		public void ParseDateString(string dataString)
		{
			// clear possible Period cached value;
			_period = null;
			
			string dateType = string.Empty;
						
			if (dataString.StartsWith("@#"))
			{
				dataString = dataString.Substring(2);
				int i = dataString.IndexOf("@",2);
				if (i != -1)
				{
					dateType = dataString.Substring(0,i).ToUpper();
					dataString = dataString.Substring(i+1);	
				}
			}
				
			switch (dateType)
			{
				case "@#DGREGORIAN@":
					DateType = GedcomDateType.Gregorian;
					break;
				case "@#DJULIAN@":
					DateType = GedcomDateType.Julian;
					break;
				case "@#DHEBREW@":
					DateType = GedcomDateType.Hebrew;
					break;
				case "@#DROMAN@":
					DateType = GedcomDateType.Roman;
					break;
				case "@#DUNKNOWN@":
					DateType = GedcomDateType.Unknown;
					break;
				default:
					DateType = GedcomDateType.Gregorian;
					break;
			}
			
			// try to determine date period, let the fun begin
			string period = dataString;
			int len = 0;

            // KBR need to reset on re-parse
            DatePeriod = GedcomDatePeriod.Exact;
						
			CultureInfo culture = System.Globalization.CultureInfo.CurrentCulture;
						
			if (period.StartsWith("BEF ", true, culture))
			{
				len = "BEF ".Length;
				DatePeriod = GedcomDatePeriod.Before;
			}
			else if (period.StartsWith("AFT ", true, culture))
			{
				len = "AFT ".Length;
				DatePeriod = GedcomDatePeriod.After;
			}
			else if (period.StartsWith("BET ", true, culture))
			{
				len = "BET ".Length;
				DatePeriod = GedcomDatePeriod.Between;
			}
			else if (period.StartsWith("FROM ", true, culture))
			{
				len = "FROM ".Length;
				DatePeriod = GedcomDatePeriod.Range;
			}
			else if (period.StartsWith("TO ", true, culture))
			{
				len = "TO ".Length;				
				DatePeriod = GedcomDatePeriod.Range;
			}
			else if (period.StartsWith("ABT ", true, culture) ||
			            period.StartsWith("EST ", true, culture))
            {
            	len = "EST ".Length;
            	DatePeriod = GedcomDatePeriod.Estimate;
            }
            else if (period.StartsWith("CAL ", true, culture))
            {
            	len = "CAL ".Length;
            	DatePeriod = GedcomDatePeriod.Calculated;
            }
            else if (period.StartsWith("INT ", true, culture))
            {
            	len = "INT ".Length;
            	DatePeriod = GedcomDatePeriod.Interpretation;
            }
            // all the rest here are not valid gedcom, but have been seen
            // in gedcom files
            else if (period.StartsWith("BEF.", true, culture))
			{
				len = "BEF.".Length;
				DatePeriod = GedcomDatePeriod.Before;
			}
			else if (period.StartsWith("AFT.", true, culture))
			{
				len = "AFT.".Length;
				DatePeriod = GedcomDatePeriod.After;
			}
			else if (period.StartsWith("BET.", true, culture))
			{
				len = "BET.".Length;
				DatePeriod = GedcomDatePeriod.Between;
			}
			else if (period.StartsWith("ABT.", true, culture) ||
			            period.StartsWith("EST.", true, culture))
            {
            	len = "EST.".Length;
            	DatePeriod = GedcomDatePeriod.Estimate;
            }
            else if (period.StartsWith("CAL. ", true, culture))
            {
            	len = "CAL.".Length;
            	DatePeriod = GedcomDatePeriod.Calculated;
            }
            else if (period.StartsWith("INT. ", true, culture))
            {
            	len = "INT.".Length;
            	DatePeriod = GedcomDatePeriod.Interpretation;
            }
            // C or CIRCA isn't valid either
            // See BROSKEEP comment below, C may be due to the date
            // being set from a baptism / christening, but if that is the case
            // estimate is still reasonable to go with
            else if (period.StartsWith("C.", true, culture))
            {
            	len = "C.".Length;
            	DatePeriod = GedcomDatePeriod.Estimate;
            }
            else if (period.StartsWith("CIRCA ", true, culture))
            {
            	len = "CIRCA ".Length;
            	DatePeriod = GedcomDatePeriod.Estimate;
            }
            // BROSKEEP seems to be stupid and doesn't make proper
            // use of CAL    e.g   BU.9-6-1825  for a death date
            // means it is really the burial date that has just been
            // copied to the death date
            else if (period.StartsWith("BU.", true, culture))
            {
            	len = "BU.".Length;
            	DatePeriod = GedcomDatePeriod.Calculated;
            }
            // same with birth / baptism, as it does this is C safe being CIRCA ?
            else if (period.StartsWith("BAP.", true, culture))
            {
            	len = "BAP.".Length;
            	DatePeriod = GedcomDatePeriod.Calculated;
            }
            // Yet another non standard prefix, seen in BROSKEEP
            else if (period.StartsWith("ABOUT ", true, culture))
            {
           		len = "ABOUT ".Length;
            	DatePeriod = GedcomDatePeriod.Estimate;
            }
            // and another
            else if (period.StartsWith("BEFORE ", true, culture))
            {
           		len = "BEFORE ".Length;
            	DatePeriod = GedcomDatePeriod.Before;
            }
            // not seen but expected
            else if (period.StartsWith("AFTER ", true, culture))
            {
            	len = "AFTER ".Length;
            	DatePeriod = GedcomDatePeriod.After;
            }
            // we may also have NOT BEFORE, NOT BEF. NOT BEF
            // NOT AFTER, NOT AFT. NOT AFT  etc.
            else if (period.StartsWith("NOT BEF ", true, culture))
            {
            	len = "NOT BEF ".Length;
            	DatePeriod = GedcomDatePeriod.After;
            }
            else if (period.StartsWith("NOT BEF.", true, culture))
            {
            	len = "NOT BEF.".Length;
            	DatePeriod = GedcomDatePeriod.After;
            }
            else if (period.StartsWith("NOT BEFORE ", true, culture))
            {
            	len = "NOT BEFORE ".Length;
            	DatePeriod = GedcomDatePeriod.After;
            }
            else if (period.StartsWith("NOT AFT ", true, culture))
            {
            	len = "NOT AFT ".Length;
            	DatePeriod = GedcomDatePeriod.Before;
            }
            else if (period.StartsWith("NOT AFT.", true, culture))
            {
            	len = "NOT AFT.".Length;
            	DatePeriod = GedcomDatePeriod.Before;
            }
            else if (period.StartsWith("NOT AFTER ", true, culture))
            {
            	len = "NOT AFTER ".Length;
            	DatePeriod = GedcomDatePeriod.Before;
            }
            dataString = dataString.Substring(len).TrimStart(new char[] { ' ', '\t' });
            	
            Calendar calendar = null;
            
            switch (DateType)
            {
            	case GedcomDateType.French:
            		// FIXME: no FrenCalendar!
					Date1 = dataString;
					throw new NotImplementedException();
                    // break;
            	case GedcomDateType.Gregorian:
            		if (_gregorian == null)
            		{
            			_gregorian = new GregorianCalendar();
            		}
            		calendar = _gregorian;
					Date1 = dataString;
            		break;
            	case GedcomDateType.Hebrew:
            		if (_hebrew == null)
            		{
            			_hebrew = new HebrewCalendar();
            		}
            		calendar = _hebrew;
					Date1 = dataString;
            		break;
            	case GedcomDateType.Julian:
            		if (_julian == null)
            		{
            			_julian = new JulianCalendar();
            		}
            		calendar = _julian;
					Date1 = dataString;
            		break;
            	case GedcomDateType.Roman:
            		// FIXME: no RomanCalendar!
					Date1 = dataString;
					throw new NotImplementedException();
            		//break;
            }
            
			// FIXME: the split here accounts for large(ish) amounts of memory allocation
			// Need to do this better, ideally without any splitting.
           	string[] dateSplit = dataString.Split(new char[] { ' ', '-' }, StringSplitOptions.RemoveEmptyEntries);
           	
           	_dateTime1 = null;
           	_dateTime2 = null;
           	
           	_partsParsed1 = 0;
           	_partsParsed2 = 0;
           	
           	if (dateSplit.Length == 1)
           	{
           		_partsParsed1 = 1;
           		_partsParsed2 = 0;
           		_dateTime1 = GetDateInfo(dateSplit, 0, 1, calendar);
           	}
           	else if (dateSplit.Length == 2)
           	{
           		_partsParsed1 = 2;
           		_partsParsed2 = 0;
           	    _dateTime1 = GetDateInfo(dateSplit, 0, 2, calendar);
           	}
           	else if (dateSplit.Length == 3)
           	{
           		// day month year  or year (AND/TO) year
           	
           		if (string.Compare(dateSplit[1], "AND", true) != 0 && 
           			string.Compare(dateSplit[1], "TO", true) != 0)
           		{
           			_partsParsed1 = 1;
           			_partsParsed2 = 0;
           			_dateTime1 = GetDateInfo(dateSplit, 0, 3, calendar);
           		}
           		else
           		{
           			_partsParsed1 = 1;
           			_partsParsed2 = 1;
           			_dateTime1 = GetDateInfo(dateSplit, 0, 1, calendar);
           			
           			_dateTime2 = GetDateInfo(dateSplit, 2, 1, calendar);
           		}
           	}
           	else if (dateSplit.Length > 4)
           	{
           		// AND ?  TO ?
           		if (DatePeriod == GedcomDatePeriod.Between ||
           			DatePeriod == GedcomDatePeriod.Range)
           		{
           			// where is the AND / TO ?
           			if (string.Compare(dateSplit[1], "AND", true) == 0 || 
           				string.Compare(dateSplit[1], "TO", true) == 0)
           			{
           				_partsParsed1 = 1;
           				_partsParsed2 = 3;
           				_dateTime1 = GetDateInfo(dateSplit, 0, 1, calendar);           				
           				_dateTime2 = GetDateInfo(dateSplit, 2, 3, calendar);
           			}
           			else if (string.Compare(dateSplit[2], "AND", true) == 0 || 
           					 string.Compare(dateSplit[2], "TO", true) == 0)
           			{
           				_partsParsed1 = 2;
           				_partsParsed2 = 3;
           			    _dateTime1 = GetDateInfo(dateSplit, 0, 2, calendar);
           			    _dateTime2 = GetDateInfo(dateSplit, 3, 3, calendar);
           			}
           			// lets assume dateSplit[3] is AND / TO
           			else
           			{
           				_partsParsed1 = 3;
           				_partsParsed2 = 3;
           				_dateTime1 = GetDateInfo(dateSplit, 0, 3, calendar);
           			    _dateTime2 = GetDateInfo(dateSplit, 4, 3, calendar);
           			}
           		}
           		else
           		{
           		    // assume date is generic text
           			// can't do much with it
           		}
           	}
           	
           	if (DatePeriod == GedcomDatePeriod.Exact && 
           		_dateTime1 == null || !_dateTime1.HasValue)
           	{
           		// unable to parse, let's try some more methods
           		// as these dates are used for analysis it doesn't matter
           		// too much if the format is wrong, e.g. we read 12-11-1994
           		// as 12 NOV 1994 when it is meant as 11 DEC 1994, we don't
           		// throw away the original data at all or use these dates
           		// when writing the data back out
				// FIXME: format provider instead of null?
           		DateTime date;
           		if(DateTime.TryParseExact(dataString, new string[] { "d-M-yyyy", "M-d-yyyy", "yyyy-M-d", "d.M.yyyy", "M.d.yyyy", "yyyy.M.d" }, null, DateTimeStyles.None, out date))
           		{
					_dateTime1 = date; 
					_partsParsed1 = 3;
           			_partsParsed2 = 0;
           		}
				
           		// other values seen,  UNKNOWN, PRIVATE, DECEASED, DEAD
      			// These have probably been entered so the system
      			// it was entered on doesn't remove the event due
      			// to lack of any date entered.
      			// accept as unparsable
           		
           	}
		}
		
		private static DateTime? GetDateInfo(string[] dateSplit, int start, int num, Calendar calendar)
		{
			string year = string.Empty;
			string month = string.Empty;
			string day = string.Empty;

			DateTime? ret = null;
			
			CultureInfo culture = CultureInfo.CurrentCulture;
			
			// only parse if we have the expected number of date parts
			if (! (start != 0 && num == 3 && (dateSplit.Length < start + num)))
			{	
				if (num == 1)
				{
					// year only
	           		year = dateSplit[start];
	           		if (year.EndsWith("B.C.", true, culture))
	           		{
	           			year = year.Substring(0, year.Length - "B.C.".Length);
	           		}
				}
				else if (num == 2)
				{
					// month
	           		month = dateSplit[start];
	           		
	           		// year
	           		year = dateSplit[start + 1];
	           		if (year.EndsWith("B.C.", true, culture))
	           		{
	           			year = year.Substring(0, year.Length - "B.C.".Length);
	           		}
				}
				else if (num == 3)
				{
					// day
					day = dateSplit[start];
					
					// month
	           		month = dateSplit[start + 1];
	           		
	           		// year
	           		year = dateSplit[start + 2];
	           		if (year.EndsWith("B.C.", true, culture))
	           		{
	           			year = year.Substring(0, year.Length - "B.C.".Length);
	           		}
				}
				
				int y;
				int m;
				int d;
							
				if ((!int.TryParse(month, out m)) && month != string.Empty)
				{			
					// month name, find month number
					foreach (string[] names in _monthNames)
					{
						int i = 1;
						bool match = false;
						foreach (string monthName in names)
						{
							if (string.Compare(monthName, month, true) == 0)
							{
								match = true;
								break;
							}
							i ++;
						}
						if (match)
						{
							m = i;
							break;
						}
					}
				}
				
				int.TryParse(day, out d);
															
				// year could be of the form 1980/81
				// have 2 datetimes for each date ?
				// only having 1 won't lose the data, could prevent proper merge
				// though as the DateTime will be used for comparison
				if (year.IndexOf('/') != -1)
				{
					year = year.Substring(0, year.IndexOf('/'));
				}

				// if we have the month as > 12 then must be mm dd yyyy
				// and not dd mm yyyy
				if (m > 12)
				{
					int tmp = d;
					d = m;
					m = tmp;
				}
				
				if (int.TryParse(year, out y))
				{
					if (m == 0)
					{
						m = 1;
					}
					if (d == 0)
					{
						d = 1;
					}
					if (y == 0)
					{
						y = 1;
					}
					
					// ignore era, dates won't be bc, no way to get info back
					// that far reliably so shouldn't be an issue
	
					// try and correct for invalid dates, such as
					// in presidents.ged with 29 FEB 1634/35

				    if (m > 0 && m <= 12 && y > 0 && y < 9999)
					{
					    int daysInMonth = calendar.GetDaysInMonth(y, m);
					    if (d > daysInMonth)
						{
							d = daysInMonth;
						}
					}

				    try
					{
						ret = new DateTime(y, m, d, calendar);
					}
					catch
					{
						// if we fail to parse not much we can do, 
						// just don't provide a datetime
					}
				}
			}
			
			return ret;
		}
		
		public override void Output(TextWriter sw)
		{
			sw.Write(Environment.NewLine);
			sw.Write(Util.IntToString(Level));
			sw.Write(" DATE ");

			// only output type if it isn't the default (Gregorian)
			if (_DateType != GedcomDateType.Gregorian)
			{
				sw.Write("@#D{0}@ ", _DateType.ToString());
			}
			
			string line = Period.Replace("@", "@@");
			sw.Write(line);
			
			if (!string.IsNullOrEmpty(Time))
			{
				line = Time.Replace("@", "@@");
				
				sw.Write(Environment.NewLine);
				sw.Write("{0} TIME {1}", Util.IntToString(Level + 1), line);
			}
			
			OutputStandard(sw);
		}

		#endregion
	}
}
