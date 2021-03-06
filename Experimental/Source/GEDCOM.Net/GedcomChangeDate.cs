/*
 *  $Id: GedcomChangeDate.cs 199 2008-11-15 15:20:44Z davek $
 * 
 *  Copyright (C) 2008 David A Knight <david@ritter.demon.co.uk>
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

namespace GEDCOM.Net
{
	public class GedcomChangeDate : GedcomDate
	{
		#region Constructors

		// For serialization		
        public GedcomChangeDate() { }

		public GedcomChangeDate(GedcomDatabase database) : base(database)
		{
		}
		
		#endregion
		
		#region Methods
		
		protected override void Changed()
		{
			
		}
		
		#endregion
	}
}
