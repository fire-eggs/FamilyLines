/*
 *  $Id: GedcomCustomRecord.cs 200 2008-11-30 14:34:07Z davek $
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
	/// GEDCOM allows for custom tags to be added by applications.
	/// This is essentially a dummy object
	/// </summary>
	public class GedcomCustomRecord : GedcomEvent
	{
		#region Variables
		
		private string _Tag = "_CUST";
		
		#endregion
		
		#region Constructors
		
		public GedcomCustomRecord()
		{
			EventType = GedcomEventType.Custom;
		}
		
		#endregion
		
		#region Properties
		
		public override GedcomRecordType RecordType
		{
			get { return GedcomRecordType.CustomRecord; }	
		}
		
		public override string GedcomTag
		{
			get { return _Tag; }
		}
		
		public string Tag
		{
			get { return _Tag; }
			set { _Tag = value; }
		}
		
		#endregion
		
		#region Methods
		
		public override void Output(TextWriter sw)
		{
		}
		
		#endregion
	}
}
