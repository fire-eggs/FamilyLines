/*
 *  $Id: GraphNode.cs 183 2008-06-08 15:31:15Z davek $
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

namespace Utility
{
	
	public class GraphNode<T>
	{
		#region Variables
		
		protected T _data;
		
		protected List<GraphNode<T>> _edges;
		
		#endregion
		
		#region Constructors
		
		public GraphNode()
		{
			_edges = new List<GraphNode<T>>();
		}
		
		#endregion
		
		#region Properties
		
		public T Data
		{
			get { return _data; }
			set { _data = value; }
		}
		
		public List<GraphNode<T>> Edges
		{
			get { return _edges; }	
		}
		
		#endregion
		
		#region Methods
		
		#endregion
		
	}
}
