/*
 *  $Id: GedcomAddress.cs 200 2008-11-30 14:34:07Z davek $
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
using System.Xml;

namespace Gedcom
{
	
	/// <summary>
	/// Stores details of an address
	/// </summary>
	public class GedcomAddress
	{
		#region Variables
		
		GedcomDatabase _database;
		
		private string _AddressLine;
		private string _AddressLine1;
		private string _AddressLine2;
		private string _AddressLine3;
		
		private string _City;
		private string _State;
		private string _PostCode;
		private string _Country;
		
		private string _Phone1;
		private string _Phone2;
		private string _Phone3;
		
		private string _Email1;
		private string _Email2;
		private string _Email3;
		
		private string _Fax1;
		private string _Fax2;
		private string _Fax3;
		
		private string _Www1;
		private string _Www2;
		private string _Www3;
		
		GedcomChangeDate _ChangeDate;
		
		#endregion
		
		#region Constructors
		
		/// <summary>
		/// Create a new GedcomAddress
		/// </summary>
		public GedcomAddress()
		{
		}
		
		#endregion
		
		#region Properties
		
		/// <value>
		/// The database the address is in
		/// </value>
		public GedcomDatabase Database
		{
			get { return _database; }
			set { _database = value; }
		}
		
		/// <value>
		/// A complete address as a single line
		/// </value>
		public string AddressLine
		{
			get { return _AddressLine; }
			set 
			{
				if (value != _AddressLine)
				{
					_AddressLine = value; 
					Changed();
				}
			}
		}
		
		/// <value>
		/// The first line in an address
		/// </value>
		public string AddressLine1
		{
			get { return _AddressLine1; }
			set 
			{
				if (value != _AddressLine1)
				{
					_AddressLine1 = value; 
					Changed();
				}
			}		
		}
		
		/// <value>
		/// The second line in an address
		/// </value>
		public string AddressLine2
		{
			get { return _AddressLine2; }
			set 
			{
				if (value != _AddressLine2)
				{
					_AddressLine2 = value; 
					Changed();
				}
			}			
		}
		
		/// <value>
		/// The third linein an address
		/// </value>
		public string AddressLine3
		{
			get { return _AddressLine3; }
			set 
			{
				if (value != _AddressLine3)
				{
					_AddressLine3 = value; 
					Changed();
				}
			}	
		}
		
		/// <value>
		/// The city for the address
		/// </value>
		public string City
		{
			get { return _City; }
			set 
			{
				if (value != _City)
				{
					_City = value; 
					Changed();
				}
			}
		}
		
		/// <value>
		/// The state or county for the address
		/// </value>
		public string State
		{
			get { return _State; }
			set 
			{ 
				if (value != _State)
				{
					_State = value; 
					Changed();
				}
			}
		}
		
		/// <value>
		/// The PostCode / zip code for the address
		/// </value>
		public string PostCode
		{
			get { return _PostCode; }
			set 
			{
				if (value != _PostCode)
				{
					_PostCode = value; 
					Changed();
				}
			}
		}
		
		/// <value>
		/// The country the address is in
		/// </value>
		public string Country
		{
			get { return _Country; }
			set 
			{ 
				if (value != _Country)
				{
					_Country = value; 
					Changed();
				}
			}
		}
		
		/// <value>
		/// Main phone number
		/// </value>
		public string Phone1
		{
			get { return _Phone1; }
			set
			{ 
				if (value != _Phone1)
				{
					_Phone1 = value;
					Changed();
				}
			}
		}
		
		/// <value>
		/// Secondary phone number
		/// </value>
		public string Phone2
		{
			get { return _Phone2; }
			set 
			{ 
				if (value != _Phone2)
				{
					_Phone2 = value; 
					Changed();
				}
			}
		}
		
		/// <value>
		/// Tertiary phone number
		/// </value>
		public string Phone3
		{
			get { return _Phone3; }
			set 
			{ 
				if (value != _Phone3)
				{
					_Phone3 = value; 
					Changed();
				}
			}
		}

		/// <value>
		/// Main email address 
		/// </value>
		public string Email1
		{
			get { return _Email1; }
			set 
			{ 
				if (value != _Email1)
				{
					_Email1 = value; 
					Changed();
				}
			}
		}
		
		/// <value>
		/// Secondary email address
		/// </value>
		public string Email2
		{
			get { return _Email2; }
			set 
			{ 
				if (value != _Email2)
				{
					_Email2 = value; 
					Changed();
				}
			}
		}
		
		/// <value>
		/// Tertiary email address
		/// </value>
		public string Email3
		{
			get { return _Email3; }
			set 
			{ 
				if (value != _Email3)
				{
					_Email3 = value; 
					Changed();
				}
			}
		}
		
		/// <value>
		/// Main fax number
		/// </value>
		public string Fax1
		{
			get { return _Fax1; }
			set 
			{
				if (value != _Fax1)
				{
					_Fax1 = value; 
					Changed();
				}
			}
		}
		
		/// <value>
		/// Secondary fax number
		/// </value>
		public string Fax2
		{
			get { return _Fax2; }
			set 
			{ 
				if (value != _Fax2)
				{
					_Fax2 = value; 
					Changed();
				}
			}
		}
		
		/// <value>
		/// Tertiary fax number
		/// </value>
		public string Fax3
		{
			get { return _Fax3; }
			set 
			{ 
				if (value != _Fax3)
				{
					_Fax3 = value; 
					Changed();
				}
			}
		}
		
		/// <value>
		/// Main website URI
		/// </value>
		public string Www1
		{
			get { return _Www1; }
			set 
			{ 
				if (value != _Www1)
				{
					_Www1 = value; 
					Changed();
				}
			}
		}
		
		/// <value>
		/// Secondary website URI
		/// </value>
		public string Www2
		{
			get { return _Www2; }
			set 
			{
				if (value != _Www2)
				{
					_Www2 = value; 
					Changed();
				}
			}
		}
		
		/// <value>
		/// Tertiary website URI
		/// </value>
		public string Www3
		{
			get { return _Www3; }
			set 
			{
				if (value != _Www3)
				{
					_Www3 = value; 
					Changed();
				}
			}
		}

		/// <value>
		/// The date the address was changed
		/// </value>
		public GedcomChangeDate ChangeDate
		{
			get { return _ChangeDate; }
			set { _ChangeDate = value; }
		}
				
		#endregion
		
		#region Methods
		
		private void Changed()
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

		
		/// <summary>
		/// Add the GEDCOM 6 XML elements for the data in this object as child
		/// nodes of the given root.
		/// </summary>
		/// <param name="root">
		/// A <see cref="XmlNode"/>
		/// </param>
		public void GenerateXML(XmlNode root)
		{
			XmlDocument doc = root.OwnerDocument;
		
			XmlNode node = doc.CreateElement("MailAddress");
			
			root.AppendChild(node);
			
			if (!string.IsNullOrEmpty(_Phone1))
			{
				node = doc.CreateElement("Phone");
				node.AppendChild(doc.CreateTextNode(_Phone1));
				root.AppendChild(node);
			}
			if (!string.IsNullOrEmpty(_Phone2))
			{
				node = doc.CreateElement("Phone");
				node.AppendChild(doc.CreateTextNode(_Phone2));
				root.AppendChild(node);
			}
			if (!string.IsNullOrEmpty(_Phone3))
			{
				node = doc.CreateElement("Phone");
				node.AppendChild(doc.CreateTextNode(_Phone3));
				root.AppendChild(node);
			}
			
			if (!string.IsNullOrEmpty(_Email1))
			{
				node = doc.CreateElement("Email");
				node.AppendChild(doc.CreateTextNode(_Email1));
				root.AppendChild(node);
			}
			if (!string.IsNullOrEmpty(_Email2))
			{
				node = doc.CreateElement("Email");
				node.AppendChild(doc.CreateTextNode(_Email2));
				root.AppendChild(node);
			}
			if (!string.IsNullOrEmpty(_Email3))
			{
				node = doc.CreateElement("Email");
				node.AppendChild(doc.CreateTextNode(_Email3));
				root.AppendChild(node);
			}
			
			if (!string.IsNullOrEmpty(_Www1))
			{
				node = doc.CreateElement("URI");
				node.AppendChild(doc.CreateTextNode(_Www1));
				root.AppendChild(node);
			}
			if (!string.IsNullOrEmpty(_Www2))
			{
				node = doc.CreateElement("URI");
				node.AppendChild(doc.CreateTextNode(_Www2));
				root.AppendChild(node);
			}
			if (!string.IsNullOrEmpty(_Www3))
			{
				node = doc.CreateElement("URI");
				node.AppendChild(doc.CreateTextNode(_Www3));
				root.AppendChild(node);
			}
		}
		
		/// <summary>
		/// Get the GEDCOM 5.5 lines for the data in this object.
		/// Lines start at the given level
		/// </summary>
		/// <param name="sw">
		/// A <see cref="StringBuilder"/>
		/// </param>
		/// <param name="level">
		/// A <see cref="System.Int32"/>
		/// </param>
		public void Output(TextWriter sw, int level)
		{
			sw.Write(Environment.NewLine);
			sw.Write(Util.IntToString(level));
			sw.Write(" ADDR");
			
			if (!string.IsNullOrEmpty(AddressLine))
			{				
				sw.Write(" ");
				
				Util.SplitLineText(sw, AddressLine, level, 60, 3, true);
			}
			
			string levelStr = null;
			string levelPlusOne = null;
			
			if (!string.IsNullOrEmpty(AddressLine1))
			{
				if (levelPlusOne == null)
				{
					levelPlusOne = Util.IntToString(level + 1);
				}
				
				string line = AddressLine1.Replace("@", "@@");
				
				sw.Write(Environment.NewLine);
				sw.Write(levelPlusOne);
				sw.Write(" ADR1 ");
				if (line.Length <= 60)
				{
					sw.Write(AddressLine1);	
				}
				else
				{
					sw.Write(line.Substring(0,60));
					System.Diagnostics.Debug.WriteLine("Truncating AddressLine1");
				}
			}
			
			if (!string.IsNullOrEmpty(AddressLine2))
			{
				if (levelPlusOne == null)
				{
					levelPlusOne = Util.IntToString(level + 1);
				}
				
				string line = AddressLine2.Replace("@", "@@");
				
				sw.Write(Environment.NewLine);
				sw.Write(levelPlusOne);
				sw.Write(" ADR2 ");
				if (line.Length <= 60)
				{
					sw.Write(line);	
				}
				else
				{
					sw.Write(line.Substring(0,60));
					System.Diagnostics.Debug.WriteLine("Truncating AddressLine2");
				}
			}
			
			if (!string.IsNullOrEmpty(AddressLine3))
			{
				if (levelPlusOne == null)
				{
					levelPlusOne = Util.IntToString(level + 1);
				}
				
				string line = AddressLine3.Replace("@", "@@");
				
				sw.Write(Environment.NewLine);
				sw.Write(levelPlusOne);
				sw.Write(" ADR3 ");
				if (line.Length <= 60)
				{
					sw.Write(line);	
				}
				else
				{
					sw.Write(line.Substring(0,60));
					System.Diagnostics.Debug.WriteLine("Truncating AddressLine3");
				}
			}
			
			if (!string.IsNullOrEmpty(City))
			{
				if (levelPlusOne == null)
				{
					levelPlusOne = Util.IntToString(level + 1);
				}
				
				string line = City.Replace("@", "@@");
				
				sw.Write(Environment.NewLine);
				sw.Write(levelPlusOne);
				sw.Write(" CITY ");
				if (line.Length <= 60)
				{
					sw.Write(line);	
				}
				else
				{
					sw.Write(line.Substring(0,60));
					System.Diagnostics.Debug.WriteLine("Truncating City");
				}
			}
			
			if (!string.IsNullOrEmpty(State))
			{
				if (levelPlusOne == null)
				{
					levelPlusOne = Util.IntToString(level + 1);
				}
				
				string line = State.Replace("@", "@@");
				
				sw.Write(Environment.NewLine);
				sw.Write(levelPlusOne);
				sw.Write(" STAE ");
				if (line.Length <= 60)
				{
					sw.Write(line);	
				}
				else
				{
					sw.Write(line.Substring(0,60));
					System.Diagnostics.Debug.WriteLine("Truncating State");
				}
			}
			
			if (!string.IsNullOrEmpty(PostCode))
			{
				if (levelPlusOne == null)
				{
					levelPlusOne = Util.IntToString(level + 1);
				}
				
				string line = PostCode.Replace("@", "@@");
				
				sw.Write(Environment.NewLine);
				sw.Write(levelPlusOne);
				sw.Write(" POST ");
				if (line.Length <= 10)
				{
					sw.Write(line);	
				}
				else
				{
					sw.Write(line.Substring(0,10));
					System.Diagnostics.Debug.WriteLine("Truncating PostCode");
				}
			}
			
			if (!string.IsNullOrEmpty(Country))
			{
				if (levelPlusOne == null)
				{
					levelPlusOne = Util.IntToString(level + 1);
				}
				
				string line = Country.Replace("@", "@@");
				
				sw.Write(Environment.NewLine);
				sw.Write(levelPlusOne);
				sw.Write(" CTRY ");
				if (line.Length <= 60)
				{
					sw.Write(line);	
				}
				else
				{
					sw.Write(line.Substring(0,60));
					System.Diagnostics.Debug.WriteLine("Truncating Country");
				}
			}
			
			if (!string.IsNullOrEmpty(Phone1))
			{
				if (levelStr == null)
				{
					levelStr = Util.IntToString(level);
				}
				
				string line = Phone1.Replace("@", "@@");
				
				sw.Write(Environment.NewLine);
				sw.Write(levelStr);
				sw.Write(" PHON ");
				if (line.Length <= 25)
				{
					sw.Write(line);	
				}
				else
				{
					sw.Write(line.Substring(0,25));
					System.Diagnostics.Debug.WriteLine("Truncating Phone1");
				}
			}
			if (!string.IsNullOrEmpty(Phone2))
			{
				if (levelStr == null)
				{
					levelStr = Util.IntToString(level);
				}
				
				string line = Phone2.Replace("@", "@@");
				
				sw.Write(Environment.NewLine);
				sw.Write(levelStr);
				sw.Write(" PHON ");
				if (line.Length <= 25)
				{
					sw.Write(line);	
				}
				else
				{
					sw.Write(line.Substring(0,25));
					System.Diagnostics.Debug.WriteLine("Truncating Phone2");
				}
			}
			if (!string.IsNullOrEmpty(Phone3))
			{
				if (levelStr == null)
				{
					levelStr = Util.IntToString(level);
				}
				
				string line = Phone3.Replace("@", "@@");
				
				sw.Write(Environment.NewLine);
				sw.Write(levelStr);
				sw.Write(" PHON ");
				if (line.Length <= 25)
				{
					sw.Write(line);	
				}
				else
				{
					sw.Write(line.Substring(0,25));
					System.Diagnostics.Debug.WriteLine("Truncating Phone3");
				}
			}
			
			if (!string.IsNullOrEmpty(Fax1))
			{
				if (levelStr == null)
				{
					levelStr = Util.IntToString(level);
				}
				
				string line = Fax1.Replace("@", "@@");
				
				sw.Write(Environment.NewLine);
				sw.Write(levelStr);
				sw.Write(" FAX ");
				if (line.Length <= 60)
				{
					sw.Write(line);	
				}
				else
				{
					sw.Write(line.Substring(0,60));
					System.Diagnostics.Debug.WriteLine("Truncating Fax1");
				}
			}
			if (!string.IsNullOrEmpty(Fax2))
			{
				if (levelStr == null)
				{
					levelStr = Util.IntToString(level);
				}
				
				string line = Fax2.Replace("@", "@@");
				
				sw.Write(Environment.NewLine);
				sw.Write(levelStr);
				sw.Write(" FAX ");
				if (line.Length <= 60)
				{
					sw.Write(line);	
				}
				else
				{
					sw.Write(line.Substring(0,60));
					System.Diagnostics.Debug.WriteLine("Truncating Fax2");
				}
			}
			if (!string.IsNullOrEmpty(Fax3))
			{
				if (levelStr == null)
				{
					levelStr = Util.IntToString(level);
				}
				
				string line = Fax3.Replace("@", "@@");
				
				sw.Write(Environment.NewLine);
				sw.Write(levelStr);
				sw.Write(" FAX ");
				if (line.Length <= 60)
				{
					sw.Write(line);	
				}
				else
				{
					sw.Write(line.Substring(0,60));
					System.Diagnostics.Debug.WriteLine("Truncating Fax3");
				}
			}
			
			if (!string.IsNullOrEmpty(Email1))
			{
				if (levelStr == null)
				{
					levelStr = Util.IntToString(level);
				}
				
				string line = Email1.Replace("@", "@@");
				
				sw.Write(Environment.NewLine);
				sw.Write(levelStr);
				sw.Write(" EMAIL ");
				if (line.Length <= 120)
				{
					sw.Write(line);	
				}
				else
				{
					sw.Write(line.Substring(0,120));
					System.Diagnostics.Debug.WriteLine("Truncating Email1");
				}
			}
			if (!string.IsNullOrEmpty(Email2))
			{
				if (levelStr == null)
				{
					levelStr = Util.IntToString(level);
				}
				
				string line = Email2.Replace("@", "@@");
				
				sw.Write(Environment.NewLine);
				sw.Write(levelStr);
				sw.Write(" EMAIL ");
				if (line.Length <= 120)
				{
					sw.Write(line);	
				}
				else
				{
					sw.Write(line.Substring(0,120));
					System.Diagnostics.Debug.WriteLine("Truncating Email2");
				}
			}
			if (!string.IsNullOrEmpty(Email3))
			{
				if (levelStr == null)
				{
					levelStr = Util.IntToString(level);
				}
				
				string line = Email3.Replace("@", "@@");
				
				sw.Write(Environment.NewLine);
				sw.Write(levelStr);
				sw.Write(" EMAIL ");
				if (line.Length <= 120)
				{
					sw.Write(line);	
				}
				else
				{
					sw.Write(line.Substring(0,120));
					System.Diagnostics.Debug.WriteLine("Truncating Email3");
				}
			}
			
			if (!string.IsNullOrEmpty(Www1))
			{
				if (levelStr == null)
				{
					levelStr = Util.IntToString(level);
				}
				
				string line = Www1.Replace("@", "@@");
				
				sw.Write(Environment.NewLine);
				sw.Write(levelStr);
				sw.Write(" WWW ");
				if (line.Length <= 120)
				{
					sw.Write(line);	
				}
				else
				{
					sw.Write(line.Substring(0,120));
					System.Diagnostics.Debug.WriteLine("Truncating Www1");
				}
			}
			if (!string.IsNullOrEmpty(Www2))
			{
				if (levelStr == null)
				{
					levelStr = Util.IntToString(level);
				}
				
				string line = Www2.Replace("@", "@@");
				
				sw.Write(Environment.NewLine);
				sw.Write(levelStr);
				sw.Write(" WWW ");
				if (line.Length <= 120)
				{
					sw.Write(line);	
				}
				else
				{
					sw.Write(line.Substring(0,120));
					System.Diagnostics.Debug.WriteLine("Truncating Www2");
				}
			}
			if (!string.IsNullOrEmpty(Www3))
			{
				if (levelStr == null)
				{
					levelStr = Util.IntToString(level);
				}
				
				string line = Www3.Replace("@", "@@");
				
				sw.Write(Environment.NewLine);
				sw.Write(levelStr);
				sw.Write(" WWW ");
				if (line.Length <= 120)
				{
					sw.Write(line);	
				}
				else
				{
					sw.Write(line.Substring(0,120));
					System.Diagnostics.Debug.WriteLine("Truncating Www3");
				}
			}
			
		}
		
		#endregion
	}
}
