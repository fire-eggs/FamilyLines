/*
 *  $Id: GedcomAssociation.cs 200 2008-11-30 14:34:07Z davek $
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
	/// How the given individual is associated to another.
	/// Each GedcomIndividal contains a list of these
	/// </summary>
	public class GedcomAssociation : GedcomRecord
	{
		#region Variables
		
		private string _Description;
		
		private string _Individual;
		
		#endregion
		
		#region Constructors
		
		public GedcomAssociation()
		{
		}
		
		#endregion
		
		#region Properties
		
		public override GedcomRecordType RecordType
		{
			get { return GedcomRecordType.Association; }	
		}
		
		public override string GedcomTag
		{
			get { return "ASSO"; }	
		}

		
		public string Description
		{
			get { return _Description; }
			set 
			{ 
				if (value != _Description)
				{
					_Description = value; 
					Changed();
				}
			}
		}
		
		public string Individual
		{
			get { return _Individual; }
			set 
			{ 
				if (value != _Individual)
				{
					_Individual = value; 
					Changed();
				}
			}
		}
		
		#endregion
		
		#region Methods
		
		public override void Output(TextWriter sw)
		{
			sw.Write(Environment.NewLine);
			sw.Write(Util.IntToString(Level));
			sw.Write(" ASSO ");
			sw.Write("@");
			sw.Write(Individual);
			sw.Write("@");
			
			string levelPlusOne = Util.IntToString(Level + 1);
			
			sw.Write(Environment.NewLine);
			sw.Write(levelPlusOne);
			sw.Write(" RELA ");
			
			string line = Description.Replace("@", "@@");
			if (line.Length > 25)
			{
				Gedcom.Util.SplitText(sw, line, Level + 1, 25, 1, true);
			}
			else
			{
				sw.Write(line);	
			}
			
			OutputStandard(sw);
		}
		#endregion
	}
}
