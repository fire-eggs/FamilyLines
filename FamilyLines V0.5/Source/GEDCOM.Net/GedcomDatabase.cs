/*
 *  $Id: GedcomDatabase.cs 191 2008-10-25 18:43:33Z davek $
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
using System.Collections;
using System.Collections.Generic;

namespace GEDCOM.Net
{
	
	/// <summary>
	/// The database for all the GEDCOM records.
	/// This is currently just in memory.  To implement a "real"
	/// database you should derive from this class and override
	/// the neccesary methods / properties
	/// </summary>
	public class GedcomDatabase : IEnumerator
	{
		#region Variables
		
		private Hashtable _Table;

		private GedcomHeader _header;
		
		private List<GedcomIndividualRecord> _Individuals;
		private List<GedcomFamilyRecord>     _Families;
		private List<GedcomSourceRecord>     _Sources;
		private List<GedcomRepositoryRecord> _Repositories;
		private List<GedcomMultimediaRecord> _Media;
		private List<GedcomNoteRecord>       _Notes;
		private List<GedcomSubmitterRecord>  _Submitters;
		
//		private int _xrefCounter = 0;
		
		private string _Name;
		
		private IndexedKeyCollection _nameCollection;
		private IndexedKeyCollection _placeNameCollection;
		// NOTE: having a collection for date strings saves memory
		// but kills GEDCOM reading time to an extent that it isn't worth it

        private Dictionary<string, int> _surnames;

		private bool _loading;
		
		#endregion
		
		#region Constructors
		
		/// <summary>
		/// Create a new database
		/// </summary>
		public GedcomDatabase()
		{
			_Table = new Hashtable();
			_Individuals = new List<GedcomIndividualRecord>();
			_Families = new List<GedcomFamilyRecord>();
			_Sources = new List<GedcomSourceRecord>();
			_Repositories = new List<GedcomRepositoryRecord>();
			_Media = new List<GedcomMultimediaRecord>();
			_Notes = new List<GedcomNoteRecord>();
			_Submitters = new List<GedcomSubmitterRecord>();
			
			_nameCollection = new IndexedKeyCollection();
			_placeNameCollection = new IndexedKeyCollection();
			
            _surnames = new Dictionary<string, int>();
		}
		
		#endregion
		
		#region Properties

		public virtual GedcomHeader Header
		{
			get { return _header; }
			set { _header = value; }
		}
		
		/// <value>
		/// Hashtable of all top level GEDCOM records, key is the XRef.
		/// Top level records are Individuals, Families, Sources, Repositories, and Media
		/// </value>
		public virtual Hashtable Table
		{
			get { return _Table; }
			set { _Table = value; }
		}
		
		/// <value>
		/// Get / Set the GedcomRecord associated with the given XRef.
		/// </value>
		public virtual GedcomRecord this[string key]
		{
			get { return _Table[key] as GedcomRecord; }
			set
			{			
				Remove(key,value);
				Add(key,value);	
			}
		}

		/// <value>
		/// Total number of top level GEDCOM records in the database
		/// Top level records are Individuals, Families, Sources, Repositories, and Media
		/// </value>		
		public virtual int Count
		{
			get { return _Table.Count; }	
		}
		
		/// <value>
		/// The current GedcomRecord when enumerating the database
		/// </value>
		public virtual object Current
		{
			get { return _Table.GetEnumerator().Current; }	
		}
		
		/// <value>
		/// A list of all the Individuals in the database
		/// </value>
		public virtual List<GedcomIndividualRecord> Individuals
		{
			get { return _Individuals; }	
		}
		
		/// <value>
		/// A list of all the Families in the database
		/// </value>
		public virtual List<GedcomFamilyRecord> Families
		{
			get { return _Families; }	
		}
		
		/// <value>
		/// A list of all the sources in the database
		/// </value>
		public virtual List<GedcomSourceRecord> Sources
		{
			get { return _Sources; }	
		}
		
		/// <value>
		/// A list of all the repositories in the database
		/// </value>
		public virtual List<GedcomRepositoryRecord> Repositories
		{
			get { return _Repositories; }	
		}
		
		/// <value>
		/// A list of all the media items in the database
		/// </value>
		public virtual List<GedcomMultimediaRecord> Media
		{
			get { return _Media; }
		}
		
		/// <value>
		/// A list of all the notes in the database
		/// </value>
		public virtual List<GedcomNoteRecord> Notes
		{
			get { return _Notes; }
		}

		//// <value>
		/// A list of all the submitters in the database
		/// </value>
		public virtual List<GedcomSubmitterRecord> Submitters
		{
			get { return _Submitters; }
		}
		
		/// <value>
		/// The name of the database, this is currently the full filename
		/// of the GEDCOM file the database was read from / saved to,
		/// but could equally be a connection string for a real backend database
		/// </value>
		public virtual string Name
		{
			get { return _Name; }
			set { _Name = value; }
		}
		
		/// <value>
		/// All the names used in the database, used primarily to save
		/// memory by storing names only once
		/// </value>
		public IndexedKeyCollection NameCollection
		{
			get { return _nameCollection; }
		}

		/// <value>
		/// All the place names used in the database, used primarily to save
		/// memory by storing names only once
		/// </value>
		public IndexedKeyCollection PlaceNameCollection
		{
			get { return _placeNameCollection; }
		}
		
		/// <value>
		/// Utility property providing all the surnames in the database, along with
		/// a count of how many people have that surname.
		/// </value>
        public virtual Dictionary<string, int> Surnames
        {
            get { return _surnames; }
            set { _surnames = value; }
        }

		public bool Loading
		{
			get { return _loading; }
			set { _loading = value; }
		}
		
		#endregion
		
		#region Methods
		
		/// <summary>
		/// Add the given record to the database with the given XRef
		/// </summary>
		/// <param name="xrefID">
		/// A <see cref="System.String"/>
		/// </param>
		/// <param name="record">
		/// A <see cref="GedcomRecord"/>
		/// </param>
		public virtual void Add(string xrefID, GedcomRecord record)
		{	
			_Table.Add(xrefID,record);
			
			if (record is GedcomIndividualRecord)
			{
                GedcomIndividualRecord indi = (GedcomIndividualRecord)record;
                _Individuals.Add(indi);

                // KBR Family.Show keeps individuals in the order read
                //int pos = _Individuals.BinarySearch(indi);
                //if (pos < 0)
                //{
                //    pos = ~pos;
                //}
                //_Individuals.Insert(pos, indi);
			}
			else if (record is GedcomFamilyRecord)
			{
				_Families.Add((GedcomFamilyRecord)record);	
			}
			else if (record is GedcomSourceRecord)
			{
				GedcomSourceRecord source = (GedcomSourceRecord)record;
				
				int pos = _Sources.BinarySearch(source);
				if (pos < 0)
				{
					pos = ~pos;
				}
				_Sources.Insert(pos, source);	
			}
			else if (record is GedcomRepositoryRecord)
			{
				GedcomRepositoryRecord repo = (GedcomRepositoryRecord)record;
				
				int pos = _Repositories.BinarySearch(repo);
				if (pos < 0)
				{
					pos = ~pos;
				}
				_Repositories.Insert(pos, repo);
			}
			else if (record is GedcomMultimediaRecord)
			{
				_Media.Add((GedcomMultimediaRecord)record);
			}
			else if (record is GedcomNoteRecord)
			{
				_Notes.Add((GedcomNoteRecord)record);
			}
			else if (record is GedcomSubmitterRecord)
			{
				_Submitters.Add((GedcomSubmitterRecord)record);
			}
			
			record.Database = this;
		}

		/// <summary>
		/// Builds up the surname list for use with the Surnames property.
		/// </summary>
        public virtual void BuildSurnameList()
        {
            foreach (GedcomIndividualRecord indi in _Individuals)
            {
                BuildSurnameList(indi);
            }
        }
        /// <summary>
        /// Add the given individual to the surnames list
        /// </summary>
        /// <param name="indi">
        /// A <see cref="GedcomIndividualRecord"/>
        /// </param>
        protected virtual void BuildSurnameList(GedcomIndividualRecord indi)
        {
            foreach (GedcomName name in indi.Names)
            {
                // FIXME: not right, need to include prefix + suffix
                string surname = name.Surname;

                if (!_surnames.ContainsKey(surname))
                {
                    _surnames[surname] = 1;
                }
                else
                {
                    _surnames[surname] = 1 + (int)_surnames[surname];
                }
            }
        }
		
		/// <summary>
		/// Remove the given record with the given XRef from the database
		/// </summary>
		/// <param name="xrefID">
		/// A <see cref="System.String"/>
		/// </param>
		/// <param name="record">
		/// A <see cref="GedcomRecord"/>
		/// </param>
		public virtual void Remove(string xrefID, GedcomRecord record)
		{
			if (_Table.Contains(xrefID))
			{
				_Table.Remove(xrefID);
				
				if (record is GedcomIndividualRecord)
				{
                    GedcomIndividualRecord indi = (GedcomIndividualRecord)record;

					_Individuals.Remove(indi);
					
					// remove names from surname cache
                    foreach (GedcomName name in indi.Names)
                    {
                        // FIXME: not right, need to include prefix + suffix
                        string surname = name.Surname;

                        if (_surnames.ContainsKey(surname))
                        {
                            int count = (int)_surnames[surname];
                            count--;
                            if (count > 0)
                            {
                                _surnames[surname] = count;
                            }
                            else
                            {
                                _surnames.Remove(surname);
                            }
                        }
                    }
				}
				else if (record is GedcomFamilyRecord)
				{
					_Families.Remove((GedcomFamilyRecord)record);	
				}
				else if (record is GedcomSourceRecord)
				{
					_Sources.Remove((GedcomSourceRecord)record);	
				}
				else if (record is GedcomRepositoryRecord)
				{
					_Repositories.Remove((GedcomRepositoryRecord)record);	
				}
				else if (record is GedcomMultimediaRecord)
				{
					_Media.Remove((GedcomMultimediaRecord)record);
				}
				else if (record is GedcomNoteRecord)
				{
					_Notes.Remove((GedcomNoteRecord)record);
				}
				else if (record is GedcomSubmitterRecord)
				{
					_Submitters.Remove((GedcomSubmitterRecord)record);
				}
				
				// FIXME: should we set this to null? part of the deletion
				// methods may still want to access the database
				//record.Database = null;
			}
		}
		
		/// <summary>
		/// Does the database contain a record with the given XRef
		/// </summary>
		/// <param name="xrefID">
		/// A <see cref="System.String"/>
		/// </param>
		/// <returns>
		/// A <see cref="System.Boolean"/>
		/// </returns>
		public virtual bool Contains(string xrefID)
		{
			return _Table.Contains(xrefID);
		}
		
		public virtual bool MoveNext()
		{
			return _Table.GetEnumerator().MoveNext();	
		}
		
		public virtual void Reset()
		{
			_Table.GetEnumerator().Reset();	
		}
		
		public virtual IDictionaryEnumerator GetEnumerator()
		{
			return _Table.GetEnumerator();	
		}
		
		/// <summary>
		/// Create a new XRef
		/// </summary>
		/// <param name="prefix">
		/// A <see cref="System.String"/>
		/// </param>
		/// <returns>
		/// A <see cref="System.String"/>
		/// </returns>
		public string GenerateXref(string prefix)
		{
		    return Guid.NewGuid().ToString(); // KBR similar to Family.Show
//			return string.Format("{0}{1}", prefix, Util.IntToString(++_xrefCounter));
		}
		
		
		/// <summary>
		/// Combines the given database with this one.
		/// This is literally what it says, no duplicate removal is performed
		/// combine will not take place if there are duplicate xrefs.
		/// </summary>
		/// <param name="database">
		/// A <see cref="GedcomDatabase"/>
		/// </param>
		/// <returns>
		/// A <see cref="System.Boolean"/>
		/// </returns>
		public virtual bool Combine(GedcomDatabase database)
		{
            // KBR TODO This check should no longer be necessary if using GUIDs
			// check the databases can be combined, i.e. unique xrefs
			bool canCombine = true;
			foreach (GedcomRecord record in database.Table.Values)
			{
				if (Contains(record.XRefID))
				{
					canCombine = false;
					break;
				}
			}
			
			if (canCombine)
			{
				foreach (GedcomRecord record in database.Table.Values)
				{
					Add(record.XRefID, record);
				}
			}

			return canCombine;
		}
		
		#endregion
	}
}
