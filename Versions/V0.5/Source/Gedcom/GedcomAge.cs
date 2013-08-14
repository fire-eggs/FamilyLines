/*
 *  $Id: GedcomAge.cs 200 2008-11-30 14:34:07Z davek $
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
using System.IO;
using System.Text;

namespace Gedcom
{
	
	/// <summary>
	/// Used for holding the age of an individual for a given event,
	/// this is an object rather than it just being a straight forward
	/// number to allow for vague values to be given, e.g. &lt; 10
	/// </summary>	
	public class GedcomAge
	{
		#region Variables
		
		private GedcomDatabase _database;
		
		private int _Equality = 0; // -1 if <, 0 if =, 1 if >
		
		private int _Years = -1;
		private int _Months = -1;
		private int _Days = -1;
		
		GedcomChangeDate _ChangeDate;
		
		#endregion
		
		#region Constructors
		
		public GedcomAge()
		{
		}
		
		#endregion
		
		#region Properties
							
		public GedcomDatabase Database
		{
			get { return _database; }
			set { _database = value; }
		}
		
		public int Equality
		{
			get { return _Equality; }
			set 
			{ 
				if (value != _Equality)
				{
					_Equality = value; 
					Changed();
				}
			}
		}
			
		public bool StillBorn
		{
			get
			{
				return (_Equality == 0 && _Years == 0 && _Months == 0 && _Days == 0);
			}
		}

		public bool Infant
		{
			get
			{
				return (_Equality == 0 && _Years < 1);
			}
		}
		
		public bool Child
		{
			get
			{
				return (_Equality == 0 && _Years < 8);
			}
		}
		
		public int Years
		{
			get { return _Years; }
			set 
			{
				if (value != _Years)
				{
					_Years = value; 
					Changed();
				}
			}
		}
		
		public int Months
		{
			get { return _Months; }
			set 
			{ 
				if (value != _Months)
				{
					_Months = value; 
					Changed();
				}
			}
		}
		
		public int Days
		{
			get { return _Days; }
			set 
			{ 
				if (value != _Days)
				{
					_Days = value; 
					Changed();
				}
			}
		}
			
		public GedcomChangeDate ChangeDate
		{
			get { return _ChangeDate; }
			set { _ChangeDate = value; }
		}
					
		#endregion
		
		#region Method

		public static GedcomAge Parse(string str, GedcomDatabase database)
		{
			GedcomAge age = null; 
			
			if (string.Compare(str, "INFANT", true) == 0)
			{
				age = new GedcomAge();
				age.Database = database;
				age.Equality = -1;
				age.Years = 1;
			}
			else if (string.Compare(str, "CHILD", true) == 0)
			{
				age = new GedcomAge();
				age.Database = database;
				age.Equality = -1;
				age.Years = 8;
			}
			else if (string.Compare(str, "STILLBORN", true) == 0)
			{
				age = new GedcomAge();
				age.Database = database;
				age.Equality = 0;
				age.Years = 0;
				age.Months = 0;
				age.Days = 0;
			}
			else
			{
				int equality = 0;
				int off = 0;
				if (str[0] == '<')
				{
					equality = -1;
					off = 1;
				}
				else if (str[0] == '>')
				{
					equality = 1;
					off = 1;
				}
		
				int val = -1;
				bool isAge = true;
				int year = -1;
				int month = -1;
				int day = -1;
				while ((isAge) && (off < str.Length))
				{
					char c = str[off];
					
					if (!char.IsWhiteSpace(c))
					{
						bool isDigit = char.IsDigit(c);
						
						if (val == -1 && !isDigit)
						{
							isAge  = false;
						}
						else if (isDigit)
						{
							int thisVal = val = (((int)c) - ((int)'0'));
							if (val == -1)
							{
								val = thisVal;
							}
							else
							{
								val *= 10;
								val += thisVal;
							}
						}
						else if (c == 'Y' || c == 'y')
						{
							if (year != -1)
							{
								isAge  = false;
							}
							else
							{
								year = val;
								val = -1;
							}
						}
						else if (c == 'M' || c == 'm')
						{
							if (month != -1)
							{
								isAge  = false;
							}
							else
							{
								month = val;
								val = -1;
							}
						}
						else if (c == 'D' || c == 'd')
						{
							if (day != -1)
							{
								isAge  = false;
							}
							else
							{
								day = val;
								val = -1;
							}
						}
						else
						{
							isAge  = false;
						}
					}
					off ++;
				}
				
				isAge &= (year != -1 || month != -1 || day != -1);
				
				if (isAge)
				{
					age = new GedcomAge();
					age.Database = database;
					age.Equality = equality;
					age.Years = year;
					age.Months = month;
					age.Days = day;
				}
			}
			
			return age;
		}

		protected virtual void Changed()
		{
			if (_database == null)
			{
//				System.Console.WriteLine("Changed() called on record with no database set");
//				
//				System.Diagnostics.StackTrace trace = new System.Diagnostics.StackTrace();
//				foreach (System.Diagnostics.StackFrame f in trace.GetFrames())
//				{
//					System.Console.WriteLine(f);
//				}
			}
			else if (!_database.Loading)
			{
				if (_ChangeDate == null)
				{
					_ChangeDate = new GedcomChangeDate(_database);
					// FIXME: what level?
				}
				DateTime now = DateTime.Now;
				
				_ChangeDate.Date1 = now.ToString("dd MMM yyyy");
				_ChangeDate.Time = now.ToString("hh:mm:ss");
			}
		}
				
		public void Output(TextWriter sw, int level)
		{
			sw.Write(Environment.NewLine);
			sw.Write(level);
			sw.Write(" AGE ");
			
			// never write out INFANT CHILD, this potentially loses information,
			// always write out < 1 or < 8  and includes months days if set
			
			if (StillBorn)
			{
				sw.Write("STILLBORN");
			}
			else
			{
				if (Equality < 0)
				{
					sw.Write("< ");	
				}
				else if (Equality > 0)
				{
					sw.Write("> ");
				}
				if (Years != -1)
				{
					sw.Write(Util.IntToString(Years));
					sw.Write("y ");
				}
				if (Months != -1)
				{
					sw.Write(Util.IntToString(Months));
					sw.Write("m ");
				}
				else if(Days != -1)
				{
					sw.Write(Util.IntToString(Days));
					sw.Write("d");
				}
			}
			
		}
		
		#endregion
	}
}
