/*
 *  $Id: IndexedKeyCollection.cs 193 2008-11-03 21:46:05Z davek $
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
using System.Collections.Generic;
using System.Text;

namespace Utility
{
	
	
	public class IndexedKeyCollection
	{
		#region Variables
		
		protected List<string> _strings;
				
		#endregion
		
		#region Constructors
		
		public IndexedKeyCollection()
		{
			_strings = new List<string>();
		}
		
		public IndexedKeyCollection(int size)
		{
			_strings = new List<string>(size);
		}
		
		#endregion
		
		#region Properties
		
		public virtual string this[string str]
		{
			get { return this[str, 0, str.Length]; }
			
		}
		
		public virtual string this[string str, int startIndex, int length]
		{
			get
			{
				string ret = null;

				if (length == 0)
				{
					ret = string.Empty;
				}
				else
				{
					int pos;
					bool found = Find(str, startIndex, length, out pos);
													
					if (!found)
					{	
						string insert = str.Substring(startIndex,length).Trim();
						if (_strings.Contains(insert))
						{
							throw new Exception("ERROR FINDING EXISTING KEY:" + insert);
						}
						_strings.Insert(pos,insert);
					}
					
					ret = _strings[pos];
				}
				return ret;
			}
		}
		
		#endregion
		
		#region Methods
		
		public override string ToString()
		{
			StringBuilder sb = new StringBuilder("{");
			foreach (string s in _strings)
			{
				if (sb.Length > 1)
				{
					sb.Append(",");
				}
				sb.Append(s);
			}
			sb.Append("}");
			
			return sb.ToString();
		}

		
		public virtual bool Find(string str, int startIndex, int length, out int pos)
		{
			bool found = false;
			
			int i = 0;
			int j = _strings.Count - 1;

			// trim leading white space
			while (length > 0 && (Char.IsWhiteSpace(str[startIndex])))
			{
				startIndex ++;
				length --;
			}			
			
			pos = 0;
			if (_strings.Count > 0)
			{
				while (i <= j)
				{
					pos = (i + j) / 2;
								
					string s = _strings[pos];

					bool match = true;
											
					for (int k = 0; k < length; k ++)
					{
						if (s.Length <= k)
						{
							// could still be a match if the rest of the
							// input string is white space, trim here
							// to avoid missing any places where we are called
							bool whiteSpace = true;
							while(k < length)
							{
								if (!Char.IsWhiteSpace(str[startIndex + k]))
								{
									whiteSpace = false;
									break;
								}
								k ++;
							}
							if (!whiteSpace)
							{
								i = pos + 1;
								match = false;
							}
							else
							{
								// need to correct length for white space removal
								length = s.Length;
							}
							break;
						}
						else
						{
							char c = s[k];
												
							char c2 = str[startIndex + k];
							
							if (c > c2)
							{
								j = pos - 1;
								match = false;
								break;
							}
							else if (c < c2)
							{
								i = pos + 1;
								match = false;
								break;
							}
						}
					}
					if (match)
					{
						if (s.Length == length)
						{
							found = true;
							
							break;
						}
						else if (s.Length > length)
						{
							j = pos -1;
						}
					}				
				}
				
				// correct insertion position if needed
				if (!found)
				{
					pos = i;
				}					
			}
			
			return found;
		}
		
		
		#endregion
	}
}
